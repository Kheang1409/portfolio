namespace KaiAssistant.Application.Donations;

public sealed record CreateDonationRequest(decimal Amount, string? Name, string? Email, string? Remark, bool ShowOnLeaderboard, string IdempotencyKey);
public sealed record CreateDonationResult(string OrderId);
public sealed record CaptureDonationRequest(string OrderId);
public sealed record DonationResult(string Status, decimal Amount, string Currency);
public sealed record DonorLeaderboardEntry(string Name, decimal Amount, string Remark, DateTimeOffset PaidAtUtc, int Rank);
public sealed record DonorLeaderboardResult(IReadOnlyList<DonorLeaderboardEntry> Entries, int TotalDonations, decimal TotalAmount);
public sealed record PayPalWebhookHeaders(string? TransmissionId, string? TransmissionTime, string? CertificateUrl, string? AuthAlgorithm, string? TransmissionSignature);
