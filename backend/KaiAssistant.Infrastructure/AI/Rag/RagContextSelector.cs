using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Application.Rag;
using Microsoft.Extensions.Options;

namespace KaiAssistant.Infrastructure.AI.Rag;

public sealed class RagContextSelector(IOptionsMonitor<RagOptions> options) : IRagContextSelector
{
    public IReadOnlyList<RetrievedChunk> Select(IReadOnlyList<RetrievedChunk> candidates)
    {
        var config = options.CurrentValue;
        var selected = new List<RetrievedChunk>();
        var tokens = 0;
        foreach (var candidate in candidates.OrderBy(x => x.Rank).ThenBy(x => x.ChunkId, StringComparer.Ordinal))
        {
            var score = candidate.RerankScore > 0 ? candidate.RerankScore : candidate.FusedScore;
            if (score < config.MinimumEvidenceScore || selected.Any(x => IsNearDuplicate(x.Content, candidate.Content, config.NearDuplicateThreshold))) continue;
            var estimated = Math.Max(1, (candidate.Content.Length + 3) / 4);
            if (tokens + estimated > config.MaxContextEstimatedTokens) continue;
            selected.Add(candidate);
            tokens += estimated;
            if (selected.Count >= config.ContextChunkCount) break;
        }
        return selected;
    }

    internal static bool IsNearDuplicate(string left, string right, double threshold)
    {
        if (string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal)) return true;
        var a = Terms(left); var b = Terms(right);
        if (a.Count == 0 || b.Count == 0) return false;
        return a.Intersect(b).Count() / (double)a.Union(b).Count() >= threshold;
    }

    private static string Normalize(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    private static HashSet<string> Terms(string value) => value.Split([' ', '\t', '\r', '\n', ',', '.', '?', '!', ':', ';'], StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
}
