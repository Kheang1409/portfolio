using FluentAssertions;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities;
using KaiAssistant.Infrastructure.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
namespace KaiAssistant.Tests;

public class ModelOrchestratorTests
{
    [Fact]
    public async Task BuildDecisionAsync_SimplePrompt_PrefersCheaperModel()
    {
        var options = BuildOptions(new AiModelOrchestrationOptions
        {
            SimplePromptCharsThreshold = 200,
            ComplexPromptCharsThreshold = 1200,
            ModelProfiles =
            [
                new AiModelProfile { Name = "fast", Priority = 1, CostWeight = 0.2m, LatencyWeight = 0.4m, MaxTokens = 4096, Enabled = true },
                new AiModelProfile { Name = "smart", Priority = 1, CostWeight = 1.5m, LatencyWeight = 0.8m, MaxTokens = 8192, Enabled = true }
            ]
        });
        var gemini = Options.Create(new GeminiSettings { ModelNames = ["fast", "smart"] });
        var health = new Mock<IModelHealthService>();
        health.Setup(x => x.CanAttempt(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(true);
        health.Setup(x => x.GetSnapshot(It.IsAny<DateTimeOffset>())).Returns(new AiModelHealthSnapshot
        {
            StateVersion = 1,
            Models =
            [
                new AiModelHealthStatus { ModelName = "fast", IsHealthy = true, WeightedSuccessRate = 0.95, DynamicScore = 0.8 },
                new AiModelHealthStatus { ModelName = "smart", IsHealthy = true, WeightedSuccessRate = 0.9, DynamicScore = 0.7 }
            ]
        });
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(options.Object, gemini, health.Object, memory);
        var result = await orchestrator.BuildDecisionAsync("short question", 120, default);
        result.SelectedPrimaryModel.Should().Be("fast");
        result.CandidateModels.Should().ContainInOrder("fast", "smart");
        result.IsComplexRequest.Should().BeFalse();
    }
    [Fact]
    public async Task BuildDecisionAsync_ComplexPrompt_PrefersHigherCapabilityModel()
    {
        var options = BuildOptions(new AiModelOrchestrationOptions
        {
            SimplePromptCharsThreshold = 200,
            ComplexPromptCharsThreshold = 600,
            ModelProfiles =
            [
                new AiModelProfile { Name = "fast", Priority = 1, CostWeight = 0.2m, LatencyWeight = 0.4m, CapabilityScore = 0.2, MaxTokens = 4096, Enabled = true },
                new AiModelProfile { Name = "smart", Priority = 1, CostWeight = 1.5m, LatencyWeight = 0.8m, CapabilityScore = 0.95, MaxTokens = 65536, Enabled = true }
            ]
        });
        var gemini = Options.Create(new GeminiSettings { ModelNames = ["fast", "smart"] });
        var health = new Mock<IModelHealthService>();
        health.Setup(x => x.CanAttempt(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(true);
        health.Setup(x => x.GetSnapshot(It.IsAny<DateTimeOffset>())).Returns(new AiModelHealthSnapshot
        {
            StateVersion = 2,
            Models =
            [
                new AiModelHealthStatus { ModelName = "fast", IsHealthy = true, WeightedSuccessRate = 0.85, DynamicScore = 0.55, AverageLatencyMs = 500 },
                new AiModelHealthStatus { ModelName = "smart", IsHealthy = true, WeightedSuccessRate = 0.9, DynamicScore = 0.8, AverageLatencyMs = 800 }
            ]
        });
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(options.Object, gemini, health.Object, memory);
        var result = await orchestrator.BuildDecisionAsync(new string('x', 2000), 3000, default);
        result.SelectedPrimaryModel.Should().Be("smart");
        result.IsComplexRequest.Should().BeTrue();
    }
    [Fact]
    public async Task BuildDecisionAsync_SkipsUnhealthyModels()
    {
        var options = BuildOptions(new AiModelOrchestrationOptions
        {
            ModelProfiles =
            [
                new AiModelProfile { Name = "primary", Priority = 1, CostWeight = 0.5m, LatencyWeight = 0.5m, MaxTokens = 4096, Enabled = true },
                new AiModelProfile { Name = "fallback", Priority = 2, CostWeight = 1.0m, LatencyWeight = 0.8m, MaxTokens = 8192, Enabled = true }
            ]
        });
        var gemini = Options.Create(new GeminiSettings { ModelNames = ["primary", "fallback"] });
        var health = new Mock<IModelHealthService>();
        health.Setup(x => x.CanAttempt("primary", It.IsAny<DateTimeOffset>())).Returns(false);
        health.Setup(x => x.CanAttempt("fallback", It.IsAny<DateTimeOffset>())).Returns(true);
        health.Setup(x => x.GetSnapshot(It.IsAny<DateTimeOffset>())).Returns(new AiModelHealthSnapshot
        {
            StateVersion = 3,
            Models =
            [
                new AiModelHealthStatus { ModelName = "primary", IsHealthy = false, WeightedSuccessRate = 0.6, DynamicScore = 0.2 },
                new AiModelHealthStatus { ModelName = "fallback", IsHealthy = true, WeightedSuccessRate = 0.9, DynamicScore = 0.8 }
            ]
        });
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(options.Object, gemini, health.Object, memory);
        var result = await orchestrator.BuildDecisionAsync("question", 100, default);
        result.SelectedPrimaryModel.Should().Be("fallback");
        result.CandidateModels.Should().ContainSingle().Which.Should().Be("fallback");
    }
    [Fact]
    public async Task BuildDecisionAsync_UsesDecisionCache_WhenStateVersionUnchanged()
    {
        var options = BuildOptions(new AiModelOrchestrationOptions
        {
            DecisionCacheSeconds = 30,
            ModelProfiles =
            [
                new AiModelProfile { Name = "m1", Priority = 1, CapabilityScore = 0.5, InputCostPer1KTokensUsd = 0.001m, OutputCostPer1KTokensUsd = 0.002m, Enabled = true }
            ]
        });
        var gemini = Options.Create(new GeminiSettings { ModelNames = ["m1"] });
        var health = new Mock<IModelHealthService>();
        health.Setup(x => x.CanAttempt(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(true);
        health.Setup(x => x.GetSnapshot(It.IsAny<DateTimeOffset>())).Returns(new AiModelHealthSnapshot
        {
            StateVersion = 11,
            Models = [new AiModelHealthStatus { ModelName = "m1", IsHealthy = true, WeightedSuccessRate = 0.95, DynamicScore = 0.9 }]
        });
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(options.Object, gemini, health.Object, memory);
        var first = await orchestrator.BuildDecisionAsync("hello", 80, default);
        first.FromCache.Should().BeFalse();
        var second = await orchestrator.BuildDecisionAsync("hello", 80, default);
        second.FromCache.Should().BeTrue();
        second.SelectedPrimaryModel.Should().Be("m1");
    }
    [Fact]
    public async Task BuildDecisionAsync_RespectsMaxEstimatedCostPerRequest()
    {
        var options = BuildOptions(new AiModelOrchestrationOptions
        {
            MaxEstimatedCostPerRequestUsd = 0.005m,
            ModelProfiles =
            [
                new AiModelProfile { Name = "expensive", Priority = 1, CapabilityScore = 0.9, InputCostPer1KTokensUsd = 0.02m, OutputCostPer1KTokensUsd = 0.03m, Enabled = true },
                new AiModelProfile { Name = "cheap", Priority = 2, CapabilityScore = 0.5, InputCostPer1KTokensUsd = 0.001m, OutputCostPer1KTokensUsd = 0.002m, Enabled = true }
            ]
        });
        var gemini = Options.Create(new GeminiSettings { ModelNames = ["expensive", "cheap"] });
        var health = new Mock<IModelHealthService>();
        health.Setup(x => x.CanAttempt(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).Returns(true);
        health.Setup(x => x.GetSnapshot(It.IsAny<DateTimeOffset>())).Returns(new AiModelHealthSnapshot
        {
            StateVersion = 4,
            Models =
            [
                new AiModelHealthStatus { ModelName = "expensive", IsHealthy = true, WeightedSuccessRate = 0.99, DynamicScore = 0.99 },
                new AiModelHealthStatus { ModelName = "cheap", IsHealthy = true, WeightedSuccessRate = 0.90, DynamicScore = 0.80 }
            ]
        });
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(options.Object, gemini, health.Object, memory);
        var result = await orchestrator.BuildDecisionAsync(new string('q', 400), 1200, default);
        result.SelectedPrimaryModel.Should().Be("cheap");
        result.CandidateModels.Should().Contain("cheap");
    }
    [Fact]
    public async Task BuildDecisionAsync_RepeatedFailures_ShiftsToFallbackModel()
    {
        var options = new AiModelOrchestrationOptions
        {
            DecisionCacheSeconds = 1,
            CircuitFailureThreshold = 2,
            CircuitOpenSeconds = 60,
            ModelProfiles =
            [
                new AiModelProfile { Name = "primary", Priority = 1, CapabilityScore = 0.9, InputCostPer1KTokensUsd = 0.001m, OutputCostPer1KTokensUsd = 0.002m, Enabled = true },
                new AiModelProfile { Name = "fallback", Priority = 2, CapabilityScore = 0.6, InputCostPer1KTokensUsd = 0.0012m, OutputCostPer1KTokensUsd = 0.0024m, Enabled = true }
            ]
        };
        var monitor = BuildOptions(options);
        var gemini = Options.Create(new GeminiSettings { ModelNames = ["primary", "fallback"] });
        using var provider = new ServiceCollection().BuildServiceProvider();
        var health = new ModelHealthService(monitor.Object, provider, NullLogger<ModelHealthService>.Instance);
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(monitor.Object, gemini, health, memory);
        var now = DateTimeOffset.UtcNow;
        health.EnsureModelsRegistered(["primary", "fallback"], now);
        health.RecordSuccess("primary", now, fallbackUsed: false);
        health.RecordSuccess("fallback", now, fallbackUsed: false);
        var initial = await orchestrator.BuildDecisionAsync("hello", 120, default);
        initial.SelectedPrimaryModel.Should().Be("primary");
        health.RecordFailure("primary", now.AddSeconds(1), "simulated_1");
        health.RecordFailure("primary", now.AddSeconds(2), "simulated_2");
        var afterFailures = await orchestrator.BuildDecisionAsync("hello", 120, default);
        afterFailures.SelectedPrimaryModel.Should().Be("fallback");
    }
    [Fact]
    public async Task BuildDecisionAsync_HighCostPressure_PrefersCheaperModel()
    {
        var options = new AiModelOrchestrationOptions
        {
            MaxEstimatedCostPerRequestUsd = 0.003m,
            ModelProfiles =
            [
                new AiModelProfile { Name = "premium", Priority = 1, CapabilityScore = 0.95, InputCostPer1KTokensUsd = 0.015m, OutputCostPer1KTokensUsd = 0.03m, Enabled = true },
                new AiModelProfile { Name = "economy", Priority = 2, CapabilityScore = 0.5, InputCostPer1KTokensUsd = 0.0008m, OutputCostPer1KTokensUsd = 0.0012m, Enabled = true }
            ]
        };
        var monitor = BuildOptions(options);
        var gemini = Options.Create(new GeminiSettings { ModelNames = ["premium", "economy"] });
        using var provider = new ServiceCollection().BuildServiceProvider();
        var health = new ModelHealthService(monitor.Object, provider, NullLogger<ModelHealthService>.Instance);
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(monitor.Object, gemini, health, memory);
        var now = DateTimeOffset.UtcNow;
        health.EnsureModelsRegistered(["premium", "economy"], now);
        health.RecordSuccess("premium", now, fallbackUsed: false);
        health.RecordSuccess("economy", now, fallbackUsed: false);
        var decision = await orchestrator.BuildDecisionAsync(new string('x', 2200), 1800, default);
        decision.SelectedPrimaryModel.Should().Be("economy");
    }
    private static Mock<IOptionsMonitor<AiModelOrchestrationOptions>> BuildOptions(AiModelOrchestrationOptions value)
    {
        var monitor = new Mock<IOptionsMonitor<AiModelOrchestrationOptions>>();
        monitor.SetupGet(x => x.CurrentValue).Returns(value);
        monitor.Setup(x => x.Get(It.IsAny<string>())).Returns(value);
        return monitor;
    }
}