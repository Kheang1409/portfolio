namespace KaiAssistant.Application.Diagnostics;

public sealed class AiTuningWeightsSnapshot
{
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public double SuccessRateWeight { get; set; }
    public double LatencyWeight { get; set; }
    public double FailureRateWeight { get; set; }
    public double CooldownWeight { get; set; }
    public double CostWeight { get; set; }
    public bool GuardrailFastMode { get; set; }
    public bool GuardrailCostMode { get; set; }
    public string Reason { get; set; } = "baseline";
}