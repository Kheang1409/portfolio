namespace KaiAssistant.Application.Interfaces;

using KaiAssistant.Application.DTOs;
public interface IAssistantOrchestrator
{
    IAsyncEnumerable<AssistantStreamEvent> OrchestrateStreamAsync(
        string userMessage,
        string conversationId,
        string? userId = null,
        CancellationToken cancellationToken = default);
    Task<OrchestratorDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
}
public class OrchestratorDiagnostics
{
    public long LastLatencyMs { get; set; }
    public int ActiveStreamCount { get; set; }
    public DateTimeOffset LastAiCallAt { get; set; }
}