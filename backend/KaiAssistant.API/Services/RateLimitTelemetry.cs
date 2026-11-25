using System.Collections.Concurrent;
using KaiAssistant.Infrastructure.Cache;
using StackExchange.Redis;
namespace KaiAssistant.API.Services;

public interface IRateLimitTelemetry
{
    Task RecordAllowedAsync(string ip, string path, CancellationToken cancellationToken = default);
    Task RecordBlockedAsync(string ip, string path, CancellationToken cancellationToken = default);
    Task<RateLimitTelemetrySnapshot> SnapshotAsync(int minutes, int top, CancellationToken cancellationToken = default);
}
public sealed class RateLimitTelemetrySnapshot
{
    public long TotalBlockedRequests { get; set; }
    public long TotalAllowedRequests { get; set; }
    public IReadOnlyList<RateLimitIpCount> TopIps { get; set; } = Array.Empty<RateLimitIpCount>();
}
public sealed class RateLimitIpCount
{
    public string Ip { get; set; } = string.Empty;
    public long Count { get; set; }
}
public sealed class RateLimitTelemetry : IRateLimitTelemetry
{
    private const string TotalBlockedKey = "telemetry:ratelimit:total:blocked";
    private const string TotalAllowedKey = "telemetry:ratelimit:total:allowed";
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisExecutionHelper _redisExecution;
    private readonly ILogger<RateLimitTelemetry> _logger;
    private readonly ConcurrentDictionary<string, long> _fallbackBlockedWindowCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _fallbackAllowedWindowCounts = new(StringComparer.OrdinalIgnoreCase);
    private long _totalBlocked;
    private long _totalAllowed;
    public RateLimitTelemetry(IRedisConnectionFactory redisFactory, RedisExecutionHelper redisExecution, ILogger<RateLimitTelemetry> logger)
    {
        _redisFactory = redisFactory;
        _redisExecution = redisExecution;
        _logger = logger;
    }
    public async Task RecordAllowedAsync(string ip, string path, CancellationToken cancellationToken = default)
    {
        var minuteWindow = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
        await _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                await db.StringIncrementAsync(TotalAllowedKey).WaitAsync(ct).ConfigureAwait(false);
                var windowKey = $"telemetry:ratelimit:allowed:{minuteWindow}";
                await db.StringIncrementAsync(windowKey).WaitAsync(ct).ConfigureAwait(false);
                await db.KeyExpireAsync(windowKey, TimeSpan.FromMinutes(90)).WaitAsync(ct).ConfigureAwait(false);
                return true;
            },
            () =>
            {
                Interlocked.Increment(ref _totalAllowed);
                var key = $"allowed:{minuteWindow}";
                _fallbackAllowedWindowCounts.AddOrUpdate(key, 1, (_, old) => old + 1);
                return false;
            },
            "ratelimit:telemetry:allowed",
            _logger,
            cancellationToken).ConfigureAwait(false);
    }
    public async Task RecordBlockedAsync(string ip, string path, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalBlocked);
        var minuteWindow = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");
        var current = await _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                await db.StringIncrementAsync(TotalBlockedKey).WaitAsync(ct).ConfigureAwait(false);
                var hashKey = $"telemetry:ratelimit:blocked:{minuteWindow}";
                var safeIp = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip;
                var blockedCount = await db.HashIncrementAsync(hashKey, safeIp, 1).WaitAsync(ct).ConfigureAwait(false);
                await db.KeyExpireAsync(hashKey, TimeSpan.FromMinutes(90)).WaitAsync(ct).ConfigureAwait(false);
                return blockedCount;
            },
            () =>
            {
                var key = $"blocked:{minuteWindow}:{ip}";
                return _fallbackBlockedWindowCounts.AddOrUpdate(key, 1, (_, old) => old + 1);
            },
            "ratelimit:telemetry:blocked",
            _logger,
            cancellationToken).ConfigureAwait(false);
        if (current % 10 == 0)
        {
            _logger.LogWarning(
                "RateLimitAggregate: path={Path} ip={Ip} blockedCount={BlockedCount} window={Window}",
                path,
                ip,
                current,
                minuteWindow);
        }
    }
    public async Task<RateLimitTelemetrySnapshot> SnapshotAsync(int minutes, int top, CancellationToken cancellationToken = default)
    {
        var boundedMinutes = Math.Clamp(minutes, 1, 60);
        var boundedTop = Math.Clamp(top, 1, 20);
        var grouped = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        long blockedTotal;
        long allowedTotal;
        var redisSnapshot = await _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                var blocked = (long?)(await db.StringGetAsync(TotalBlockedKey).WaitAsync(ct).ConfigureAwait(false)) ?? 0;
                var allowed = (long?)(await db.StringGetAsync(TotalAllowedKey).WaitAsync(ct).ConfigureAwait(false)) ?? 0;
                var aggregate = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < boundedMinutes; i++)
                {
                    var window = DateTimeOffset.UtcNow.AddMinutes(-i).ToString("yyyyMMddHHmm");
                    var hashKey = $"telemetry:ratelimit:blocked:{window}";
                    var entries = await db.HashGetAllAsync(hashKey).WaitAsync(ct).ConfigureAwait(false);
                    foreach (var entry in entries)
                    {
                        var ip = entry.Name.ToString();
                        if (string.IsNullOrWhiteSpace(ip))
                        {
                            continue;
                        }
                        var value = (long)entry.Value;
                        aggregate[ip] = aggregate.TryGetValue(ip, out var count) ? count + value : value;
                    }
                }
                return (blocked, allowed, aggregate, usedRedis: true);
            },
            () => (0L, 0L, new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase), usedRedis: false),
            "ratelimit:telemetry:snapshot",
            _logger,
            cancellationToken).ConfigureAwait(false);
        if (redisSnapshot.usedRedis)
        {
            blockedTotal = redisSnapshot.Item1;
            allowedTotal = redisSnapshot.Item2;
            foreach (var kvp in redisSnapshot.Item3)
            {
                grouped[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            blockedTotal = Interlocked.Read(ref _totalBlocked);
            allowedTotal = Interlocked.Read(ref _totalAllowed);
            var earliest = DateTimeOffset.UtcNow.AddMinutes(-boundedMinutes).ToString("yyyyMMddHHmm");
            foreach (var item in _fallbackBlockedWindowCounts)
            {
                if (!item.Key.StartsWith("blocked:", StringComparison.Ordinal))
                {
                    continue;
                }
                var parts = item.Key.Split(':');
                if (parts.Length < 3)
                {
                    continue;
                }
                var window = parts[1];
                if (string.Compare(window, earliest, StringComparison.Ordinal) < 0)
                {
                    continue;
                }
                var ip = parts[2];
                grouped[ip] = grouped.TryGetValue(ip, out var existing) ? existing + item.Value : item.Value;
            }
        }
        var topIps = grouped
            .OrderByDescending(x => x.Value)
            .Take(boundedTop)
            .Select(x => new RateLimitIpCount
            {
                Ip = AnonymizeIp(x.Key),
                Count = x.Value
            })
            .ToList();
        return new RateLimitTelemetrySnapshot
        {
            TotalBlockedRequests = blockedTotal,
            TotalAllowedRequests = allowedTotal,
            TopIps = topIps
        };
    }
    private static string AnonymizeIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return "unknown";
        }
        if (ip.Contains(':', StringComparison.Ordinal))
        {
            var parts = ip.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 2)
            {
                return "ipv6:masked";
            }
            return string.Join(':', parts.Take(2)) + ":****";
        }
        var octets = ip.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (octets.Length != 4)
        {
            return "ip:masked";
        }
        return $"{octets[0]}.{octets[1]}.***.***";
    }
}