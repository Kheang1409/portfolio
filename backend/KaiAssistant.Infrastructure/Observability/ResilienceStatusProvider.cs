using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
namespace KaiAssistant.Infrastructure.Observability;

public sealed class ResilienceStatusProvider : IResilienceStatusProvider
{
    private static readonly string[] Components = ["ai", "email"];
    private readonly object _sync = new();
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly RedisExecutionHelper _redisExecution;
    private readonly ILogger<ResilienceStatusProvider> _logger;
    private readonly Dictionary<string, ResilienceComponentStatus> _fallbackState = new(StringComparer.OrdinalIgnoreCase);
    public ResilienceStatusProvider(
        IRedisConnectionFactory redisFactory,
        RedisExecutionHelper redisExecution,
        ILogger<ResilienceStatusProvider> logger)
    {
        _redisFactory = redisFactory;
        _redisExecution = redisExecution;
        _logger = logger;
        foreach (var component in Components)
        {
            _fallbackState[component] = Create(component);
        }
    }
    public ResilienceSnapshot GetSnapshot()
    {
        _ = RefreshFromRedisAsync();
        lock (_sync)
        {
            var list = _fallbackState.Values
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => new ResilienceComponentStatus
                {
                    Name = x.Name,
                    CircuitState = x.CircuitState,
                    FailureCount = x.FailureCount,
                    LastFailureAtUtc = x.LastFailureAtUtc,
                    LastFailureReason = x.LastFailureReason
                })
                .ToList();
            return new ResilienceSnapshot { Components = list };
        }
    }
    public void RecordFailure(string component, string reason)
    {
        var safeComponent = Normalize(component);
        lock (_sync)
        {
            var status = _fallbackState.TryGetValue(safeComponent, out var existing) ? existing : Create(safeComponent);
            status.FailureCount++;
            status.LastFailureAtUtc = DateTimeOffset.UtcNow;
            status.LastFailureReason = reason;
            _fallbackState[safeComponent] = status;
        }
        _ = PersistFailureAsync(safeComponent, reason);
    }
    public void RecordSuccess(string component)
    {
        RecordCircuitState(component, "Closed");
    }
    public void RecordCircuitState(string component, string state)
    {
        var safeComponent = Normalize(component);
        var safeState = string.IsNullOrWhiteSpace(state) ? "Closed" : state;
        lock (_sync)
        {
            var status = _fallbackState.TryGetValue(safeComponent, out var existing) ? existing : Create(safeComponent);
            status.CircuitState = safeState;
            _fallbackState[safeComponent] = status;
        }
        _ = PersistCircuitStateAsync(safeComponent, safeState);
    }
    private async Task RefreshFromRedisAsync()
    {
        if (!_redisFactory.IsConfigured)
        {
            return;
        }
        var redisState = await _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                var updated = new Dictionary<string, ResilienceComponentStatus>(StringComparer.OrdinalIgnoreCase);
                foreach (var component in Components)
                {
                    var key = BuildKey(component);
                    var values = await db.HashGetAllAsync(key).WaitAsync(ct).ConfigureAwait(false);
                    var status = Create(component);
                    foreach (var value in values)
                    {
                        if (value.Name == "circuitState")
                        {
                            status.CircuitState = value.Value.ToString();
                            continue;
                        }
                        if (value.Name == "failureCount" && long.TryParse(value.Value.ToString(), out var failureCount))
                        {
                            status.FailureCount = failureCount;
                            continue;
                        }
                        if (value.Name == "lastFailureAtUtc" && DateTimeOffset.TryParse(value.Value.ToString(), out var lastFailureAtUtc))
                        {
                            status.LastFailureAtUtc = lastFailureAtUtc;
                            continue;
                        }
                        if (value.Name == "lastFailureReason")
                        {
                            status.LastFailureReason = value.Value.ToString();
                        }
                    }
                    updated[component] = status;
                }
                return updated;
            },
            () => new Dictionary<string, ResilienceComponentStatus>(StringComparer.OrdinalIgnoreCase),
            "resilience:refresh",
            _logger).ConfigureAwait(false);
        if (redisState.Count == 0)
        {
            return;
        }
        lock (_sync)
        {
            foreach (var kvp in redisState)
            {
                _fallbackState[kvp.Key] = kvp.Value;
            }
        }
    }
    private async Task PersistFailureAsync(string component, string reason)
    {
        await _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                var key = BuildKey(component);
                var now = DateTimeOffset.UtcNow;
                await db.HashIncrementAsync(key, "failureCount", 1).WaitAsync(ct).ConfigureAwait(false);
                await db.HashSetAsync(key, [
                    new HashEntry("name", component),
                    new HashEntry("lastFailureAtUtc", now.ToString("O")),
                    new HashEntry("lastFailureReason", reason ?? string.Empty)
                ]).WaitAsync(ct).ConfigureAwait(false);
                await db.KeyExpireAsync(key, TimeSpan.FromDays(7)).WaitAsync(ct).ConfigureAwait(false);
                return true;
            },
            () => false,
            "resilience:record_failure",
            _logger).ConfigureAwait(false);
    }
    private async Task PersistCircuitStateAsync(string component, string state)
    {
        await _redisExecution.ExecuteSafeAsync(
            async (db, ct) =>
            {
                var key = BuildKey(component);
                await db.HashSetAsync(key, [
                    new HashEntry("name", component),
                    new HashEntry("circuitState", state)
                ]).WaitAsync(ct).ConfigureAwait(false);
                await db.KeyExpireAsync(key, TimeSpan.FromDays(7)).WaitAsync(ct).ConfigureAwait(false);
                return true;
            },
            () => false,
            "resilience:record_state",
            _logger).ConfigureAwait(false);
    }
    private static ResilienceComponentStatus Create(string name)
    {
        return new ResilienceComponentStatus
        {
            Name = name,
            CircuitState = "Closed"
        };
    }
    private static string Normalize(string component)
    {
        return string.IsNullOrWhiteSpace(component) ? "unknown" : component.Trim().ToLowerInvariant();
    }
    private static string BuildKey(string component)
    {
        return $"resilience:component:{component}";
    }
}