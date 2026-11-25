using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Infrastructure.AI;

public sealed class ModelHealthStateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public DateTimeOffset? LastSuccessAtUtc { get; set; }
    public DateTimeOffset? LastFailureAtUtc { get; set; }
    public string? LastFailureReason { get; set; }
    public DateTimeOffset? CircuitOpenUntilUtc { get; set; }
    public int ConsecutiveFailures { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public long FallbackUsageCount { get; set; }
    public long CooldownCount { get; set; }
    public long LatencySampleCount { get; set; }
    public double TotalLatencyMs { get; set; }
    public double AverageLatencyMs { get; set; }
    public DateTimeOffset? WindowStartedAtUtc { get; set; }
    public long RecentSuccessCount { get; set; }
    public long RecentFailureCount { get; set; }
    public long RecentCooldownCount { get; set; }
    public double RecentFailureRate { get; set; }
    public double WeightedSuccessRate { get; set; }
    public double CooldownFrequency { get; set; }
    public double DynamicScore { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public decimal TotalEstimatedCostUsd { get; set; }
    public long StateVersion { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}