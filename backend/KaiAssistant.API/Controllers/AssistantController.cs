using Microsoft.AspNetCore.Mvc;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.DTOs;
using KaiAssistant.Application.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace KaiAssistant.API.Controllers;

[ApiController]
[Route("/api/assistant")]
public class AssistantController : ControllerBase
{
    private static readonly System.Text.Json.JsonSerializerOptions StreamJsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private readonly IAssistantOrchestrator _orchestrator;
    private readonly ILogger<AssistantController> _logger;
    private readonly KaiAssistant.Application.Interfaces.IConversationRepository _conversationRepo;
    public AssistantController(
        IAssistantOrchestrator orchestrator,
        ILogger<AssistantController> logger,
        KaiAssistant.Application.Interfaces.IConversationRepository conversationRepo)
    {
        _orchestrator = orchestrator;
        _logger = logger;
        _conversationRepo = conversationRepo;
    }
    [HttpPost]
    public async Task Stream([FromBody] AssistantRequestDto dto, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { message = "Invalid request payload." }, cancellationToken)
                .ConfigureAwait(false);
            return;
        }
        // Resolve conversation id: prefer explicit ConversationId, else try sessionId from context metadata
        var conversationId = dto.ConversationId;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            var sessionId = dto.Context?.Metadata != null && dto.Context.Metadata.TryGetValue("sessionId", out var sid)
                ? sid
                : null;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                // Try to find an existing conversation for this session (stored as UserId)
                var list = await _conversationRepo.GetByUserIdAsync(sessionId, 1, cancellationToken).ConfigureAwait(false);
                if (list != null && list.Count > 0)
                {
                    conversationId = list[0].ConversationId;
                }
                else
                {
                    var created = await _conversationRepo.CreateAsync(sessionId, cancellationToken).ConfigureAwait(false);
                    conversationId = created.ConversationId;
                }
            }
            else
            {
                conversationId = Guid.NewGuid().ToString("N");
            }
        }
        var userId = HttpContext.User?.FindFirst("sub")?.Value; // From JWT claims
        _logger.LogInformation(
            "Stream start: RequestId={RequestId}, ConversationId={ConversationId}, UserMessage={Len} chars",
            requestId, conversationId, dto.Message?.Length ?? 0);
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson";
        Response.Headers.CacheControl = "no-cache";
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, HttpContext.RequestAborted);
        try
        {
            await foreach (var evt in _orchestrator.OrchestrateStreamAsync(
                dto.Message ?? string.Empty,
                conversationId,
                userId,
                linkedCts.Token)
                .ConfigureAwait(false))
            {
                var json = SerializeStreamEvent(evt, requestId);
                await WriteStreamLineAsync(json, linkedCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            _logger.LogInformation("Stream cancelled: RequestId={RequestId}", requestId);
            // Ensure terminal event
            var cancelJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "error",
                messageId = requestId,
                errorCode = "REQUEST_ABORTED",
                retryable = false,
                errorMessage = "Request cancelled."
            }, StreamJsonOptions);
            await WriteStreamLineAsync(cancelJson, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stream error: RequestId={RequestId}", requestId);
            var err = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "error",
                messageId = requestId,
                errorCode = "STREAM_FAILED",
                retryable = true,
                errorMessage = "Streaming failed. Please try again."
            }, StreamJsonOptions);
            await WriteStreamLineAsync(err, CancellationToken.None).ConfigureAwait(false);
        }
        _logger.LogInformation("Stream complete: RequestId={RequestId}", requestId);
    }

    private static string SerializeStreamEvent(AssistantStreamEvent evt, string messageId)
    {
        object payload = evt.Type switch
        {
            StreamEventType.Token => new
            {
                type = "delta",
                messageId,
                text = evt.Content
            },
            StreamEventType.End => new
            {
                type = "completed",
                messageId
            },
            StreamEventType.Error => new
            {
                type = "error",
                messageId,
                errorCode = "STREAM_ERROR",
                retryable = false,
                errorMessage = evt.Error ?? "Streaming failed."
            },
            StreamEventType.Start => new
            {
                type = "delta",
                messageId,
                text = string.Empty
            },
            StreamEventType.Metadata => new
            {
                type = "delta",
                messageId,
                text = string.Empty
            },
            StreamEventType.ToolCall => new
            {
                type = "delta",
                messageId,
                text = evt.Content ?? string.Empty
            },
            StreamEventType.ToolResult => new
            {
                type = "delta",
                messageId,
                text = evt.Content ?? string.Empty
            },
            _ => new
            {
                type = "error",
                messageId,
                errorCode = "UNSUPPORTED_STREAM_EVENT",
                retryable = false,
                errorMessage = "Unsupported stream event."
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(payload, StreamJsonOptions);
    }

    private async Task WriteStreamLineAsync(string chunk, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(chunk);
        await Response.Body.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("\n"), cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}