using FluentAssertions;
using KaiAssistant.API.Services;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace KaiAssistant.Tests;
#nullable enable
public sealed class RedisResilienceTests
{
    [Fact]
    public async Task RateLimitTelemetry_ShouldFallback_WhenRedisUnavailable()
    {
        var factory = new NullRedisConnectionFactory(isConfigured: true);
        var helper = new RedisExecutionHelper(factory);
        var telemetry = new RateLimitTelemetry(factory, helper, NullLogger<RateLimitTelemetry>.Instance);
        await telemetry.RecordAllowedAsync("10.0.0.1", "/api/test");
        await telemetry.RecordBlockedAsync("10.0.0.1", "/api/test");
        var snapshot = await telemetry.SnapshotAsync(5, 5);
        snapshot.TotalAllowedRequests.Should().Be(1);
        snapshot.TotalBlockedRequests.Should().Be(1);
        snapshot.TopIps.Should().NotBeEmpty();
    }
    [Fact]
    public async Task OperationalSimulationState_ShouldUseMemoryFallback_WhenRedisUnavailable()
    {
        var factory = new NullRedisConnectionFactory(isConfigured: true);
        var helper = new RedisExecutionHelper(factory);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var state = new OperationalSimulationState(factory, helper, memoryCache, NullLogger<OperationalSimulationState>.Instance);
        await state.ForceAiThrottleForAsync(TimeSpan.FromSeconds(10));
        await state.SetOutboxArtificialDelayAsync(2500);
        var throttleUntil = await state.GetForceAiThrottleUntilUtcAsync();
        var delay = await state.GetOutboxArtificialDelayMsAsync();
        throttleUntil.Should().NotBeNull();
        throttleUntil.Should().BeAfter(DateTimeOffset.UtcNow.AddSeconds(5));
        delay.Should().Be(2500);
    }
    [Fact]
    public void RedisConnectionFactory_ShouldThrowInProduction_WhenRedisUrlIsNotRediss()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "redis://localhost:6379"
            })
            .Build();
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var act = () => new RedisConnectionFactory(config, env, NullLogger<RedisConnectionFactory>.Instance);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rediss://*");
    }
    [Fact]
    public void RedisConnectionFactory_ShouldAllowRedisUrlInDevelopment()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:ConnectionString"] = "redis://localhost:6379"
            })
            .Build();
        var env = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var factory = new RedisConnectionFactory(config, env, NullLogger<RedisConnectionFactory>.Instance);
        factory.IsConfigured.Should().BeTrue();
    }
    private sealed class NullRedisConnectionFactory : IRedisConnectionFactory
    {
        public NullRedisConnectionFactory(bool isConfigured)
        {
            IsConfigured = isConfigured;
        }
        public bool IsConfigured { get; }
        public bool IsConnected => false;
        public ValueTask<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IConnectionMultiplexer?>(null);
        }
    }
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "KaiAssistant.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}