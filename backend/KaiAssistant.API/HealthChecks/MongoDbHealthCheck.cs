using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
namespace KaiAssistant.API.HealthChecks;

public sealed class MongoDbHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;
    public MongoDbHealthCheck(IMongoDatabase database)
    {
        _database = database;
    }
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BsonDocument("ping", 1);
            await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("MongoDB connection is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB connection failed.", ex);
        }
    }
}