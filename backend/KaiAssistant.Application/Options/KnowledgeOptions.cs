using System.ComponentModel.DataAnnotations;

namespace KaiAssistant.Application.Options;

public sealed class KnowledgeOptions
{
    public const string SectionName = "Knowledge";
    public bool SemanticIndexingEnabled { get; set; } = true;
    [Range(100, 4000)] public int ChunkSize { get; set; } = 1000;
    [Range(0, 1000)] public int ChunkOverlap { get; set; } = 150;
}
