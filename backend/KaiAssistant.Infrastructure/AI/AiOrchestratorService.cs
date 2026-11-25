using System.Diagnostics;
using System.Diagnostics.Metrics;
using KaiAssistant.Application.AI;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
namespace KaiAssistant.Infrastructure.AI;

public sealed class AiOrchestratorService : IAiOrchestratorService
{
    private static readonly ActivitySource ActivitySource = new("KaiAssistant.AiOrchestrator", "1.0.0");
    private static readonly Meter Meter = new("KaiAssistant.AiOrchestrator", "1.0.0");
    private static readonly Histogram<double> AiLatency = Meter.CreateHistogram<double>("kai_ai_orchestrator_latency_ms", "ms");
    private static readonly Counter<long> AiFallbackCount = Meter.CreateCounter<long>("kai_ai_orchestrator_fallback_total");
    private readonly IReadOnlyDictionary<string, IAiProvider> _providers;
    private readonly IOptionsMonitor<AiOrchestrationOptions> _options;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ILogger<AiOrchestratorService> _logger;
    private readonly IObservabilityService _observability;
    private readonly AsyncPolicy _executionPolicy;
    public AiOrchestratorService(
        IEnumerable<IAiProvider> providers,
        IOptionsMonitor<AiOrchestrationOptions> options,
        IFeatureFlagService featureFlags,
        ILogger<AiOrchestratorService> logger,
        IObservabilityService observability)
    {
        _providers = providers.ToDictionary(x => x.ProviderName, StringComparer.OrdinalIgnoreCase);
        _options = options;
        _featureFlags = featureFlags;
        _logger = logger;
        _observability = observability;
        var retry = Policy
            .Handle<TimeoutRejectedException>()
            .Or<BrokenCircuitException>()
            .Or<Exception>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(Math.Min(2000, 150 * Math.Pow(2, attempt))) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 120)),
                onRetry: (ex, delay, attempt, _) =>
                {
                    _logger.LogWarning(ex, "AI orchestrator retry {Attempt} after {DelayMs}ms.", attempt, delay.TotalMilliseconds);
                });
        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(20));
        var circuit = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                8,
                TimeSpan.FromSeconds(30),
                (_, breakDelay) => _logger.LogWarning("AI orchestrator circuit opened for {BreakDelay}.", breakDelay),
                () => _logger.LogInformation("AI orchestrator circuit reset."),
                () => _logger.LogInformation("AI orchestrator circuit half-open."));
        _executionPolicy = Policy.WrapAsync(retry, timeout, circuit);
    }
    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var requestActivity = ActivitySource.StartActivity("ai.request", ActivityKind.Internal);
        var options = _options.CurrentValue;
        var preferred = _featureFlags.PreferredAiProvider;
        var primaryName = string.IsNullOrWhiteSpace(preferred) ? options.PrimaryProvider : preferred;
        var secondaryName = options.SecondaryProvider;
        if (!_providers.TryGetValue(primaryName, out var primary))
        {
            throw new InvalidOperationException($"Primary AI provider '{primaryName}' is not registered.");
        }
        var timeout = TimeSpan.FromMilliseconds(options.ProviderTimeoutMs);
        bool fallbackUsed = false;
        string activeProvider = primaryName;
        try
        {
            var primaryResponse = await _executionPolicy
                .ExecuteAsync(ct => ExecuteWithTimeoutAsync(primary, request, timeout, ct), cancellationToken)
                .ConfigureAwait(false);
            requestActivity?.SetTag("ai.provider", primary.ProviderName);
            requestActivity?.SetTag("ai.fallback", false);
            AiLatency.Record(primaryResponse.LatencyMs, KeyValuePair.Create<string, object?>("provider", primary.ProviderName));
            _observability.RecordAiCallMetric(primaryResponse.LatencyMs, true, fallbackUsed);
            return primaryResponse;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            var isTimeout = ex is TimeoutRejectedException || ex.InnerException is TimeoutRejectedException;
            _logger.LogWarning(ex, "Primary AI provider failed. Provider={Provider} IsTimeout={IsTimeout}", primary.ProviderName, isTimeout);
            if (!_providers.TryGetValue(secondaryName, out var secondary))
            {
                _observability.RecordAiCallMetric(0, false, fallbackUsed, isTimeout);
                throw;
            }
            fallbackUsed = true;
            activeProvider = secondaryName;
            using var fallbackActivity = ActivitySource.StartActivity("ai.fallback", ActivityKind.Internal);
            var fallbackResponse = await _executionPolicy
                .ExecuteAsync(ct => ExecuteWithTimeoutAsync(secondary, request, timeout, ct), cancellationToken)
                .ConfigureAwait(false);
            fallbackResponse.FallbackUsed = true;
            _logger.LogWarning(
                "Fallback triggered. PrimaryProvider={PrimaryProvider} SecondaryProvider={SecondaryProvider}",
                primary.ProviderName,
                secondary.ProviderName);
            fallbackActivity?.SetTag("ai.provider.primary", primary.ProviderName);
            fallbackActivity?.SetTag("ai.provider.secondary", secondary.ProviderName);
            requestActivity?.SetTag("ai.provider", secondary.ProviderName);
            requestActivity?.SetTag("ai.fallback", true);
            AiFallbackCount.Add(1,
                KeyValuePair.Create<string, object?>("from", primary.ProviderName),
                KeyValuePair.Create<string, object?>("to", secondary.ProviderName));
            AiLatency.Record(fallbackResponse.LatencyMs, KeyValuePair.Create<string, object?>("provider", secondary.ProviderName));
            _observability.RecordAiCallMetric(fallbackResponse.LatencyMs, true, fallbackUsed);
            return fallbackResponse;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && fallbackUsed)
        {
            var isTimeout = ex is TimeoutRejectedException || ex.InnerException is TimeoutRejectedException;
            _observability.RecordAiCallMetric(0, false, fallbackUsed, isTimeout);
            throw;
        }
    }
    private static async Task<AiResponse> ExecuteWithTimeoutAsync(
        IAiProvider provider,
        AiRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);
        return await provider.GenerateAsync(request, linkedCts.Token).ConfigureAwait(false);
    }
}