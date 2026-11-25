using System.Diagnostics;
using System.Runtime.CompilerServices;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.DTOs;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Logging;
namespace KaiAssistant.Application.Services;

public class AssistantOrchestrator : IAssistantOrchestrator
{
    private readonly IAssistantService _assistantService;
    private readonly IAssistantContextBuilder _contextBuilder;
    private readonly IConversationRepository _conversationRepo;
    private readonly IPromptSecurityService _promptSecurity;
    private readonly ILogger<AssistantOrchestrator> _logger;
    private long _lastLatencyMs;
    private int _activeStreamCount;
    private DateTimeOffset _lastAiCallAt = DateTimeOffset.UtcNow;
    public AssistantOrchestrator(
        IAssistantService assistantService,
        IAssistantContextBuilder contextBuilder,
        IConversationRepository conversationRepo,
        IPromptSecurityService promptSecurity,
        ILogger<AssistantOrchestrator> logger)
    {
        _assistantService = assistantService;
        _contextBuilder = contextBuilder;
        _conversationRepo = conversationRepo;
        _promptSecurity = promptSecurity;
        _logger = logger;
    }
    public async IAsyncEnumerable<AssistantStreamEvent> OrchestrateStreamAsync(
        string userMessage,
        string conversationId,
        string? userId = null,
        [EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        _activeStreamCount++;
        var terminalEmitted = false;
        try
        {
            _logger.LogInformation(
                "Orchestrate: RequestId={RequestId}, ConversationId={ConversationId}, MessageLen={Len}",
                requestId, conversationId, userMessage.Length);
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                yield return new AssistantStreamEvent { Type = StreamEventType.Error, Error = "User message cannot be empty" };
                yield break;
            }
            if (userMessage.Length > 10000)
            {
                yield return new AssistantStreamEvent { Type = StreamEventType.Error, Error = "Message too long (max 10000 chars)" };
                yield break;
            }
            // 2. Prompt security: sanitize and check for injection
            var sanitizedMessage = _promptSecurity.SanitizeInput(userMessage);
            if (_promptSecurity.LooksLikePromptInjection(sanitizedMessage))
            {
                _logger.LogWarning(
                    "Prompt injection detected: RequestId={RequestId}, ConversationId={ConversationId}",
                    requestId, conversationId);
                yield return new AssistantStreamEvent
                {
                    Type = StreamEventType.Error,
                    Error = "Invalid input detected. Please rephrase your request."
                };
                yield break;
            }
            // 2. Build composed context (using sanitized message)
            var context = await _contextBuilder.BuildContextAsync(
                conversationId,
                sanitizedMessage,
                userId,
                null,
                cancellationToken)
                .ConfigureAwait(false);
            // Response caching happens in AssistantService after retrieval so its key includes selected evidence.
            // 3. Stream from AI, collecting response
            var stopwatch = Stopwatch.StartNew();
            var responseBuilder = new System.Text.StringBuilder();
            var tokenCount = 0;
            _lastAiCallAt = DateTimeOffset.UtcNow;
            await foreach (var chunk in _assistantService.StreamAsync(
                sanitizedMessage,
                context.ContextMessages.ToArray(),
                new AssistantContext { },
                cancellationToken)
                .ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _promptSecurity.SanitizeOutputStreaming(string.Empty);
                    yield return new AssistantStreamEvent { Type = StreamEventType.Error, Error = "request cancelled" };
                    terminalEmitted = true;
                    yield break;
                }
                // Sanitize output before sending to client (stateful streaming sanitization)
                var sanitizedOutput = _promptSecurity.SanitizeOutputStreaming(chunk.Text ?? string.Empty);
                if (!string.IsNullOrEmpty(sanitizedOutput))
                {
                    yield return new AssistantStreamEvent { Type = StreamEventType.Token, Content = sanitizedOutput };
                    responseBuilder.Append(sanitizedOutput);
                }
                tokenCount++;
            }
            stopwatch.Stop();
            _lastLatencyMs = stopwatch.ElapsedMilliseconds;
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            if (responseBuilder.Length > 0)
            {
                var fullResponse = responseBuilder.ToString();
                var conversation = await _conversationRepo.GetByIdAsync(conversationId, cancellationToken)
                    .ConfigureAwait(false);
                if (conversation == null && userId != null)
                {
                    conversation = await _conversationRepo.CreateAsync(userId, cancellationToken)
                        .ConfigureAwait(false);
                    conversationId = conversation.ConversationId;
                }
                if (conversation != null)
                {
                    conversation.Messages.Add(new ConversationMessage
                    {
                        Role = "user",
                        Content = userMessage
                    });
                    conversation.Messages.Add(new ConversationMessage
                    {
                        Role = "assistant",
                        Content = fullResponse
                    });
                    conversation.UpdatedAt = DateTimeOffset.UtcNow;
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await _conversationRepo.UpdateAsync(conversation, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            if (!terminalEmitted)
            {
                _promptSecurity.SanitizeOutputStreaming(string.Empty);
                yield return new AssistantStreamEvent { Type = StreamEventType.End };
            }
            _logger.LogInformation(
                "Orchestrate complete: RequestId={RequestId}, Latency={Latency}ms, Tokens={Tokens}",
                requestId, _lastLatencyMs, tokenCount);
        }
        finally
        {
            _activeStreamCount--;
        }
    }
    public async Task<OrchestratorDiagnostics> GetDiagnosticsAsync(
        System.Threading.CancellationToken cancellationToken = default)
    {
        return new OrchestratorDiagnostics
        {
            LastLatencyMs = _lastLatencyMs,
            ActiveStreamCount = _activeStreamCount,
            LastAiCallAt = _lastAiCallAt
        };
    }
}
