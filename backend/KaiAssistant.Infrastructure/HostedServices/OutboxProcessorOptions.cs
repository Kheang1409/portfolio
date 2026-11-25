using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "Outbox";
    public bool Enabled { get; set; } = true;
    [Range(1, 500)]
    public int BatchSize { get; set; } = 25;
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; set; } = 5;
    [Range(1, 3600)]
    public int MaxBackoffSeconds { get; set; } = 300;
    [Range(1, 64)]
    public int MaxConcurrency { get; set; } = 4;
    [Range(0, 10_000)]
    public int RetryJitterMs { get; set; } = 250;
    [Range(0, 10_000)]
    public int BackpressureDelayMs { get; set; } = 250;
    [Range(5, 3600)]
    public int LeaseDurationSeconds { get; set; } = 45;
    [Range(1, 256)]
    public int PartitionCount { get; set; } = 1;
    [Range(0, 255)]
    public int PartitionIndex { get; set; } = 0;
    [Range(30, 86400)]
    public int IdempotencyProcessedTtlSeconds { get; set; } = 86400;
    [Range(5, 3600)]
    public int IdempotencyProcessingTtlSeconds { get; set; } = 90;
    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 10;
}