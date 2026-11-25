using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using KaiAssistant.Application.AI;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.AI.Providers;

public sealed class GeminiProvider : IAiProvider
{
    private readonly IAiModelGateway _gateway;
    private readonly IOptions<GeminiSettings> _settings;
    private readonly ILogger<GeminiProvider> _logger;
    public GeminiProvider(IAiModelGateway gateway, IOptions<GeminiSettings> settings, ILogger<GeminiProvider> logger)
    {
        _gateway = gateway;
        _settings = settings;
        _logger = logger;
    }
    public string ProviderName => "gemini";
    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var models = _settings.Value.ModelNames ?? [];
        if (models.Count == 0)
        {
            throw new InvalidOperationException("Gemini model list is empty.");
        }
        var payload = new JsonObject
        {
            ["contents"] = new JsonArray(
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray(new JsonObject { ["text"] = request.Prompt })
                }),
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = _settings.Value.Temperature,
                ["topK"] = _settings.Value.TopK,
                ["topP"] = _settings.Value.TopP,
                ["maxOutputTokens"] = request.MaxOutputTokens ?? _settings.Value.MaxOutputTokens,
                ["candidateCount"] = _settings.Value.CandidateCount
            }
        };
        var sw = Stopwatch.StartNew();
        var (body, modelUsed) = await _gateway
            .SendGenerationRequestAsync(payload.ToJsonString(), models, cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Gemini did not return a response body.");
        }
        var content = ExtractText(body);
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Gemini response could not be parsed as text.");
            throw new InvalidOperationException("Gemini response text could not be extracted.");
        }
        return new AiResponse
        {
            Content = content,
            ModelUsed = modelUsed ?? models[0],
            LatencyMs = (long)sw.Elapsed.TotalMilliseconds,
            FallbackUsed = !string.Equals(modelUsed, models[0], StringComparison.OrdinalIgnoreCase),
            InputTokens = EstimateTokens(request.Prompt),
            OutputTokens = EstimateTokens(content)
        };
    }
    private static string? ExtractText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        return TryExtractText(doc.RootElement);
    }
    private static string? TryExtractText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            {
                return text.GetString();
            }
            if (element.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
            {
                return outputText.GetString();
            }
            if (element.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in parts.EnumerateArray())
                {
                    var extracted = TryExtractText(item);
                    if (!string.IsNullOrWhiteSpace(extracted))
                    {
                        return extracted;
                    }
                }
            }
            foreach (var property in element.EnumerateObject())
            {
                var extracted = TryExtractText(property.Value);
                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    return extracted;
                }
            }
        }
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var extracted = TryExtractText(item);
                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    return extracted;
                }
            }
        }
        return null;
    }
    private static int EstimateTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }
        return (int)Math.Ceiling(value.Length / 4d);
    }
}