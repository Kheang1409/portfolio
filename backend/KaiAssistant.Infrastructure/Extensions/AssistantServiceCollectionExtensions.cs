using Microsoft.Extensions.Configuration;
using KaiAssistant.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using System.Net;
using KaiAssistant.Application.Services;
namespace KaiAssistant.Infrastructure.Extensions;

public static class AssistantServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var geminiSettings = configuration.GetSection("GeminiSettings");
        var configuredSettings = geminiSettings.Get<GeminiSettings>() ?? new GeminiSettings();
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                  ?? configuredSettings.ApiKey
                  ?? throw new ArgumentException("Gemini setting 'ApiKey' is missing or empty.");
        List<string> modelNames;
        var envModelNames = Environment.GetEnvironmentVariable("GEMINI_MODEL_NAMES");
        if (!string.IsNullOrWhiteSpace(envModelNames))
        {
            modelNames = envModelNames.Split(';', ',').Select(m => m.Trim()).Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        }
        else
        {
            modelNames = configuredSettings.ModelNames ?? new List<string>();
        }
        if (modelNames.Count == 0)
            throw new ArgumentException("Gemini setting 'ModelNames' is missing or empty.");
        string endpoint = Environment.GetEnvironmentVariable("GEMINI_ENDPOINT")
                          ?? configuredSettings.Endpoint
                          ?? throw new ArgumentException("Gemini setting 'Endpoint' is missing or empty.");
        string systemPrompt = Environment.GetEnvironmentVariable("GEMINI_SYSTEM_PROMPT")
                           ?? configuredSettings.SystemPrompt
                           ?? string.Empty;
        int promptMaxChars = configuredSettings.PromptMaxChars > 0 ? configuredSettings.PromptMaxChars : 10000;
        var envPromptMaxChars = Environment.GetEnvironmentVariable("GEMINI_PROMPT_MAX_CHARS");
        if (!string.IsNullOrWhiteSpace(envPromptMaxChars))
        {
            if (int.TryParse(envPromptMaxChars, out var parsed) && parsed > 0)
                promptMaxChars = parsed;
        }
        bool includePersonalDetails = configuredSettings.IncludePersonalDetails;
        var envIncludePersonal = Environment.GetEnvironmentVariable("GEMINI_INCLUDE_PERSONAL_DETAILS");
        if (!string.IsNullOrWhiteSpace(envIncludePersonal) && bool.TryParse(envIncludePersonal, out var parsedInclude))
        {
            includePersonalDetails = parsedInclude;
        }
        var temperature = configuredSettings.Temperature;
        var topK = configuredSettings.TopK;
        var topP = configuredSettings.TopP;
        var maxOutputTokens = configuredSettings.MaxOutputTokens;
        var candidateCount = configuredSettings.CandidateCount;
        if (double.TryParse(Environment.GetEnvironmentVariable("GEMINI_TEMPERATURE"), out var envTemperature))
            temperature = envTemperature;
        if (int.TryParse(Environment.GetEnvironmentVariable("GEMINI_TOPK"), out var envTopK))
            topK = envTopK;
        if (double.TryParse(Environment.GetEnvironmentVariable("GEMINI_TOPP"), out var envTopP))
            topP = envTopP;
        if (int.TryParse(Environment.GetEnvironmentVariable("GEMINI_MAX_OUTPUT_TOKENS"), out var envMaxOutputTokens))
            maxOutputTokens = envMaxOutputTokens;
        if (int.TryParse(Environment.GetEnvironmentVariable("GEMINI_CANDIDATE_COUNT"), out var envCandidateCount))
            candidateCount = envCandidateCount;
        services.Configure<GeminiSettings>(opts =>
        {
            opts.LastSuccessfulModelCacheTtlSeconds = configuredSettings.LastSuccessfulModelCacheTtlSeconds;
            opts.SkipRetryDelayThresholdSeconds = configuredSettings.SkipRetryDelayThresholdSeconds;
            opts.DeprioritizeOn429Count = configuredSettings.DeprioritizeOn429Count;
            opts.DeprioritizeSkip = configuredSettings.DeprioritizeSkip;
            opts.ApiKey = apiKey;
            opts.ModelNames = modelNames;
            opts.Endpoint = endpoint;
            opts.SystemPrompt = systemPrompt;
            opts.PromptMaxChars = promptMaxChars;
            opts.IncludePersonalDetails = includePersonalDetails;
            opts.Temperature = temperature;
            opts.TopK = topK;
            opts.TopP = topP;
            opts.MaxOutputTokens = maxOutputTokens;
            opts.CandidateCount = candidateCount;
        });
        var registry = services.AddPolicyRegistry();
        registry.Add("gemini-retry", Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(5, retryAttempt =>
                TimeSpan.FromMilliseconds(Math.Min(3000, 250 * Math.Pow(2, retryAttempt - 1))) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200))));
        registry.Add("gemini-timeout", Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15)));
        services.AddHttpClient("Gemini", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandlerFromRegistry("gemini-timeout")
        .AddPolicyHandler((sp, request) =>
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Min(3000, 250 * Math.Pow(2, retryAttempt - 1))) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("GeminiHttpClient");
                        logger?.LogWarning("Retry {Retry} for {Request} due to {Reason}", retryCount, request.RequestUri, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        })
        .AddPolicyHandler((sp, _) =>
        {
            var resilience = sp.GetRequiredService<IResilienceStatusProvider>();
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("GeminiHttpClient");
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.ServiceUnavailable)
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromSeconds(30),
                    (result, breakDelay) =>
                    {
                        var reason = result.Exception?.Message ?? result.Result?.StatusCode.ToString() ?? "unknown";
                        resilience.RecordFailure("ai", reason);
                        resilience.RecordCircuitState("ai", "Open");
                        logger?.LogWarning("AI circuit opened for {BreakDelay} due to {Reason}", breakDelay, reason);
                    },
                    () =>
                    {
                        resilience.RecordCircuitState("ai", "Closed");
                        logger?.LogInformation("AI circuit reset.");
                    },
                    () => resilience.RecordCircuitState("ai", "HalfOpen"));
        });
        services.AddSingleton<IGeminiGateway, Gateways.GeminiGateway>();
        return services;
    }
}