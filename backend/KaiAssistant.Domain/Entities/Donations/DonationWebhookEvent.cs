using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KaiAssistant.Domain.Entities.Donations;

public sealed class DonationWebhookEvent
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = "Received";
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessingStartedAtUtc { get; set; }
    public string? ProcessingLeaseId { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }
}
