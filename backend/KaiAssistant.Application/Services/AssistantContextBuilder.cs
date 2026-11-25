using KaiAssistant.Application.Interfaces;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Logging;
namespace KaiAssistant.Application.Services;

public class AssistantContextBuilder : IAssistantContextBuilder
{
    private readonly IConversationRepository _conversationRepo;
    private readonly ITokenBudgetService _tokenBudget;
    private readonly ILogger<AssistantContextBuilder> _logger;
    // Adjust based on model context window (Gemini ~30k tokens)
    private const int MAX_CONTEXT_TOKENS = 6000;
    private const int MAX_HISTORY_MESSAGES = 15;
    public AssistantContextBuilder(
        IConversationRepository conversationRepo,
        ITokenBudgetService tokenBudget,
        ILogger<AssistantContextBuilder> logger)
    {
        _conversationRepo = conversationRepo;
        _tokenBudget = tokenBudget;
        _logger = logger;
    }
    public async Task<ConversationContext> BuildContextAsync(
        string conversationId,
        string userMessage,
        string? userId = null,
        Dictionary<string, object?>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = GetSystemPrompt();
        var contextMessages = new List<ConversationMessage>();
        // Load conversation history
        var conversation = await _conversationRepo.GetByIdAsync(conversationId, cancellationToken)
            .ConfigureAwait(false);
        if (conversation != null)
        {
            // Get sliding window (last N messages)
            var window = conversation.GetContextWindow(MAX_HISTORY_MESSAGES);
            // Trim to token budget, keeping oldest messages first for coherence
            contextMessages = _tokenBudget.TrimContextToTokenBudget(
                window,
                MAX_CONTEXT_TOKENS,
                keepOldest: true);
            _logger.LogInformation(
                "Built context: ConversationId={ConversationId}, HistoryMessages={HistoryCount}, Tokens≈{Tokens}",
                conversationId,
                contextMessages.Count,
                _tokenBudget.CalculateContextTokens(new ConversationContext
                {
                    SystemPrompt = systemPrompt,
                    UserMessage = userMessage,
                    ContextMessages = contextMessages
                }));
        }
        var context = new ConversationContext
        {
            SystemPrompt = systemPrompt,
            UserMessage = userMessage,
            ContextMessages = contextMessages,
            Metadata = metadata ?? new()
        };
        context.EstimatedTokens = _tokenBudget.CalculateContextTokens(context);
        return context;
    }
    public string GetSystemPrompt()
    {
        return @"You are Kai, a thoughtful AI assistant. Your role is to help users with their questions clearly and concisely.
Guidelines:
- Be direct and honest
- Ask clarifying questions if needed
- Provide practical examples
- Acknowledge limitations
- Stay focused on the user's actual question";
    }
}