using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.AI;

public sealed class AiTrafficSimulationService : IAiTrafficSimulationService
{
    private readonly IModelOrchestrator _orchestrator;
    private readonly IModelHealthService _health;
    private readonly IOptionsMonitor<AiModelOrchestrationOptions> _options;
    private readonly ILogger<AiTrafficSimulationService> _logger;
    private readonly object _sync = new();
    private readonly Queue<AiSimulationMetricSnapshot> _snapshots = new();
    private readonly Dictionary<string, long> _selectionCounts = new(StringComparer.OrdinalIgnoreCase);
    private AiSimulationRunReport? _lastReport;
    public AiTrafficSimulationService(
        IModelOrchestrator orchestrator,
        IModelHealthService health,
        IOptionsMonitor<AiModelOrchestrationOptions> options,
        ILogger<AiTrafficSimulationService> logger)
    {
        _orchestrator = orchestrator;
        _health = health;
        _options = options;
        _logger = logger;
    }
    public async Task<AiSimulationRunReport> RunOnceAsync(int? requestCountOverride, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var config = _options.CurrentValue;
        var requestCount = Math.Clamp(requestCountOverride ?? config.SimulationRequestCount, 1, 100_000);
        var before = _health.GetSnapshot(now);
        CaptureSnapshot(before, now);
        var startedAt = DateTimeOffset.UtcNow;
        var runStarted = DateTime.UtcNow;
        var success = 0;
        var failure = 0;
        var simulatedLatencyTotal = 0d;
        var decisionLatencyTotal = 0d;
        for (var i = 0; i < requestCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var requestClassRoll = Random.Shared.NextDouble();
            var requestClass = requestClassRoll < 0.45 ? "short" : requestClassRoll < 0.8 ? "medium" : "long";
            var inputTokens = requestClass switch
            {
                "short" => Random.Shared.Next(40, 240),
                "medium" => Random.Shared.Next(400, 1200),
                _ => Random.Shared.Next(1600, 3600)
            };
            var outputTokens = requestClass switch
            {
                "short" => Random.Shared.Next(60, 200),
                "medium" => Random.Shared.Next(280, 760),
                _ => Random.Shared.Next(700, 1500)
            };
            var questionChars = inputTokens * 3;
            var question = new string('q', Math.Clamp(questionChars, 40, 12_000));
            var decisionStart = DateTime.UtcNow;
            var decision = await _orchestrator.BuildDecisionAsync(question, inputTokens, cancellationToken).ConfigureAwait(false);
            decisionLatencyTotal += (DateTime.UtcNow - decisionStart).TotalMilliseconds;
            if (string.IsNullOrWhiteSpace(decision.SelectedPrimaryModel))
            {
                failure++;
                continue;
            }
            var model = decision.SelectedPrimaryModel;
            var eventTime = DateTimeOffset.UtcNow;
            var baseLatency = requestClass switch
            {
                "short" => Random.Shared.Next(140, 520),
                "medium" => Random.Shared.Next(450, 1400),
                _ => Random.Shared.Next(900, 2400)
            };
            var spike = Random.Shared.NextDouble() < 0.08 ? Random.Shared.Next(1200, 2600) : 0;
            var simulatedLatency = baseLatency + spike;
            var injectFailure = config.EnableFailureSimulation && Random.Shared.NextDouble() < config.FailureInjectionRate;
            if (injectFailure)
            {
                if (Random.Shared.NextDouble() < 0.5)
                {
                    _health.MarkRateLimited(model, eventTime, eventTime.AddSeconds(Random.Shared.Next(20, 90)), "simulated_429");
                }
                else
                {
                    _health.RecordFailure(model, eventTime, "simulated_timeout");
                }
                _health.RecordLatency(model, eventTime, simulatedLatency);
                failure++;
            }
            else
            {
                _health.RecordLatency(model, eventTime, simulatedLatency);
                _health.RecordSuccess(model, eventTime, fallbackUsed: decision.CandidateModels.Count > 1 && !string.Equals(decision.CandidateModels[0], model, StringComparison.OrdinalIgnoreCase));
                _health.RecordUsage(model, eventTime, inputTokens, outputTokens, decision.EstimatedCostUsd);
                _health.SetActiveModel(model, eventTime);
                success++;
            }
            simulatedLatencyTotal += simulatedLatency;
            IncrementSelection(model);
            if ((i + 1) % 20 == 0)
            {
                CaptureSnapshot(_health.GetSnapshot(DateTimeOffset.UtcNow), DateTimeOffset.UtcNow);
            }
            var delayMs = requestClass switch
            {
                "short" => Random.Shared.Next(10, 40),
                "medium" => Random.Shared.Next(20, 70),
                _ => Random.Shared.Next(30, 90)
            };
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
        }
        var completedAt = DateTimeOffset.UtcNow;
        var elapsedSeconds = Math.Max(0.001d, (DateTime.UtcNow - runStarted).TotalSeconds);
        var after = _health.GetSnapshot(completedAt);
        CaptureSnapshot(after, completedAt);
        var totalCost = after.Models.Sum(x => x.TotalEstimatedCostUsd);
        var costPerMinute = (decimal)(totalCost / (decimal)Math.Max(0.01d, elapsedSeconds / 60d));
        var beforeTop = before.Models.OrderByDescending(x => x.DynamicScore).FirstOrDefault()?.ModelName;
        var afterTop = after.Models.OrderByDescending(x => x.DynamicScore).FirstOrDefault()?.ModelName;
        var unhealthyModels = after.Models.Where(x => !x.IsHealthy).Select(x => x.ModelName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unhealthyAvoided = unhealthyModels.Count == 0 || _selectionCounts.Keys.All(model => !unhealthyModels.Contains(model));
        var report = new AiSimulationRunReport
        {
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            RequestCount = requestCount,
            SuccessCount = success,
            FailureCount = failure,
            AverageSimulatedLatencyMs = requestCount == 0 ? 0 : simulatedLatencyTotal / requestCount,
            AverageDecisionLatencyMs = requestCount == 0 ? 0 : decisionLatencyTotal / requestCount,
            ThroughputRequestsPerSecond = requestCount / elapsedSeconds,
            CostPerMinuteUsd = Math.Round(costPerMinute, 6),
            SelectionShiftObserved = !string.Equals(beforeTop, afterTop, StringComparison.OrdinalIgnoreCase),
            UnhealthyAvoidanceObserved = unhealthyAvoided,
            CostWithinLimit = config.MaxCostPerMinute <= 0 || costPerMinute <= config.MaxCostPerMinute,
            BeforeTopModel = beforeTop,
            AfterTopModel = afterTop
        };
        lock (_sync)
        {
            _lastReport = report;
        }
        _logger.LogInformation(
            "AI traffic simulation complete: requests={Requests} success={Success} failure={Failure} avgLatencyMs={AvgLatency} avgDecisionMs={AvgDecision} throughput={Throughput} costPerMinute={CostPerMinute}",
            report.RequestCount,
            report.SuccessCount,
            report.FailureCount,
            report.AverageSimulatedLatencyMs,
            report.AverageDecisionLatencyMs,
            report.ThroughputRequestsPerSecond,
            report.CostPerMinuteUsd);
        return report;
    }
    public IReadOnlyList<AiSimulationMetricSnapshot> GetSnapshots(int maxEntries = 120)
    {
        var bounded = Math.Clamp(maxEntries, 1, 500);
        lock (_sync)
        {
            return _snapshots.Reverse().Take(bounded).ToList();
        }
    }
    public AiSimulationRunReport? GetLastReport()
    {
        lock (_sync)
        {
            return _lastReport;
        }
    }
    private void IncrementSelection(string model)
    {
        lock (_sync)
        {
            if (_selectionCounts.TryGetValue(model, out var current))
            {
                _selectionCounts[model] = current + 1;
            }
            else
            {
                _selectionCounts[model] = 1;
            }
        }
    }
    private void CaptureSnapshot(AiModelHealthSnapshot snapshot, DateTimeOffset capturedAtUtc)
    {
        var selectionCopy = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        lock (_sync)
        {
            foreach (var kvp in _selectionCounts)
            {
                selectionCopy[kvp.Key] = kvp.Value;
            }
        }
        var metric = new AiSimulationMetricSnapshot
        {
            CapturedAtUtc = capturedAtUtc,
            StateVersion = snapshot.StateVersion,
            ActiveModel = snapshot.ActiveModel,
            TotalEstimatedCostUsd = snapshot.Models.Sum(x => x.TotalEstimatedCostUsd),
            TotalInputTokens = snapshot.Models.Sum(x => x.TotalInputTokens),
            TotalOutputTokens = snapshot.Models.Sum(x => x.TotalOutputTokens),
            UnhealthyModelCount = snapshot.Models.Count(x => !x.IsHealthy),
            SelectionCounts = selectionCopy
        };
        lock (_sync)
        {
            _snapshots.Enqueue(metric);
            while (_snapshots.Count > 500)
            {
                _snapshots.Dequeue();
            }
        }
    }
}