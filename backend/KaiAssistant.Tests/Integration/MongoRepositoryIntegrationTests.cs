using Mongo2Go;
using MongoDB.Driver;
using KaiAssistant.Infrastructure.Persistence;
using KaiAssistant.Infrastructure.Mongo;
using KaiAssistant.Domain.Entities.Resumes;
using KaiAssistant.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;
namespace KaiAssistant.Tests.Integration;

[Trait("Category", "Integration")]
public class MongoRepositoryIntegrationTests
{
    [Fact]
    public async Task MongoRepository_BasicCrud_Works_With_Session()
    {
        using var runner = MongoTestRunner.Start();
        var client = new MongoClient(runner.ConnectionString);
        var db = client.GetDatabase("testdb");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new MongoSettings { ConnectionString = runner.ConnectionString, DatabaseName = "testdb" });
        services.AddSingleton<IMongoClient>(client);
        services.AddSingleton<IMongoDatabase>(db);
        services.AddSingleton<IMongoReadProvider, MongoReadProvider>();
        services.AddSingleton<IMongoWriteProvider, MongoWriteProvider>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<Resume>>();
        var resume = new Resume { Summary = "Test resume" };
        await repo.InsertAsync(resume);
        var all = await repo.GetAllAsync();
        Assert.NotEmpty(all);
    }
}
