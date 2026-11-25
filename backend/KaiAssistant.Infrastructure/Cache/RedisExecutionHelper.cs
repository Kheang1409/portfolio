using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
namespace KaiAssistant.Infrastructure.Cache;

public sealed class RedisExecutionHelper
{
    private static readonly TimeSpan WarningWindow = TimeSpan.FromMinutes(1);
    private readonly IRedisConnectionFactory _factory;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastWarningAt = new(StringComparer.Ordinal);
    public RedisExecutionHelper(IRedisConnectionFactory factory)
    {
        _factory = factory;
    }
    public async Task<T> ExecuteSafeAsync<T>(
        Func<IDatabase, CancellationToken, Task<T>> action,
        Func<T> fallback,
        string operation,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var connection = await _factory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (connection is null)
        {
            RedisMetrics.RecordFallback(operation);
            LogWarningOnce(logger, operation, "Redis is unavailable; fallback path is active.");
            return fallback();
        }
        try
        {
            return await action(connection.GetDatabase(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
        {
            RedisMetrics.RecordFailure(operation);
            RedisMetrics.RecordFallback(operation);
            LogWarningOnce(logger, operation, "Redis operation failed; fallback path is active.", ex);
            return fallback();
        }
    }
    private void LogWarningOnce(ILogger logger, string key, string message, Exception? exception = null)
    {
        var now = DateTimeOffset.UtcNow;
        var last = _lastWarningAt.GetOrAdd(key, DateTimeOffset.MinValue);
        if (now - last < WarningWindow)
        {
            return;
        }
        _lastWarningAt[key] = now;
        if (exception is null)
        {
            logger.LogWarning("{Message} Operation={Operation}", message, key);
            return;
        }
        logger.LogWarning(exception, "{Message} Operation={Operation}", message, key);
    }
}