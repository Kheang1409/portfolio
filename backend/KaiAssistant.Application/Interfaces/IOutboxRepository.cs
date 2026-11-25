using KaiAssistant.Domain.Entities.Outbox;
using KaiAssistant.Application.Diagnostics;
namespace KaiAssistant.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> LeasePendingAsync(
        int batchSize,
        DateTimeOffset utcNow,
        TimeSpan leaseDuration,
        string instanceId,
        int partitionCount = 1,
        int partitionIndex = 0,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, DateTimeOffset utcNow, CancellationToken cancellationToken = default);
    Task<OutboxMessage?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(string id, DateTimeOffset processedAtUtc, string? expectedLockedBy = null, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string id, string error, DateTimeOffset nextAttemptAtUtc, string? expectedLockedBy = null, CancellationToken cancellationToken = default);
    Task<bool> ReplayFailedAsync(string id, DateTimeOffset nextAttemptAtUtc, CancellationToken cancellationToken = default);
    Task<int> ReplayFailedBatchAsync(int batchSize, DateTimeOffset nextAttemptAtUtc, CancellationToken cancellationToken = default);
    Task<bool> DeadLetterAsync(string id, string reason, DateTimeOffset deadLetteredAtUtc, CancellationToken cancellationToken = default);
    Task<OutboxStats> GetStatsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);
}