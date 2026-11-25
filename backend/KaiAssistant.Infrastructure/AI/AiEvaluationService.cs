using KaiAssistant.Application.AI;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities.AI;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.AI;

public sealed class AiEvaluationService : IAiEvaluationService
{
    private readonly IAiOrchestratorService _orchestrator;
    private readonly IPromptSecurityService _promptSecurity;
    private readonly IOptionsMonitor<AiEvaluationOptions> _options;
    private readonly IMongoCollection<AiEvaluationRecord> _collection;
    public AiEvaluationService(
        IAiOrchestratorService orchestrator,
        IPromptSecurityService promptSecurity,
        IOptionsMonitor<AiEvaluationOptions> options,
        IMongoDatabase database)
    {
        _orchestrator = orchestrator;
        _promptSecurity = promptSecurity;
        _options = options;
        _collection = database.GetCollection<AiEvaluationRecord>("ai_evaluations");
    }
    public async Task<AiEvaluationRunResult> EvaluateAsync(IReadOnlyList<string> prompts, CancellationToken cancellationToken = default)
    {
        var bounded = prompts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(Math.Max(1, _options.CurrentValue.MaxPromptsPerRun))
            .ToList();
        var now = DateTime.UtcNow;
        var entries = new List<AiEvaluationEntryResult>(bounded.Count);
        foreach (var prompt in bounded)
        {
            var cleanedPrompt = _promptSecurity.SanitizeInput(prompt);
            var response = await _orchestrator
                .GenerateAsync(new AiRequest { Prompt = cleanedPrompt }, cancellationToken)
                .ConfigureAwait(false);
            var cleanResponse = _promptSecurity.SanitizeOutput(response.Content);
            entries.Add(new AiEvaluationEntryResult
            {
                Prompt = cleanedPrompt,
                Response = cleanResponse,
                ModelUsed = response.ModelUsed,
                LatencyMs = response.LatencyMs,
                InputTokens = response.InputTokens.GetValueOrDefault(),
                OutputTokens = response.OutputTokens.GetValueOrDefault(),
                QualityScore = Score(cleanResponse)
            });
        }
        var avgLatency = entries.Count == 0 ? 0 : entries.Average(x => x.LatencyMs);
        var avgQuality = entries.Count == 0 ? 0 : entries.Average(x => x.QualityScore);
        var record = new AiEvaluationRecord
        {
            CreatedAtUtc = now,
            AverageLatencyMs = avgLatency,
            AverageQualityScore = avgQuality,
            Entries = entries.Select(x => new AiEvaluationEntry
            {
                Prompt = x.Prompt,
                Response = x.Response,
                ModelUsed = x.ModelUsed,
                LatencyMs = x.LatencyMs,
                InputTokens = x.InputTokens,
                OutputTokens = x.OutputTokens,
                QualityScore = x.QualityScore
            }).ToList()
        };
        await _collection.InsertOneAsync(record, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new AiEvaluationRunResult
        {
            CreatedAtUtc = now,
            AverageLatencyMs = avgLatency,
            AverageQualityScore = avgQuality,
            Entries = entries
        };
    }
    private static double Score(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return 0;
        }
        var score = 0.4;
        if (response.Length is >= 80 and <= 2500)
        {
            score += 0.3;
        }
        if (response.Contains("I don't know", StringComparison.OrdinalIgnoreCase))
        {
            score -= 0.1;
        }
        if (response.Contains("<script", StringComparison.OrdinalIgnoreCase))
        {
            score = 0;
        }
        if (response.Contains('\n'))
        {
            score += 0.1;
        }
        return Math.Clamp(score, 0, 1);
    }
}