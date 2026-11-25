using System;
using KaiAssistant.Domain.Utilities;
namespace KaiAssistant.Domain.Entities;

public class EmailSettings
{
    public string SmtpServer { get; private set; } = string.Empty;
    public int Port { get; private set; }
    public string SenderEmail { get; private set; } = string.Empty;
    public string RecieverEmail { get; private set; } = string.Empty;
    public string SenderPassword { get; private set; } = string.Empty;
    public bool Enabled { get; private set; } = true;
    public EmailSettings(
        string smtpServer,
        int port,
        string senderEmail,
        string recieverEmail,
        string senderPassword,
        bool enabled = true)
    {
        if (enabled)
        {
            Guard.AgainstNullOrWhiteSpace(smtpServer, nameof(smtpServer));
            Guard.AgainstOutOfRange(port, 1, 65535, nameof(port));
            Guard.AgainstNullOrWhiteSpace(senderEmail, nameof(senderEmail));
            Guard.AgainstNullOrWhiteSpace(recieverEmail, nameof(recieverEmail));
        }
        SmtpServer = smtpServer;
        Port = port;
        SenderEmail = senderEmail;
        RecieverEmail = recieverEmail;
        SenderPassword = senderPassword;
        Enabled = enabled;
    }
}
