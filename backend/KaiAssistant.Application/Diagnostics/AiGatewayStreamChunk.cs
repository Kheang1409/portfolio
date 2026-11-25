namespace KaiAssistant.Application.Diagnostics;

public sealed class AiGatewayStreamChunk
{
    public string DeltaText { get; set; } = string.Empty;
    public string? UsedModel { get; set; }
    public bool FallbackUsed { get; set; }
    public bool IsCompleted { get; set; }
}