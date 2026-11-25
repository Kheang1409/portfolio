using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.AI;

public sealed class AiRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string? ConversationId { get; init; }
    public IReadOnlyList<ConversationMessage> History { get; init; } = Array.Empty<ConversationMessage>();
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public int? MaxOutputTokens { get; init; }
}