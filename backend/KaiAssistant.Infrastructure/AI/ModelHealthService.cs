using System.Collections.Concurrent;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.AI;

public sealed class ModelHealthService : IModelHealthService
{
    private readonly ConcurrentDictionary<string, MutableModelHealth> _health = new(StringComparer.OrdinalIgnoreCase);
    private readonly IOptionsMonitor<AiModelOrchestrationOptions> _options;
    private readonly IMongoCollection<ModelHealthStateDocument>? _collection;
    private readonly ILogger<ModelHealthService> _logger;
    private readonly object _activeModelSync = new();
    private readonly object _refreshSync = new();
    private readonly object _persistSync = new();
    private string? _activeModel;
    private DateTimeOffset _lastRefreshAtUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _lastPersistAtUtc = DateTimeOffset.MinValue;
    private long _stateVersion;
    public ModelHealthService(
        IOptionsMonitor<AiModelOrchestrationOptions> options,
        IServiceProvider serviceProvider,
        ILogger<ModelHealthService> logger)
    {
        _options = options;
        _logger = logger;
        var database = serviceProvider.GetService<IMongoDatabase>();
        if (database is not null)
        {
            _collection = database.GetCollection<ModelHealthStateDocument>("assistant_model_health");
        }
    }
    public bool CanAttempt(string modelName, DateTimeOffset utcNow)
    {
        RefreshFromStoreIfNeeded(utcNow);
        FlushIfDue(utcNow);
        var state = _health.GetOrAdd(modelName, _ => new MutableModelHealth(modelName));
        return !(state.CircuitOpenUntilUtc.HasValue && state.CircuitOpenUntilUtc.Value > utcNow);
    }
    public void RecordSuccess(string modelName, DateTimeOffset utcNow, bool fallbackUsed)
    {
        var state = GetState(modelName);
        RotateWindowIfNeeded(state, utcNow);
        state.SuccessCount++;
        state.RecentSuccessCount++;
        state.LastSuccessAtUtc = utcNow;
        state.LastUsedAtUtc = utcNow;
        state.LastFailureReason = null;
        state.CircuitOpenUntilUtc = null;
        state.ConsecutiveFailures = 0;
        if (fallbackUsed)
        {
            state.FallbackUsageCount++;
        }
        RecomputeState(state);
        TouchState(state, utcNow, immediatePersist: false);
    }
    public void RecordFailure(string modelName, DateTimeOffset utcNow, string reason)
    {
        var settings = _options.CurrentValue;
        var state = GetState(modelName);
        RotateWindowIfNeeded(state, utcNow);
        state.FailureCount++;
        state.RecentFailureCount++;
        state.ConsecutiveFailures++;
        state.LastFailureAtUtc = utcNow;
        state.LastUsedAtUtc = utcNow;
        state.LastFailureReason = reason;
        if (state.ConsecutiveFailures >= Math.Max(1, settings.CircuitFailureThreshold))
        {
            state.CircuitOpenUntilUtc = utcNow.AddSeconds(Math.Max(5, settings.CircuitOpenSeconds));
            state.ConsecutiveFailures = 0;
        }
        RecomputeState(state);
        TouchState(state, utcNow, immediatePersist: false);
    }
    public void RecordLatency(string modelName, DateTimeOffset utcNow, double latencyMs)
    {
        if (latencyMs <= 0)
        {
            return;
        }
        var state = GetState(modelName);
        RotateWindowIfNeeded(state, utcNow);
        state.TotalLatencyMs += latencyMs;
        state.LatencySampleCount++;
        state.LastUsedAtUtc = utcNow;
        RecomputeState(state);
        TouchState(state, utcNow, immediatePersist: false);
    }
    public void RecordUsage(string modelName, DateTimeOffset utcNow, int inputTokens, int outputTokens, decimal estimatedCostUsd)
    {
        var state = GetState(modelName);
        RotateWindowIfNeeded(state, utcNow);
        state.TotalInputTokens += Math.Max(0, inputTokens);
        state.TotalOutputTokens += Math.Max(0, outputTokens);
        state.TotalEstimatedCostUsd += Math.Max(0m, estimatedCostUsd);
        state.LastUsedAtUtc = utcNow;
        RecomputeState(state);
        TouchState(state, utcNow, immediatePersist: false);
    }
    public void MarkRateLimited(string modelName, DateTimeOffset utcNow, DateTimeOffset? retryAtUtc, string reason)
    {
        var settings = _options.CurrentValue;
        var state = GetState(modelName);
        RotateWindowIfNeeded(state, utcNow);
        var cooldown = retryAtUtc ?? utcNow.AddMinutes(Math.Max(1, settings.QuotaCooldownMinutes));
        state.FailureCount++;
        state.RecentFailureCount++;
        state.CooldownCount++;
        state.RecentCooldownCount++;
        state.LastFailureAtUtc = utcNow;
        state.LastUsedAtUtc = utcNow;
        state.LastFailureReason = reason;
        state.CircuitOpenUntilUtc = cooldown;
        state.ConsecutiveFailures = 0;
        RecomputeState(state);
        TouchState(state, utcNow, immediatePersist: true);
    }
    public void EnsureModelsRegistered(IEnumerable<string> modelNames, DateTimeOffset utcNow)
    {
        foreach (var modelName in modelNames.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var state = GetState(modelName);
            RecomputeState(state);
            TouchState(state, utcNow, immediatePersist: false);
        }
        FlushIfDue(utcNow);
    }
    public void SetActiveModel(string modelName, DateTimeOffset utcNow)
    {
        lock (_activeModelSync)
        {
            _activeModel = modelName;
        }
        foreach (var kvp in _health)
        {
            kvp.Value.IsActive = string.Equals(kvp.Key, modelName, StringComparison.OrdinalIgnoreCase);
            TouchState(kvp.Value, utcNow, immediatePersist: false);
        }
        if (!_health.ContainsKey(modelName))
        {
            var state = GetState(modelName);
            state.IsActive = true;
            TouchState(state, utcNow, immediatePersist: false);
        }
        PersistDirtyStates(utcNow);
    }
    public string? GetActiveModel()
    {
        lock (_activeModelSync)
        {
            return _activeModel;
        }
    }
    public long GetStateVersion()
    {
        return Interlocked.Read(ref _stateVersion);
    }
    public void FlushPendingChanges(DateTimeOffset utcNow)
    {
        PersistDirtyStates(utcNow);
    }
    public AiModelHealthSnapshot GetSnapshot(DateTimeOffset utcNow)
    {
        RefreshFromStoreIfNeeded(utcNow);
        FlushIfDue(utcNow);
        var models = _health.Values
            .OrderBy(x => x.ModelName, StringComparer.OrdinalIgnoreCase)
            .Select(x =>
            {
                var total = x.SuccessCount + x.FailureCount;
                var failureRate = total == 0 ? 0d : (double)x.FailureCount / total;
                return new AiModelHealthStatus
                {
                    ModelName = x.ModelName,
                    IsHealthy = !(x.CircuitOpenUntilUtc.HasValue && x.CircuitOpenUntilUtc.Value > utcNow),
                    LastSuccessAtUtc = x.LastSuccessAtUtc,
                    LastFailureAtUtc = x.LastFailureAtUtc,
                    LastFailureReason = x.LastFailureReason,
                    SuccessCount = x.SuccessCount,
                    FailureCount = x.FailureCount,
                    FallbackUsageCount = x.FallbackUsageCount,
                    FailureRate = failureRate,
                    RecentFailureRate = x.RecentFailureRate,
                    WeightedSuccessRate = x.WeightedSuccessRate,
                    AverageLatencyMs = x.AverageLatencyMs,
                    CooldownFrequency = x.CooldownFrequency,
                    DynamicScore = x.DynamicScore,
                    TotalInputTokens = x.TotalInputTokens,
                    TotalOutputTokens = x.TotalOutputTokens,
                    TotalEstimatedCostUsd = x.TotalEstimatedCostUsd,
                    CircuitOpenUntilUtc = x.CircuitOpenUntilUtc
                };
            })
            .ToList();
        return new AiModelHealthSnapshot
        {
            CapturedAtUtc = utcNow,
            ActiveModel = GetActiveModel(),
            StateVersion = GetStateVersion(),
            Models = models
        };
    }
    private void RefreshFromStoreIfNeeded(DateTimeOffset utcNow)
    {
        if (_collection is null)
        {
            return;
        }
        var refreshSeconds = Math.Clamp(_options.CurrentValue.ModelHealthStateRefreshSeconds, 1, 300);
        if (utcNow - _lastRefreshAtUtc < TimeSpan.FromSeconds(refreshSeconds))
        {
            return;
        }
        lock (_refreshSync)
        {
            if (utcNow - _lastRefreshAtUtc < TimeSpan.FromSeconds(refreshSeconds))
            {
                return;
            }
            try
            {
                var docs = _collection
                    .Find(Builders<ModelHealthStateDocument>.Filter.Empty)
                    .ToList();
                foreach (var doc in docs)
                {
                    var state = GetState(doc.ModelName);
                    state.LastSuccessAtUtc = doc.LastSuccessAtUtc;
                    state.LastFailureAtUtc = doc.LastFailureAtUtc;
                    state.LastFailureReason = doc.LastFailureReason;
                    state.CircuitOpenUntilUtc = doc.CircuitOpenUntilUtc;
                    state.ConsecutiveFailures = doc.ConsecutiveFailures;
                    state.SuccessCount = doc.SuccessCount;
                    state.FailureCount = doc.FailureCount;
                    state.FallbackUsageCount = doc.FallbackUsageCount;
                    state.CooldownCount = doc.CooldownCount;
                    state.LatencySampleCount = doc.LatencySampleCount;
                    state.TotalLatencyMs = doc.TotalLatencyMs;
                    state.AverageLatencyMs = doc.AverageLatencyMs;
                    state.WindowStartedAtUtc = doc.WindowStartedAtUtc;
                    state.RecentSuccessCount = doc.RecentSuccessCount;
                    state.RecentFailureCount = doc.RecentFailureCount;
                    state.RecentCooldownCount = doc.RecentCooldownCount;
                    state.RecentFailureRate = doc.RecentFailureRate;
                    state.WeightedSuccessRate = doc.WeightedSuccessRate;
                    state.CooldownFrequency = doc.CooldownFrequency;
                    state.DynamicScore = doc.DynamicScore;
                    state.TotalInputTokens = doc.TotalInputTokens;
                    state.TotalOutputTokens = doc.TotalOutputTokens;
                    state.TotalEstimatedCostUsd = doc.TotalEstimatedCostUsd;
                    state.StateVersion = doc.StateVersion;
                    state.IsActive = doc.IsActive;
                    state.LastUsedAtUtc = doc.UpdatedAtUtc;
                    state.Dirty = false;
                }
                var maxVersion = docs.Count == 0 ? 0 : docs.Max(x => x.StateVersion);
                Interlocked.Exchange(ref _stateVersion, Math.Max(GetStateVersion(), maxVersion));
                var active = docs.FirstOrDefault(x => x.IsActive)?.ModelName;
                if (!string.IsNullOrWhiteSpace(active))
                {
                    lock (_activeModelSync)
                    {
                        _activeModel = active;
                    }
                }
                _lastRefreshAtUtc = utcNow;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed refreshing model health state from Mongo.");
            }
        }
    }
    private void FlushIfDue(DateTimeOffset utcNow)
    {
        var persistSeconds = Math.Clamp(_options.CurrentValue.MetricsPersistIntervalSeconds, 1, 300);
        if (utcNow - _lastPersistAtUtc < TimeSpan.FromSeconds(persistSeconds))
        {
            return;
        }
        PersistDirtyStates(utcNow);
    }
    private MutableModelHealth GetState(string modelName)
    {
        return _health.GetOrAdd(modelName, x => new MutableModelHealth(x)
        {
            WindowStartedAtUtc = DateTimeOffset.UtcNow
        });
    }
    private void RotateWindowIfNeeded(MutableModelHealth state, DateTimeOffset utcNow)
    {
        var windowMinutes = Math.Clamp(_options.CurrentValue.MetricsRollingWindowMinutes, 1, 120);
        if (!state.WindowStartedAtUtc.HasValue)
        {
            state.WindowStartedAtUtc = utcNow;
            return;
        }
        if (utcNow - state.WindowStartedAtUtc.Value < TimeSpan.FromMinutes(windowMinutes))
        {
            return;
        }
        state.WindowStartedAtUtc = utcNow;
        state.RecentSuccessCount = 0;
        state.RecentFailureCount = 0;
        state.RecentCooldownCount = 0;
    }
    private void RecomputeState(MutableModelHealth state)
    {
        var total = Math.Max(1L, state.SuccessCount + state.FailureCount);
        var lifetimeSuccessRate = (double)state.SuccessCount / total;
        var recentTotal = Math.Max(1L, state.RecentSuccessCount + state.RecentFailureCount);
        var recentSuccessRate = (double)state.RecentSuccessCount / recentTotal;
        state.RecentFailureRate = (double)state.RecentFailureCount / recentTotal;
        state.CooldownFrequency = (double)state.RecentCooldownCount / recentTotal;
        state.WeightedSuccessRate = (lifetimeSuccessRate * 0.7d) + (recentSuccessRate * 0.3d);
        state.AverageLatencyMs = state.LatencySampleCount <= 0
            ? 0
            : state.TotalLatencyMs / state.LatencySampleCount;
        var settings = _options.CurrentValue;
        var normalizedLatency = state.AverageLatencyMs <= 0
            ? 0d
            : Math.Min(1d, state.AverageLatencyMs / Math.Max(50, settings.LatencyReferenceMs));
        var successComponent = state.WeightedSuccessRate * settings.SuccessRateWeight;
        var latencyPenalty = normalizedLatency * settings.LatencyWeight;
        var failurePenalty = state.RecentFailureRate * settings.FailureRateWeight;
        var cooldownPenalty = state.CooldownFrequency * settings.CooldownWeight;
        state.DynamicScore = successComponent - latencyPenalty - failurePenalty - cooldownPenalty;
    }
    private void TouchState(MutableModelHealth state, DateTimeOffset utcNow, bool immediatePersist)
    {
        state.StateVersion = Interlocked.Increment(ref _stateVersion);
        state.LastUpdatedAtUtc = utcNow;
        state.Dirty = true;
        if (immediatePersist)
        {
            PersistDirtyStates(utcNow);
            return;
        }
        FlushIfDue(utcNow);
    }
    private void PersistDirtyStates(DateTimeOffset utcNow)
    {
        if (_collection is null)
        {
            return;
        }
        lock (_persistSync)
        {
            var dirty = _health.Values.Where(x => x.Dirty).ToList();
            foreach (var state in dirty)
            {
                try
                {
                    var filter = Builders<ModelHealthStateDocument>.Filter.Eq(x => x.ModelName, state.ModelName);
                    var update = Builders<ModelHealthStateDocument>.Update
                        .Set(x => x.ModelName, state.ModelName)
                        .Set(x => x.LastSuccessAtUtc, state.LastSuccessAtUtc)
                        .Set(x => x.LastFailureAtUtc, state.LastFailureAtUtc)
                        .Set(x => x.LastFailureReason, state.LastFailureReason)
                        .Set(x => x.CircuitOpenUntilUtc, state.CircuitOpenUntilUtc)
                        .Set(x => x.ConsecutiveFailures, state.ConsecutiveFailures)
                        .Set(x => x.SuccessCount, state.SuccessCount)
                        .Set(x => x.FailureCount, state.FailureCount)
                        .Set(x => x.FallbackUsageCount, state.FallbackUsageCount)
                        .Set(x => x.CooldownCount, state.CooldownCount)
                        .Set(x => x.LatencySampleCount, state.LatencySampleCount)
                        .Set(x => x.TotalLatencyMs, state.TotalLatencyMs)
                        .Set(x => x.AverageLatencyMs, state.AverageLatencyMs)
                        .Set(x => x.WindowStartedAtUtc, state.WindowStartedAtUtc)
                        .Set(x => x.RecentSuccessCount, state.RecentSuccessCount)
                        .Set(x => x.RecentFailureCount, state.RecentFailureCount)
                        .Set(x => x.RecentCooldownCount, state.RecentCooldownCount)
                        .Set(x => x.RecentFailureRate, state.RecentFailureRate)
                        .Set(x => x.WeightedSuccessRate, state.WeightedSuccessRate)
                        .Set(x => x.CooldownFrequency, state.CooldownFrequency)
                        .Set(x => x.DynamicScore, state.DynamicScore)
                        .Set(x => x.TotalInputTokens, state.TotalInputTokens)
                        .Set(x => x.TotalOutputTokens, state.TotalOutputTokens)
                        .Set(x => x.TotalEstimatedCostUsd, state.TotalEstimatedCostUsd)
                        .Set(x => x.StateVersion, state.StateVersion)
                        .Set(x => x.IsActive, state.IsActive)
                        .Set(x => x.UpdatedAtUtc, utcNow);
                    _collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
                    state.Dirty = false;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed persisting model health state for {ModelName}.", state.ModelName);
                }
            }
            _lastPersistAtUtc = utcNow;
        }
    }
    private sealed class MutableModelHealth
    {
        public MutableModelHealth(string modelName)
        {
            ModelName = modelName;
        }
        public string ModelName { get; }
        public DateTimeOffset? LastSuccessAtUtc { get; set; }
        public DateTimeOffset? LastFailureAtUtc { get; set; }
        public string? LastFailureReason { get; set; }
        public DateTimeOffset? CircuitOpenUntilUtc { get; set; }
        public DateTimeOffset? LastUsedAtUtc { get; set; }
        public DateTimeOffset? LastUpdatedAtUtc { get; set; }
        public DateTimeOffset? WindowStartedAtUtc { get; set; }
        public int ConsecutiveFailures { get; set; }
        public long SuccessCount { get; set; }
        public long FailureCount { get; set; }
        public long FallbackUsageCount { get; set; }
        public long CooldownCount { get; set; }
        public long LatencySampleCount { get; set; }
        public double TotalLatencyMs { get; set; }
        public double AverageLatencyMs { get; set; }
        public long RecentSuccessCount { get; set; }
        public long RecentFailureCount { get; set; }
        public long RecentCooldownCount { get; set; }
        public double RecentFailureRate { get; set; }
        public double WeightedSuccessRate { get; set; }
        public double CooldownFrequency { get; set; }
        public double DynamicScore { get; set; }
        public long TotalInputTokens { get; set; }
        public long TotalOutputTokens { get; set; }
        public decimal TotalEstimatedCostUsd { get; set; }
        public long StateVersion { get; set; }
        public bool IsActive { get; set; }
        public bool Dirty { get; set; }
    }
}