using Microsoft.Extensions.DependencyInjection;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Configuration;
using KaiAssistant.Application.Services;
using KaiAssistant.Infrastructure.Services;
namespace KaiAssistant.Infrastructure.Extensions;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        var emailSettingsSection = configuration.GetSection("EmailSettings");
        var enabled = true;
        var enabledEnv = Environment.GetEnvironmentVariable("SMTP_ENABLED");
        if (!string.IsNullOrWhiteSpace(enabledEnv) && bool.TryParse(enabledEnv, out var enabledParsed))
        {
            enabled = enabledParsed;
        }
        else if (!string.IsNullOrWhiteSpace(emailSettingsSection["Enabled"]) && bool.TryParse(emailSettingsSection["Enabled"], out var enabledConfig))
        {
            enabled = enabledConfig;
        }
        else
        {
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            enabled = !string.Equals(aspnetEnv, "Development", StringComparison.OrdinalIgnoreCase);
        }
        var smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER")
            ?? emailSettingsSection["SmtpServer"]
            ?? string.Empty;
        var portEnv = Environment.GetEnvironmentVariable("SMTP_PORT") ?? emailSettingsSection["Port"];
        var port = 465;
        if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, out var parsedPort))
        {
            port = parsedPort;
        }
        var senderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL")
            ?? emailSettingsSection["SenderEmail"]
            ?? string.Empty;
        var receiverEmail = Environment.GetEnvironmentVariable("SMTP_RECEIVER_EMAIL")
            ?? emailSettingsSection["ReceiverEmail"]
            ?? string.Empty;
        var senderPassword = Environment.GetEnvironmentVariable("SMTP_SENDER_PASSWORD")
            ?? emailSettingsSection["SenderPassword"]
            ?? string.Empty;
        senderPassword = senderPassword.Replace(" ", string.Empty);
        if (enabled)
        {
            if (string.IsNullOrWhiteSpace(smtpServer))
                throw new ArgumentException("Email setting 'SmtpServer' is missing or empty.");
            if (string.IsNullOrWhiteSpace(senderEmail))
                throw new ArgumentException("Email setting 'SenderEmail' is missing or empty.");
            if (string.IsNullOrWhiteSpace(receiverEmail))
                throw new ArgumentException("Email setting 'ReceiverEmail' is missing or empty.");
            if (port <= 0)
                throw new ArgumentException("Email setting 'Port' is missing or not a valid integer.");
        }
        var emailSettings = new EmailSettings(smtpServer, port, senderEmail, receiverEmail, senderPassword, enabled);
        services.AddSingleton(emailSettings);
        services.AddSingleton<IEmailService, SmtpEmailService>();
        return services;
    }
}