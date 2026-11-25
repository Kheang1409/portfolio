using System;
using System.Collections.Generic;
namespace KaiAssistant.Application.DTOs;

public sealed class AssistantStreamEvent
{
    public StreamEventType Type { get; set; }
    public string? Content { get; set; }
    public string? Error { get; set; }
    public StreamMetadata? Metadata { get; set; }
    public KaiAssistant.Application.DTOs.ToolCall? ToolCall { get; set; }
    public KaiAssistant.Application.DTOs.ToolExecutionResult? ToolResult { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
public enum StreamEventType
{
    Token,
    Error,
    End,
    Metadata,
    ToolCall,
    ToolResult,
    Start
}