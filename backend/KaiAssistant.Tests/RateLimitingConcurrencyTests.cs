using KaiAssistant.API.Middleware;
using KaiAssistant.API.Options;
using KaiAssistant.API.Services;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace KaiAssistant.Tests;

public sealed class RateLimitingConcurrencyTests
{
    [Fact]
    public async Task InMemoryFallbackLimiter_ShouldThrottleUnderConcurrentLoad()
    {
        var redisFactory = new NoRedisFactory();
        var redisHelper = new RedisExecutionHelper(redisFactory);
        var options = Options.Create(new RateLimitingOptions
        {
            PermitLimit = 3,
            WindowSeconds = 30,
            BurstMultiplier = 1,
            StrictDistributedMode = false,
            UseSlidingWindow = false,
            PathPrefixes = ["/api"],
            EndpointRules =
            [
                new EndpointRateLimitRule
                {
                    Name = "ask",
                    PathContains = "/ask",
                    PermitLimit = 3,
                    WindowSeconds = 30
                }
            ]
        });
        var middleware = new RedisRateLimitingMiddleware(
            _ => Task.CompletedTask,
            redisFactory,
            redisHelper,
            new MemoryCache(new MemoryCacheOptions()),
            options,
            new EnabledFlags(),
            new NoopTelemetry(),
            NullLogger<RedisRateLimitingMiddleware>.Instance);
        var tasks = Enumerable.Range(0, 10).Select(_ => InvokeOnceAsync(middleware)).ToArray();
        var statuses = await Task.WhenAll(tasks);
        Assert.Contains(StatusCodes.Status429TooManyRequests, statuses);
        Assert.True(statuses.Count(x => x == StatusCodes.Status200OK) <= 3);
    }
    private static async Task<int> InvokeOnceAsync(RedisRateLimitingMiddleware middleware)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/assistants/ask";
        context.Request.Headers["X-Api-Key"] = "load-test-client";
        await middleware.InvokeAsync(context);
        return context.Response.StatusCode;
    }
    private sealed class NoRedisFactory : IRedisConnectionFactory
    {
        public bool IsConfigured => false;
        public bool IsConnected => false;
        public ValueTask<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IConnectionMultiplexer?>(null);
    }
    private sealed class EnabledFlags : IFeatureFlagService
    {
        public bool EnableOutboxProcessing => false;
        public bool EnableOutboxRecovery => false;
        public bool EnableAiResponseCache => false;
        public bool EnableAssistantBatching => true;
        public bool EnableCache => false;
        public bool EnableRateLimiting => true;
        public bool EnableStreaming => true;
        public bool EnableSemanticCaching => true;
        public bool EnableRag => true;
        public bool EnableConversationMemory => true;
        public string PreferredAiProvider => "gemini";
        public Task<bool> SetFlagAsync(string name, string value, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
        public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }
    private sealed class NoopTelemetry : IRateLimitTelemetry
    {
        public Task RecordAllowedAsync(string ip, string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordBlockedAsync(string ip, string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<RateLimitTelemetrySnapshot> SnapshotAsync(int minutes, int top, CancellationToken cancellationToken = default)
            => Task.FromResult(new RateLimitTelemetrySnapshot());
    }
}