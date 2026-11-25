using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
namespace KaiAssistant.Infrastructure.EventBus;

public sealed class RedisEventIdempotencyStore : IEventIdempotencyStore
{
    private const string ProcessedValue = "processed";
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisExecutionHelper _redisExecution;
    private readonly ILogger<RedisEventIdempotencyStore> _logger;
    private readonly IMemoryCache _memoryCache;
    public RedisEventIdempotencyStore(
        IRedisConnectionFactory redisFactory,
        RedisExecutionHelper redisExecution,
        IMemoryCache memoryCache,
        ILogger<RedisEventIdempotencyStore> logger)
    {
        _redisFactory = redisFactory;
        _redisExecution = redisExecution;
        _memoryCache = memoryCache;
        _logger = logger;
    }
    public async Task<IdempotencyAcquireResult> TryAcquireAsync(string idempotencyKey, TimeSpan processingTtl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return IdempotencyAcquireResult.Acquired;
        }
        var key = BuildKey(idempotencyKey);
        if (_redisFactory.IsConfigured)
        {
            return await _redisExecution.ExecuteSafeAsync(
                async (db, ct) =>
                {
                    var existing = await db.StringGetAsync(key).WaitAsync(ct).ConfigureAwait(false);
                    if (existing.HasValue && string.Equals(existing.ToString(), ProcessedValue, StringComparison.Ordinal))
                    {
                        return IdempotencyAcquireResult.AlreadyProcessed;
                    }
                    var lockValue = $"processing:{Environment.MachineName}:{Guid.NewGuid():N}";
                    var acquired = await db.StringSetAsync(key, lockValue, processingTtl, When.NotExists).WaitAsync(ct).ConfigureAwait(false);
                    if (acquired)
                    {
                        return IdempotencyAcquireResult.Acquired;
                    }
                    var current = await db.StringGetAsync(key).WaitAsync(ct).ConfigureAwait(false);
                    if (current.HasValue && string.Equals(current.ToString(), ProcessedValue, StringComparison.Ordinal))
                    {
                        return IdempotencyAcquireResult.AlreadyProcessed;
                    }
                    return IdempotencyAcquireResult.Busy;
                },
                () =>
                {
                    if (_memoryCache.TryGetValue<string>(key, out var status) && string.Equals(status, ProcessedValue, StringComparison.Ordinal))
                    {
                        return IdempotencyAcquireResult.AlreadyProcessed;
                    }
                    if (_memoryCache.TryGetValue<string>(key, out _))
                    {
                        return IdempotencyAcquireResult.Busy;
                    }
                    _memoryCache.Set(key, "processing", processingTtl);
                    return IdempotencyAcquireResult.Acquired;
                },
                "idempotency:acquire",
                _logger,
                cancellationToken).ConfigureAwait(false);
        }
        if (_memoryCache.TryGetValue<string>(key, out var status) && string.Equals(status, ProcessedValue, StringComparison.Ordinal))
        {
            return IdempotencyAcquireResult.AlreadyProcessed;
        }
        if (_memoryCache.TryGetValue<string>(key, out _))
        {
            return IdempotencyAcquireResult.Busy;
        }
        _memoryCache.Set(key, "processing", processingTtl);
        return IdempotencyAcquireResult.Acquired;
    }
    public async Task MarkProcessedAsync(string idempotencyKey, TimeSpan processedTtl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return;
        }
        var key = BuildKey(idempotencyKey);
        if (_redisFactory.IsConfigured)
        {
            var wrote = await _redisExecution.ExecuteSafeAsync(
                async (db, ct) => await db.StringSetAsync(key, ProcessedValue, processedTtl).WaitAsync(ct).ConfigureAwait(false),
                () => false,
                "idempotency:mark_processed",
                _logger,
                cancellationToken).ConfigureAwait(false);
            if (wrote)
            {
                return;
            }
        }
        _memoryCache.Set(key, ProcessedValue, processedTtl);
    }
    public async Task<bool> IsProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return false;
        }
        var key = BuildKey(idempotencyKey);
        if (_redisFactory.IsConfigured)
        {
            return await _redisExecution.ExecuteSafeAsync(
                async (db, ct) =>
                {
                    var val = await db.StringGetAsync(key).WaitAsync(ct).ConfigureAwait(false);
                    return val.HasValue && string.Equals(val.ToString(), ProcessedValue, StringComparison.Ordinal);
                },
                () => _memoryCache.TryGetValue<string>(key, out var status)
                      && string.Equals(status, ProcessedValue, StringComparison.Ordinal),
                "idempotency:is_processed",
                _logger,
                cancellationToken).ConfigureAwait(false);
        }
        return _memoryCache.TryGetValue<string>(key, out var status)
            && string.Equals(status, ProcessedValue, StringComparison.Ordinal);
    }
    public async Task ReleaseAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return;
        }
        var key = BuildKey(idempotencyKey);
        if (_redisFactory.IsConfigured)
        {
            var deleted = await _redisExecution.ExecuteSafeAsync(
                async (db, ct) =>
                {
                    var current = await db.StringGetAsync(key).WaitAsync(ct).ConfigureAwait(false);
                    if (!current.HasValue || string.Equals(current.ToString(), ProcessedValue, StringComparison.Ordinal))
                    {
                        return true;
                    }
                    await db.KeyDeleteAsync(key).WaitAsync(ct).ConfigureAwait(false);
                    return true;
                },
                () => false,
                "idempotency:release",
                _logger,
                cancellationToken).ConfigureAwait(false);
            if (deleted)
            {
                return;
            }
        }
        if (_memoryCache.TryGetValue<string>(key, out var status) && !string.Equals(status, ProcessedValue, StringComparison.Ordinal))
        {
            _memoryCache.Remove(key);
        }
    }
    private static string BuildKey(string idempotencyKey)
    {
        return $"idem:event:{idempotencyKey}";
    }
}