using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<Conversation> CreateAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);
}
