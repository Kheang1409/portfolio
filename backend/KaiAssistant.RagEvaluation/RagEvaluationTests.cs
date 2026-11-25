using FluentAssertions;
using KaiAssistant.Application.Rag;
using KaiAssistant.Infrastructure.AI.Rag;
using Xunit;
using Xunit.Abstractions;
using System.ComponentModel.DataAnnotations;
using KaiAssistant.Application.Options;

namespace KaiAssistant.RagEvaluation;

public sealed class RagEvaluationTests(ITestOutputHelper output)
{
    [Fact]
    public void Dataset_IsCorpusGrounded_Diverse_AndSplit()
    {
        var cases = RagEvaluationDataset.Load();
        cases.Should().HaveCountGreaterThanOrEqualTo(30);
        cases.Select(x => x.Category).Distinct().Should().HaveCountGreaterThanOrEqualTo(6);
        cases.Should().Contain(x => x.Split == "tuning").And.Contain(x => x.Split == "holdout");
    }

    [Theory]
    [InlineData(RetrievalStrategy.Keyword)]
    [InlineData(RetrievalStrategy.Semantic)]
    [InlineData(RetrievalStrategy.Hybrid)]
    [InlineData(RetrievalStrategy.HybridReranked)]
    public void Offline_baseline_is_reported_without_Gemini(RetrievalStrategy strategy)
    {
        var cases = RagEvaluationDataset.Load().Select(x => OfflineEvaluationEngine.Run(x, strategy)).ToArray();
        var report = RetrievalMetrics.Summarize(strategy.ToString(), cases);
        output.WriteLine("{0,-16} Recall@1={1:F3} Recall@3={2:F3} Recall@5={3:F3} Recall@10={4:F3} P@3={5:F3} P@5={6:F3} P@10={7:F3} MRR={8:F3} mean={9:F3}ms p50={10:F3}ms p95={11:F3}ms",
            report.Strategy, report.Recall1, report.Recall3, report.Recall5, report.Recall10, report.Precision3, report.Precision5, report.Precision10, report.Mrr, report.MeanLatencyMs, report.P50LatencyMs, report.P95LatencyMs);
        foreach (var category in report.Categories) output.WriteLine("  {0}: R@5={1:F3} P@5={2:F3} MRR={3:F3}", category.Key, category.Value.Recall5, category.Value.Precision5, category.Value.Mrr);
        if (report.FailedCaseIds.Count > 0) output.WriteLine("  Failed: " + string.Join(", ", report.FailedCaseIds));
        report.CaseCount.Should().Be(35);
    }

    [Fact]
    public void Metric_formulas_are_correct()
    {
        string[] relevant = ["b", "d"];
        string[] ranked = ["a", "b", "c", "d"];
        RetrievalMetrics.RecallAtK(relevant, ranked, 3).Should().Be(.5);
        RetrievalMetrics.PrecisionAtK(relevant, ranked, 3).Should().BeApproximately(1d / 3, 0.0001);
        RetrievalMetrics.ReciprocalRank(relevant, ranked).Should().Be(.5);
    }

    [Fact]
    public void Holdout_keyword_regression_gate_matches_measured_baseline()
    {
        var holdout = RagEvaluationDataset.Load().Where(x => x.Split == "holdout").Select(x => OfflineEvaluationEngine.Run(x, RetrievalStrategy.Keyword)).ToArray();
        var report = RetrievalMetrics.Summarize("Keyword holdout", holdout);
        report.Recall5.Should().BeGreaterThanOrEqualTo(.90);
        report.Mrr.Should().BeGreaterThanOrEqualTo(.90);
    }

    [Fact]
    public void Retrieval_configuration_requires_pipeline_and_index_versions()
    {
        var options = new RagOptions { PipelineVersion = "", IndexVersion = "" };
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), errors, true).Should().BeFalse();
        errors.Should().Contain(x => x.MemberNames.Contains(nameof(RagOptions.PipelineVersion)))
            .And.Contain(x => x.MemberNames.Contains(nameof(RagOptions.IndexVersion)));
    }

    [Fact]
    public void Rrf_merges_duplicates_and_breaks_ties_by_chunk_id()
    {
        var metadata = new Dictionary<string, string>();
        VectorRecord Record(string id) => new(id, "doc-" + id, id, [1], metadata);
        var semantic = new[] { new VectorSearchResult(Record("b"), .9), new VectorSearchResult(Record("b"), .8), new VectorSearchResult(Record("a"), .7) };
        var keyword = new[] { new RetrievedChunk("a", "doc-a", "a", 0, 2, 0, 1, metadata), new RetrievedChunk("b", "doc-b", "b", 0, 1, 0, 2, metadata) };
        var result = ReciprocalRankFusion.Fuse(semantic, keyword, 60, 10);
        result.Should().HaveCount(2);
        result.Select(x => x.ChunkId).Should().Equal("a", "b");
    }

    [Fact]
    public void Rrf_handles_one_or_both_empty_lists()
    {
        var metadata = new Dictionary<string, string>();
        var keyword = new[] { new RetrievedChunk("k", "doc", "text", 0, 1, 0, 1, metadata) };
        ReciprocalRankFusion.Fuse([], [], 60, 10).Should().BeEmpty();
        ReciprocalRankFusion.Fuse([], keyword, 60, 10).Single().ChunkId.Should().Be("k");
        var vector = new[] { new VectorSearchResult(new VectorRecord("v", "doc", "text", [1], metadata), .9) };
        ReciprocalRankFusion.Fuse(vector, [], 60, 10).Single().ChunkId.Should().Be("v");
    }
}
