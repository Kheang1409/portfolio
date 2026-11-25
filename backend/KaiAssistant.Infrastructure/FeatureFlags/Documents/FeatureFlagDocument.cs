using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Infrastructure.FeatureFlags.Documents;

public sealed class FeatureFlagDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}