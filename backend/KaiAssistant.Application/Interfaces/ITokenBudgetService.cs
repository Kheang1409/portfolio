using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface ITokenBudgetService
{
    int EstimateTokens(string text);
    List<ConversationMessage> TrimContextToTokenBudget(
        List<ConversationMessage> messages,
        int maxTokens,
        bool keepOldest = false);
    int CalculateContextTokens(ConversationContext context);
}