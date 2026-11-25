using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KaiAssistant.Application.AI;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Infrastructure.AI;
using KaiAssistant.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
namespace KaiAssistant.Tests;

public class AiOrchestratorServiceTests
{
    [Fact]
    public async Task GenerateAsync_ShouldFallbackToSecondary_WhenPrimaryFails()
    {
        var primary = new TestProvider("gemini", _ => throw new InvalidOperationException("boom"));
        var secondary = new TestProvider("openai", _ => Task.FromResult(new AiResponse
        {
            Content = "fallback",
            ModelUsed = "openai-model",
            LatencyMs = 5,
            FallbackUsed = false
        }));
        var options = Options.Create(new AiOrchestrationOptions
        {
            PrimaryProvider = "gemini",
            SecondaryProvider = "openai",
            ProviderTimeoutMs = 2000
        });
        var service = new AiOrchestratorService(
            new IAiProvider[] { primary, secondary },
            new StaticOptionsMonitor<AiOrchestrationOptions>(options.Value),
            new TestFeatureFlags(),
            NullLogger<AiOrchestratorService>.Instance,
            new TestObservabilityService());
        var result = await service.GenerateAsync(new AiRequest { Prompt = "hello" });
        result.Content.Should().Be("fallback");
        result.FallbackUsed.Should().BeTrue();
        result.ModelUsed.Should().Be("openai-model");
    }
    [Fact]
    public async Task GenerateAsync_ShouldHandleConcurrentRequestsWithoutCrossTalk()
    {
        var primary = new TestProvider("gemini", request => Task.FromResult(new AiResponse
        {
            Content = "primary:" + request.Prompt,
            ModelUsed = "gemini-model",
            LatencyMs = 2
        }));
        var options = Options.Create(new AiOrchestrationOptions
        {
            PrimaryProvider = "gemini",
            SecondaryProvider = "openai",
            ProviderTimeoutMs = 2000
        });
        var service = new AiOrchestratorService(
            new IAiProvider[] { primary },
            new StaticOptionsMonitor<AiOrchestrationOptions>(options.Value),
            new TestFeatureFlags(),
            NullLogger<AiOrchestratorService>.Instance,
            new TestObservabilityService());
        var tasks = Enumerable.Range(1, 64)
            .Select(i => service.GenerateAsync(new AiRequest { Prompt = "q-" + i }))
            .ToArray();
        var results = await Task.WhenAll(tasks);
        results.Should().HaveCount(64);
        results.Select(x => x.Content).Distinct().Should().HaveCount(64);
        results.All(x => x.ModelUsed == "gemini-model").Should().BeTrue();
    }
    [Fact]
    public async Task GenerateAsync_ShouldFallbackWhenPrimaryTimesOut()
    {
        var primary = new TestProvider("gemini", _ =>
            Task.FromException<AiResponse>(new TimeoutException("simulated-timeout")));
        var secondary = new TestProvider("openai", _ => Task.FromResult(new AiResponse
        {
            Content = "fallback-timeout",
            ModelUsed = "openai-model",
            LatencyMs = 7
        }));
        var options = Options.Create(new AiOrchestrationOptions
        {
            PrimaryProvider = "gemini",
            SecondaryProvider = "openai",
            ProviderTimeoutMs = 250
        });
        var service = new AiOrchestratorService(
            new IAiProvider[] { primary, secondary },
            new StaticOptionsMonitor<AiOrchestrationOptions>(options.Value),
            new TestFeatureFlags(),
            NullLogger<AiOrchestratorService>.Instance,
            new TestObservabilityService());
        var result = await service.GenerateAsync(new AiRequest { Prompt = "timeout" });
        result.Content.Should().Be("fallback-timeout");
        result.FallbackUsed.Should().BeTrue();
        result.ModelUsed.Should().Be("openai-model");
    }
    private sealed class TestProvider : IAiProvider
    {
        private readonly Func<AiRequest, Task<AiResponse>> _handler;
        public TestProvider(string name, Func<AiRequest, Task<AiResponse>> handler)
        {
            ProviderName = name;
            _handler = handler;
        }
        public string ProviderName { get; }
        public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
    private sealed class TestFeatureFlags : IFeatureFlagService
    {
        public bool EnableOutboxProcessing => false;
        public bool EnableOutboxRecovery => false;
        public bool EnableAiResponseCache => false;
        public bool EnableAssistantBatching => false;
        public bool EnableCache => false;
        public bool EnableRateLimiting => false;
        public bool EnableStreaming => true;
        public bool EnableSemanticCaching => true;
        public bool EnableRag => true;
        public bool EnableConversationMemory => true;
        public string PreferredAiProvider => "gemini";
        public Task<bool> SetFlagAsync(string name, string value, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }
    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public StaticOptionsMonitor(T value)
        {
            CurrentValue = value;
        }
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;
        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose()
            {
            }
        }
    }
    private sealed class TestObservabilityService : IObservabilityService
    {
        public void RecordAiCallMetric(long latencyMs, bool success = true, bool fallbackUsed = false, bool isTimeout = false) { }
        public void RecordCacheMetric(bool hit) { }
        public void RecordRateLimitViolation() { }
        public System.Collections.Generic.IEnumerable<long> GetRecentLatencies(int count = 100) => [];
        public Task<SystemObservabilityData> GetDashboardAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new SystemObservabilityData());
    }
}