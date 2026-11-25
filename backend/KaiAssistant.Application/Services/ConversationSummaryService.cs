namespace KaiAssistant.Application.Services;

using global::KaiAssistant.Application.Interfaces;
using global::KaiAssistant.Domain.Entities;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
public sealed class ConversationSummaryService : IConversationSummaryService
{
    private readonly IAssistantService _assistantService;
    private readonly ITokenBudgetService _tokenBudget;
    private readonly ILogger<ConversationSummaryService> _logger;
    private const int SUMMARIZE_THRESHOLD = 10;  // After 10 messages
    private const int SUMMARY_MAX_LENGTH = 500;
    public ConversationSummaryService(
        IAssistantService assistantService,
        ITokenBudgetService tokenBudget,
        ILogger<ConversationSummaryService> logger)
    {
        _assistantService = assistantService;
        _tokenBudget = tokenBudget;
        _logger = logger;
    }
    public async Task<string> SummarizeAsync(
        IEnumerable<ConversationMessage> messages,
        int maxLength = 500,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
        {
            return string.Empty;
        }
        // Build summarization prompt
        var messagesText = new StringBuilder();
        foreach (var msg in messageList)
        {
            messagesText.AppendLine($"{msg.Role}: {msg.Content}");
        }
        var summaryPrompt = $@"Summarize the following conversation in {maxLength} characters or less.
Include key topics, decisions, and important details.
Be concise and factual.
Conversation:
{messagesText}
Summary:";
        try
        {
            // Call AI to generate summary
            var contextMessages = new[]
            {
                new ConversationMessage
                {
                    Role = "user",
                    Content = summaryPrompt
                }
            };
            var summary = new StringBuilder();
            await foreach (var chunk in _assistantService.StreamAsync(
                summaryPrompt,
                contextMessages,
                null,
                cancellationToken)
                .ConfigureAwait(false))
            {
                summary.Append(chunk.Text);
                if (summary.Length >= maxLength)
                {
                    break;
                }
            }
            var result = summary.ToString()[..Math.Min(maxLength, summary.Length)].Trim();
            _logger.LogInformation(
                "Generated summary: {Length} chars from {MessageCount} messages",
                result.Length,
                messageList.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Summary generation failed, using fallback");
            return GenerateFallbackSummary(messageList, maxLength);
        }
    }
    public async Task<string> GenerateAndUpdateSummaryAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        if (conversation.Messages.Count < SUMMARIZE_THRESHOLD)
        {
            return string.Empty;
        }
        var summary = await SummarizeAsync(
            conversation.Messages,
            SUMMARY_MAX_LENGTH,
            cancellationToken)
            .ConfigureAwait(false);
        conversation.Summary = summary;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "Updated conversation summary: ConversationId={ConversationId}",
            conversation.ConversationId);
        return summary;
    }
    public bool ShouldSummarize(int messageCount, int lastSummaryMessageCount = 0)
    {
        // Summarize when message count exceeds threshold
        // AND after new messages since last summary
        return messageCount >= SUMMARIZE_THRESHOLD &&
               (messageCount - lastSummaryMessageCount) >= (SUMMARIZE_THRESHOLD / 2);
    }
    private static string GenerateFallbackSummary(List<ConversationMessage> messages, int maxLength)
    {
        var summary = new StringBuilder("Conversation summary: ");
        // Extract first user message
        var firstUserMsg = messages.FirstOrDefault(m => m.Role == "user");
        if (firstUserMsg != null)
        {
            var preview = firstUserMsg.Content[..Math.Min(100, firstUserMsg.Content.Length)];
            summary.Append($"User asked about '{preview}'. ");
        }
        // Extract last assistant response summary
        var lastAssistantMsg = messages.LastOrDefault(m => m.Role == "assistant");
        if (lastAssistantMsg != null)
        {
            var preview = lastAssistantMsg.Content[..Math.Min(80, lastAssistantMsg.Content.Length)];
            summary.Append($"Assistant responded with information about '{preview}'.");
        }
        var result = summary.ToString();
        return result.Length > maxLength ? result[..maxLength].Trim() : result;
    }
}