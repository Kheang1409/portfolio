using KaiAssistant.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
namespace KaiAssistant.Infrastructure.FeatureFlags;

public sealed class ConfigurationFeatureFlagService : IFeatureFlagService
{
    private readonly IOptionsMonitor<FeatureFlagsOptions> _options;
    private readonly ILogger<ConfigurationFeatureFlagService> _logger;
    public ConfigurationFeatureFlagService(IOptionsMonitor<FeatureFlagsOptions> options, ILogger<ConfigurationFeatureFlagService> logger)
    {
        _options = options;
        _logger = logger;
        _options.OnChange(flags =>
        {
            _logger.LogInformation(
                "AuditFeatureFlagsChanged: outbox={Outbox} outboxRecovery={OutboxRecovery} aiCache={AiCache} assistantBatching={AssistantBatching} cache={Cache} rateLimit={RateLimit} streaming={Streaming}",
                flags.EnableOutboxProcessing,
                flags.EnableOutboxRecovery,
                flags.EnableAiResponseCache,
                flags.EnableAssistantBatching,
                flags.EnableCache,
                flags.EnableRateLimiting,
                flags.EnableStreaming);
        });
    }
    public bool EnableOutboxProcessing => _options.CurrentValue.EnableOutboxProcessing;
    public bool EnableOutboxRecovery => _options.CurrentValue.EnableOutboxRecovery;
    public bool EnableAiResponseCache => _options.CurrentValue.EnableAiResponseCache;
    public bool EnableAssistantBatching => _options.CurrentValue.EnableAssistantBatching;
    public bool EnableCache => _options.CurrentValue.EnableCache;
    public bool EnableRateLimiting => _options.CurrentValue.EnableRateLimiting;
    public bool EnableStreaming => _options.CurrentValue.EnableStreaming;
    public bool EnableSemanticCaching => _options.CurrentValue.EnableSemanticCaching;
    public bool EnableRag => _options.CurrentValue.EnableRag;
    public bool EnableConversationMemory => _options.CurrentValue.EnableConversationMemory;
    public string PreferredAiProvider => _options.CurrentValue.PreferredAiProvider;
    public Task<bool> SetFlagAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Configuration-backed feature flag service cannot persist runtime changes. Flag={Flag} Value={Value}", name, value);
        return Task.FromResult(false);
    }
    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(EnableOutboxProcessing)] = EnableOutboxProcessing.ToString(),
            [nameof(EnableOutboxRecovery)] = EnableOutboxRecovery.ToString(),
            [nameof(EnableAiResponseCache)] = EnableAiResponseCache.ToString(),
            [nameof(EnableAssistantBatching)] = EnableAssistantBatching.ToString(),
            [nameof(EnableCache)] = EnableCache.ToString(),
            [nameof(EnableRateLimiting)] = EnableRateLimiting.ToString(),
            [nameof(EnableStreaming)] = EnableStreaming.ToString(),
            [nameof(EnableSemanticCaching)] = EnableSemanticCaching.ToString(),
            [nameof(EnableRag)] = EnableRag.ToString(),
            [nameof(EnableConversationMemory)] = EnableConversationMemory.ToString(),
            [nameof(PreferredAiProvider)] = PreferredAiProvider
        };
        return Task.FromResult<IReadOnlyDictionary<string, string>>(flags);
    }
}