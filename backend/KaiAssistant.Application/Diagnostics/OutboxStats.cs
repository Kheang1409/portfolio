namespace KaiAssistant.Application.Diagnostics;

public sealed class OutboxStats
{
    public long PendingCount { get; set; }
    public long FailedCount { get; set; }
    public long LockedCount { get; set; }
    public long ExpiredLeaseCount { get; set; }
    public double? OldestUnprocessedAgeSeconds { get; set; }
    public Dictionary<int, long> RetryDistribution { get; set; } = new();
}