using FluentAssertions;
using KaiAssistant.Application.Options;
using KaiAssistant.Application.Rag;
using KaiAssistant.Infrastructure.AI.Rag;
using Microsoft.Extensions.Options;
using Xunit;

namespace KaiAssistant.RagEvaluation;

public sealed class ContextSelectionTests
{
    [Fact]
    public void Selection_enforces_count_budget_threshold_and_duplicate_suppression()
    {
        var options = new RagOptions { ContextChunkCount = 2, MaxContextEstimatedTokens = 20, MinimumEvidenceScore = .01, NearDuplicateThreshold = .8 };
        var selector = new RagContextSelector(new FixedOptionsMonitor<RagOptions>(options));
        var metadata = new Dictionary<string, string> { ["source"] = "source", ["title"] = "title", ["section"] = "section", ["version"] = "2" };
        var candidates = new[] {
            new RetrievedChunk("a", "doc-a", "MongoDB stores durable state", 0, 1, .03, 1, metadata),
            new RetrievedChunk("b", "doc-a", "MongoDB stores durable state", 0, 1, .02, 2, metadata),
            new RetrievedChunk("c", "doc-c", "Redis controls distributed paths", 0, 1, .02, 3, metadata),
            new RetrievedChunk("d", "doc-d", new string('x', 100), 0, 1, .02, 4, metadata),
            new RetrievedChunk("e", "doc-e", "weak", 0, 1, .001, 5, metadata) };
        selector.Select(candidates).Select(x => x.ChunkId).Should().Equal("a", "c");
    }

    [Fact]
    public void Selection_returns_empty_when_evidence_is_below_threshold()
    {
        var selector = new RagContextSelector(new FixedOptionsMonitor<RagOptions>(new RagOptions { MinimumEvidenceScore = .05 }));
        selector.Select([new RetrievedChunk("a", "doc", "irrelevant", 0, 0, .01, 1, new Dictionary<string, string>())]).Should().BeEmpty();
    }
}

internal sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;
    public T Get(string? name) => value;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
