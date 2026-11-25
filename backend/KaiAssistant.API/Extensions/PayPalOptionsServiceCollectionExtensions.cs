using KaiAssistant.Infrastructure.Payments;

namespace KaiAssistant.API.Extensions;

public static class PayPalOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddPayPalOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PayPalOptions>()
            .Bind(configuration.GetSection(PayPalOptions.SectionName))
            .PostConfigure(options =>
            {
                options.ClientId = FirstConfigured(options.ClientId, configuration["PAYPAL_CLIENT_ID"]);
                options.ClientSecret = FirstConfigured(options.ClientSecret, configuration["PAYPAL_CLIENT_SECRET"]);
                options.Environment = FirstConfigured(configuration["PayPal:Environment"], configuration["PAYPAL_ENVIRONMENT"], options.Environment);
                options.WebhookId = FirstConfigured(options.WebhookId, configuration["PAYPAL_WEBHOOK_ID"]);
                options.MerchantId = FirstConfigured(options.MerchantId, configuration["PAYPAL_MERCHANT_ID"]);

                if (string.IsNullOrWhiteSpace(configuration["PayPal:EmailRetentionDays"]) &&
                    int.TryParse(configuration["PAYPAL_EMAIL_RETENTION_DAYS"], out var retentionDays))
                {
                    options.EmailRetentionDays = retentionDays;
                }
            })
            .Validate(options => string.IsNullOrWhiteSpace(options.Environment) || options.Environment is "sandbox" or "live", "PayPal Environment must be sandbox or live.")
            .Validate(options => options.MinimumDonation > 0 && options.MaximumDonation >= options.MinimumDonation, "PayPal donation limits are invalid.")
            .Validate(options => string.IsNullOrWhiteSpace(options.MerchantId) || options.MerchantId.Length <= 64, "PayPal merchant ID is invalid.")
            .Validate(options => options.EmailRetentionDays >= 1 && options.EmailRetentionDays <= 3650, "PayPal email retention must be 1 to 3650 days.")
            .ValidateOnStart();

        return services;
    }

    private static string FirstConfigured(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
