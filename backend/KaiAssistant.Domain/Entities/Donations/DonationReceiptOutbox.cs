using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KaiAssistant.Domain.Entities.Donations;

public sealed class DonationReceiptOutbox
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string DonationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public DateTimeOffset? LockExpiresAtUtc { get; set; }
    public DateTimeOffset RetainUntilUtc { get; set; }
    public string? LastError { get; set; }
}
