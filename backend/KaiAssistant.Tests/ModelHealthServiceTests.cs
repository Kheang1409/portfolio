using FluentAssertions;
using KaiAssistant.Application.Options;
using KaiAssistant.Infrastructure.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using Xunit;
namespace KaiAssistant.Tests;

public class ModelHealthServiceTests
{
    [Fact]
    public void RecordUsage_AccumulatesTokenAndCostTotals()
    {
        var monitor = BuildOptions(new AiModelOrchestrationOptions());
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = new ModelHealthService(monitor.Object, provider, NullLogger<ModelHealthService>.Instance);
        var now = DateTimeOffset.UtcNow;
        service.EnsureModelsRegistered(["gemini-a"], now);
        service.RecordSuccess("gemini-a", now, fallbackUsed: false);
        service.RecordUsage("gemini-a", now, inputTokens: 420, outputTokens: 180, estimatedCostUsd: 0.0042m);
        var snapshot = service.GetSnapshot(now);
        var model = snapshot.Models.Single(x => x.ModelName == "gemini-a");
        model.TotalInputTokens.Should().Be(420);
        model.TotalOutputTokens.Should().Be(180);
        model.TotalEstimatedCostUsd.Should().Be(0.0042m);
        snapshot.StateVersion.Should().BeGreaterThan(0);
    }
    [Fact]
    public void Score_ReflectsLatencyFailuresAndCooldowns()
    {
        var options = new AiModelOrchestrationOptions
        {
            LatencyReferenceMs = 1000,
            SuccessRateWeight = 1.4,
            LatencyWeight = 0.8,
            FailureRateWeight = 1.2,
            CooldownWeight = 1.0
        };
        var monitor = BuildOptions(options);
        using var provider = new ServiceCollection().BuildServiceProvider();
        var service = new ModelHealthService(monitor.Object, provider, NullLogger<ModelHealthService>.Instance);
        var now = DateTimeOffset.UtcNow;
        service.EnsureModelsRegistered(["m1"], now);
        service.RecordSuccess("m1", now, fallbackUsed: false);
        service.RecordLatency("m1", now, 120);
        var healthyScore = service.GetSnapshot(now).Models.Single(x => x.ModelName == "m1").DynamicScore;
        service.RecordFailure("m1", now.AddSeconds(1), "timeout");
        service.MarkRateLimited("m1", now.AddSeconds(2), now.AddMinutes(1), "http_429");
        service.RecordLatency("m1", now.AddSeconds(2), 1800);
        var degradedScore = service.GetSnapshot(now.AddSeconds(2)).Models.Single(x => x.ModelName == "m1").DynamicScore;
        degradedScore.Should().BeLessThan(healthyScore);
    }
    private static Mock<IOptionsMonitor<AiModelOrchestrationOptions>> BuildOptions(AiModelOrchestrationOptions value)
    {
        var monitor = new Mock<IOptionsMonitor<AiModelOrchestrationOptions>>();
        monitor.SetupGet(x => x.CurrentValue).Returns(value);
        monitor.Setup(x => x.Get(It.IsAny<string>())).Returns(value);
        return monitor;
    }
}