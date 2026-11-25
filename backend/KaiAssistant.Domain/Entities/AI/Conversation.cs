using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Domain.Entities.AI;

public sealed class Conversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public List<ConversationEntry> Messages { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
public sealed class ConversationEntry
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}