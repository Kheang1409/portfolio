using System.ComponentModel.DataAnnotations;

namespace KaiAssistant.Application.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    [Required] public string Provider { get; set; } = "gemini";
    [Required] public string Model { get; set; } = "gemini-embedding-001";
    [Range(128, 3072)] public int Dimensions { get; set; } = 768;
    [Range(1, 100)] public int BatchSize { get; set; } = 20;
    [Range(1, 8)] public int MaxConcurrency { get; set; } = 2;
    [Range(1, 120)] public int RequestTimeoutSeconds { get; set; } = 20;
    [Range(0, 6)] public int MaxRetries { get; set; } = 3;
    [Range(128, 50000)] public int MaxInputChars { get; set; } = 8000;
    public bool CacheEnabled { get; set; } = true;
    [Range(60, 86400)] public int QueryCacheTtlSeconds { get; set; } = 900;
    [Range(60, 604800)] public int DocumentCacheTtlSeconds { get; set; } = 86400;
    [Required] public string Version { get; set; } = "gemini-001-768-v1";
    [Required] public string IndexVersion { get; set; } = "knowledge-gemini-001-v2";
}
