using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Domain.Entities.AI;

public sealed class KnowledgeDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MimeType { get; set; } = "text/plain";
    public string Status { get; set; } = "Indexed";
    public string StorageKey { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public int Version { get; set; } = 1;
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string Source { get; set; } = string.Empty;
}
