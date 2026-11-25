using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Domain.Entities.Outbox;
using KaiAssistant.Infrastructure.Mongo;
using System.Diagnostics.Metrics;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.Persistence;

public sealed class OutboxRepository : IOutboxRepository
{
    private static readonly Meter Meter = new("KaiAssistant.Outbox", "1.0.0");
    private static readonly Counter<long> OutboxProcessed = Meter.CreateCounter<long>("outbox_processed_total");
    private static readonly Counter<long> OutboxFailed = Meter.CreateCounter<long>("outbox_failed_total");
    private static readonly Counter<long> OutboxRetry = Meter.CreateCounter<long>("outbox_retry_total");
    private static readonly Counter<long> OutboxLeaseAcquired = Meter.CreateCounter<long>("outbox_lease_acquired_total");
    private static readonly Counter<long> OutboxLeaseFailed = Meter.CreateCounter<long>("outbox_lease_failed_total");
    private static readonly Counter<long> OutboxDuplicatePrevented = Meter.CreateCounter<long>("outbox_duplicate_prevented_total");
    private readonly IMongoCollection<OutboxMessage> _collection;
    private readonly IMongoReadProvider _readProvider;
    private readonly IMongoWriteProvider _writeProvider;
    public OutboxRepository(IMongoReadProvider readProvider, IMongoWriteProvider writeProvider)
    {
        _readProvider = readProvider;
        _writeProvider = writeProvider;
        _collection = writeProvider.Database.GetCollection<OutboxMessage>("outbox_messages");
    }
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _writeProvider.ExecuteWriteAsync(
                db => db.GetCollection<OutboxMessage>("outbox_messages").InsertOneAsync(message, cancellationToken: cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            OutboxDuplicatePrevented.Add(1, KeyValuePair.Create<string, object?>("operation", "insert"));
        }
    }
    public async Task<IReadOnlyList<OutboxMessage>> LeasePendingAsync(
        int batchSize,
        DateTimeOffset utcNow,
        TimeSpan leaseDuration,
        string instanceId,
        int partitionCount = 1,
        int partitionIndex = 0,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(batchSize, 1, 200);
        var lease = leaseDuration <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : leaseDuration;
        var now = utcNow;
        var eligibleFilter = Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                             Builders<OutboxMessage>.Filter.Lte(x => x.NextAttemptAtUtc, now) &
                             (Builders<OutboxMessage>.Filter.Eq(x => x.LockExpiresAtUtc, null) |
                              Builders<OutboxMessage>.Filter.Lte(x => x.LockExpiresAtUtc, now));
        var candidates = await _readProvider.ExecuteReadAsync(async db =>
        {
            var collection = db.GetCollection<OutboxMessage>("outbox_messages");
            return await collection
                .Find(eligibleFilter)
                .SortBy(x => x.OccurredAtUtc)
                .Limit(Math.Min(limit * 4, 1000))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        if (partitionCount > 1)
        {
            partitionIndex = Math.Clamp(partitionIndex, 0, partitionCount - 1);
            candidates = candidates
                .Where(x => MatchesPartition(x, partitionCount, partitionIndex))
                .ToList();
        }
        var leased = new List<OutboxMessage>(limit);
        foreach (var candidate in candidates)
        {
            if (leased.Count >= limit)
            {
                break;
            }
            if (string.IsNullOrWhiteSpace(candidate.Id))
            {
                continue;
            }
            var claimFilter = Builders<OutboxMessage>.Filter.Eq(x => x.Id, candidate.Id) &
                              Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                              Builders<OutboxMessage>.Filter.Lte(x => x.NextAttemptAtUtc, now) &
                              (Builders<OutboxMessage>.Filter.Eq(x => x.LockExpiresAtUtc, null) |
                               Builders<OutboxMessage>.Filter.Lte(x => x.LockExpiresAtUtc, now));
            var claimUpdate = Builders<OutboxMessage>.Update
                .Set(x => x.LockedBy, instanceId)
                .Set(x => x.LockExpiresAtUtc, now.Add(lease));
            var claimed = await _writeProvider.ExecuteWriteAsync(async db =>
            {
                var collection = db.GetCollection<OutboxMessage>("outbox_messages");
                return await collection
                    .FindOneAndUpdateAsync(
                        claimFilter,
                        claimUpdate,
                        new FindOneAndUpdateOptions<OutboxMessage>
                        {
                            ReturnDocument = ReturnDocument.After
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
            if (claimed is null)
            {
                OutboxLeaseFailed.Add(1);
                continue;
            }
            leased.Add(claimed);
            OutboxLeaseAcquired.Add(1);
        }
        return leased;
    }
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(batchSize, 1, 200);
        var filter = Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                     Builders<OutboxMessage>.Filter.Lte(x => x.NextAttemptAtUtc, utcNow);
        var pending = await _collection
            .Find(filter)
            .SortBy(x => x.OccurredAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return pending;
    }
    public async Task<OutboxMessage?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task MarkProcessedAsync(string id, DateTimeOffset processedAtUtc, string? expectedLockedBy = null, CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxMessage>.Update
            .Set(x => x.ProcessedAtUtc, processedAtUtc)
            .Set(x => x.LastError, null)
            .Set(x => x.LockedBy, null)
            .Set(x => x.LockExpiresAtUtc, null);
        var filter = Builders<OutboxMessage>.Filter.Eq(x => x.Id, id);
        if (!string.IsNullOrWhiteSpace(expectedLockedBy))
        {
            filter &= Builders<OutboxMessage>.Filter.Eq(x => x.LockedBy, expectedLockedBy);
        }
        var result = await _collection
            .UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (result.ModifiedCount == 0 && !string.IsNullOrWhiteSpace(expectedLockedBy))
        {
            OutboxDuplicatePrevented.Add(1, KeyValuePair.Create<string, object?>("operation", "mark_processed"));
            return;
        }
        OutboxProcessed.Add(1);
    }
    public async Task MarkFailedAsync(string id, string error, DateTimeOffset nextAttemptAtUtc, string? expectedLockedBy = null, CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxMessage>.Update
            .Inc(x => x.AttemptCount, 1)
            .Set(x => x.LastError, error)
            .Set(x => x.NextAttemptAtUtc, nextAttemptAtUtc)
            .Set(x => x.LockedBy, null)
            .Set(x => x.LockExpiresAtUtc, null);
        var filter = Builders<OutboxMessage>.Filter.Eq(x => x.Id, id);
        if (!string.IsNullOrWhiteSpace(expectedLockedBy))
        {
            filter &= Builders<OutboxMessage>.Filter.Eq(x => x.LockedBy, expectedLockedBy);
        }
        var result = await _collection
            .UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (result.ModifiedCount == 0 && !string.IsNullOrWhiteSpace(expectedLockedBy))
        {
            OutboxDuplicatePrevented.Add(1, KeyValuePair.Create<string, object?>("operation", "mark_failed"));
            return;
        }
        OutboxFailed.Add(1);
        OutboxRetry.Add(1);
    }
    public async Task<bool> ReplayFailedAsync(string id, DateTimeOffset nextAttemptAtUtc, CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(x => x.Id, id) &
                     Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                     Builders<OutboxMessage>.Filter.Ne(x => x.LastError, null);
        var update = Builders<OutboxMessage>.Update
            .Set(x => x.AttemptCount, 0)
            .Set(x => x.NextAttemptAtUtc, nextAttemptAtUtc)
            .Set(x => x.LastError, null)
            .Set(x => x.LockedBy, null)
            .Set(x => x.LockExpiresAtUtc, null);
        var result = await _collection
            .UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return result.ModifiedCount > 0;
    }
    public async Task<int> ReplayFailedBatchAsync(int batchSize, DateTimeOffset nextAttemptAtUtc, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(batchSize, 1, 500);
        var failedFilter = Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                           Builders<OutboxMessage>.Filter.Ne(x => x.LastError, null);
        var ids = await _collection
            .Find(failedFilter)
            .SortBy(x => x.OccurredAtUtc)
            .Limit(limit)
            .Project(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var nonNullIds = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList();
        if (nonNullIds.Count == 0)
        {
            return 0;
        }
        var update = Builders<OutboxMessage>.Update
            .Set(x => x.AttemptCount, 0)
            .Set(x => x.NextAttemptAtUtc, nextAttemptAtUtc)
            .Set(x => x.LastError, null)
            .Set(x => x.LockedBy, null)
            .Set(x => x.LockExpiresAtUtc, null);
        var updateFilter = Builders<OutboxMessage>.Filter.In(x => x.Id, nonNullIds) & failedFilter;
        var result = await _collection
            .UpdateManyAsync(updateFilter, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return (int)result.ModifiedCount;
    }
    public async Task<bool> DeadLetterAsync(string id, string reason, DateTimeOffset deadLetteredAtUtc, CancellationToken cancellationToken = default)
    {
        var filter = Builders<OutboxMessage>.Filter.Eq(x => x.Id, id) &
                     Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null);
        var update = Builders<OutboxMessage>.Update
            .Set(x => x.ProcessedAtUtc, deadLetteredAtUtc)
            .Set(x => x.NextAttemptAtUtc, deadLetteredAtUtc)
            .Set(x => x.LastError, $"dead-letter:{reason}")
            .Set(x => x.LockedBy, null)
            .Set(x => x.LockExpiresAtUtc, null);
        var result = await _collection
            .UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return result.ModifiedCount > 0;
    }
    public async Task<OutboxStats> GetStatsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        var pendingFilter = Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null);
        var failedFilter = Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                           Builders<OutboxMessage>.Filter.Ne(x => x.LastError, null);
        var lockedFilter = Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                           Builders<OutboxMessage>.Filter.Ne(x => x.LockedBy, null) &
                           Builders<OutboxMessage>.Filter.Gt(x => x.LockExpiresAtUtc, utcNow);
        var expiredLeaseFilter = Builders<OutboxMessage>.Filter.Eq(x => x.ProcessedAtUtc, null) &
                                 Builders<OutboxMessage>.Filter.Ne(x => x.LockedBy, null) &
                                 Builders<OutboxMessage>.Filter.Lte(x => x.LockExpiresAtUtc, utcNow);
        var pendingCountTask = _collection.CountDocumentsAsync(pendingFilter, cancellationToken: cancellationToken);
        var failedCountTask = _collection.CountDocumentsAsync(failedFilter, cancellationToken: cancellationToken);
        var lockedCountTask = _collection.CountDocumentsAsync(lockedFilter, cancellationToken: cancellationToken);
        var expiredLeaseCountTask = _collection.CountDocumentsAsync(expiredLeaseFilter, cancellationToken: cancellationToken);
        var oldestTask = _collection
            .Find(pendingFilter)
            .SortBy(x => x.OccurredAtUtc)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        var retryDataTask = _collection
            .Aggregate()
            .Match(pendingFilter)
            .Group(x => x.AttemptCount, g => new { AttemptCount = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        await Task.WhenAll(pendingCountTask, failedCountTask, lockedCountTask, expiredLeaseCountTask, oldestTask, retryDataTask).ConfigureAwait(false);
        var oldest = oldestTask.Result;
        var oldestAge = oldest is null
            ? (double?)null
            : Math.Max(0, (utcNow - oldest.OccurredAtUtc).TotalSeconds);
        var retryDistribution = retryDataTask.Result.ToDictionary(x => x.AttemptCount, x => (long)x.Count);
        return new OutboxStats
        {
            PendingCount = pendingCountTask.Result,
            FailedCount = failedCountTask.Result,
            LockedCount = lockedCountTask.Result,
            ExpiredLeaseCount = expiredLeaseCountTask.Result,
            OldestUnprocessedAgeSeconds = oldestAge,
            RetryDistribution = retryDistribution
        };
    }
    private static bool MatchesPartition(OutboxMessage message, int partitionCount, int partitionIndex)
    {
        if (partitionCount <= 1)
        {
            return true;
        }
        var hashSource = message.EventId == Guid.Empty
            ? message.Id ?? string.Empty
            : message.EventId.ToString("N");
        if (string.IsNullOrWhiteSpace(hashSource))
        {
            return true;
        }
        var hash = StringComparer.Ordinal.GetHashCode(hashSource);
        var normalized = Math.Abs(hash % partitionCount);
        return normalized == partitionIndex;
    }
}