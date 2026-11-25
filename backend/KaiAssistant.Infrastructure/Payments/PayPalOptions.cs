namespace KaiAssistant.Infrastructure.Payments;

public sealed class PayPalOptions
{
    public const string SectionName = "PayPal";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox";
    public string Currency { get; set; } = "USD";
    public string BrandName { get; set; } = "Portfolio";
    public string WebhookId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public int EmailRetentionDays { get; set; } = 365;
    public decimal MinimumDonation { get; set; } = 1m;
    public decimal MaximumDonation { get; set; } = 5000m;
}
