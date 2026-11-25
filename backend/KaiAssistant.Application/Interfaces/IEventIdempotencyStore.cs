namespace KaiAssistant.Application.Interfaces;

public interface IEventIdempotencyStore
{
    Task<IdempotencyAcquireResult> TryAcquireAsync(string idempotencyKey, TimeSpan processingTtl, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(string idempotencyKey, TimeSpan processedTtl, CancellationToken cancellationToken = default);
    Task<bool> IsProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task ReleaseAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
public enum IdempotencyAcquireResult
{
    Acquired = 0,
    AlreadyProcessed = 1,
    Busy = 2
}