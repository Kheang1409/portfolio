namespace KaiAssistant.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<IUnitOfWorkSession> StartSessionAsync(CancellationToken cancellationToken = default);
    Task RunInTransactionAsync(Func<IUnitOfWorkSession, Task> operation, CancellationToken cancellationToken = default);
}