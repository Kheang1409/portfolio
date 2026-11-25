using System.Diagnostics;
using System.Diagnostics.Metrics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Application.Rag;
using KaiAssistant.Domain.Entities.AI;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KaiAssistant.Infrastructure.AI.Rag;

internal static class QueryTerms
{
    private static readonly HashSet<string> StopWords = new(["the", "and", "for", "with", "what", "which", "does", "has", "have", "how", "kai", "used", "use", "that", "this", "from", "about", "into", "was", "were", "are", "its"], StringComparer.Ordinal);
    private static readonly Dictionary<string, string[]> Expansions = new(StringComparer.Ordinal)
    {
        ["graphed"] = ["dashboard", "metrics"],
        ["signals"] = ["metrics"],
        ["long-lived"] = ["durable"],
        ["information"] = ["state"],
        ["database"] = ["mongodb"]
    };
    public static string[] Parse(string query)
    {
        var terms = query
        .Split([' ', '\t', '\r', '\n', ',', '.', '?', '!', ':', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.ToLowerInvariant()).Where(x => x.Length > 2 && !StopWords.Contains(x))
        .Distinct(StringComparer.Ordinal).Take(16).ToList();
        terms.AddRange(terms.Where(Expansions.ContainsKey).SelectMany(x => Expansions[x]).ToArray());
        return terms.Distinct(StringComparer.Ordinal).Take(20).ToArray();
    }
}

public sealed class MongoKeywordSearch(IMongoDatabase database, IOptionsMonitor<RagOptions> ragOptions) : IKeywordSearch
{
    private readonly IMongoCollection<KnowledgeChunk> _chunks = database.GetCollection<KnowledgeChunk>("knowledge_chunks");
    private readonly IMongoCollection<KnowledgeDocument> _documents = database.GetCollection<KnowledgeDocument>("knowledge_base_documents");

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        var terms = QueryTerms.Parse(query);
        if (terms.Length == 0) return [];
        var activeIds = await _documents.Find(x => x.Status == "Completed" || x.Status == "Indexed")
            .Project(x => x.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (activeIds.Count == 0) return [];
        var patterns = terms.Select(term => new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(term), "i")).ToArray();
        var content = Builders<KnowledgeChunk>.Filter.Or(patterns.SelectMany(pattern => new[] {
            Builders<KnowledgeChunk>.Filter.Regex(x => x.Content, pattern), Builders<KnowledgeChunk>.Filter.Regex(x => x.Title, pattern), Builders<KnowledgeChunk>.Filter.Regex(x => x.Source, pattern) }));
        var filter = Builders<KnowledgeChunk>.Filter.In(x => x.DocumentId, activeIds!) &
            Builders<KnowledgeChunk>.Filter.Eq(x => x.IndexVersion, ragOptions.CurrentValue.IndexVersion) & content;
        var chunks = await _chunks.Find(filter).Limit(500).ToListAsync(cancellationToken).ConfigureAwait(false);
        return chunks.Select(chunk => new RetrievedChunk(chunk.Id ?? string.Empty, chunk.DocumentId, chunk.Content, 0,
                terms.Count(term => chunk.Content.Contains(term, StringComparison.OrdinalIgnoreCase) || chunk.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || chunk.Source.Contains(term, StringComparison.OrdinalIgnoreCase)), 0, 0, chunk.Metadata))
            .OrderByDescending(x => x.KeywordScore).ThenBy(x => x.ChunkId, StringComparer.Ordinal)
            .Take(topK).Select((x, i) => x with { Rank = i + 1 }).ToArray();
    }
}

public static class ReciprocalRankFusion
{
    public static IReadOnlyList<RetrievedChunk> Fuse(IReadOnlyList<VectorSearchResult> semantic,
        IReadOnlyList<RetrievedChunk> keyword, int rrfK, int topK)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rrfK, 1);
        var vectors = semantic.GroupBy(x => x.Record.Id, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(x => x.Score).First()).ToArray();
        var keywords = keyword.GroupBy(x => x.ChunkId, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(x => x.KeywordScore).First()).ToArray();
        var ids = vectors.Select(x => x.Record.Id).Concat(keywords.Select(x => x.ChunkId)).Distinct(StringComparer.Ordinal);
        var results = new List<RetrievedChunk>();
        foreach (var id in ids)
        {
            var vi = Array.FindIndex(vectors, x => x.Record.Id == id);
            var ki = Array.FindIndex(keywords, x => x.ChunkId == id);
            var vector = vi >= 0 ? vectors[vi] : null;
            var term = ki >= 0 ? keywords[ki] : null;
            var record = vector?.Record;
            var fused = (vi >= 0 ? 1d / (rrfK + vi + 1) : 0) + (ki >= 0 ? 1d / (rrfK + ki + 1) : 0);
            results.Add(new RetrievedChunk(id, record?.DocumentId ?? term!.DocumentId, record?.Content ?? term!.Content,
                vector?.Score ?? 0, term?.KeywordScore ?? 0, fused, 0, record?.Metadata ?? term!.Metadata));
        }
        return results.OrderByDescending(x => x.FusedScore).ThenBy(x => x.ChunkId, StringComparer.Ordinal)
            .Take(topK).Select((x, i) => x with { Rank = i + 1 }).ToArray();
    }
}

public sealed class HybridRetriever(IEmbeddingService embeddings, IVectorStore vectors, IKeywordSearch keywords,
    IOptionsMonitor<EmbeddingOptions> embeddingOptions, IOptionsMonitor<RagOptions> options) : IHybridRetriever
{
    private static readonly Meter Meter = new("KaiAssistant.Rag", "1.0.0");
    private static readonly Histogram<double> SemanticDuration = Meter.CreateHistogram<double>("semantic_search_duration", "ms");
    private static readonly Histogram<double> KeywordDuration = Meter.CreateHistogram<double>("keyword_search_duration", "ms");
    private static readonly Histogram<double> FusionDuration = Meter.CreateHistogram<double>("fusion_duration", "ms");
    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken = default)
    {
        var keyword = await DiagnoseAsync(query, RetrievalStrategy.Keyword, cancellationToken).ConfigureAwait(false);
        // Exact portfolio terminology normally produces multiple direct matches. Avoiding
        // a query embedding on this path keeps the common request fast and inexpensive.
        if (keyword.Results.Count >= 2 && keyword.Results[0].KeywordScore >= 2)
        {
            return keyword.Results;
        }

        try
        {
            var semantic = await DiagnoseAsync(query, RetrievalStrategy.Semantic, cancellationToken).ConfigureAwait(false);
            return ReciprocalRankFusion.Fuse(
                semantic.Results.Select(x => new VectorSearchResult(
                    new VectorRecord(x.ChunkId, x.DocumentId, x.Content, Array.Empty<float>(), x.Metadata), x.SemanticScore)).ToArray(),
                keyword.Results, options.CurrentValue.RrfK, options.CurrentValue.FusionCandidateCount);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return keyword.Results;
        }
    }

    public async Task<RetrievalDiagnostics> DiagnoseAsync(string query, RetrievalStrategy strategy, CancellationToken cancellationToken = default)
    {
        var config = options.CurrentValue;
        IReadOnlyList<VectorSearchResult> semantic = [];
        IReadOnlyList<RetrievedChunk> keyword = [];
        var semanticDuration = TimeSpan.Zero;
        var keywordDuration = TimeSpan.Zero;
        if (strategy is not RetrievalStrategy.Keyword)
        {
            var provider = embeddingOptions.CurrentValue;
            var activeVersion = options.CurrentValue.IndexVersion;
            if (string.Equals("gemini", provider.Provider, StringComparison.OrdinalIgnoreCase) && provider.Dimensions == 768)
            {
                var watch = Stopwatch.StartNew();
                try
                {
                    var embedding = await embeddings.GenerateEmbeddingAsync(query, EmbeddingPurpose.RetrievalQuery, cancellationToken: cancellationToken).ConfigureAwait(false);
                    semantic = await vectors.SearchAsync(new VectorSearchRequest(embedding, config.SemanticCandidateCount, config.MinSimilarity,
                        new Dictionary<string, string> { ["indexVersion"] = activeVersion, ["embeddingPipelineVersion"] = provider.Version }), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested && strategy is not RetrievalStrategy.Semantic) { semantic = []; }
                semanticDuration = watch.Elapsed; SemanticDuration.Record(semanticDuration.TotalMilliseconds);
            }
        }
        if (strategy is not RetrievalStrategy.Semantic)
        {
            var watch = Stopwatch.StartNew();
            keyword = await keywords.SearchAsync(query, config.KeywordCandidateCount, cancellationToken).ConfigureAwait(false);
            keywordDuration = watch.Elapsed;
            KeywordDuration.Record(keywordDuration.TotalMilliseconds);
        }
        if (strategy == RetrievalStrategy.Keyword)
            return new(query, strategy, keyword.Take(config.FusionCandidateCount).ToArray(), semanticDuration, keywordDuration, TimeSpan.Zero, TimeSpan.Zero, config.PipelineVersion);
        if (strategy == RetrievalStrategy.Semantic)
        {
            var mapped = semantic.Select((x, i) => new RetrievedChunk(x.Record.Id, x.Record.DocumentId, x.Record.Content,
                x.Score, 0, x.Score, i + 1, x.Record.Metadata)).ToArray();
            return new(query, strategy, mapped, semanticDuration, keywordDuration, TimeSpan.Zero, TimeSpan.Zero, config.PipelineVersion);
        }
        var watchFusion = Stopwatch.StartNew();
        var fused = ReciprocalRankFusion.Fuse(semantic, keyword, config.RrfK, config.FusionCandidateCount);
        FusionDuration.Record(watchFusion.Elapsed.TotalMilliseconds);
        return new(query, strategy, fused, semanticDuration, keywordDuration, watchFusion.Elapsed, TimeSpan.Zero, config.PipelineVersion);
    }
}
