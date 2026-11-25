namespace KaiAssistant.Application.Diagnostics;

public sealed class ResilienceSnapshot
{
    public IReadOnlyList<ResilienceComponentStatus> Components { get; set; } = Array.Empty<ResilienceComponentStatus>();
}