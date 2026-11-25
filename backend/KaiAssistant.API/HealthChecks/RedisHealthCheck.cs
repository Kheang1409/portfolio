using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
namespace KaiAssistant.API.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly IFeatureFlagService _flags;
    public RedisHealthCheck(IRedisConnectionFactory redisFactory, IFeatureFlagService flags)
    {
        _redisFactory = redisFactory;
        _flags = flags;
    }
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_flags.EnableCache)
        {
            return HealthCheckResult.Healthy("Redis not required because cache feature is disabled.");
        }
        var redis = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (redis is null)
        {
            return HealthCheckResult.Degraded("Redis connection is not configured; fallback cache may be used.");
        }
        try
        {
            var db = redis.GetDatabase();
            var sw = Stopwatch.StartNew();
            await db.PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            sw.Stop();
            RedisMetrics.RecordLatency(sw.Elapsed.TotalMilliseconds, "health_ping");
            RedisMetrics.SetConnected(redis.IsConnected);
            if (!redis.IsConnected)
            {
                return HealthCheckResult.Degraded("Redis multiplexer is disconnected.");
            }
            if (sw.ElapsedMilliseconds > 200)
            {
                return HealthCheckResult.Degraded($"Redis is reachable but latency is elevated ({sw.ElapsedMilliseconds}ms).");
            }
            return HealthCheckResult.Healthy($"Redis is healthy (latency: {sw.ElapsedMilliseconds}ms).");
        }
        catch (Exception ex)
        {
            RedisMetrics.RecordFailure("health_ping");
            RedisMetrics.SetConnected(false);
            return HealthCheckResult.Unhealthy("Redis connectivity check failed.", ex);
        }
    }
}