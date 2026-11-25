namespace KaiAssistant.Application.Diagnostics;

public sealed class AiRoutingDecision
{
    public string? SelectedPrimaryModel { get; set; }
    public IReadOnlyList<string> CandidateModels { get; set; } = Array.Empty<string>();
    public bool IsComplexRequest { get; set; }
    public string RequestClass { get; set; } = "simple";
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public bool FromCache { get; set; }
    public IReadOnlyList<AiModelScoreBreakdown> ScoreBreakdown { get; set; } = Array.Empty<AiModelScoreBreakdown>();
}
public sealed class AiModelScoreBreakdown
{
    public string ModelName { get; set; } = string.Empty;
    public double SuccessRateComponent { get; set; }
    public double LatencyPenalty { get; set; }
    public double FailurePenalty { get; set; }
    public double CooldownPenalty { get; set; }
    public double CapabilityBonus { get; set; }
    public double CostPenalty { get; set; }
    public double FinalScore { get; set; }
}