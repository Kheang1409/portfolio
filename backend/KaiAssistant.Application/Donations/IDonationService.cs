namespace KaiAssistant.Application.Donations;

public interface IDonationService
{
    Task<CreateDonationResult> CreateOrderAsync(CreateDonationRequest request, CancellationToken cancellationToken);
    Task<DonationResult> CaptureAsync(CaptureDonationRequest request, CancellationToken cancellationToken);
    Task<DonorLeaderboardResult> GetLeaderboardAsync(CancellationToken cancellationToken);
    Task<bool> HandleWebhookAsync(PayPalWebhookHeaders headers, string rawBody, CancellationToken cancellationToken);
}
