using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";
    public bool Enabled { get; set; } = true;
    [Range(1, 20)]
    public int TopK { get; set; } = 5;
    [Range(0.1, 1.0)]
    public double MinSimilarity { get; set; } = 0.7;
    [Range(200, 12000)]
    public int MaxContextChars { get; set; } = 4000;
    [Range(1, 100)]
    public int CandidateCount { get; set; } = 20;
    [Range(1, 20)]
    public int RerankedTopK { get; set; } = 6;
    [Range(1, 200)] public int SemanticCandidateCount { get; set; } = 20;
    [Range(1, 200)] public int KeywordCandidateCount { get; set; } = 20;
    [Range(1, 200)] public int FusionCandidateCount { get; set; } = 40;
    [Range(1, 1000)] public int RrfK { get; set; } = 60;
    [Range(1, 20)] public int ContextChunkCount { get; set; } = 6;
    public bool EnableReranking { get; set; } = false;
    [Range(100, 12000)] public int MaxContextEstimatedTokens { get; set; } = 1000;
    [Range(0, 1)] public double MinimumEvidenceScore { get; set; } = 0.01;
    [Range(0.5, 1)] public double NearDuplicateThreshold { get; set; } = 0.85;
    [Required] public string PipelineVersion { get; set; } = "rag-v1";
    [Required] public string IndexVersion { get; set; } = "knowledge-gemini-001-v2";
}
