using KaiAssistant.Application.Rag;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities.AI;
using MongoDB.Driver;

namespace KaiAssistant.Infrastructure.AI.Rag;

/// <summary>Portable vector-store implementation backed by the existing Mongo collection.</summary>
public sealed class MongoVectorStore(IMongoDatabase database) : IVectorStore
{
    private readonly IMongoCollection<KnowledgeChunk> _chunks = database.GetCollection<KnowledgeChunk>("knowledge_chunks");
    private readonly IMongoCollection<KnowledgeDocument> _documents = database.GetCollection<KnowledgeDocument>("knowledge_base_documents");

    public async Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default)
    {
        if (!record.Metadata.TryGetValue("embeddingDimensions", out var dimensionText) || !int.TryParse(dimensionText, out var expectedDimensions)) expectedDimensions = record.Embedding.Length;
        if (record.Embedding.Length != expectedDimensions) throw new InvalidOperationException($"Embedding dimension mismatch: metadata requires {expectedDimensions}, vector contains {record.Embedding.Length}.");
        var chunk = new KnowledgeChunk
        {
            Id = record.Id,
            DocumentId = record.DocumentId,
            Content = record.Content,
            ContentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(record.Content))).ToLowerInvariant(),
            Embedding = record.Embedding,
            Metadata = new Dictionary<string, string>(record.Metadata, StringComparer.OrdinalIgnoreCase),
            Source = record.Metadata.TryGetValue("source", out var source) ? source : string.Empty,
            Title = record.Metadata.TryGetValue("title", out var title) ? title : string.Empty,
            ChunkIndex = record.Metadata.TryGetValue("chunkIndex", out var index) && int.TryParse(index, out var parsed) ? parsed : 0
        };
        chunk.EmbeddingDimensions = record.Embedding.Length;
        chunk.EmbeddingProvider = record.Metadata.TryGetValue("embeddingProvider", out var provider) ? provider : string.Empty;
        chunk.EmbeddingModel = record.Metadata.TryGetValue("embeddingModel", out var model) ? model : string.Empty;
        chunk.EmbeddingPipelineVersion = record.Metadata.TryGetValue("embeddingPipelineVersion", out var pipeline) ? pipeline : string.Empty;
        chunk.IndexVersion = record.Metadata.TryGetValue("indexVersion", out var version) ? version : string.Empty;
        await _chunks.ReplaceOneAsync(x => x.Id == record.Id, chunk, new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteByDocumentAsync(string documentId, CancellationToken cancellationToken = default) =>
        _chunks.DeleteManyAsync(x => x.DocumentId == documentId, cancellationToken);

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(VectorSearchRequest request, CancellationToken cancellationToken = default)
    {
        // Mongo Atlas vector search can replace this bounded fallback without changing the application contract.
        var activeIds = await _documents.Find(x => x.Status == "Completed" || x.Status == "Indexed")
            .Project(x => x.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (activeIds.Count == 0) return [];
        var activeDimensions = request.Embedding.Length;
        var embeddingVersion = request.MetadataFilter is not null && request.MetadataFilter.TryGetValue("embeddingPipelineVersion", out var version)
            ? version
            : string.Empty;
        var candidates = await _chunks.Find(Builders<KnowledgeChunk>.Filter.In(x => x.DocumentId, activeIds!) &
            Builders<KnowledgeChunk>.Filter.Eq(x => x.IndexVersion, request.MetadataFilter is not null && request.MetadataFilter.TryGetValue("indexVersion", out var indexVersion) ? indexVersion : string.Empty) &
            Builders<KnowledgeChunk>.Filter.Eq(x => x.EmbeddingDimensions, activeDimensions) &
            Builders<KnowledgeChunk>.Filter.Eq(x => x.EmbeddingPipelineVersion, embeddingVersion))
            .Limit(500).ToListAsync(cancellationToken).ConfigureAwait(false);
        return candidates.Where(x => Matches(x, request.MetadataFilter))
            .Select(x => new VectorSearchResult(ToRecord(x), VectorMath.CosineSimilarity(request.Embedding, x.Embedding)))
            .Where(x => x.Score >= request.MinimumScore)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Record.Id, StringComparer.Ordinal).Take(request.TopK).ToArray();
    }

    private static bool Matches(KnowledgeChunk chunk, IReadOnlyDictionary<string, string>? filter) =>
        filter is null || filter.All(x => chunk.Metadata.TryGetValue(x.Key, out var value) && string.Equals(value, x.Value, StringComparison.OrdinalIgnoreCase));
    internal static VectorRecord ToRecord(KnowledgeChunk x) => new(x.Id ?? string.Empty, x.DocumentId, x.Content, x.Embedding, x.Metadata);
}
