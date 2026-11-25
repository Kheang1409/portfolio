using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Infrastructure.Services;

public class TokenBudgetService : ITokenBudgetService
{
    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        // Average: 1 token ≈ 4 chars
        return (text.Length / 4) + (text.Split(' ').Length / 2);
    }
    public List<ConversationMessage> TrimContextToTokenBudget(
        List<ConversationMessage> messages,
        int maxTokens,
        bool keepOldest = false)
    {
        var result = new List<ConversationMessage>();
        var currentTokens = 0;
        var orderedMessages = keepOldest
            ? messages
            : messages.AsEnumerable().Reverse().ToList();
        foreach (var msg in orderedMessages)
        {
            var msgTokens = EstimateTokens(msg.Content);
            if (currentTokens + msgTokens > maxTokens)
                break;
            result.Add(msg);
            currentTokens += msgTokens;
        }
        return keepOldest ? result : result.AsEnumerable().Reverse().ToList();
    }
    public int CalculateContextTokens(ConversationContext context)
    {
        var tokens = 0;
        tokens += EstimateTokens(context.SystemPrompt);
        tokens += EstimateTokens(context.UserMessage);
        foreach (var msg in context.ContextMessages)
        {
            tokens += EstimateTokens(msg.Content);
        }
        return tokens;
    }
}