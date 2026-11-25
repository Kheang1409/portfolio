using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.Persistence;

public class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<Conversation> _collection;
    public ConversationRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Conversation>("conversations");
        // Ensure indexes
        var indexModel = new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys.Ascending(c => c.UserId)
                .Descending(c => c.UpdatedAt));
        _collection.Indexes.CreateOneAsync(indexModel).GetAwaiter().GetResult();
    }
    public async Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.ConversationId, conversationId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }
    public async Task<Conversation> CreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var conversation = new Conversation
        {
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _collection.InsertOneAsync(conversation, null, cancellationToken).ConfigureAwait(false);
        return conversation;
    }
    public async Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        var filter = Builders<Conversation>.Filter.Eq(c => c.ConversationId, conversation.ConversationId);
        var options = new ReplaceOptions { IsUpsert = true };
        await _collection.ReplaceOneAsync(filter, conversation, options, cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task<List<Conversation>> GetByUserIdAsync(
        string userId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.UserId, userId);
        var sort = Builders<Conversation>.Sort.Descending(c => c.UpdatedAt);
        return await _collection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
