namespace KaiAssistant.Application.Rag;

public sealed record VectorRecord(
    string Id,
    string DocumentId,
    string Content,
    float[] Embedding,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record VectorSearchRequest(
    float[] Embedding,
    int TopK,
    double MinimumScore,
    IReadOnlyDictionary<string, string>? MetadataFilter = null);

public sealed record VectorSearchResult(VectorRecord Record, double Score);

public sealed record RetrievedChunk(
    string ChunkId,
    string DocumentId,
    string Content,
    double SemanticScore,
    double KeywordScore,
    double FusedScore,
    int Rank,
    IReadOnlyDictionary<string, string> Metadata,
    double RerankScore = 0);

public enum RetrievalStrategy { Keyword, Semantic, Hybrid, HybridReranked }

public sealed record RetrievalDiagnostics(
    string Query,
    RetrievalStrategy Strategy,
    IReadOnlyList<RetrievedChunk> Results,
    TimeSpan SemanticDuration,
    TimeSpan KeywordDuration,
    TimeSpan FusionDuration,
    TimeSpan RerankDuration,
    string PipelineVersion);
