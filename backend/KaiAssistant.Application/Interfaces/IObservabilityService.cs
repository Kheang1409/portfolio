namespace KaiAssistant.Application.Interfaces;

using global::KaiAssistant.Application.DTOs;
public interface IObservabilityService
{
    Task<SystemObservabilityData> GetDashboardAsync(CancellationToken cancellationToken = default);
    void RecordAiCallMetric(long latencyMs, bool success = true, bool fallbackUsed = false, bool isTimeout = false);
    void RecordCacheMetric(bool hit);
    void RecordRateLimitViolation();
    IEnumerable<long> GetRecentLatencies(int count = 100);
}
public class SystemObservabilityData
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public CacheMetrics Cache { get; set; } = new();
    public AiPerformanceMetrics AiPerformance { get; set; } = new();
    public ConversationMetrics Conversations { get; set; } = new();
    public RateLimitingMetrics RateLimiting { get; set; } = new();
    public VisitMetrics Visits { get; set; } = new();
    public SystemHealth Health { get; set; } = new();
}
public class CacheMetrics
{
    public double HitRatio { get; set; }
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public double AvgRetrievalMs { get; set; }
}
public class AiPerformanceMetrics
{
    public double AvgLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double SuccessRate { get; set; }
    public long TotalCalls { get; set; }
    public long FailedCalls { get; set; }
    public long FallbackCalls { get; set; }
    public long Timeouts { get; set; }
}
public class ConversationMetrics
{
    public int ActiveStreams { get; set; }
    public long TotalConversations { get; set; }
    public double AvgMessagesPerConversation { get; set; }
    public int ConversationsWithSummaries { get; set; }
}
public class RateLimitingMetrics
{
    public long ViolationsLastHour { get; set; }
    public long ViolationsLast24h { get; set; }
    public string? TopViolatorIp { get; set; }
}
public class VisitMetrics
{
    public long UniqueVisitorsToday { get; set; }
    public long TotalRequestsToday { get; set; }
    public double AvgRequestsPerVisitor { get; set; }
    public int PeakHourUtc { get; set; }
}
public class SystemHealth
{
    public string Status { get; set; } = "healthy";
    public double Uptime { get; set; } = 100.0;
    public int CircuitBreakersOpen { get; set; }
    public bool RedisConnected { get; set; }
    public bool MongoConnected { get; set; }
    public bool AiServiceConnected { get; set; }
    public long MemoryMb { get; set; }
    public int ThreadCount { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
}