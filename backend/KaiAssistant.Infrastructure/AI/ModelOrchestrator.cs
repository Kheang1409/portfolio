using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
namespace KaiAssistant.Infrastructure.AI;

public sealed class ModelOrchestrator : IModelOrchestrator
{
    private static readonly Meter Meter = new("KaiAssistant.AiModels", "1.0.0");
    private static readonly Histogram<double> ModelScore = Meter.CreateHistogram<double>("ai_model_score");
    private static readonly Counter<long> ModelSelectionCount = Meter.CreateCounter<long>("ai_model_selection_count");
    private readonly IOptionsMonitor<AiModelOrchestrationOptions> _options;
    private readonly IOptions<GeminiSettings> _gemini;
    private readonly IModelHealthService _health;
    private readonly IMemoryCache _cache;
    private readonly IAiTuningState? _tuningState;
    private readonly IAiDecisionAuditStore? _auditStore;
    public ModelOrchestrator(
        IOptionsMonitor<AiModelOrchestrationOptions> options,
        IOptions<GeminiSettings> gemini,
        IModelHealthService health,
        IMemoryCache cache,
        IAiTuningState? tuningState = null,
        IAiDecisionAuditStore? auditStore = null)
    {
        _options = options;
        _gemini = gemini;
        _health = health;
        _cache = cache;
        _tuningState = tuningState;
        _auditStore = auditStore;
    }
    public Task<AiRoutingDecision> BuildDecisionAsync(
        string question,
        int estimatedInputTokens,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var options = _options.CurrentValue;
        var tuned = _tuningState?.GetCurrent(options) ?? new AiTuningWeightsSnapshot
        {
            SuccessRateWeight = options.SuccessRateWeight,
            LatencyWeight = options.LatencyWeight,
            FailureRateWeight = options.FailureRateWeight,
            CooldownWeight = options.CooldownWeight,
            CostWeight = options.CostWeight
        };
        var safeInputTokens = Math.Max(0, estimatedInputTokens);
        var defaultModels = (_gemini.Value.ModelNames ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var profiles = options.ModelProfiles
            .Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Name))
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (defaultModels.Count == 0)
        {
            return Task.FromResult(new AiRoutingDecision
            {
                SelectedPrimaryModel = null,
                CandidateModels = Array.Empty<string>(),
                IsComplexRequest = false,
                RequestClass = "simple",
                EstimatedInputTokens = safeInputTokens,
                EstimatedOutputTokens = 0,
                EstimatedCostUsd = 0m
            });
        }
        var isComplex = question.Length >= Math.Max(1, options.ComplexPromptCharsThreshold) ||
                        safeInputTokens >= Math.Max(1, options.ComplexPromptTokensThreshold);
        var isSimple = question.Length <= Math.Max(1, options.SimplePromptCharsThreshold) &&
                       safeInputTokens <= Math.Max(1, options.SimplePromptTokensThreshold);
        var requestClass = isComplex ? "complex" : (isSimple ? "simple" : "medium");
        var expectedOutputTokens = isComplex ? 1024 : (isSimple ? 256 : 512);
        var snapshot = _health.GetSnapshot(now);
        var cacheSeconds = Math.Clamp(options.DecisionCacheSeconds, 1, 120);
        var cacheKey = $"ai:decision:{requestClass}:{Math.Min(4000, safeInputTokens / 64)}:{snapshot.StateVersion}";
        if (_cache.TryGetValue(cacheKey, out AiRoutingDecision? cached) && cached is not null)
        {
            cached.FromCache = true;
            TryAudit(cached, new List<AiRejectedModelReason>());
            return Task.FromResult(cached);
        }
        var statusByModel = snapshot.Models.ToDictionary(x => x.ModelName, StringComparer.OrdinalIgnoreCase);
        var ranked = defaultModels
            .Select((model, index) =>
            {
                profiles.TryGetValue(model, out var profile);
                profile ??= new AiModelProfile
                {
                    Name = model,
                    Priority = index + 1,
                    MaxTokens = 4096,
                    CostWeight = 1.0m,
                    LatencyWeight = 1.0m,
                    CapabilityScore = 0.5,
                    InputCostPer1KTokensUsd = 0.001m,
                    OutputCostPer1KTokensUsd = 0.002m,
                    Enabled = true
                };
                statusByModel.TryGetValue(model, out var healthStatus);
                var canAttempt = _health.CanAttempt(model, now) && (healthStatus?.IsHealthy ?? true);
                var estCost = EstimateRequestCost(profile, safeInputTokens, expectedOutputTokens);
                var blockedByCost = options.MaxEstimatedCostPerRequestUsd > 0 && estCost > options.MaxEstimatedCostPerRequestUsd;
                var successComponent = (healthStatus?.WeightedSuccessRate ?? 0.5d) * tuned.SuccessRateWeight;
                var latencyPenalty = NormalizeLatency(healthStatus?.AverageLatencyMs ?? 0, options.LatencyReferenceMs) * tuned.LatencyWeight;
                var failurePenalty = (healthStatus?.RecentFailureRate ?? 0d) * tuned.FailureRateWeight;
                var cooldownPenalty = (healthStatus?.CooldownFrequency ?? 0d) * tuned.CooldownWeight;
                var capabilityBonus = ComputeCapabilityBonus(requestClass, profile.CapabilityScore);
                var costPenalty = (double)estCost * tuned.CostWeight;
                if (tuned.GuardrailFastMode)
                {
                    latencyPenalty += NormalizeLatency(healthStatus?.AverageLatencyMs ?? 0, Math.Max(100, options.MaxLatencyMs)) * 0.5d;
                }
                if (tuned.GuardrailCostMode)
                {
                    costPenalty *= 1.25d;
                }
                // Lower priority value means higher preference, so convert it into a small bonus.
                var priorityBonus = 1d / Math.Max(1, profile.Priority + 1);
                var finalScore = successComponent + capabilityBonus + priorityBonus - latencyPenalty - failurePenalty - cooldownPenalty - costPenalty;
                return new RankedModel
                {
                    Model = model,
                    Profile = profile,
                    CanAttempt = canAttempt,
                    BlockedByCost = blockedByCost,
                    EstimatedCostUsd = estCost,
                    Breakdown = new AiModelScoreBreakdown
                    {
                        ModelName = model,
                        SuccessRateComponent = successComponent,
                        LatencyPenalty = latencyPenalty,
                        FailurePenalty = failurePenalty,
                        CooldownPenalty = cooldownPenalty,
                        CapabilityBonus = capabilityBonus,
                        CostPenalty = costPenalty,
                        FinalScore = finalScore
                    }
                };
            })
            .OrderByDescending(x => x.Breakdown.FinalScore)
            .ThenBy(x => x.Profile.Priority)
            .ToList();
        foreach (var item in ranked)
        {
            ModelScore.Record(item.Breakdown.FinalScore, KeyValuePair.Create<string, object?>("model", item.Model));
        }
        var candidates = ranked
            .Where(x => x.CanAttempt && !x.BlockedByCost)
            .Take(Math.Max(1, options.MaxFallbackModels))
            .Select(x => x.Model)
            .ToList();
        if (candidates.Count == 0)
        {
            candidates = ranked
                .Where(x => x.CanAttempt)
                .Take(Math.Max(1, options.MaxFallbackModels))
                .Select(x => x.Model)
                .ToList();
        }
        if (candidates.Count == 0)
        {
            candidates = defaultModels.Take(Math.Max(1, options.MaxFallbackModels)).ToList();
        }
        var primary = candidates.FirstOrDefault();
        var primaryRank = ranked.FirstOrDefault(x => string.Equals(x.Model, primary, StringComparison.OrdinalIgnoreCase));
        var estimatedCost = primaryRank?.EstimatedCostUsd ?? 0m;
        if (!string.IsNullOrWhiteSpace(primary))
        {
            ModelSelectionCount.Add(1,
                KeyValuePair.Create<string, object?>("model", primary),
                KeyValuePair.Create<string, object?>("request_class", requestClass));
        }
        var decision = new AiRoutingDecision
        {
            SelectedPrimaryModel = primary,
            CandidateModels = candidates,
            IsComplexRequest = isComplex,
            RequestClass = requestClass,
            EstimatedInputTokens = safeInputTokens,
            EstimatedOutputTokens = expectedOutputTokens,
            EstimatedCostUsd = estimatedCost,
            FromCache = false,
            ScoreBreakdown = ranked.Select(x => x.Breakdown).ToList()
        };
        var rejected = ranked
            .Where(x => !string.Equals(x.Model, primary, StringComparison.OrdinalIgnoreCase))
            .Select(x => new AiRejectedModelReason
            {
                ModelName = x.Model,
                Reason = !x.CanAttempt
                    ? "unhealthy_or_circuit_open"
                    : x.BlockedByCost
                        ? "estimated_cost_exceeds_limit"
                        : "lower_ranked_score"
            })
            .ToList();
        TryAudit(decision, rejected);
        _cache.Set(cacheKey, decision, TimeSpan.FromSeconds(cacheSeconds));
        return Task.FromResult(decision);
    }
    private void TryAudit(AiRoutingDecision decision, IReadOnlyList<AiRejectedModelReason> rejected)
    {
        if (_auditStore is null)
        {
            return;
        }
        var sampleRate = Math.Clamp(_options.CurrentValue.DecisionAuditSampleRate, 0d, 1d);
        if (sampleRate <= 0 || Random.Shared.NextDouble() > sampleRate)
        {
            return;
        }
        _auditStore.Record(new AiDecisionAuditEntry
        {
            CapturedAtUtc = DateTimeOffset.UtcNow,
            RequestClass = decision.RequestClass,
            EstimatedInputTokens = decision.EstimatedInputTokens,
            EstimatedOutputTokens = decision.EstimatedOutputTokens,
            EstimatedCostUsd = decision.EstimatedCostUsd,
            SelectedModel = decision.SelectedPrimaryModel,
            FromCache = decision.FromCache,
            Scores = decision.ScoreBreakdown,
            Rejected = rejected
        });
    }
    private static decimal EstimateRequestCost(AiModelProfile profile, int inputTokens, int outputTokens)
    {
        var inputCost = (Math.Max(0, inputTokens) / 1000m) * Math.Max(0m, profile.InputCostPer1KTokensUsd);
        var outputCost = (Math.Max(0, outputTokens) / 1000m) * Math.Max(0m, profile.OutputCostPer1KTokensUsd);
        return Math.Round(inputCost + outputCost, 6);
    }
    private static double NormalizeLatency(double latencyMs, int referenceMs)
    {
        if (latencyMs <= 0)
        {
            return 0;
        }
        return Math.Min(1d, latencyMs / Math.Max(50d, referenceMs));
    }
    private static double ComputeCapabilityBonus(string requestClass, double capabilityScore)
    {
        var bounded = Math.Clamp(capabilityScore, 0d, 1d);
        return requestClass switch
        {
            "complex" => bounded * 0.9d,
            "simple" => (1d - bounded) * 0.5d,
            _ => (0.5d - Math.Abs(0.5d - bounded)) * 0.4d
        };
    }
    private sealed class RankedModel
    {
        public string Model { get; set; } = string.Empty;
        public AiModelProfile Profile { get; set; } = new();
        public bool CanAttempt { get; set; }
        public bool BlockedByCost { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public AiModelScoreBreakdown Breakdown { get; set; } = new();
    }
}