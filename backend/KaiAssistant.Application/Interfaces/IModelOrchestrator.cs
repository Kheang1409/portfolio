using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IModelOrchestrator
{
    Task<AiRoutingDecision> BuildDecisionAsync(
        string question,
        int estimatedInputTokens,
        CancellationToken cancellationToken = default);
}