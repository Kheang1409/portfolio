namespace KaiAssistant.Domain.Entities;

public class ConversationContext
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public List<ConversationMessage> ContextMessages { get; set; } = new();
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public int EstimatedTokens { get; set; }
}