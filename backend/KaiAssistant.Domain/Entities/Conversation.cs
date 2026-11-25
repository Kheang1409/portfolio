using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Domain.Entities;

public class Conversation
{
    [BsonId]
    public string ConversationId { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public List<ConversationMessage> Messages { get; set; } = new();
    public string? Summary { get; set; }
    public int EstimatedTokens { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ConversationMessage> GetContextWindow(int maxMessages = 10)
    {
        return Messages.TakeLast(maxMessages).ToList();
    }
}
