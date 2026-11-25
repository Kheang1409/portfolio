using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;
namespace KaiAssistant.API.Services;

public sealed class OperationalSimulationState : IOperationalSimulationState
{
    private const string AiThrottleKey = "sim:ai:throttle:until";
    private const string OutboxDelayKey = "sim:outbox:delay:ms";
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisExecutionHelper _redisExecution;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<OperationalSimulationState> _logger;
    public OperationalSimulationState(
        IRedisConnectionFactory redisFactory,
        RedisExecutionHelper redisExecution,
        IMemoryCache memoryCache,
        ILogger<OperationalSimulationState> logger)
    {
        _redisFactory = redisFactory;
        _redisExecution = redisExecution;
        _memoryCache = memoryCache;
        _logger = logger;
    }
    public Task<DateTimeOffset?> GetForceAiThrottleUntilUtcAsync(CancellationToken cancellationToken = default)
    {
        return _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                var value = await db.StringGetAsync(AiThrottleKey).WaitAsync(ct).ConfigureAwait(false);
                if (!value.HasValue)
                {
                    return null;
                }
                return DateTimeOffset.TryParse(value.ToString(), out var parsed) ? parsed : null;
            },
            () => _memoryCache.Get<DateTimeOffset?>(AiThrottleKey),
            "simulation:get_ai_throttle",
            _logger,
            cancellationToken);
    }
    public Task<int> GetOutboxArtificialDelayMsAsync(CancellationToken cancellationToken = default)
    {
        return _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                var value = await db.StringGetAsync(OutboxDelayKey).WaitAsync(ct).ConfigureAwait(false);
                return value.HasValue && int.TryParse(value.ToString(), out var parsed)
                    ? Math.Clamp(parsed, 0, 15_000)
                    : 0;
            },
            () => _memoryCache.Get<int?>(OutboxDelayKey) ?? 0,
            "simulation:get_outbox_delay",
            _logger,
            cancellationToken);
    }
    public async Task ForceAiThrottleForAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var until = DateTimeOffset.UtcNow.Add(duration <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : duration);
        _memoryCache.Set(AiThrottleKey, until, TimeSpan.FromHours(1));
        var connection = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (connection is null)
        {
            RedisMetrics.RecordFallback("simulation:set_ai_throttle");
            return;
        }
        try
        {
            var db = connection.GetDatabase();
            await db.StringSetAsync(AiThrottleKey, until.ToString("O"), expiry: TimeSpan.FromHours(1))
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is StackExchange.Redis.RedisConnectionException or StackExchange.Redis.RedisTimeoutException)
        {
            RedisMetrics.RecordFailure("simulation:set_ai_throttle");
            RedisMetrics.RecordFallback("simulation:set_ai_throttle");
            _logger.LogWarning(ex, "Failed to persist AI throttle simulation state to Redis. Using memory fallback.");
        }
    }
    public async Task SetOutboxArtificialDelayAsync(int delayMs, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(delayMs, 0, 15_000);
        _memoryCache.Set(OutboxDelayKey, bounded, TimeSpan.FromHours(1));
        var connection = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (connection is null)
        {
            RedisMetrics.RecordFallback("simulation:set_outbox_delay");
            return;
        }
        try
        {
            var db = connection.GetDatabase();
            await db.StringSetAsync(OutboxDelayKey, bounded.ToString(), expiry: TimeSpan.FromHours(1))
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is StackExchange.Redis.RedisConnectionException or StackExchange.Redis.RedisTimeoutException)
        {
            RedisMetrics.RecordFailure("simulation:set_outbox_delay");
            RedisMetrics.RecordFallback("simulation:set_outbox_delay");
            _logger.LogWarning(ex, "Failed to persist outbox delay simulation state to Redis. Using memory fallback.");
        }
    }
}