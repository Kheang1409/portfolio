using KaiAssistant.Application.AI;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Application.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics;
using System.Diagnostics.Metrics;
namespace KaiAssistant.Infrastructure.AI.Rag;

public sealed class RagService : IRagService
{
    private static readonly ActivitySource ActivitySource = new("KaiAssistant.Rag", "1.0.0");
    private static readonly Meter Meter = new("KaiAssistant.Rag", "1.0.0");
    private static readonly Histogram<double> RetrievalDuration = Meter.CreateHistogram<double>("retrieval_total_duration", "ms");
    private static readonly Histogram<double> ContextDuration = Meter.CreateHistogram<double>("context_build_duration", "ms");
    private static readonly Histogram<long> CandidateCount = Meter.CreateHistogram<long>("retrieval_candidate_count");
    private static readonly Histogram<long> SelectedCount = Meter.CreateHistogram<long>("retrieval_selected_count");
    private static readonly Counter<long> EmptyCount = Meter.CreateCounter<long>("retrieval_empty_count");
    private readonly IHybridRetriever _retriever;
    private readonly IRagContextSelector _contextSelector;
    private readonly IOptionsMonitor<RagOptions> _options;
    private readonly ILogger<RagService> _logger;
    public RagService(
        IHybridRetriever retriever,
        IRagContextSelector contextSelector,
        IOptionsMonitor<RagOptions> options,
        ILogger<RagService> logger)
    {
        _retriever = retriever;
        _contextSelector = contextSelector;
        _options = options;
        _logger = logger;
    }
    public async Task<RagContextResult> BuildAugmentedPromptAsync(string question, CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled)
        {
            return new RagContextResult { AugmentedPrompt = question };
        }
        using var activity = ActivitySource.StartActivity("rag.retrieve", ActivityKind.Internal);
        var retrievalWatch = Stopwatch.StartNew();
        var candidates = await _retriever.RetrieveAsync(question, cancellationToken).ConfigureAwait(false);
        RetrievalDuration.Record(retrievalWatch.Elapsed.TotalMilliseconds);
        CandidateCount.Record(candidates.Count);
        var contextWatch = Stopwatch.StartNew();
        var selected = _contextSelector.Select(candidates);
        ContextDuration.Record(contextWatch.Elapsed.TotalMilliseconds);
        SelectedCount.Record(selected.Count);
        activity?.SetTag("rag.strategy", "keyword_first_semantic_fallback");
        activity?.SetTag("rag.semantic_candidate_count", _options.CurrentValue.SemanticCandidateCount);
        activity?.SetTag("rag.keyword_candidate_count", _options.CurrentValue.KeywordCandidateCount);
        activity?.SetTag("rag.final_candidate_count", candidates.Count);
        activity?.SetTag("rag.context_chunk_count", selected.Count);
        activity?.SetTag("rag.pipeline_version", _options.CurrentValue.PipelineVersion);
        if (selected.Count == 0)
        {
            EmptyCount.Add(1);
            return new RagContextResult { AugmentedPrompt = question };
        }
        _logger.LogInformation("Hybrid RAG context retrieved. RetrievedSnippets={Count}", selected.Count);
        var snippets = selected
            .Select(x => new RagSnippet
            {
                ChunkId = x.ChunkId,
                DocumentId = x.DocumentId,
                Title = x.Metadata.TryGetValue("title", out var title) ? title : string.Empty,
                Section = x.Metadata.TryGetValue("section", out var section) ? section : string.Empty,
                Version = x.Metadata.TryGetValue("version", out var version) && int.TryParse(version, out var parsedVersion) ? parsedVersion : 1,
                Source = x.Metadata.TryGetValue("source", out var source) ? source : string.Empty,
                Content = x.Content,
                Similarity = x.RerankScore > 0 ? x.RerankScore : x.FusedScore
            })
            .ToList();
        var contextLines = new List<string>(snippets.Count);
        for (var i = 0; i < snippets.Count; i++)
        {
            var x = snippets[i];
            contextLines.Add($"[{i + 1}] source={x.Source}; relevance={x.Similarity:F3}; content={x.Content}");
        }
        var contextBlock = string.Join('\n', contextLines);
        if (contextBlock.Length > _options.CurrentValue.MaxContextChars)
        {
            contextBlock = contextBlock[.._options.CurrentValue.MaxContextChars];
        }
        var augmented =
            "Use the following knowledge base context when relevant. If context is insufficient, say what is missing.\n\n" +
            contextBlock +
            "\n\nUser question:\n" +
            question;
        return new RagContextResult
        {
            AugmentedPrompt = augmented,
            Snippets = snippets
        };
    }
}
