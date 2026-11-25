namespace KaiAssistant.Application.Diagnostics;

public sealed class AiSimulationMetricSnapshot
{
    public DateTimeOffset CapturedAtUtc { get; set; }
    public long StateVersion { get; set; }
    public string? ActiveModel { get; set; }
    public decimal TotalEstimatedCostUsd { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public int UnhealthyModelCount { get; set; }
    public IReadOnlyDictionary<string, long> SelectionCounts { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
}
public sealed class AiSimulationRunReport
{
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
    public int RequestCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double AverageSimulatedLatencyMs { get; set; }
    public double AverageDecisionLatencyMs { get; set; }
    public double ThroughputRequestsPerSecond { get; set; }
    public decimal CostPerMinuteUsd { get; set; }
    public bool SelectionShiftObserved { get; set; }
    public bool UnhealthyAvoidanceObserved { get; set; }
    public bool CostWithinLimit { get; set; }
    public string? BeforeTopModel { get; set; }
    public string? AfterTopModel { get; set; }
}