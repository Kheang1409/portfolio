using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities.AI;
using KaiAssistant.Domain.Entities.Resumes;
using KaiAssistant.Domain.Interfaces.Repositories;
using KaiAssistant.Infrastructure.HostedServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace KaiAssistant.Tests.Integration;

public sealed class KnowledgeBootstrapHostedServiceTests
{
    [Fact]
    public async Task StartAsync_IsIdempotent_WhenResumeContentIsAlreadyIndexed()
    {
        using var runner = MongoTestRunner.Start();
        var database = new MongoClient(runner.ConnectionString).GetDatabase("knowledge-bootstrap-" + Guid.NewGuid().ToString("N"));
        var documents = database.GetCollection<KnowledgeDocument>("knowledge_base_documents");
        await CreateContentHashIndexAsync(documents);
        var resume = new Resume { Summary = "same resume content" };
        var contentHash = Hash("Summary: same resume content");
        await documents.InsertOneAsync(new KnowledgeDocument
        {
            Source = "resume:latest",
            Content = "Summary: same resume content",
            ContentHash = contentHash,
            Status = "Completed",
            Metadata = new() { ["indexVersion"] = "test-index" }
        });
        var service = CreateService(database, resume);

        await service.StartAsync(default);
        await service.StartAsync(default);

        (await documents.CountDocumentsAsync(FilterDefinition<KnowledgeDocument>.Empty)).Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentStarts_DoNotFailHostOnDuplicateContentHash()
    {
        using var runner = MongoTestRunner.Start();
        var database = new MongoClient(runner.ConnectionString).GetDatabase("knowledge-bootstrap-race-" + Guid.NewGuid().ToString("N"));
        var documents = database.GetCollection<KnowledgeDocument>("knowledge_base_documents");
        await CreateContentHashIndexAsync(documents);
        var resume = new Resume { Summary = "concurrent resume content" };
        var service1 = CreateService(database, resume);
        var service2 = CreateService(database, resume);

        await Task.WhenAll(service1.StartAsync(default), service2.StartAsync(default));

        (await documents.CountDocumentsAsync(FilterDefinition<KnowledgeDocument>.Empty)).Should().Be(1);
        (await documents.Find(FilterDefinition<KnowledgeDocument>.Empty).FirstAsync()).Status.Should().Be("Completed");
    }

    private static KnowledgeBootstrapHostedService CreateService(IMongoDatabase database, Resume resume)
    {
        var resumes = new Mock<IResumeRepository>();
        resumes.Setup(x => x.GetLatestAsync(It.IsAny<CancellationToken>())).ReturnsAsync(resume);
        var options = new KnowledgeOptions { SemanticIndexingEnabled = false, ChunkSize = 1000, ChunkOverlap = 100 };
        var embeddingOptions = new EmbeddingOptions { IndexVersion = "test-index" };
        return new KnowledgeBootstrapHostedService(database, resumes.Object, Mock.Of<IEmbeddingService>(),
            Options.Create(options), Options.Create(embeddingOptions), NullLogger<KnowledgeBootstrapHostedService>.Instance);
    }

    private static Task CreateContentHashIndexAsync(IMongoCollection<KnowledgeDocument> documents) =>
        documents.Indexes.CreateOneAsync(new CreateIndexModel<KnowledgeDocument>(
            Builders<KnowledgeDocument>.IndexKeys.Ascending(x => x.ContentHash),
            new CreateIndexOptions { Name = "ix_knowledge_content_hash", Unique = true }));

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
