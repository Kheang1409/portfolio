namespace KaiAssistant.Application.Diagnostics;

public sealed class AiResponseDecision
{
    public bool Allowed { get; set; }
    public string NormalizedResponse { get; set; } = string.Empty;
    public string? BlockReason { get; set; }
    public string UserFacingMessage { get; set; } = "I can't provide that response safely right now. Please try rephrasing your request.";
}