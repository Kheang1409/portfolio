using FluentAssertions;
using KaiAssistant.Domain.Entities.AI;
using KaiAssistant.Infrastructure.AI.Rag;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using System;
using System.Threading.Tasks;
using KaiAssistant.Application.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace KaiAssistant.Tests.Integration;

public sealed class ActiveKnowledgeRetrievalTests
{
    [Fact]
    public async Task Keyword_search_excludes_inactive_versions_and_preserves_source_metadata()
    {
        using var runner = MongoTestRunner.Start();
        var database = new MongoClient(runner.ConnectionString).GetDatabase("rag-active-" + Guid.NewGuid().ToString("N"));
        var documents = database.GetCollection<KnowledgeDocument>("knowledge_base_documents");
        var chunks = database.GetCollection<KnowledgeChunk>("knowledge_chunks");
        var activeId = ObjectId.GenerateNewId().ToString();
        var archivedId = ObjectId.GenerateNewId().ToString();
        await documents.InsertManyAsync([
            new KnowledgeDocument { Id = activeId, Source = "active", Status = "Completed", ContentHash = "active" },
            new KnowledgeDocument { Id = archivedId, Source = "old", Status = "Archived", ContentHash = "old" }]);
        await chunks.InsertManyAsync([
            new KnowledgeChunk { Id = ObjectId.GenerateNewId().ToString(), DocumentId = activeId, Content = "MongoDB durable state", Source = "active", Title = "Runtime", IndexVersion = "knowledge-gemini-001-v2", Metadata = new() { ["source"]="active", ["title"]="Runtime", ["version"]="2" } },
            new KnowledgeChunk { Id = ObjectId.GenerateNewId().ToString(), DocumentId = archivedId, Content = "MongoDB legacy state", Source = "old", Title = "Legacy", Metadata = new() { ["source"]="old", ["title"]="Legacy", ["version"]="1" } }]);

        var options = new Mock<IOptionsMonitor<RagOptions>>();
        options.SetupGet(x => x.CurrentValue).Returns(new RagOptions { IndexVersion = "knowledge-gemini-001-v2" });
        var result = await new MongoKeywordSearch(database, options.Object).SearchAsync("MongoDB", 10);

        result.Should().ContainSingle();
        result[0].DocumentId.Should().Be(activeId);
        result[0].Metadata.Should().ContainKey("source").WhoseValue.Should().Be("active");
        result[0].Metadata.Should().ContainKey("version").WhoseValue.Should().Be("2");
    }
}
