namespace KaiAssistant.Application.Interfaces;

public interface IFeatureFlagService
{
    bool EnableOutboxProcessing { get; }
    bool EnableOutboxRecovery { get; }
    bool EnableAiResponseCache { get; }
    bool EnableAssistantBatching { get; }
    bool EnableCache { get; }
    bool EnableRateLimiting { get; }
    bool EnableStreaming { get; }
    bool EnableSemanticCaching { get; }
    bool EnableRag { get; }
    bool EnableConversationMemory { get; }
    string PreferredAiProvider { get; }
    Task<bool> SetFlagAsync(string name, string value, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
}