namespace KaiAssistant.Infrastructure.FeatureFlags;

public sealed class FeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";
    public bool EnableOutboxProcessing { get; set; } = true;
    public bool EnableOutboxRecovery { get; set; } = false;
    public bool EnableAiResponseCache { get; set; } = false;
    public bool EnableAssistantBatching { get; set; } = false;
    public bool EnableCache { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableStreaming { get; set; } = true;
    public bool EnableSemanticCaching { get; set; } = true;
    public bool EnableRag { get; set; } = true;
    public bool EnableConversationMemory { get; set; } = true;
    public string PreferredAiProvider { get; set; } = "gemini";
}