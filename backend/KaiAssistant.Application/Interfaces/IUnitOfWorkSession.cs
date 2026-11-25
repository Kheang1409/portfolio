namespace KaiAssistant.Application.Interfaces;

public interface IUnitOfWorkSession : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task AbortAsync(CancellationToken cancellationToken = default);
    bool TransactionSupported { get; }
    object? NativeSession { get; }
}