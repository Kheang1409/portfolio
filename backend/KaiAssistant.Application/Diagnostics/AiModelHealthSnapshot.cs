namespace KaiAssistant.Application.Diagnostics;

public sealed class AiModelHealthSnapshot
{
    public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ActiveModel { get; set; }
    public long StateVersion { get; set; }
    public IReadOnlyList<AiModelHealthStatus> Models { get; set; } = Array.Empty<AiModelHealthStatus>();
}
public sealed class AiModelHealthStatus
{
    public string ModelName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; } = true;
    public DateTimeOffset? LastSuccessAtUtc { get; set; }
    public DateTimeOffset? LastFailureAtUtc { get; set; }
    public string? LastFailureReason { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public long FallbackUsageCount { get; set; }
    public double FailureRate { get; set; }
    public double RecentFailureRate { get; set; }
    public double WeightedSuccessRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public double CooldownFrequency { get; set; }
    public double DynamicScore { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public decimal TotalEstimatedCostUsd { get; set; }
    public DateTimeOffset? CircuitOpenUntilUtc { get; set; }
}