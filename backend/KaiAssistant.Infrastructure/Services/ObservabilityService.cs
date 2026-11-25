namespace KaiAssistant.Infrastructure.Services;

using global::KaiAssistant.Application.Interfaces;
using global::KaiAssistant.Application.DTOs;
using global::KaiAssistant.Infrastructure.Cache;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public sealed class ObservabilityService : IObservabilityService
{
    private readonly ConcurrentQueue<long> _latencyHistory = new();
    private readonly ConcurrentDictionary<string, long> _metrics = new();
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IVisitTrackingService _visitTracking;
    private readonly ICacheDiagnosticsService _cacheDiagnostics;
    private readonly IMongoDatabase _database;
    private readonly ILogger<ObservabilityService> _logger;
    private const int MaxLatencyHistorySize = 1000;
    private static readonly Stopwatch ProcessUptime = Stopwatch.StartNew();
    public ObservabilityService(
        IRedisConnectionFactory redisFactory,
        IServiceScopeFactory scopeFactory,
        IVisitTrackingService visitTracking,
        ICacheDiagnosticsService cacheDiagnostics,
        IMongoDatabase database,
        ILogger<ObservabilityService> logger)
    {
        _redisFactory = redisFactory;
        _scopeFactory = scopeFactory;
        _visitTracking = visitTracking;
        _cacheDiagnostics = cacheDiagnostics;
        _database = database;
        _logger = logger;
    }
    public async Task<SystemObservabilityData> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            OrchestratorDiagnostics diagnostics;
            using (var scope = _scopeFactory.CreateScope())
            {
                var orchestrator = scope.ServiceProvider.GetRequiredService<IAssistantOrchestrator>();
                diagnostics = await orchestrator.GetDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
            }
            var data = new SystemObservabilityData
            {
                Timestamp = DateTimeOffset.UtcNow,
                // Cache metrics
                Cache = new CacheMetrics
                {
                    HitRatio = _cacheDiagnostics.HitRatio,
                    TotalHits = _cacheDiagnostics.HitCount,
                    TotalMisses = _cacheDiagnostics.MissCount,
                    AvgRetrievalMs = _cacheDiagnostics.AverageLockWaitMs
                },
                // AI performance
                AiPerformance = new AiPerformanceMetrics
                {
                    AvgLatencyMs = _latencyHistory.Count > 0
                        ? _latencyHistory.Average()
                        : diagnostics.LastLatencyMs,
                    P95LatencyMs = CalculatePercentile(95),
                    P99LatencyMs = CalculatePercentile(99),
                    MaxLatencyMs = _latencyHistory.Count > 0
                        ? _latencyHistory.Max()
                        : diagnostics.LastLatencyMs,
                    SuccessRate = GetSuccessRate(),
                    TotalCalls = _metrics.GetOrAdd("ai_calls", 0),
                    FailedCalls = _metrics.GetOrAdd("ai_failures", 0),
                    FallbackCalls = _metrics.GetOrAdd("ai_fallback_calls", 0),
                    Timeouts = _metrics.GetOrAdd("ai_timeouts", 0)
                },
                // Conversation metrics
                Conversations = new ConversationMetrics
                {
                    ActiveStreams = diagnostics.ActiveStreamCount,
                    TotalConversations = _metrics.GetOrAdd("conversations_total", 0),
                    AvgMessagesPerConversation = GetAverageMessagesPerConversation(),
                    ConversationsWithSummaries = GetConversationsWithSummaries()
                },
                // Rate limiting
                RateLimiting = new RateLimitingMetrics
                {
                    ViolationsLastHour = _metrics.GetOrAdd("rate_limit_violations_hour", 0),
                    ViolationsLast24h = _metrics.GetOrAdd("rate_limit_violations_24h", 0)
                },
                // Visits
                Visits = new VisitMetrics
                {
                    UniqueVisitorsToday = await GetUniqueVisitorsToday(cancellationToken),
                    TotalRequestsToday = await GetTotalRequestsToday(cancellationToken),
                    AvgRequestsPerVisitor = 0,  // Calculated below
                    PeakHourUtc = await GetPeakHour(cancellationToken)
                },
                // System health
                Health = new SystemHealth
                {
                    Status = DetermineHealthStatus(diagnostics),
                    Uptime = ProcessUptime.Elapsed.TotalSeconds / 86400 * 100,  // % of 24 hours
                    CircuitBreakersOpen = (int)_metrics.GetOrAdd("circuit_breakers_open", 0),
                    RedisConnected = await IsRedisConnected(cancellationToken),
                    MongoConnected = await IsMongoConnected(cancellationToken),
                    AiServiceConnected = diagnostics.LastLatencyMs > 0,
                    MemoryMb = (int)(process.WorkingSet64 / (1024 * 1024)),
                    ThreadCount = process.Threads.Count
                }
            };
            // Calculate avg requests per visitor
            if (data.Visits.UniqueVisitorsToday > 0)
            {
                data.Visits.AvgRequestsPerVisitor =
                    (double)data.Visits.TotalRequestsToday / data.Visits.UniqueVisitorsToday;
            }
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get observability dashboard");
            return new SystemObservabilityData();
        }
    }
    public void RecordAiCallMetric(long latencyMs, bool success = true, bool fallbackUsed = false, bool isTimeout = false)
    {
        _latencyHistory.Enqueue(latencyMs);
        // Keep queue bounded
        while (_latencyHistory.Count > MaxLatencyHistorySize)
        {
            _latencyHistory.TryDequeue(out _);
        }
        _metrics.AddOrUpdate("ai_calls", 1, (_, count) => count + 1);
        if (!success)
        {
            _metrics.AddOrUpdate("ai_failures", 1, (_, count) => count + 1);
        }
        if (fallbackUsed)
        {
            _metrics.AddOrUpdate("ai_fallback_calls", 1, (_, count) => count + 1);
        }
        if (isTimeout)
        {
            _metrics.AddOrUpdate("ai_timeouts", 1, (_, count) => count + 1);
        }
    }
    public void RecordCacheMetric(bool hit)
    {
        if (hit)
        {
            _metrics.AddOrUpdate("cache_hits", 1, (_, count) => count + 1);
        }
        else
        {
            _metrics.AddOrUpdate("cache_misses", 1, (_, count) => count + 1);
        }
    }
    public void RecordRateLimitViolation()
    {
        _metrics.AddOrUpdate("rate_limit_violations_hour", 1, (_, count) => count + 1);
        _metrics.AddOrUpdate("rate_limit_violations_24h", 1, (_, count) => count + 1);
    }
    public IEnumerable<long> GetRecentLatencies(int count = 100)
    {
        return _latencyHistory.TakeLast(count);
    }
    private double CalculatePercentile(int percentile)
    {
        if (_latencyHistory.Count == 0)
            return 0;
        var sorted = _latencyHistory.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
    private double GetSuccessRate()
    {
        var total = _metrics.GetOrAdd("ai_calls", 0);
        var failures = _metrics.GetOrAdd("ai_failures", 0);
        return total > 0 ? 1.0 - ((double)failures / total) : 1.0;
    }
    private double GetAverageMessagesPerConversation()
    {
        var totalConvs = _metrics.GetOrAdd("conversations_total", 0);
        var totalMessages = _metrics.GetOrAdd("messages_total", 0);
        return totalConvs > 0 ? (double)totalMessages / totalConvs : 0;
    }
    private int GetConversationsWithSummaries()
    {
        return (int)_metrics.GetOrAdd("conversations_with_summaries", 0);
    }
    private string DetermineHealthStatus(OrchestratorDiagnostics diagnostics)
    {
        if (diagnostics.LastLatencyMs > 10000)
            return "critical";
        if (diagnostics.LastLatencyMs > 5000)
            return "degraded";
        return "healthy";
    }
    private async Task<bool> IsRedisConnected(CancellationToken cancellationToken)
    {
        try
        {
            return _redisFactory.IsConnected;
        }
        catch
        {
            return false;
        }
    }
    private async Task<long> GetUniqueVisitorsToday(CancellationToken cancellationToken)
    {
        return await _visitTracking.GetUniqueVisitorCountAsync(
            TimeSpan.FromHours(24),
            cancellationToken);
    }
    private async Task<long> GetTotalRequestsToday(CancellationToken cancellationToken)
    {
        return await _visitTracking.GetTotalVisitCountAsync(
            TimeSpan.FromHours(24),
            cancellationToken);
    }
    private async Task<int> GetPeakHour(CancellationToken cancellationToken)
    {
        var stats = await _visitTracking.GetStatisticsAsync(cancellationToken);
        return stats.PeakHourUtc;
    }
    private async Task<bool> IsMongoConnected(CancellationToken cancellationToken)
    {
        try
        {
            var command = new MongoDB.Bson.BsonDocument("ping", 1);
            await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}