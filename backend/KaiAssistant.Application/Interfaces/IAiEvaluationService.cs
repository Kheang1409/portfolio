namespace KaiAssistant.Application.Interfaces;

public interface IAiEvaluationService
{
    Task<AiEvaluationRunResult> EvaluateAsync(IReadOnlyList<string> prompts, CancellationToken cancellationToken = default);
}
public sealed class AiEvaluationRunResult
{
    public DateTime CreatedAtUtc { get; init; }
    public double AverageLatencyMs { get; init; }
    public double AverageQualityScore { get; init; }
    public IReadOnlyList<AiEvaluationEntryResult> Entries { get; init; } = Array.Empty<AiEvaluationEntryResult>();
}
public sealed class AiEvaluationEntryResult
{
    public string Prompt { get; init; } = string.Empty;
    public string Response { get; init; } = string.Empty;
    public string ModelUsed { get; init; } = string.Empty;
    public long LatencyMs { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public double QualityScore { get; init; }
}