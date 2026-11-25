namespace KaiAssistant.Application.DTOs;

public class ToolExecutionResult
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }
}
public class EnhancedStreamChunk
{
    public enum StreamChunkType
    {
        Start,
        Token,
        ToolCall,
        ToolResult,
        Metadata,
        Error,
        End
    }
    public StreamChunkType Type { get; set; } = StreamChunkType.Token;
    public string? Content { get; set; }
    public ToolCall? ToolCall { get; set; }
    public ToolExecutionResult? ToolResult { get; set; }
    public StreamMetadata? Metadata { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
public class ToolCall
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}
public class StreamMetadata
{
    public long? TtftMs { get; set; }
    public long? LatencyMs { get; set; }
    public bool? CacheHit { get; set; }
    public int? TokensEstimated { get; set; }
    public int? ToolCallsCount { get; set; }
    public int? RagDocumentsCount { get; set; }
}