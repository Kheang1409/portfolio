namespace KaiAssistant.Application.Diagnostics;

public sealed class AiResponse
{
    public string Text { get; set; } = string.Empty;
    public string? ModelUsed { get; set; }
    public bool FallbackUsed { get; set; }
    public double LatencyMs { get; set; }
    public decimal EstimatedCostUsd { get; set; }
}