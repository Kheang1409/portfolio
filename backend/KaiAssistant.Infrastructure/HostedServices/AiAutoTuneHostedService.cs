using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class AiAutoTuneHostedService : BackgroundService
{
    private readonly IOptionsMonitor<AiModelOrchestrationOptions> _options;
    private readonly IModelHealthService _health;
    private readonly IAiTuningState _tuningState;
    private readonly ILogger<AiAutoTuneHostedService> _logger;
    private decimal _lastTotalCost;
    private DateTimeOffset _lastCostObservedAtUtc = DateTimeOffset.UtcNow;
    public AiAutoTuneHostedService(
        IOptionsMonitor<AiModelOrchestrationOptions> options,
        IModelHealthService health,
        IAiTuningState tuningState,
        ILogger<AiAutoTuneHostedService> logger)
    {
        _options = options;
        _health = health;
        _tuningState = tuningState;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var config = _options.CurrentValue;
            var intervalSeconds = Math.Clamp(config.AutoTuneIntervalSeconds, 1, 3600);
            if (!config.EnableAutoTuning)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken).ConfigureAwait(false);
                continue;
            }
            var now = DateTimeOffset.UtcNow;
            var snapshot = _health.GetSnapshot(now);
            var models = snapshot.Models;
            var avgLatency = models.Count == 0 ? 0d : models.Average(x => x.AverageLatencyMs);
            var avgFailureRate = models.Count == 0 ? 0d : models.Average(x => x.RecentFailureRate);
            var totalCost = models.Sum(x => x.TotalEstimatedCostUsd);
            var elapsedMinutes = Math.Max(0.01d, (now - _lastCostObservedAtUtc).TotalMinutes);
            var deltaCost = Math.Max(0m, totalCost - _lastTotalCost);
            var costPerMinute = deltaCost / (decimal)elapsedMinutes;
            var tuned = new AiTuningWeightsSnapshot
            {
                UpdatedAtUtc = now,
                SuccessRateWeight = config.SuccessRateWeight,
                LatencyWeight = config.LatencyWeight,
                FailureRateWeight = config.FailureRateWeight,
                CooldownWeight = config.CooldownWeight,
                CostWeight = config.CostWeight,
                GuardrailFastMode = false,
                GuardrailCostMode = false,
                Reason = "baseline"
            };
            if (avgLatency > config.MaxLatencyMs)
            {
                tuned.GuardrailFastMode = true;
                tuned.LatencyWeight = Math.Min(10d, tuned.LatencyWeight + 0.4d);
                tuned.FailureRateWeight = Math.Min(10d, tuned.FailureRateWeight + 0.1d);
                tuned.Reason = "latency_guardrail";
            }
            if (avgFailureRate > 0.2d)
            {
                tuned.FailureRateWeight = Math.Min(10d, tuned.FailureRateWeight + 0.5d);
                tuned.CooldownWeight = Math.Min(10d, tuned.CooldownWeight + 0.25d);
                tuned.Reason = tuned.Reason == "baseline" ? "failure_guardrail" : $"{tuned.Reason}+failure_guardrail";
            }
            if (config.MaxCostPerMinute > 0 && costPerMinute > config.MaxCostPerMinute)
            {
                tuned.GuardrailCostMode = true;
                tuned.CostWeight = Math.Min(10d, tuned.CostWeight + 0.6d);
                tuned.Reason = tuned.Reason == "baseline" ? "cost_guardrail" : $"{tuned.Reason}+cost_guardrail";
            }
            _tuningState.Update(tuned);
            _lastTotalCost = totalCost;
            _lastCostObservedAtUtc = now;
            _logger.LogInformation(
                "AI auto-tune updated weights: success={SuccessWeight} latency={LatencyWeight} failure={FailureWeight} cooldown={CooldownWeight} cost={CostWeight} fastMode={FastMode} costMode={CostMode} reason={Reason}",
                tuned.SuccessRateWeight,
                tuned.LatencyWeight,
                tuned.FailureRateWeight,
                tuned.CooldownWeight,
                tuned.CostWeight,
                tuned.GuardrailFastMode,
                tuned.GuardrailCostMode,
                tuned.Reason);
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }
}