namespace KaiAssistant.Application.Interfaces;

public enum EmbeddingPurpose { RetrievalQuery, RetrievalDocument }
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        GenerateEmbeddingAsync(text, EmbeddingPurpose.RetrievalQuery, null, cancellationToken);
    Task<float[]> GenerateEmbeddingAsync(string text, EmbeddingPurpose purpose, string? title = null, CancellationToken cancellationToken = default);
    async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, EmbeddingPurpose purpose, IReadOnlyList<string?>? titles = null, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>(texts.Count);
        for (var i = 0; i < texts.Count; i++) results.Add(await GenerateEmbeddingAsync(texts[i], purpose, titles is null ? null : titles[i], cancellationToken).ConfigureAwait(false));
        return results;
    }
}
