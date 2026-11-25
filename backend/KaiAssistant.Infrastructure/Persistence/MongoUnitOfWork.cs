using KaiAssistant.Application.Interfaces;
using MongoDB.Driver;
using System;
namespace KaiAssistant.Infrastructure.Persistence;

public class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    public MongoUnitOfWork(IMongoClient client, IMongoDatabase database)
    {
        _client = client;
        _database = database;
    }
    public async Task<IUnitOfWorkSession> StartSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await _client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        session.StartTransaction();
        return new MongoUnitOfWorkSession(session);
    }
    public async Task RunInTransactionAsync(Func<IUnitOfWorkSession, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var session = await StartSessionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await operation(session).ConfigureAwait(false);
            await session.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try { await session.AbortAsync(cancellationToken).ConfigureAwait(false); } catch { }
            throw;
        }
    }
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
    private class MongoUnitOfWorkSession : IUnitOfWorkSession
    {
        private readonly IClientSessionHandle _session;
        public MongoUnitOfWorkSession(IClientSessionHandle session) => _session = session;
        public bool TransactionSupported => true;
        public object? NativeSession => _session;
        public async Task CommitAsync(CancellationToken cancellationToken = default) => await _session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        public async Task AbortAsync(CancellationToken cancellationToken = default) => await _session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
        public async ValueTask DisposeAsync() { _session.Dispose(); await Task.CompletedTask; }
    }
}