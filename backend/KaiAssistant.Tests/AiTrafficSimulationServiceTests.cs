using FluentAssertions;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities;
using KaiAssistant.Infrastructure.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
namespace KaiAssistant.Tests;

public class AiTrafficSimulationServiceTests
{
    [Fact]
    public async Task RunOnceAsync_CapturesSnapshotsAndBuildsReport()
    {
        var options = new AiModelOrchestrationOptions
        {
            EnableFailureSimulation = true,
            FailureInjectionRate = 0.2,
            SimulationRequestCount = 90,
            MaxEstimatedCostPerRequestUsd = 0.04m,
            MaxCostPerMinute = 2.0m,
            ModelProfiles =
            [
                new AiModelProfile
                {
                    Name = "fast-cheap",
                    Priority = 1,
                    CapabilityScore = 0.4,
                    InputCostPer1KTokensUsd = 0.0008m,
                    OutputCostPer1KTokensUsd = 0.0012m,
                    Enabled = true
                },
                new AiModelProfile
                {
                    Name = "smart-expensive",
                    Priority = 2,
                    CapabilityScore = 0.9,
                    InputCostPer1KTokensUsd = 0.004m,
                    OutputCostPer1KTokensUsd = 0.006m,
                    Enabled = true
                }
            ]
        };
        var monitor = BuildOptions(options);
        var gemini = Options.Create(new GeminiSettings
        {
            ModelNames = ["fast-cheap", "smart-expensive"]
        });
        using var provider = new ServiceCollection().BuildServiceProvider();
        var health = new ModelHealthService(monitor.Object, provider, NullLogger<ModelHealthService>.Instance);
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var orchestrator = new ModelOrchestrator(
            monitor.Object,
            gemini,
            health,
            memory,
            new AiTuningState(),
            new AiDecisionAuditStore());
        var simulator = new AiTrafficSimulationService(orchestrator, health, monitor.Object, NullLogger<AiTrafficSimulationService>.Instance);
        var report = await simulator.RunOnceAsync(90, default);
        report.RequestCount.Should().Be(90);
        report.SuccessCount.Should().BeGreaterThan(0);
        report.FailureCount.Should().BeGreaterThan(0);
        report.AverageDecisionLatencyMs.Should().BeGreaterThan(0);
        report.ThroughputRequestsPerSecond.Should().BeGreaterThan(0);
        var snapshots = simulator.GetSnapshots(50);
        snapshots.Should().NotBeEmpty();
        var afterSnapshot = health.GetSnapshot(DateTimeOffset.UtcNow);
        afterSnapshot.Models.Should().NotBeEmpty();
        afterSnapshot.Models.Sum(x => x.TotalInputTokens).Should().BeGreaterThan(0);
    }
    private static Mock<IOptionsMonitor<AiModelOrchestrationOptions>> BuildOptions(AiModelOrchestrationOptions value)
    {
        var monitor = new Mock<IOptionsMonitor<AiModelOrchestrationOptions>>();
        monitor.SetupGet(x => x.CurrentValue).Returns(value);
        monitor.Setup(x => x.Get(It.IsAny<string>())).Returns(value);
        return monitor;
    }
}