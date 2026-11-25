namespace KaiAssistant.Application.Interfaces;

public interface ICacheDiagnosticsService
{
    long HitCount { get; }
    long MissCount { get; }
    double HitRatio { get; }
    long RebuildCount { get; }
    double AverageLockWaitMs { get; }
    bool IsRedisConnected { get; }
    Task<long?> GetKeyCountAsync(CancellationToken cancellationToken = default);
}