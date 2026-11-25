namespace KaiAssistant.Application.AI;

public sealed class AiResponse
{
    public string Content { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public bool FallbackUsed { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}