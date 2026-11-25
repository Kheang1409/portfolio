using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IAssistantContextBuilder
{
    Task<ConversationContext> BuildContextAsync(
        string conversationId,
        string userMessage,
        string? userId = null,
        Dictionary<string, object?>? metadata = null,
        CancellationToken cancellationToken = default);
    string GetSystemPrompt();
}