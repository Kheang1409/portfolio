using KaiAssistant.Infrastructure.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.Extensions;

public static class MongoServiceCollectionExtensions
{
    public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration)
    {
        MongoClassMapRegistrar.RegisterClassMaps();
        var section = configuration.GetSection("MongoDB");
        string connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTIONSTRING")
                      ?? section["ConnectionString"]
                      ?? throw new ArgumentException("MongoDB setting 'ConnectionString' is missing or empty.");
        string databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE")
                    ?? section["DatabaseName"]
                    ?? throw new ArgumentException("MongoDB setting 'DatabaseName' is missing or empty.");
        var settings = new MongoSettings { ConnectionString = connectionString, DatabaseName = databaseName };
        services.AddSingleton(settings);
        var client = new MongoClient(connectionString);
        services.AddSingleton<IMongoClient>(client);
        services.AddSingleton(sp => client.GetDatabase(databaseName));
        services.AddSingleton<IMongoReadProvider, MongoReadProvider>();
        services.AddSingleton<IMongoWriteProvider, MongoWriteProvider>();
        return services;
    }
}