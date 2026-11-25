using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Domain.Entities.AI;

public sealed class AiEvaluationRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<AiEvaluationEntry> Entries { get; set; } = [];
    public double AverageLatencyMs { get; set; }
    public double AverageQualityScore { get; set; }
}
public sealed class AiEvaluationEntry
{
    public string Prompt { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public long LatencyMs { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public double QualityScore { get; set; }
}