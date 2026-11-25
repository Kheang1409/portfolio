using FluentAssertions;
using KaiAssistant.Application.Rag;
using KaiAssistant.Infrastructure.AI.Rag;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace KaiAssistant.Tests;

public sealed class HybridRetrievalTests
{
    [Fact]
    public void Reranker_PrefersChunksContainingQueryTerms()
    {
        var candidates = new RetrievedChunk[]
        {
            new("one", "doc", "General portfolio overview", .8, 0, .03, 1, new Dictionary<string, string>()),
            new("two", "doc", "Built .NET distributed systems and APIs", .7, 0, .029, 2, new Dictionary<string, string>())
        };

        var result = new DeterministicReranker().Rerank(".NET APIs", candidates, 2);

        result.First().ChunkId.Should().Be("two");
        result.First().Rank.Should().Be(1);
    }
}
