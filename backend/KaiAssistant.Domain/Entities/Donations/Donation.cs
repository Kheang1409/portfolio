using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KaiAssistant.Domain.Entities.Donations;

public sealed class Donation
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string PayPalOrderId { get; set; } = string.Empty;
    public string PayPalCaptureId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "CREATED";
    public DateTimeOffset? LastPaymentEventAtUtc { get; set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public bool DonorEmailProvided { get; set; }
    public string? DonorEmail { get; set; }
    public bool ReceiptSent { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public DateTimeOffset? LastOrderAttemptAtUtc { get; set; }
    public string Remark { get; set; } = string.Empty;
    public bool ShowOnLeaderboard { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAtUtc { get; set; }
    public DateTimeOffset? EmailRetentionUntilUtc { get; set; }
}
