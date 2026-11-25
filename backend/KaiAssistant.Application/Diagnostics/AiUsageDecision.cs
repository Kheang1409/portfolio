using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Diagnostics;

public sealed class AiUsageDecision
{
    public bool Allowed { get; set; }
    public string Question { get; set; } = string.Empty;
    public ConversationMessage[]? History { get; set; }
    public AssistantContext? Context { get; set; }
    public int EstimatedInputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public bool Truncated { get; set; }
    public string? BlockReason { get; set; }
    public string UserFacingMessage { get; set; } = "Your request exceeds AI safety limits. Please shorten your input and try again.";
}