namespace KaiAssistant.Application.Diagnostics;

public sealed class AiStreamChunk
{
    public string Type { get; set; } = "delta";
    public string MessageId { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? ModelUsed { get; set; }
    public bool? FallbackUsed { get; set; }
    public double? TtftMs { get; set; }
    public double? LatencyMs { get; set; }
    public double? ThroughputTokensPerSecond { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public string? ErrorCode { get; set; }
    public bool? Retryable { get; set; }
    public string? ErrorMessage { get; set; }
}