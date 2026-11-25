using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using KaiAssistant.Application.Rag;
using KaiAssistant.Infrastructure.AI.Rag;

namespace KaiAssistant.RagEvaluation;

public static class OfflineEvaluationEngine
{
    public static readonly (string Id, string Text)[] Corpus =
    [
        ("project:kaiassistant-api", "The KaiAssistant API orchestrates multiple AI providers with timeout control, fallback, RAG, semantic cache, and conversation memory."),
        ("project:outbox-reliability", "The outbox processor claims pending events with leases, ensures idempotency with Redis, retries with backoff, and moves exhausted events to dead-letters."),
        ("project:observability", "Observability provides structured logs, traces, and dashboard-ready metrics for request latency, fallback rates, and streaming outputs."),
        ("architecture:clean-architecture", "The domain layer remains isolated, the application layer defines abstractions, infrastructure implements integrations, and the API is a thin transport layer."),
        ("scalability:runtime", "The runtime uses MongoDB for durable state and retrieval, Redis for distributed control paths, and Lambda for horizontal elasticity.")
    ];
    private static readonly HashSet<string> StopWords = new(["the", "and", "for", "with", "what", "which", "does", "has", "have", "how", "kai", "used", "use", "that", "this", "from", "about", "into", "was", "were", "are", "its"], StringComparer.Ordinal);
    private static readonly Dictionary<string, string[]> Expansions = new(StringComparer.Ordinal) { ["graphed"] = ["dashboard", "metrics"], ["signals"] = ["metrics"], ["long-lived"] = ["durable"], ["information"] = ["state"], ["database"] = ["mongodb"] };

    public static CaseMetrics Run(RagEvaluationCase item, RetrievalStrategy strategy)
    {
        var watch = Stopwatch.StartNew();
        var query = string.IsNullOrWhiteSpace(item.ConversationContext) ? item.Question : $"{item.ConversationContext} {item.Question}";
        var keyword = Keyword(query);
        var semantic = Semantic(query);
        IReadOnlyList<RetrievedChunk> results = strategy switch
        {
            RetrievalStrategy.Keyword => keyword,
            RetrievalStrategy.Semantic => semantic.Select((x, i) => new RetrievedChunk(x.Record.Id, x.Record.DocumentId, x.Record.Content, x.Score, 0, x.Score, i + 1, x.Record.Metadata)).ToArray(),
            _ => ReciprocalRankFusion.Fuse(semantic, keyword, 60, 10)
        };
        if (strategy == RetrievalStrategy.HybridReranked) results = results.Take(10).ToArray();
        watch.Stop();
        var ranked = results.Select(x => x.DocumentId).ToArray();
        return new(item.Id, item.Category, item.ShouldHaveAnswer,
            RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 1), RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 3),
            RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 5), RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 10),
            RetrievalMetrics.PrecisionAtK(item.ExpectedDocumentIds, ranked, 3), RetrievalMetrics.PrecisionAtK(item.ExpectedDocumentIds, ranked, 5),
            RetrievalMetrics.PrecisionAtK(item.ExpectedDocumentIds, ranked, 10), RetrievalMetrics.ReciprocalRank(item.ExpectedDocumentIds, ranked), watch.Elapsed.TotalMilliseconds, results);
    }

    private static RetrievedChunk[] Keyword(string query)
    {
        var terms = Terms(query);
        return Corpus.Select(x => (x, score: terms.Count(t => x.Text.Contains(t, StringComparison.OrdinalIgnoreCase) || x.Id.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.score > 0).OrderByDescending(x => x.score).ThenBy(x => x.x.Id, StringComparer.Ordinal)
            .Select((x, i) => Chunk(x.x, keyword: x.score, fused: 0, rank: i + 1)).ToArray();
    }
    private static VectorSearchResult[] Semantic(string query)
    {
        var embedding = Embed(query);
        return Corpus.Select(x => new VectorSearchResult(new VectorRecord(x.Id + ":0", x.Id, x.Text, Embed(x.Text), Metadata(x.Id)), Cosine(embedding, Embed(x.Text))))
            .Where(x => x.Score >= .7).OrderByDescending(x => x.Score).ThenBy(x => x.Record.Id, StringComparer.Ordinal).Take(20).ToArray();
    }
    private static RetrievedChunk Chunk((string Id, string Text) x, double keyword, double fused, int rank) =>
        new(x.Id + ":0", x.Id, x.Text, 0, keyword, fused, rank, Metadata(x.Id));
    private static Dictionary<string, string> Metadata(string id) => new() { ["source"] = id, ["title"] = id, ["version"] = "1", ["indexVersion"] = "evaluation-v1" };
    private static string[] Terms(string query)
    {
        var terms = query.Split([' ', '\t', '\r', '\n', ',', '.', '?', '!', ':', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.ToLowerInvariant()).Where(x => x.Length > 2 && !StopWords.Contains(x)).Distinct().ToList();
        terms.AddRange(terms.Where(Expansions.ContainsKey).SelectMany(x => Expansions[x]).ToArray());
        return terms.Distinct().ToArray();
    }
    private static float[] Embed(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim().ToLowerInvariant()));
        var vector = new float[64]; double sum = 0;
        for (var i = 0; i < vector.Length; i++) { vector[i] = (hash[i % hash.Length] / 255f) * 2f - 1f; sum += vector[i] * vector[i]; }
        var length = (float)Math.Sqrt(sum); if (length > 0) for (var i = 0; i < vector.Length; i++) vector[i] /= length;
        return vector;
    }
    private static double Cosine(float[] left, float[] right)
    {
        double dot = 0, a = 0, b = 0;
        for (var i = 0; i < Math.Min(left.Length, right.Length); i++) { dot += left[i] * right[i]; a += left[i] * left[i]; b += right[i] * right[i]; }
        return a == 0 || b == 0 ? -1 : dot / (Math.Sqrt(a) * Math.Sqrt(b));
    }
}
