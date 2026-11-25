using KaiAssistant.Application.Rag;

namespace KaiAssistant.Application.Interfaces;

public interface IVectorStore
{
    Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default);
    Task DeleteByDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(VectorSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IKeywordSearch
{
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default);
}

public interface IHybridRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken = default);
    Task<RetrievalDiagnostics> DiagnoseAsync(string query, RetrievalStrategy strategy, CancellationToken cancellationToken = default);
}

public interface IRagContextSelector
{
    IReadOnlyList<RetrievedChunk> Select(IReadOnlyList<RetrievedChunk> candidates);
}
