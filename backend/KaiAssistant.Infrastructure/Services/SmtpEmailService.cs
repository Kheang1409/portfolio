using System.Net;
using System.Net.Mail;
using System.Text;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Services;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Logging;
using Polly;
namespace KaiAssistant.Infrastructure.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly IResilienceStatusProvider _resilience;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly AsyncPolicy _sendPolicy;
    public SmtpEmailService(
        EmailSettings emailSettings,
        IResilienceStatusProvider resilience,
        ILogger<SmtpEmailService> logger)
    {
        _emailSettings = emailSettings;
        _resilience = resilience;
        _logger = logger;
        var retry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(Math.Min(2500, 200 * Math.Pow(2, attempt))) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 120)));
        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(30));
        var circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                (ex, breakDelay) =>
                {
                    _resilience.RecordFailure("email", ex.Message);
                    _resilience.RecordCircuitState("email", "Open");
                    _logger.LogWarning(ex, "Email circuit opened for {BreakDelay}.", breakDelay);
                },
                () =>
                {
                    _resilience.RecordCircuitState("email", "Closed");
                    _logger.LogInformation("Email circuit reset.");
                },
                () => _resilience.RecordCircuitState("email", "HalfOpen"));
        _sendPolicy = Policy.WrapAsync(retry, timeout, circuitBreaker);
    }
    public async Task SendContactEmailAsync(string name, string email, string message, CancellationToken cancellationToken = default)
    {
        if (!_emailSettings.Enabled)
        {
            _logger.LogInformation("Email delivery is disabled; skipping portfolio contact notification for {Email}.", email);
            return;
        }
        var utcNow = DateTime.UtcNow;
        var subject = $"New portfolio contact: {name}";
        var htmlBody = BuildOwnerNotificationHtml(name, email, message, utcNow);
        var textBody = BuildOwnerNotificationText(name, email, message, utcNow);
        await SendEmailAsync(
            recipient: _emailSettings.RecieverEmail,
            subject: subject,
            htmlBody: htmlBody,
            textBody: textBody,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    public async Task SendConfirmationEmailAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        if (!_emailSettings.Enabled)
        {
            _logger.LogInformation("Email delivery is disabled; skipping portfolio confirmation email for {Email}.", email);
            return;
        }
        var utcNow = DateTime.UtcNow;
        var subject = "Thanks for reaching out - your message is in";
        var htmlBody = BuildAutoReplyHtml(name, utcNow);
        var textBody = BuildAutoReplyText(name, utcNow);
        await SendEmailAsync(
            recipient: email,
            subject: subject,
            htmlBody: htmlBody,
            textBody: textBody,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    public async Task<bool> SendDonationReceiptAsync(string name, string email, decimal amount, string currency, CancellationToken cancellationToken = default)
    {
        if (!_emailSettings.Enabled)
        {
            _logger.LogInformation("Email delivery is disabled; skipping donation receipt.");
            return false;
        }
        var safeName = HtmlEncode(string.IsNullOrWhiteSpace(name) ? "friend" : name);
        var amountText = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        var utcNow = DateTime.UtcNow;
        var subject = "Thank you for your support";
        var htmlBody = BuildDonationReceiptHtml(safeName, HtmlEncode(currency), amountText, utcNow);
        var textBody = BuildDonationReceiptText(name, currency, amountText, utcNow);
        return await SendEmailAsync(email, subject, htmlBody, textBody, cancellationToken).ConfigureAwait(false);
    }
    private static string BuildDonationReceiptHtml(string safeName, string safeCurrency, string safeAmount, DateTime utcNow)
    {
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Donation Receipt</title>
    <style>
        @media only screen and (max-width: 640px) {
            .shell { padding: 16px !important; }
            .card { border-radius: 16px !important; }
            .content { padding: 24px !important; }
            .hero { padding: 24px !important; }
            .title { font-size: 24px !important; }
        }
    </style>
</head>
<body style="margin:0; padding:0; background-color:#f5f5f7; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#1d1d1f;">
    <table width="100%" cellpadding="0" cellspacing="0" role="presentation" class="shell" style="padding:48px 24px; background:#f5f5f7;">
        <tr>
            <td align="center">
                <table width="640" cellpadding="0" cellspacing="0" role="presentation" class="card" style="max-width:640px; width:100%; background:#ffffff; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 20px; overflow:hidden; box-shadow:0 12px 40px rgba(29, 29, 31, 0.06);">
                    <tr>
                        <td class="hero" style="padding:32px 40px; background:#1d1d1f; position:relative;">
                            <div style="font-size:11px; letter-spacing:.15em; text-transform:uppercase; color:#86868b;">DONATION RECEIPT</div>
                            <div class="title" style="margin-top:16px; font-size:32px; line-height:1.15; font-weight:650; color:#ffffff;">Thank you for your support!</div>
                            <div style="margin-top:12px; font-size:14px; color:#86868b;">Processed {{utcNow:yyyy-MM-dd HH:mm}} UTC</div>
                        </td>
                    </tr>
                    <tr>
                        <td class="content" style="padding:40px 48px;">
                            <p style="margin:0 0 24px; font-size:16px; line-height:1.65; color:#1d1d1f;">Hi <strong>{{safeName}}</strong>,</p>
                            <div style="padding:20px 24px; background:#f5f5f7; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 14px; margin:0 0 28px;">
                                <div style="font-size:14px; line-height:1.6; color:#3a3a3c;">
                                    <strong style="color:#1d1d1f;">✓ Payment confirmed</strong> Your donation of <strong style="color:#1d1d1f;">{{safeCurrency}} {{safeAmount}}</strong> has been processed securely by PayPal.
                                </div>
                            </div>
                            <p style="margin:0 0 18px; font-size:15px; line-height:1.7; color:#515154;">Your generosity directly supports the development of open-source projects, technical content, and the continued growth of this portfolio. Every contribution matters.</p>
                            <p style="margin:0 0 28px; font-size:15px; line-height:1.7; color:#515154;">This receipt serves as confirmation of your donation. No goods or services were provided in exchange for this contribution.</p>
                            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="margin:0 0 28px;">
                                <tr>
                                    <td align="left">
                                        <a href="https://kaitaing.netlify.app" style="display:inline-block; padding:14px 24px; background:#1d1d1f; color:#ffffff; text-decoration:none; border-radius: 999px; font:600 13px -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; letter-spacing:.02em;">View Portfolio</a>
                                    </td>
                                </tr>
                            </table>
                            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="margin:0 0 28px;">
                                <tr>
                                    <td style="padding:16px 20px; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 12px; background:#f5f5f7; font-size:13px; color:#515154; font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;">
                                        Transaction ID: {{HtmlEncode($"TXN-{utcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000,9999)}")}}
                                    </td>
                                </tr>
                            </table>
                            <p style="margin:32px 0 0; font-size:15px; line-height:1.7; color:#515154;">With gratitude,<br><strong style="color:#1d1d1f;">Portfolio team</strong></p>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:24px 48px; border-top:1px solid rgba(29, 29, 31, 0.08); background:#fafafa;">
                            <div style="font-size:12px; line-height:1.6; color:#86868b;">
                                <div style="margin-bottom:10px;">
                                    <strong style="color:#1d1d1f;">Receipt Details:</strong> Keep this email for your records.
                                </div>
                                <div>
                                    Date: {{utcNow:yyyy-MM-dd}} | Amount: {{safeCurrency}} {{safeAmount}} | Processor: PayPal
                                </div>
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";
    }
    private static string BuildDonationReceiptText(string name, string currency, string amountText, DateTime utcNow)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DONATION RECEIPT");
        sb.AppendLine($"Processed: {utcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();
        sb.AppendLine($"Hi {name},");
        sb.AppendLine();
        sb.AppendLine($"Thank you for your donation of {currency} {amountText}. Your payment was processed securely by PayPal.");
        sb.AppendLine();
        sb.AppendLine("Your generosity directly supports the development of open-source projects, technical content, and the continued growth of this portfolio. Every contribution matters.");
        sb.AppendLine();
        sb.AppendLine("This receipt serves as confirmation of your donation. No goods or services were provided in exchange for this contribution.");
        sb.AppendLine();
        sb.AppendLine($"Transaction ID: TXN-{utcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000,9999)}");
        sb.AppendLine();
        sb.AppendLine("View Portfolio: https://kaitaing.netlify.app");
        sb.AppendLine();
        sb.AppendLine("With gratitude,");
        sb.AppendLine("Portfolio team");
        return sb.ToString();
    }
    private async Task<bool> SendEmailAsync(
        string recipient,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Skipping email send for subject {Subject} because the recipient address is empty.", subject);
            return false;
        }
        try
        {
            _logger.LogInformation("Starting email send for subject {Subject}. Timeout: {TimeoutMs}ms", subject, 10000);
            await _sendPolicy.ExecuteAsync(async ct =>
            {
                using var mail = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail),
                    Subject = subject,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mail.To.Add(recipient);
                using var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                    EnableSsl = _emailSettings.Port == 465 || _emailSettings.Port == 587,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };
                _logger.LogInformation("SMTP client configured - attempting email delivery via {SmtpServer}:{Port} with SSL={UseSSL}.", _emailSettings.SmtpServer, _emailSettings.Port, _emailSettings.Port == 465 || _emailSettings.Port == 587);
#if NET10_0_OR_GREATER
                await smtp.SendMailAsync(mail, ct).ConfigureAwait(false);
#else
                ct.ThrowIfCancellationRequested();
                await smtp.SendMailAsync(mail).ConfigureAwait(false);
#endif
                _logger.LogInformation("Email successfully sent with subject {Subject}.", subject);
                _resilience.RecordSuccess("email");
            }, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _resilience.RecordFailure("email", "SMTP delivery failed");
            _logger.LogError("Email send failed for subject {Subject}. Exception type: {ExceptionType}.", subject, ex.GetType().Name);
            return false;
        }
    }
    private static string BuildOwnerNotificationHtml(string name, string email, string messageText, DateTime utcNow)
    {
        var safeName = HtmlEncode(name);
        var safeEmail = HtmlEncode(email);
        var safeMessage = HtmlEncode(messageText);
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>New Portfolio Message</title>
    <style>
        @media only screen and (max-width: 640px) {
            .shell { padding: 16px !important; }
            .card { border-radius: 16px !important; }
            .content { padding: 24px !important; }
            .hero { padding: 24px !important; }
            .title { font-size: 24px !important; }
        }
    </style>
</head>
<body style="margin:0; padding:0; background-color:#f5f5f7; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#1d1d1f;">
    <table width="100%" cellpadding="0" cellspacing="0" role="presentation" class="shell" style="padding:48px 24px; background:#f5f5f7;">
        <tr>
            <td align="center">
                <table width="640" cellpadding="0" cellspacing="0" role="presentation" class="card" style="max-width:640px; width:100%; background:#ffffff; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 20px; overflow:hidden; box-shadow:0 12px 40px rgba(29, 29, 31, 0.06);">
                    <tr>
                        <td class="hero" style="padding:32px 40px; background:#1d1d1f; position:relative;">
                            <div style="font-size:11px; letter-spacing:.15em; text-transform:uppercase; color:#86868b;">PORTFOLIO CONTACT FORM</div>
                            <div class="title" style="margin-top:16px; font-size:32px; line-height:1.15; font-weight:650; color:#ffffff;">New message received</div>
                            <div style="margin-top:12px; font-size:14px; color:#86868b;">Received {{utcNow:yyyy-MM-dd HH:mm}} UTC</div>
                        </td>
                    </tr>
                    <tr>
                        <td class="content" style="padding:40px 48px;">
                            <p style="margin:0 0 24px; font-size:15px; line-height:1.65; color:#515154;">A visitor reached out through your portfolio contact form. The message summary is below, with a direct reply shortcut for faster follow-up.</p>
                            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="margin:0 0 24px;">
                                <tr>
                                    <td style="padding:20px 24px; background:#f5f5f7; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 14px;">
                                        <div style="font-size:10px; color:#86868b; text-transform:uppercase; letter-spacing:.12em;">FROM</div>
                                        <div style="margin-top:8px; font-size:20px; font-weight:650; color:#1d1d1f;">{{safeName}}</div>
                                        <div style="margin-top:6px; font-size:14px; color:#515154;"><a href="mailto:{{safeEmail}}" style="color:#1d1d1f; text-decoration:none; border-bottom:1px solid rgba(29, 29, 31, 0.2);">{{safeEmail}}</a></div>
                                        <div style="margin-top:16px; display:inline-block; padding:6px 12px; background:#f5f5f7; color:#1d1d1f; font-size:11px; font-weight:600; letter-spacing:.08em; border-radius: 6px; border:1px solid rgba(29, 29, 31, 0.08);">NEW INQUIRY</div>
                                    </td>
                                </tr>
                            </table>
                            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="margin:0 0 24px;">
                                <tr>
                                    <td align="left">
                                        <a href="mailto:{{safeEmail}}" style="display:inline-block; padding:14px 24px; background:#1d1d1f; color:#ffffff; text-decoration:none; border-radius: 999px; font:600 13px -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; letter-spacing:.02em;">Reply to {{safeName}}</a>
                                    </td>
                                </tr>
                            </table>
                            <div style="font-size:10px; color:#86868b; text-transform:uppercase; letter-spacing:.12em; margin-bottom:10px;">Message</div>
                            <div style="padding:20px 24px; background:#f5f5f7; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 14px;">
                                <div style="white-space:pre-wrap; word-break:break-word; font-size:15px; line-height:1.7; color:#1d1d1f;">{{safeMessage}}</div>
                            </div>
                            <p style="margin:24px 0 0; font-size:12px; line-height:1.6; color:#86868b;">This message was sanitized before rendering. Use the reply button above to continue the conversation quickly.</p>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:24px 48px; border-top:1px solid rgba(29, 29, 31, 0.08); background:#fafafa;">
                            <div style="font-size:12px; line-height:1.6; color:#86868b;">
                                <strong style="color:#1d1d1f;">System Notice:</strong> This is an automated notification from your portfolio contact form. Reply directly using the button above to reach the sender.
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";
    }
    private static string BuildOwnerNotificationText(string name, string email, string messageText, DateTime utcNow)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NEW PORTFOLIO MESSAGE");
        sb.AppendLine($"Received: {utcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();
        sb.AppendLine($"Name: {name}");
        sb.AppendLine($"Email: {email}");
        sb.AppendLine();
        sb.AppendLine("Message:");
        sb.AppendLine(messageText);
        sb.AppendLine();
        sb.AppendLine("Reply directly: mailto:" + email);
        sb.AppendLine();
        sb.AppendLine("--");
        sb.AppendLine("This is an automated notification from your portfolio contact form.");
        return sb.ToString();
    }
    private static string BuildAutoReplyHtml(string name, DateTime utcNow)
    {
        var safeName = HtmlEncode(name);
        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Message Received</title>
    <style>
        @media only screen and (max-width: 640px) {
            .shell { padding: 16px !important; }
            .card { border-radius: 16px !important; }
            .content { padding: 24px !important; }
            .hero { padding: 24px !important; }
            .title { font-size: 24px !important; }
        }
    </style>
</head>
<body style="margin:0; padding:0; background-color:#f5f5f7; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#1d1d1f;">
    <table width="100%" cellpadding="0" cellspacing="0" role="presentation" class="shell" style="padding:48px 24px; background:#f5f5f7;">
        <tr>
            <td align="center">
                <table width="640" cellpadding="0" cellspacing="0" role="presentation" class="card" style="max-width:640px; width:100%; background:#ffffff; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 20px; overflow:hidden; box-shadow:0 12px 40px rgba(29, 29, 31, 0.06);">
                    <tr>
                        <td class="hero" style="padding:32px 40px; background:#1d1d1f; position:relative;">
                            <div style="font-size:11px; letter-spacing:.15em; text-transform:uppercase; color:#86868b;">PORTFOLIO CONTACT FORM</div>
                            <div class="title" style="margin-top:16px; font-size:32px; line-height:1.15; font-weight:650; color:#ffffff;">Message Received!</div>
                            <div style="margin-top:12px; font-size:14px; color:#86868b;">Confirmed {{utcNow:yyyy-MM-dd HH:mm}} UTC</div>
                        </td>
                    </tr>
                    <tr>
                        <td class="content" style="padding:40px 48px;">
                            <p style="margin:0 0 24px; font-size:16px; line-height:1.65; color:#1d1d1f;">Hi <strong>{{safeName}}</strong>,</p>
                            <div style="padding:16px 20px; background:#f5f5f7; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 14px; margin:0 0 24px;">
                                <div style="font-size:14px; line-height:1.6; color:#3a3a3c;">
                                    <strong style="color:#1d1d1f;">✓ Message delivered</strong> Your contact form submission has been successfully received. Thank you for reaching out.
                                </div>
                            </div>
                            <p style="margin:0 0 18px; font-size:15px; line-height:1.7; color:#515154;">I appreciate you taking the time to message me through my portfolio. Your message has been received and I'll review it carefully.</p>
                            <p style="margin:0 0 24px; font-size:15px; line-height:1.7; color:#515154;"><strong style="color:#1d1d1f;">Response timeline:</strong> I typically respond within 1-2 business days. If your inquiry is time-sensitive, please mark it as urgent in your message.</p>
                            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="margin:0 0 28px;">
                                <tr>
                                    <td align="left">
                                        <a href="https://kaitaing.netlify.app" style="display:inline-block; padding:14px 24px; background:#1d1d1f; color:#ffffff; text-decoration:none; border-radius: 999px; font:600 13px -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; letter-spacing:.02em;">View Portfolio</a>
                                    </td>
                                </tr>
                            </table>
                            <table width="100%" cellpadding="0" cellspacing="0" role="presentation" style="margin:0 0 28px;">
                                <tr>
                                    <td style="padding:16px 20px; border:1px solid rgba(29, 29, 31, 0.08); border-radius: 12px; background:#f5f5f7; font-size:13px; color:#515154;">
                                        Explore recent projects and technical write-ups on the portfolio.
                                    </td>
                                </tr>
                            </table>
                            <p style="margin:28px 0 0; font-size:15px; line-height:1.7; color:#515154;">Best regards,<br><strong style="color:#1d1d1f;">Portfolio team</strong></p>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:24px 48px; border-top:1px solid rgba(29, 29, 31, 0.08); background:#fafafa;">
                            <div style="font-size:12px; line-height:1.6; color:#86868b;">
                                <div style="margin-bottom:10px;">
                                    <strong style="color:#1d1d1f;">⚠ System Notice:</strong> This is an auto-generated confirmation email.
                                </div>
                                <div style="margin-bottom:10px;">
                                    <strong>Do not reply to this email.</strong> This is a no-reply address and <strong>no one monitors</strong> messages sent here.
                                </div>
                                <div>
                                    Your message has been delivered to the portfolio owner and will be reviewed. You will receive a direct response if needed.
                                </div>
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";
    }
    private static string BuildAutoReplyText(string name, DateTime utcNow)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MESSAGE RECEIVED — AUTO-REPLY");
        sb.AppendLine($"Received: {utcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();
        sb.AppendLine($"Hi {name},");
        sb.AppendLine();
        sb.AppendLine("Your contact form submission has been successfully received. Thank you for reaching out!");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: This is an auto-generated confirmation email.");
        sb.AppendLine("- Please do not reply to this email");
        sb.AppendLine("- This is a no-reply address and no one monitors messages sent here");
        sb.AppendLine("- Your message has been delivered to the portfolio owner");
        sb.AppendLine("- You will receive a direct response if needed");
        sb.AppendLine();
        sb.AppendLine("Response timeline: Usually within 1-2 business days");
        sb.AppendLine();
        sb.AppendLine("Portfolio: https://kaitaing.netlify.app");
        sb.AppendLine();
        sb.AppendLine("Best regards,");
        sb.AppendLine("Portfolio team");
        return sb.ToString();
    }
    private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}