using KaiAssistant.API.Options;
using KaiAssistant.API.Services;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.Cache;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
namespace KaiAssistant.API.Middleware;

public sealed class RedisRateLimitingMiddleware
{
    private static readonly Meter Meter = new("KaiAssistant.RateLimiting", "1.0.0");
    private static readonly Counter<long> AllowedRequests = Meter.CreateCounter<long>("rate_limit_allowed_total");
    private static readonly Counter<long> BlockedRequests = Meter.CreateCounter<long>("rate_limit_blocked_total");
    private static readonly object MemoryFallbackLock = new();
    private const string SlidingWindowScript = @"
local key = KEYS[1]
local now = tonumber(ARGV[1])
local window = tonumber(ARGV[2])
local permit = tonumber(ARGV[3])
local member = ARGV[4]
redis.call('ZREMRANGEBYSCORE', key, '-inf', now - window)
local count = redis.call('ZCARD', key)
if count >= permit then
    local first = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
    local retryAfter = 1
    if first[2] ~= nil then
        retryAfter = math.max(1, math.floor((tonumber(first[2]) + window - now + 999) / 1000))
    end
    return {0, retryAfter}
end
redis.call('ZADD', key, now, member)
redis.call('EXPIRE', key, math.max(1, math.floor(window / 1000)))
return {1, 0}
";
    private readonly RequestDelegate _next;
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisExecutionHelper _redisExecution;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisRateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private readonly IFeatureFlagService _flags;
    private readonly IRateLimitTelemetry _telemetry;
    public RedisRateLimitingMiddleware(
        RequestDelegate next,
        IRedisConnectionFactory redisFactory,
        RedisExecutionHelper redisExecution,
        IMemoryCache memoryCache,
        IOptions<RateLimitingOptions> options,
        IFeatureFlagService flags,
        IRateLimitTelemetry telemetry,
        ILogger<RedisRateLimitingMiddleware> logger)
    {
        _next = next;
        _redisFactory = redisFactory;
        _redisExecution = redisExecution;
        _memoryCache = memoryCache;
        _logger = logger;
        _options = options.Value;
        _flags = flags;
        _telemetry = telemetry;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }
        if (!_flags.EnableRateLimiting)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }
        if (!ShouldApply(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }
        var (allowed, retryAfterSeconds, clientIp, endpointName) = await IsAllowedAsync(context).ConfigureAwait(false);
        if (!allowed)
        {
            await _telemetry.RecordBlockedAsync(clientIp, context.Request.Path.Value ?? "unknown", context.RequestAborted).ConfigureAwait(false);
            BlockedRequests.Add(1,
                KeyValuePair.Create<string, object?>("path", context.Request.Path.Value ?? "unknown"),
                KeyValuePair.Create<string, object?>("endpoint", endpointName));
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Rate limit exceeded. Please retry later."
            }).ConfigureAwait(false);
            return;
        }
        await _telemetry.RecordAllowedAsync(clientIp, context.Request.Path.Value ?? "unknown", context.RequestAborted).ConfigureAwait(false);
        AllowedRequests.Add(1,
            KeyValuePair.Create<string, object?>("path", context.Request.Path.Value ?? "unknown"),
            KeyValuePair.Create<string, object?>("endpoint", endpointName));
        await _next(context).ConfigureAwait(false);
    }
    private bool ShouldApply(PathString path)
    {
        foreach (var prefix in _options.PathPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
    private async Task<(bool Allowed, int RetryAfterSeconds, string ClientIp, string EndpointName)> IsAllowedAsync(HttpContext context)
    {
        var matchedRule = ResolveRule(context.Request.Path);
        var permitLimit = Math.Max(1, matchedRule.PermitLimit);
        var burstMultiplier = Math.Max(1, _options.BurstMultiplier);
        permitLimit *= burstMultiplier;
        var windowSeconds = Math.Max(1, matchedRule.WindowSeconds);
        var clientIp = ResolveClientIdentity(context);
        var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / windowSeconds;
        var key = _options.UseSlidingWindow
            ? $"ratelimit:{matchedRule.Name}:{clientIp}:sliding"
            : $"ratelimit:{matchedRule.Name}:{clientIp}:{window}";
        if (_redisFactory.IsConfigured)
        {
            var redis = await _redisFactory.GetConnectionAsync(context.RequestAborted).ConfigureAwait(false);
            if (redis is null)
            {
                if (_options.StrictDistributedMode)
                {
                    _logger.LogWarning("Rate limit request denied because strict distributed mode is enabled and Redis is unavailable.");
                    return (false, windowSeconds, clientIp, matchedRule.Name);
                }
                return await EvaluateMemoryFallbackAsync(key, windowSeconds, permitLimit, clientIp, matchedRule.Name).ConfigureAwait(false);
            }
            var db = redis.GetDatabase();
            var globalPermit = _options.GlobalPermitLimit.GetValueOrDefault(0);
            if (globalPermit > 0)
            {
                var globalKey = _options.UseSlidingWindow
                    ? "ratelimit:global:sliding"
                    : $"ratelimit:global:{window}";
                if (_options.UseSlidingWindow)
                {
                    var windowMsGlobal = windowSeconds * 1000L;
                    var nowMsGlobal = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var memberGlobal = $"{nowMsGlobal}:{Guid.NewGuid():N}";
                    var globalResult = await _redisExecution.ExecuteSafeAsync(
                        async (database, ct) => (RedisResult[]?)(await database
                            .ScriptEvaluateAsync(
                                SlidingWindowScript,
                                [new RedisKey(globalKey)],
                                [nowMsGlobal, windowMsGlobal, globalPermit, memberGlobal])
                            .WaitAsync(ct)
                            .ConfigureAwait(false)) ?? Array.Empty<RedisResult>(),
                        () => Array.Empty<RedisResult>(),
                        "ratelimit:global_sliding",
                        _logger,
                        context.RequestAborted).ConfigureAwait(false);
                    var globalTuple = globalResult;
                    if (globalTuple is null || globalTuple.Length < 2)
                    {
                        return await EvaluateMemoryFallbackAsync(key, windowSeconds, permitLimit, clientIp, matchedRule.Name).ConfigureAwait(false);
                    }
                    if (globalTuple is not null && globalTuple.Length >= 2 && (long)globalTuple[0] == 0)
                    {
                        return (false, (int)(long)globalTuple[1], clientIp, matchedRule.Name);
                    }
                }
                else
                {
                    var globalCount = await _redisExecution.ExecuteSafeAsync(
                        async (database, ct) => await database.StringIncrementAsync(globalKey).WaitAsync(ct).ConfigureAwait(false),
                        () => -1L,
                        "ratelimit:global_increment",
                        _logger,
                        context.RequestAborted).ConfigureAwait(false);
                    if (globalCount < 0)
                    {
                        return await EvaluateMemoryFallbackAsync(key, windowSeconds, permitLimit, clientIp, matchedRule.Name).ConfigureAwait(false);
                    }
                    if (globalCount == 1)
                    {
                        await db.KeyExpireAsync(globalKey, TimeSpan.FromSeconds(windowSeconds)).WaitAsync(context.RequestAborted).ConfigureAwait(false);
                    }
                    if (globalCount > globalPermit)
                    {
                        return (false, windowSeconds, clientIp, matchedRule.Name);
                    }
                }
            }
            if (_options.UseSlidingWindow)
            {
                var windowMs = windowSeconds * 1000L;
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var member = $"{nowMs}:{Guid.NewGuid():N}";
                var result = await _redisExecution.ExecuteSafeAsync(
                    async (database, ct) => (RedisResult[]?)(await database
                        .ScriptEvaluateAsync(
                            SlidingWindowScript,
                            [new RedisKey(key)],
                            [nowMs, windowMs, permitLimit, member])
                        .WaitAsync(ct)
                        .ConfigureAwait(false)) ?? Array.Empty<RedisResult>(),
                    () => Array.Empty<RedisResult>(),
                    "ratelimit:sliding",
                    _logger,
                    context.RequestAborted).ConfigureAwait(false);
                var tuple = result;
                if (tuple is null || tuple.Length < 2)
                {
                    return await EvaluateMemoryFallbackAsync(key, windowSeconds, permitLimit, clientIp, matchedRule.Name).ConfigureAwait(false);
                }
                var allowed = (long)tuple[0] == 1;
                var retryAfter = (int)(long)tuple[1];
                return (allowed, Math.Max(0, retryAfter), clientIp, matchedRule.Name);
            }
            var count = await _redisExecution.ExecuteSafeAsync(
                async (database, ct) => await database.StringIncrementAsync(key).WaitAsync(ct).ConfigureAwait(false),
                () => -1L,
                "ratelimit:increment",
                _logger,
                context.RequestAborted).ConfigureAwait(false);
            if (count < 0)
            {
                return await EvaluateMemoryFallbackAsync(key, windowSeconds, permitLimit, clientIp, matchedRule.Name).ConfigureAwait(false);
            }
            if (count == 1)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds)).WaitAsync(context.RequestAborted).ConfigureAwait(false);
            }
            if (count > permitLimit)
            {
                return (false, windowSeconds, clientIp, matchedRule.Name);
            }
            return (true, 0, clientIp, matchedRule.Name);
        }
        if (_options.StrictDistributedMode)
        {
            _logger.LogWarning("Rate limit request denied because strict distributed mode is enabled and Redis is unavailable.");
            return (false, windowSeconds, clientIp, matchedRule.Name);
        }
        return await EvaluateMemoryFallbackAsync(key, windowSeconds, permitLimit, clientIp, matchedRule.Name).ConfigureAwait(false);
    }
    private Task<(bool Allowed, int RetryAfterSeconds, string ClientIp, string EndpointName)> EvaluateMemoryFallbackAsync(
        string key,
        int windowSeconds,
        int permitLimit,
        string clientIp,
        string endpointName)
    {
        RedisMetrics.RecordFallback("ratelimit:fallback");
        long fallbackCount;
        // Keep increment atomic for correctness under concurrent requests.
        lock (MemoryFallbackLock)
        {
            fallbackCount = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(windowSeconds);
                return 0L;
            });
            fallbackCount++;
            _memoryCache.Set(key, fallbackCount, TimeSpan.FromSeconds(windowSeconds));
        }
        if (fallbackCount > permitLimit)
        {
            _logger.LogWarning("Request throttled for {ClientIp} using in-memory fallback limiter.", clientIp);
            return Task.FromResult((false, windowSeconds, clientIp, endpointName));
        }
        return Task.FromResult((true, 0, clientIp, endpointName));
    }
    private EndpointRateLimitRule ResolveRule(PathString path)
    {
        if (path.StartsWithSegments("/api/donations/webhook", StringComparison.OrdinalIgnoreCase))
        {
            return new EndpointRateLimitRule { Name = "paypal-webhook", PathContains = "/api/donations/webhook", PermitLimit = 120, WindowSeconds = 60 };
        }
        if (path.StartsWithSegments("/api/donations", StringComparison.OrdinalIgnoreCase))
        {
            return new EndpointRateLimitRule { Name = "donations", PathContains = "/api/donations", PermitLimit = 8, WindowSeconds = 60 };
        }
        if (_options.EndpointRules is null || _options.EndpointRules.Length == 0)
        {
            return new EndpointRateLimitRule
            {
                Name = "default",
                PermitLimit = _options.PermitLimit,
                WindowSeconds = _options.WindowSeconds
            };
        }
        var pathValue = path.Value ?? string.Empty;
        foreach (var rule in _options.EndpointRules)
        {
            if (!string.IsNullOrWhiteSpace(rule.PathContains) &&
                pathValue.Contains(rule.PathContains, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }
        return new EndpointRateLimitRule
        {
            Name = "default",
            PermitLimit = _options.PermitLimit,
            WindowSeconds = _options.WindowSeconds
        };
    }
    private static string ResolveClientIdentity(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey))
        {
            return $"api:{apiKey.ToString().Trim()}";
        }
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return $"ip:{forwarded.Split(',')[0].Trim()}";
        }
        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}