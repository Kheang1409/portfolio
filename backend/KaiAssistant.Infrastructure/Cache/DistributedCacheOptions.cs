namespace KaiAssistant.Infrastructure.Cache;

public sealed class DistributedCacheOptions
{
    public const string SectionName = "DistributedCache";
    public string KeyPrefix { get; set; } = "kaiassistant";
    public string Namespace { get; set; } = "default";
    public int DefaultTtlSeconds { get; set; } = 120;
    public int TtlJitterSeconds { get; set; } = 15;
    public int RebuildLockTimeoutMs { get; set; } = 5000;
}