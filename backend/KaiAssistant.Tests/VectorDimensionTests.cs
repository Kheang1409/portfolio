using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Rag;
using KaiAssistant.Infrastructure.AI.Rag;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace KaiAssistant.Tests;

public sealed class VectorDimensionTests
{
    [Fact]
    public async Task Vector_store_rejects_metadata_dimension_mismatch()
    {
        var database = new Mock<IMongoDatabase>();
        var store = new MongoVectorStore(database.Object);
        var metadata = new Dictionary<string, string> { ["embeddingDimensions"] = "768" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UpsertAsync(new VectorRecord("id", "doc", "text", new float[64], metadata)));
    }
}
