namespace KaiAssistant.Application.Interfaces;

public interface IOperationalSimulationState
{
    Task<DateTimeOffset?> GetForceAiThrottleUntilUtcAsync(CancellationToken cancellationToken = default);
    Task<int> GetOutboxArtificialDelayMsAsync(CancellationToken cancellationToken = default);
    Task ForceAiThrottleForAsync(TimeSpan duration, CancellationToken cancellationToken = default);
    Task SetOutboxArtificialDelayAsync(int delayMs, CancellationToken cancellationToken = default);
}