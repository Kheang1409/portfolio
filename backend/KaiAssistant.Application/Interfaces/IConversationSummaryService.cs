using System;
using System.Collections.Generic;
using System.Threading;
using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.Interfaces;

public interface IConversationSummaryService
{
    Task<string> SummarizeAsync(
        IEnumerable<ConversationMessage> messages,
        int maxLength = 500,
        CancellationToken cancellationToken = default);
    Task<string> GenerateAndUpdateSummaryAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default);
    bool ShouldSummarize(int messageCount, int lastSummaryMessageCount = 0);
}
public static class ConversationSummaryExtensions
{
    public static void InvalidateSummary(this Conversation conversation)
    {
        conversation.Summary = null;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
    }
}