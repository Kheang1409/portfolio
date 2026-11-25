using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Polly;
namespace KaiAssistant.Infrastructure.Mongo;

public sealed class MongoReadProvider : IMongoReadProvider
{
    private readonly ILogger<MongoReadProvider> _logger;
    private readonly AsyncPolicy _retryPolicy;
    public IMongoDatabase Database { get; }
    public MongoReadProvider(IMongoClient mongoClient, MongoSettings settings, ILogger<MongoReadProvider>? logger = null)
    {
        _logger = logger ?? NullLogger<MongoReadProvider>.Instance;
        var readSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        readSettings.ReadPreference = ReadPreference.SecondaryPreferred;
        var readClient = new MongoClient(readSettings);
        Database = readClient.GetDatabase(settings.DatabaseName);
        _retryPolicy = Policy
            .Handle<MongoConnectionException>()
            .Or<MongoExecutionTimeoutException>()
            .WaitAndRetryAsync(
                3,
                retry => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retry)),
                (ex, delay, attempt, _) => _logger.LogWarning(ex, "Mongo read retry {Attempt} after {DelayMs}ms.", attempt, delay.TotalMilliseconds));
    }
    public Task<T> ExecuteReadAsync<T>(Func<IMongoDatabase, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return _retryPolicy.ExecuteAsync(_ => operation(Database), cancellationToken);
    }
    public Task ExecuteReadAsync(Func<IMongoDatabase, Task> operation, CancellationToken cancellationToken = default)
    {
        return _retryPolicy.ExecuteAsync(_ => operation(Database), cancellationToken);
    }
}
public sealed class MongoWriteProvider : IMongoWriteProvider
{
    private readonly ILogger<MongoWriteProvider> _logger;
    private readonly AsyncPolicy _retryPolicy;
    public IMongoDatabase Database { get; }
    public MongoWriteProvider(IMongoDatabase database, ILogger<MongoWriteProvider>? logger = null)
    {
        Database = database;
        _logger = logger ?? NullLogger<MongoWriteProvider>.Instance;
        _retryPolicy = Policy
            .Handle<MongoConnectionException>()
            .Or<MongoExecutionTimeoutException>()
            .WaitAndRetryAsync(
                3,
                retry => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retry)),
                (ex, delay, attempt, _) => _logger.LogWarning(ex, "Mongo write retry {Attempt} after {DelayMs}ms.", attempt, delay.TotalMilliseconds));
    }
    public Task<T> ExecuteWriteAsync<T>(Func<IMongoDatabase, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return _retryPolicy.ExecuteAsync(_ => operation(Database), cancellationToken);
    }
    public Task ExecuteWriteAsync(Func<IMongoDatabase, Task> operation, CancellationToken cancellationToken = default)
    {
        return _retryPolicy.ExecuteAsync(_ => operation(Database), cancellationToken);
    }
}