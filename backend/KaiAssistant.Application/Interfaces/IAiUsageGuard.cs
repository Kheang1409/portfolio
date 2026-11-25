using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IAiUsageGuard
{
    Task<AiUsageDecision> EvaluateAsync(
        string question,
        ConversationMessage[]? history,
        AssistantContext? context,
        CancellationToken cancellationToken = default);
    void RecordTokensUsed(int inputTokens, int outputTokens);
    bool TryChargeBudget(decimal requestCostUsd, AssistantContext? context, out string? rejectionMessage);
    AiResponseDecision EvaluateResponse(string response);
}