using System.Diagnostics.Metrics;
using System.Threading;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.FeatureFlags;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
namespace KaiAssistant.Infrastructure.Cache;

public sealed class RedisCacheService : ICacheService, ICacheDiagnosticsService
{
    private static readonly Meter Meter = new("KaiAssistant.Cache", "1.0.0");
    private static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("cache_hits_total");
    private static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>("cache_misses_total");
    private static readonly Counter<long> CacheRebuilds = Meter.CreateCounter<long>("cache_rebuild_total");
    private static readonly Histogram<double> CacheLockWaitMs = Meter.CreateHistogram<double>("cache_rebuild_lock_wait_ms", "ms");
    private static readonly ObservableGauge<double> CacheHitRatioGauge = Meter.CreateObservableGauge(
        "cache_hit_ratio",
        () =>
        {
            var total = _hitCount + _missCount;
            var ratio = total == 0 ? 1d : (double)_hitCount / total;
            return new Measurement<double>(ratio);
        });
    private static long _hitCount;
    private static long _missCount;
    private static long _rebuildCount;
    private static long _totalLockWaitMs;
    private static long _lockWaitCount;
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisExecutionHelper _redisExecution;
    private readonly IMemoryCache _memoryCache;
    private readonly DistributedCacheOptions _options;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fallbackKeyLocks = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public RedisCacheService(
        IRedisConnectionFactory redisFactory,
        RedisExecutionHelper redisExecution,
        IMemoryCache memoryCache,
        IOptions<DistributedCacheOptions> options,
        IFeatureFlagService featureFlags,
        ILogger<RedisCacheService> logger)
    {
        _redisFactory = redisFactory;
        _redisExecution = redisExecution;
        _memoryCache = memoryCache;
        _options = options.Value;
        _featureFlags = featureFlags;
        _logger = logger;
    }
    public long HitCount => Interlocked.Read(ref _hitCount);
    public long MissCount => Interlocked.Read(ref _missCount);
    public double HitRatio
    {
        get
        {
            var total = HitCount + MissCount;
            return total == 0 ? 1d : (double)HitCount / total;
        }
    }
    public long RebuildCount => Interlocked.Read(ref _rebuildCount);
    public double AverageLockWaitMs
    {
        get
        {
            var count = Interlocked.Read(ref _lockWaitCount);
            if (count <= 0)
            {
                return 0;
            }
            return (double)Interlocked.Read(ref _totalLockWaitMs) / count;
        }
    }
    public bool IsRedisConnected => _redisFactory.IsConnected;
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_featureFlags.EnableCache)
        {
            return default;
        }
        var finalKey = BuildKey(key);
        var redisResult = await _redisExecution.ExecuteSafeAsync(
            async (db, ct) => await db.StringGetAsync(finalKey).WaitAsync(ct).ConfigureAwait(false),
            () => RedisValue.Null,
            "cache:get",
            _logger,
            cancellationToken).ConfigureAwait(false);
        if (redisResult.HasValue)
        {
            CacheHits.Add(1, KeyValuePair.Create<string, object?>("layer", "redis"));
            Interlocked.Increment(ref _hitCount);
            _logger.LogDebug("Cache hit (redis): {Key}", finalKey);
            return JsonSerializer.Deserialize<T>((string)redisResult!, SerializerOptions);
        }
        if (_redisFactory.IsConfigured)
        {
            CacheMisses.Add(1, KeyValuePair.Create<string, object?>("layer", "redis"));
            Interlocked.Increment(ref _missCount);
            _logger.LogDebug("Cache miss (redis): {Key}", finalKey);
        }
        if (_memoryCache.TryGetValue(finalKey, out T? memoryValue))
        {
            CacheHits.Add(1, KeyValuePair.Create<string, object?>("layer", "memory"));
            Interlocked.Increment(ref _hitCount);
            _logger.LogDebug("Cache hit (memory): {Key}", finalKey);
            return memoryValue;
        }
        CacheMisses.Add(1, KeyValuePair.Create<string, object?>("layer", "memory"));
        Interlocked.Increment(ref _missCount);
        _logger.LogDebug("Cache miss (memory): {Key}", finalKey);
        return default;
    }
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (!_featureFlags.EnableCache)
        {
            return;
        }
        var finalKey = BuildKey(key);
        var effectiveTtl = ttl <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(Math.Max(1, _options.DefaultTtlSeconds))
            : ttl;
        effectiveTtl = ApplyTtlJitter(effectiveTtl);
        if (_redisFactory.IsConfigured)
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            var wroteRedis = await _redisExecution.ExecuteSafeAsync(
                async (db, ct) => await db.StringSetAsync(finalKey, payload, effectiveTtl).WaitAsync(ct).ConfigureAwait(false),
                () => false,
                "cache:set",
                _logger,
                cancellationToken).ConfigureAwait(false);
            if (wroteRedis)
            {
                return;
            }
        }
        _memoryCache.Set(finalKey, value, effectiveTtl);
    }
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_featureFlags.EnableCache)
        {
            return;
        }
        var finalKey = BuildKey(key);
        if (_redisFactory.IsConfigured)
        {
            var removedRedis = await _redisExecution.ExecuteSafeAsync(
                async (db, ct) => await db.KeyDeleteAsync(finalKey).WaitAsync(ct).ConfigureAwait(false),
                () => false,
                "cache:remove",
                _logger,
                cancellationToken).ConfigureAwait(false);
            if (removedRedis)
            {
                return;
            }
        }
        _memoryCache.Remove(finalKey);
    }
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }
        var finalKey = BuildKey(key);
        if (_redisFactory.IsConfigured)
        {
            var connection = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection is null)
            {
                RedisMetrics.RecordFallback("cache:get_or_create");
                return await FallbackGetOrCreateAsync(finalKey, key, factory, ttl, cancellationToken).ConfigureAwait(false);
            }
            var db = connection.GetDatabase();
            var lockKey = $"{finalKey}:rebuild:lock";
            var lockToken = $"{Environment.MachineName}:{Guid.NewGuid():N}";
            var lockExpiry = TimeSpan.FromMilliseconds(Math.Max(250, _options.RebuildLockTimeoutMs));
            var waitTimeout = TimeSpan.FromMilliseconds(Math.Max(250, _options.RebuildLockTimeoutMs));
            var acquired = await db.StringSetAsync(lockKey, lockToken, lockExpiry, when: When.NotExists).ConfigureAwait(false);
            if (!acquired)
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed < waitTimeout && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                    cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
                    if (cached is not null)
                    {
                        sw.Stop();
                        RecordLockWait(sw.Elapsed.TotalMilliseconds);
                        return cached;
                    }
                }
                sw.Stop();
                RecordLockWait(sw.Elapsed.TotalMilliseconds);
                acquired = await db.StringSetAsync(lockKey, lockToken, lockExpiry, when: When.NotExists).ConfigureAwait(false);
                if (!acquired)
                {
                    var fallbackValue = await factory(cancellationToken).ConfigureAwait(false);
                    if (fallbackValue is not null)
                    {
                        await SetAsync(key, fallbackValue, ttl, cancellationToken).ConfigureAwait(false);
                    }
                    return fallbackValue;
                }
            }
            try
            {
                cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
                if (cached is not null)
                {
                    return cached;
                }
                var value = await factory(cancellationToken).ConfigureAwait(false);
                if (value is not null)
                {
                    await SetAsync(key, value, ttl, cancellationToken).ConfigureAwait(false);
                    CacheRebuilds.Add(1);
                    Interlocked.Increment(ref _rebuildCount);
                }
                return value;
            }
            finally
            {
                const string releaseScript = @"
if redis.call('GET', KEYS[1]) == ARGV[1] then
  return redis.call('DEL', KEYS[1])
end
return 0";
                await db.ScriptEvaluateAsync(releaseScript, [new RedisKey(lockKey)], [lockToken]).ConfigureAwait(false);
            }
        }
        return await FallbackGetOrCreateAsync(finalKey, key, factory, ttl, cancellationToken).ConfigureAwait(false);
    }
    private async Task<T> FallbackGetOrCreateAsync<T>(
        string finalKey,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var gate = _fallbackKeyLocks.GetOrAdd(finalKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var cached = default(T);
        try
        {
            // Re-check after acquiring lock to avoid duplicate factory execution.
            cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
            var value = await factory(cancellationToken).ConfigureAwait(false);
            if (value is not null)
            {
                await SetAsync(key, value, ttl, cancellationToken).ConfigureAwait(false);
                CacheRebuilds.Add(1);
                Interlocked.Increment(ref _rebuildCount);
            }
            return value;
        }
        finally
        {
            gate.Release();
            if (gate.CurrentCount == 1)
            {
                _fallbackKeyLocks.TryRemove(finalKey, out _);
            }
        }
    }
    private string BuildKey(string key)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.KeyPrefix) ? "cache" : _options.KeyPrefix;
        var ns = string.IsNullOrWhiteSpace(_options.Namespace) ? "default" : _options.Namespace;
        var input = $"{prefix}:{ns}:{key}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"cache:{ns}:{hash}";
    }
    public async Task<long?> GetKeyCountAsync(CancellationToken cancellationToken = default)
    {
        if (!_redisFactory.IsConnected)
        {
            return null;
        }
        try
        {
            var redis = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (redis is null)
            {
                return null;
            }
            var db = redis.GetDatabase();
            var result = await db.ExecuteAsync("DBSIZE").WaitAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsNull)
            {
                return null;
            }
            return (long)result;
        }
        catch
        {
            return null;
        }
    }
    private TimeSpan ApplyTtlJitter(TimeSpan baseTtl)
    {
        var jitterSeconds = Math.Max(0, _options.TtlJitterSeconds);
        if (jitterSeconds == 0)
        {
            return baseTtl;
        }
        var jitter = Random.Shared.Next(0, jitterSeconds + 1);
        return baseTtl.Add(TimeSpan.FromSeconds(jitter));
    }
    private static void RecordLockWait(double elapsedMs)
    {
        var bounded = Math.Max(0, elapsedMs);
        CacheLockWaitMs.Record(bounded);
        Interlocked.Add(ref _totalLockWaitMs, (long)bounded);
        Interlocked.Increment(ref _lockWaitCount);
    }
}