using System.Security.Cryptography;
using System.Text;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities.AI;
using KaiAssistant.Domain.Entities.Resumes;
using KaiAssistant.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class KnowledgeBootstrapHostedService(
    IMongoDatabase database,
    IResumeRepository resumes,
    IEmbeddingService embeddings,
    IOptions<KnowledgeOptions> options,
    IOptions<EmbeddingOptions> embeddingOptions,
    ILogger<KnowledgeBootstrapHostedService> logger) : IHostedService
{
    private readonly IMongoCollection<KnowledgeDocument> _documents = database.GetCollection<KnowledgeDocument>("knowledge_base_documents");
    private readonly IMongoCollection<KnowledgeChunk> _chunks = database.GetCollection<KnowledgeChunk>("knowledge_chunks");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var resume = await resumes.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        if (resume is null)
        {
            logger.LogInformation("No resume data is available; knowledge bootstrap skipped.");
            return;
        }

        var content = BuildContent(resume);
        if (string.IsNullOrWhiteSpace(content)) return;
        var embedding = embeddingOptions.Value;
        var indexVersion = embedding.IndexVersion;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
        const string source = "resume:latest";
        var title = resume.Personals?.PreferredName ?? resume.Personals?.LegalName ?? "Resume";

        // Check before inserting: the content-hash index is unique, so reinserting the same
        // completed resume on every Lambda cold start would otherwise fail host startup.
        var existing = await _documents.Find(x => x.Source == source && x.Status == "Completed")
            .SortByDescending(x => x.UpdatedAtUtc).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (existing is not null && existing.ContentHash == hash)
        {
            existing.Metadata.TryGetValue("indexVersion", out var priorVersion);
            if (priorVersion == indexVersion)
            {
                logger.LogDebug("Resume knowledge bootstrap is already current.");
                return;
            }
        }

        var document = new KnowledgeDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Source = source,
            Title = title,
            Content = content,
            ContentHash = hash,
            Status = "Processing",
            MimeType = "text/plain",
            Metadata = new(StringComparer.OrdinalIgnoreCase) { ["source"] = source, ["title"] = title, ["contentHash"] = hash, ["indexVersion"] = indexVersion }
        };
        try
        {
            await _documents.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Another Lambda instance may have won the same cold-start race. Treat that as an
            // idempotent no-op; only swallow the collision if the expected content is present.
            var competing = await _documents.Find(x => x.ContentHash == hash).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (competing is null) throw;
            logger.LogInformation("Resume knowledge bootstrap was already claimed by another instance.");
            return;
        }
        try
        {
            var pieces = Split(content, options.Value.ChunkSize, options.Value.ChunkOverlap);
            IReadOnlyList<float[]>? vectors = null;
            if (options.Value.SemanticIndexingEnabled)
            {
                try
                {
                    vectors = await embeddings.GenerateEmbeddingsAsync(pieces, EmbeddingPurpose.RetrievalDocument,
                        Enumerable.Repeat<string?>(title, pieces.Count).ToArray(), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Semantic indexing failed for resume data; retaining keyword retrieval.");
                }
            }

            var chunks = pieces.Select((piece, index) => new KnowledgeChunk
            {
                Id = ObjectId.GenerateNewId().ToString(),
                DocumentId = document.Id!,
                Source = source,
                Title = title,
                Content = piece,
                ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(piece))).ToLowerInvariant(),
                ChunkIndex = index,
                Embedding = vectors is not null && index < vectors.Count ? vectors[index] : [],
                EmbeddingProvider = embedding.Provider,
                EmbeddingModel = embedding.Model,
                EmbeddingDimensions = vectors is not null && index < vectors.Count ? vectors[index].Length : 0,
                EmbeddingPipelineVersion = embedding.Version,
                IndexVersion = indexVersion,
                Metadata = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["source"] = source,
                    ["title"] = title,
                    ["contentHash"] = hash,
                    ["chunkIndex"] = index.ToString(),
                    ["embeddingPipelineVersion"] = embedding.Version,
                    ["indexVersion"] = indexVersion,
                    ["embeddingProvider"] = embedding.Provider,
                    ["embeddingModel"] = embedding.Model,
                    ["embeddingDimensions"] = (vectors is not null && index < vectors.Count ? vectors[index].Length : 0).ToString()
                }
            }).ToArray();
            if (chunks.Length > 0) await _chunks.InsertManyAsync(chunks, cancellationToken: cancellationToken).ConfigureAwait(false);
            document.Status = "Completed";
            document.ChunkCount = chunks.Length;
            document.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _documents.ReplaceOneAsync(x => x.Id == document.Id, document, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _documents.UpdateManyAsync(x => x.Source == source && x.Id != document.Id && x.Status == "Completed",
                Builders<KnowledgeDocument>.Update.Set(x => x.Status, "Retired"), cancellationToken: cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Resume knowledge bootstrap completed. Chunks={ChunkCount} Semantic={Semantic}", chunks.Length, vectors is not null);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            await _documents.UpdateOneAsync(x => x.Id == document.Id, Builders<KnowledgeDocument>.Update.Set(x => x.Status, "Failed"), cancellationToken: CancellationToken.None).ConfigureAwait(false);
            logger.LogError(ex, "Resume knowledge bootstrap failed; existing knowledge remains active.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string BuildContent(Resume resume)
    {
        var lines = new List<string>();
        var personals = resume.Personals;
        if (personals is not null)
        {
            if (!string.IsNullOrWhiteSpace(personals.LegalName)) lines.Add($"Name: {personals.LegalName}");
            if (!string.IsNullOrWhiteSpace(personals.PreferredName)) lines.Add($"Preferred name: {personals.PreferredName}");
            if (!string.IsNullOrWhiteSpace(personals.WorkAuthorization)) lines.Add($"Work authorization: {personals.WorkAuthorization}");
        }
        if (!string.IsNullOrWhiteSpace(resume.Summary)) lines.Add($"Summary: {resume.Summary}");
        if (resume.Skills.Count > 0) lines.Add($"Skills: {string.Join(", ", resume.Skills)}");
        foreach (var item in resume.Experiences)
        {
            lines.Add($"Experience: {item.Role} at {item.Company}; {item.Location}; {string.Join("; ", item.BulletPoints)}");
        }
        foreach (var item in resume.Projects) lines.Add($"Project: {item.Name}. {item.Description}. Skills: {string.Join(", ", item.Skills)}");
        foreach (var item in resume.Educations) lines.Add($"Education: {item.Degree}, {item.Institution}, {item.Location}");
        foreach (var item in resume.Certifications) lines.Add($"Certification: {item.Title}, {item.Issuer}");
        return string.Join("\n", lines);
    }

    private static List<string> Split(string content, int size, int overlap)
    {
        var result = new List<string>();
        for (var start = 0; start < content.Length;)
        {
            var end = Math.Min(content.Length, start + size);
            if (end < content.Length)
            {
                var boundary = content.LastIndexOfAny(['.', '!', '?', '\n'], end - 1, end - start);
                if (boundary > start + size / 2) end = boundary + 1;
            }
            var piece = content[start..end].Trim();
            if (piece.Length > 0) result.Add(piece);
            if (end == content.Length) break;
            start = Math.Max(start + 1, end - overlap);
        }
        return result;
    }
}
