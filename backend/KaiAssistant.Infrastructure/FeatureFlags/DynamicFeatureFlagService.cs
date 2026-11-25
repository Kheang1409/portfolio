using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.FeatureFlags.Documents;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.FeatureFlags;

public sealed class DynamicFeatureFlagService : IFeatureFlagService
{
    private const string CacheKey = "feature-flags:dynamic";
    private readonly IMongoCollection<FeatureFlagDocument> _collection;
    private readonly IOptionsMonitor<FeatureFlagsOptions> _defaults;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DynamicFeatureFlagService> _logger;
    public DynamicFeatureFlagService(
        IMongoDatabase database,
        IOptionsMonitor<FeatureFlagsOptions> defaults,
        IMemoryCache cache,
        ILogger<DynamicFeatureFlagService> logger)
    {
        _collection = database.GetCollection<FeatureFlagDocument>("feature_flags");
        _defaults = defaults;
        _cache = cache;
        _logger = logger;
    }
    public bool EnableOutboxProcessing => GetBool(nameof(EnableOutboxProcessing), _defaults.CurrentValue.EnableOutboxProcessing);
    public bool EnableOutboxRecovery => GetBool(nameof(EnableOutboxRecovery), _defaults.CurrentValue.EnableOutboxRecovery);
    public bool EnableAiResponseCache => GetBool(nameof(EnableAiResponseCache), _defaults.CurrentValue.EnableAiResponseCache);
    public bool EnableAssistantBatching => GetBool(nameof(EnableAssistantBatching), _defaults.CurrentValue.EnableAssistantBatching);
    public bool EnableCache => GetBool(nameof(EnableCache), _defaults.CurrentValue.EnableCache);
    public bool EnableRateLimiting => GetBool(nameof(EnableRateLimiting), _defaults.CurrentValue.EnableRateLimiting);
    public bool EnableStreaming => GetBool(nameof(EnableStreaming), _defaults.CurrentValue.EnableStreaming);
    public bool EnableSemanticCaching => GetBool(nameof(EnableSemanticCaching), _defaults.CurrentValue.EnableSemanticCaching);
    public bool EnableRag => GetBool(nameof(EnableRag), _defaults.CurrentValue.EnableRag);
    public bool EnableConversationMemory => GetBool(nameof(EnableConversationMemory), _defaults.CurrentValue.EnableConversationMemory);
    public string PreferredAiProvider
    {
        get
        {
            var values = GetValues();
            if (values.TryGetValue(nameof(PreferredAiProvider), out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return _defaults.CurrentValue.PreferredAiProvider;
        }
    }
    public async Task<bool> SetFlagAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        var filter = Builders<FeatureFlagDocument>.Filter.Eq(x => x.Name, name);
        var update = Builders<FeatureFlagDocument>.Update
            .Set(x => x.Name, name)
            .Set(x => x.Value, value)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
        await _collection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken).ConfigureAwait(false);
        _cache.Remove(CacheKey);
        _logger.LogInformation("Feature flag updated at runtime. Flag={Flag} Value={Value}", name, value);
        return true;
    }
    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(EnableOutboxProcessing)] = _defaults.CurrentValue.EnableOutboxProcessing.ToString(),
            [nameof(EnableOutboxRecovery)] = _defaults.CurrentValue.EnableOutboxRecovery.ToString(),
            [nameof(EnableAiResponseCache)] = _defaults.CurrentValue.EnableAiResponseCache.ToString(),
            [nameof(EnableAssistantBatching)] = _defaults.CurrentValue.EnableAssistantBatching.ToString(),
            [nameof(EnableCache)] = _defaults.CurrentValue.EnableCache.ToString(),
            [nameof(EnableRateLimiting)] = _defaults.CurrentValue.EnableRateLimiting.ToString(),
            [nameof(EnableStreaming)] = _defaults.CurrentValue.EnableStreaming.ToString(),
            [nameof(EnableSemanticCaching)] = _defaults.CurrentValue.EnableSemanticCaching.ToString(),
            [nameof(EnableRag)] = _defaults.CurrentValue.EnableRag.ToString(),
            [nameof(EnableConversationMemory)] = _defaults.CurrentValue.EnableConversationMemory.ToString(),
            [nameof(PreferredAiProvider)] = _defaults.CurrentValue.PreferredAiProvider
        };
        var overrides = await _collection
            .Find(Builders<FeatureFlagDocument>.Filter.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var item in overrides)
        {
            defaults[item.Name] = item.Value;
        }
        _cache.Set(CacheKey, defaults, TimeSpan.FromSeconds(15));
        return defaults;
    }
    private bool GetBool(string key, bool fallback)
    {
        var values = GetValues();
        if (values.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed))
        {
            return parsed;
        }
        return fallback;
    }
    private IReadOnlyDictionary<string, string> GetValues()
    {
        if (_cache.TryGetValue<IReadOnlyDictionary<string, string>>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }
        try
        {
            var items = _collection
                .Find(Builders<FeatureFlagDocument>.Filter.Empty)
                .Limit(200)
                .ToList();
            var map = items.ToDictionary(x => x.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);
            _cache.Set(CacheKey, map, TimeSpan.FromSeconds(15));
            return map;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load dynamic feature flags from MongoDB. Falling back to configuration defaults.");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}