namespace KaiAssistant.Application.Diagnostics;

public sealed class ResilienceComponentStatus
{
    public string Name { get; set; } = string.Empty;
    public string CircuitState { get; set; } = "Closed";
    public long FailureCount { get; set; }
    public DateTimeOffset? LastFailureAtUtc { get; set; }
    public string? LastFailureReason { get; set; }
}