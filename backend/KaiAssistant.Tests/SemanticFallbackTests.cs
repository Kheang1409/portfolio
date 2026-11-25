using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using FluentAssertions;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Application.Rag;
using KaiAssistant.Infrastructure.AI.Rag;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KaiAssistant.Tests;

public sealed class SemanticFallbackTests
{
    [Fact]
    public async Task Hybrid_degrades_to_keyword_when_query_embedding_fails()
    {
        var embeddings = new Mock<IEmbeddingService>();
        embeddings.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), EmbeddingPurpose.RetrievalQuery, null, It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
        var vectors = new Mock<IVectorStore>();
        var keywords = new Mock<IKeywordSearch>();
        keywords.Setup(x => x.SearchAsync("MongoDB", 20, It.IsAny<CancellationToken>())).ReturnsAsync([
            new RetrievedChunk("chunk", "doc", "MongoDB durable state", 0, 1, 0, 1, new Dictionary<string,string>())]);
        var embeddingOptions = new Mock<IOptionsMonitor<EmbeddingOptions>>();
        embeddingOptions.SetupGet(x => x.CurrentValue).Returns(new EmbeddingOptions { Version = "gemini-v1", IndexVersion = "v2" });
        var ragOptions = new Mock<IOptionsMonitor<RagOptions>>(); ragOptions.SetupGet(x => x.CurrentValue).Returns(new RagOptions());
        var retriever = new HybridRetriever(embeddings.Object, vectors.Object, keywords.Object, embeddingOptions.Object, ragOptions.Object);

        var result = await retriever.DiagnoseAsync("MongoDB", RetrievalStrategy.Hybrid);

        result.Results.Should().ContainSingle(x => x.ChunkId == "chunk");
        vectors.Verify(x => x.SearchAsync(It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
