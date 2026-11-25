namespace KaiAssistant.Application.AI;

public sealed class RagContextResult
{
    public string AugmentedPrompt { get; init; } = string.Empty;
    public IReadOnlyList<RagSnippet> Snippets { get; init; } = Array.Empty<RagSnippet>();
}
public sealed class RagSnippet
{
    public string ChunkId { get; init; } = string.Empty;
    public string DocumentId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public string Source { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double Similarity { get; init; }
}
