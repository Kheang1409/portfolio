using System.Text.Json;
using KaiAssistant.Application.Rag;

namespace KaiAssistant.RagEvaluation;

public sealed record RagEvaluationCase(string Id, string Category, string Question, string? ConversationContext,
    string[] ExpectedDocumentIds, string[]? ExpectedChunkIds, string[] ExpectedTerms, string[] ExpectedAnswerFacts,
    bool ShouldHaveAnswer, string Split, string? Notes);

public sealed record CaseMetrics(string CaseId, string Category, bool ShouldHaveAnswer, double Recall1, double Recall3,
    double Recall5, double Recall10, double Precision3, double Precision5, double Precision10, double ReciprocalRank,
    double LatencyMs, IReadOnlyList<RetrievedChunk> Results);

public sealed record EvaluationSummary(string Strategy, int CaseCount, double Recall1, double Recall3, double Recall5,
    double Recall10, double Precision3, double Precision5, double Precision10, double Mrr, double MeanLatencyMs,
    double P50LatencyMs, double P95LatencyMs, IReadOnlyDictionary<string, CategoryMetrics> Categories,
    IReadOnlyList<string> FailedCaseIds);

public sealed record CategoryMetrics(double Recall5, double Precision5, double Mrr);

public static class RagEvaluationDataset
{
    public static IReadOnlyList<RagEvaluationCase> Load() => JsonSerializer.Deserialize<RagEvaluationCase[]>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "rag-evaluation-cases.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
}
public static class SemanticChallengeDataset
{
    public static IReadOnlyList<RagEvaluationCase> Load() => JsonSerializer.Deserialize<RagEvaluationCase[]>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "semantic-challenge-cases.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
}

public static class RetrievalMetrics
{
    public static double RecallAtK(IReadOnlyCollection<string> relevant, IReadOnlyList<string> ranked, int k) =>
        relevant.Count == 0 ? (ranked.Take(k).Any() ? 0 : 1) : relevant.Intersect(ranked.Take(k), StringComparer.Ordinal).Count() / (double)relevant.Count;
    public static double PrecisionAtK(IReadOnlyCollection<string> relevant, IReadOnlyList<string> ranked, int k) =>
        ranked.Count == 0 ? 0 : relevant.Intersect(ranked.Take(k), StringComparer.Ordinal).Count() / (double)Math.Min(k, ranked.Count);
    public static double ReciprocalRank(IReadOnlyCollection<string> relevant, IReadOnlyList<string> ranked)
    {
        for (var i = 0; i < ranked.Count; i++) if (relevant.Contains(ranked[i], StringComparer.Ordinal)) return 1d / (i + 1);
        return 0;
    }
    public static EvaluationSummary Summarize(string strategy, IReadOnlyList<CaseMetrics> cases)
    {
        static double Avg(IEnumerable<double> x) => x.DefaultIfEmpty().Average();
        var positive = cases.Where(x => x.ShouldHaveAnswer).ToArray();
        var latency = cases.Select(x => x.LatencyMs).OrderBy(x => x).ToArray();
        double Percentile(double p) => latency.Length == 0 ? 0 : latency[(int)Math.Ceiling(p * latency.Length) - 1];
        var categories = cases.GroupBy(x => x.Category).ToDictionary(g => g.Key,
            g => new CategoryMetrics(Avg(g.Select(x => x.Recall5)), Avg(g.Select(x => x.Precision5)), Avg(g.Select(x => x.ReciprocalRank))));
        return new(strategy, cases.Count, Avg(positive.Select(x => x.Recall1)), Avg(positive.Select(x => x.Recall3)),
            Avg(positive.Select(x => x.Recall5)), Avg(positive.Select(x => x.Recall10)), Avg(positive.Select(x => x.Precision3)),
            Avg(positive.Select(x => x.Precision5)), Avg(positive.Select(x => x.Precision10)), Avg(positive.Select(x => x.ReciprocalRank)),
            Avg(latency), Percentile(.5), Percentile(.95), categories, cases.Where(x => x.ShouldHaveAnswer && x.Recall5 == 0).Select(x => x.CaseId).ToArray());
    }
}
