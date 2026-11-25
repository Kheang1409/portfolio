namespace KaiAssistant.Domain.Entities;

public class VisitFingerprint
{
    public string Fingerprint { get; set; } = string.Empty;
    public DateTimeOffset VisitedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RequestCount { get; set; } = 1;
    public DateTimeOffset LastRequestAt { get; set; } = DateTimeOffset.UtcNow;
}