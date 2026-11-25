using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IConversationService
{
    Task<IReadOnlyList<ConversationMessage>> GetShortTermMemoryAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<string?> GetSummaryAsync(string conversationId, CancellationToken cancellationToken = default);
    Task AppendAsync(string conversationId, ConversationMessage userMessage, ConversationMessage assistantMessage, CancellationToken cancellationToken = default);
}