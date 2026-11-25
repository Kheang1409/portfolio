using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Extensions;
using KaiAssistant.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;
#nullable enable
namespace KaiAssistant.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void ServiceProvider_Resolves_EssentialServices()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["GeminiSettings:ApiKey"] = "test",
            ["GeminiSettings:ModelNames:0"] = "test-model",
            ["GeminiSettings:Endpoint"] = "https://api.test/",
            ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDB:DatabaseName"] = "testdb",
            ["EmailSettings:SmtpServer"] = "smtp.test",
            ["EmailSettings:Port"] = "25",
            ["EmailSettings:SenderEmail"] = "from@test",
            ["EmailSettings:ReceiverEmail"] = "to@test",
            ["EmailSettings:SenderPassword"] = "pass"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
        services.AddApplicationServices();
        services.AddInfrastructure(configuration);
        var provider = services.BuildServiceProvider();
        var assistant = provider.GetService<IAssistantService>();
        assistant.Should().NotBeNull();
        var resumeWriteService = provider.GetService<IResumeWriteService>();
        resumeWriteService.Should().NotBeNull();
        var resumeRepo = provider.GetService<KaiAssistant.Domain.Interfaces.Repositories.IResumeRepository>();
        resumeRepo.Should().NotBeNull();
    }
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "KaiAssistant.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}