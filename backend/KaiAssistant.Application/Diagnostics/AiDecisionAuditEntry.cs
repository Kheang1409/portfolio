namespace KaiAssistant.Application.Diagnostics;

public sealed class AiDecisionAuditEntry
{
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string RequestClass { get; set; } = "simple";
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public string? SelectedModel { get; set; }
    public bool FromCache { get; set; }
    public IReadOnlyList<AiModelScoreBreakdown> Scores { get; set; } = Array.Empty<AiModelScoreBreakdown>();
    public IReadOnlyList<AiRejectedModelReason> Rejected { get; set; } = Array.Empty<AiRejectedModelReason>();
}
public sealed class AiRejectedModelReason
{
    public string ModelName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}