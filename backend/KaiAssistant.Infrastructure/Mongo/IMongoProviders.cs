using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.Mongo;

public interface IMongoReadProvider
{
    IMongoDatabase Database { get; }
    Task<T> ExecuteReadAsync<T>(Func<IMongoDatabase, Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteReadAsync(Func<IMongoDatabase, Task> operation, CancellationToken cancellationToken = default);
}
public interface IMongoWriteProvider
{
    IMongoDatabase Database { get; }
    Task<T> ExecuteWriteAsync<T>(Func<IMongoDatabase, Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteWriteAsync(Func<IMongoDatabase, Task> operation, CancellationToken cancellationToken = default);
}