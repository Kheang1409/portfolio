namespace KaiAssistant.Application.Diagnostics;

public sealed class OutboxProcessorSnapshot
{
    public string InstanceId { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public DateTimeOffset? LastCycleStartedAtUtc { get; set; }
    public DateTimeOffset? LastSuccessAtUtc { get; set; }
    public string? LastError { get; set; }
}