using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Infrastructure.AI.Embeddings;
using KaiAssistant.Infrastructure.AI.Rag;
using KaiAssistant.Domain.Entities.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace KaiAssistant.RagEvaluation;

public sealed class LiveGeminiEmbeddingEvaluationTests(ITestOutputHelper output)
{
    [Fact, Trait("Category", "LiveEmbedding")]
    public async Task Gemini_semantic_challenge_report_is_opt_in()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_EMBEDDING_EVAL"), "1", StringComparison.Ordinal))
        { output.WriteLine("Live Gemini embedding evaluation not requested. Set RUN_LIVE_EMBEDDING_EVAL=1 and GEMINI_API_KEY to opt in."); return; }
        var options = new EmbeddingOptions();
        using var service = new GeminiEmbeddingService(new SimpleHttpClientFactory(), new FixedMonitor<EmbeddingOptions>(options),
            new MemoryCache(new MemoryCacheOptions()), new ConfigurationBuilder().Build());
        var smokeDocument = await service.GenerateEmbeddingAsync("ASP.NET Core backend development", EmbeddingPurpose.RetrievalDocument, "Backend skills");
        var smokeQuery = await service.GenerateEmbeddingAsync("building server-side applications using Microsoft's .NET web framework", EmbeddingPurpose.RetrievalQuery);
        var unrelated = await service.GenerateEmbeddingAsync("gardening and landscaping", EmbeddingPurpose.RetrievalDocument, "Unrelated topic");
        Assert.Equal(options.Dimensions, smokeDocument.Length);
        Assert.Equal(options.Dimensions, smokeQuery.Length);
        Assert.All(smokeDocument.Concat(smokeQuery).Concat(unrelated), value => Assert.True(float.IsFinite(value)));
        var documentNorm = Math.Sqrt(smokeDocument.Sum(value => value * value));
        var queryNorm = Math.Sqrt(smokeQuery.Sum(value => value * value));
        Assert.InRange(documentNorm, 0.999, 1.001);
        Assert.InRange(queryNorm, 0.999, 1.001);
        Assert.True(Cosine(smokeQuery, smokeDocument) > Cosine(smokeQuery, unrelated));
        output.WriteLine("model={0} dimensions={1} document-norm={2:F6} query-norm={3:F6} related={4:F4} unrelated={5:F4}",
            options.Model, options.Dimensions, documentNorm, queryNorm, Cosine(smokeQuery, smokeDocument), Cosine(smokeQuery, unrelated));
        var documents = await service.GenerateEmbeddingsAsync(OfflineEvaluationEngine.Corpus.Select(x => x.Text).ToArray(), EmbeddingPurpose.RetrievalDocument,
            OfflineEvaluationEngine.Corpus.Select(x => (string?)x.Id).ToArray());
        var metrics = new List<CaseMetrics>();
        foreach (var item in SemanticChallengeDataset.Load())
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            var query = await service.GenerateEmbeddingAsync(item.Question, EmbeddingPurpose.RetrievalQuery);
            var ranked = documents.Select((vector, i) => (OfflineEvaluationEngine.Corpus[i].Id, Score: Cosine(query, vector)))
                .OrderByDescending(x => x.Score).Select(x => x.Id).ToArray();
            metrics.Add(new(item.Id, item.Category, true,
                RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 1), RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 3),
                RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 5), RetrievalMetrics.RecallAtK(item.ExpectedDocumentIds, ranked, 10),
                RetrievalMetrics.PrecisionAtK(item.ExpectedDocumentIds, ranked, 3), RetrievalMetrics.PrecisionAtK(item.ExpectedDocumentIds, ranked, 5),
                RetrievalMetrics.PrecisionAtK(item.ExpectedDocumentIds, ranked, 10), RetrievalMetrics.ReciprocalRank(item.ExpectedDocumentIds, ranked),
                started.Elapsed.TotalMilliseconds, []));
        }
        var report = RetrievalMetrics.Summarize("Gemini semantic challenge", metrics);
        output.WriteLine("Recall@1={0:F3} Recall@5={1:F3} MRR={2:F3} mean-query={3:F1}ms p95={4:F1}ms", report.Recall1, report.Recall5, report.Mrr, report.MeanLatencyMs, report.P95LatencyMs);
    }

    private static double Cosine(float[] a, float[] b) { double dot = 0, aa = 0, bb = 0; for (var i = 0; i < a.Length; i++) { dot += a[i] * b[i]; aa += a[i] * a[i]; bb += b[i] * b[i]; } return dot / (Math.Sqrt(aa) * Math.Sqrt(bb)); }
    private sealed class SimpleHttpClientFactory : IHttpClientFactory { private readonly HttpClient _client = new() { BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/") }; public HttpClient CreateClient(string name) => _client; }
}

internal sealed class FixedMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value; public T Get(string? name) => value; public IDisposable? OnChange(Action<T, string?> listener) => null;
}
