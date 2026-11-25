using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
namespace KaiAssistant.Infrastructure.AI;

public sealed class AiTuningState : IAiTuningState
{
    private readonly object _sync = new();
    private AiTuningWeightsSnapshot? _current;
    public AiTuningWeightsSnapshot GetCurrent(AiModelOrchestrationOptions baseline)
    {
        lock (_sync)
        {
            if (_current is null)
            {
                return new AiTuningWeightsSnapshot
                {
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                    SuccessRateWeight = baseline.SuccessRateWeight,
                    LatencyWeight = baseline.LatencyWeight,
                    FailureRateWeight = baseline.FailureRateWeight,
                    CooldownWeight = baseline.CooldownWeight,
                    CostWeight = baseline.CostWeight,
                    GuardrailFastMode = false,
                    GuardrailCostMode = false,
                    Reason = "baseline"
                };
            }
            return new AiTuningWeightsSnapshot
            {
                UpdatedAtUtc = _current.UpdatedAtUtc,
                SuccessRateWeight = _current.SuccessRateWeight,
                LatencyWeight = _current.LatencyWeight,
                FailureRateWeight = _current.FailureRateWeight,
                CooldownWeight = _current.CooldownWeight,
                CostWeight = _current.CostWeight,
                GuardrailFastMode = _current.GuardrailFastMode,
                GuardrailCostMode = _current.GuardrailCostMode,
                Reason = _current.Reason
            };
        }
    }
    public void Update(AiTuningWeightsSnapshot snapshot)
    {
        lock (_sync)
        {
            _current = new AiTuningWeightsSnapshot
            {
                UpdatedAtUtc = snapshot.UpdatedAtUtc,
                SuccessRateWeight = snapshot.SuccessRateWeight,
                LatencyWeight = snapshot.LatencyWeight,
                FailureRateWeight = snapshot.FailureRateWeight,
                CooldownWeight = snapshot.CooldownWeight,
                CostWeight = snapshot.CostWeight,
                GuardrailFastMode = snapshot.GuardrailFastMode,
                GuardrailCostMode = snapshot.GuardrailCostMode,
                Reason = snapshot.Reason
            };
        }
    }
}