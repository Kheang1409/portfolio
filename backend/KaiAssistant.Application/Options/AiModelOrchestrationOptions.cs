using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class AiModelOrchestrationOptions
{
    public const string SectionName = "AiModelOrchestration";
    public bool Enabled { get; set; } = true;
    [Range(1, 1_000_000)]
    public int SimplePromptCharsThreshold { get; set; } = 500;
    [Range(1, 1_000_000)]
    public int SimplePromptTokensThreshold { get; set; } = 400;
    [Range(1, 1_000_000)]
    public int ComplexPromptCharsThreshold { get; set; } = 2000;
    [Range(1, 1_000_000)]
    public int ComplexPromptTokensThreshold { get; set; } = 1500;
    [Range(1, 10)]
    public int MaxFallbackModels { get; set; } = 3;
    [Range(5, 3600)]
    public int HealthTtlSeconds { get; set; } = 120;
    [Range(5, 3600)]
    public int CircuitOpenSeconds { get; set; } = 30;
    [Range(1, 100)]
    public int CircuitFailureThreshold { get; set; } = 5;
    [Range(1, 300)]
    public int ModelHealthStateRefreshSeconds { get; set; } = 10;
    [Range(1, 1440)]
    public int QuotaCooldownMinutes { get; set; } = 60;
    [Range(1, 120)]
    public int MetricsRollingWindowMinutes { get; set; } = 30;
    [Range(1, 300)]
    public int MetricsPersistIntervalSeconds { get; set; } = 15;
    [Range(50, 10_000)]
    public int LatencyReferenceMs { get; set; } = 2500;
    [Range(0, 10)]
    public double SuccessRateWeight { get; set; } = 1.4;
    [Range(0, 10)]
    public double LatencyWeight { get; set; } = 0.8;
    [Range(0, 10)]
    public double FailureRateWeight { get; set; } = 1.2;
    [Range(0, 10)]
    public double CooldownWeight { get; set; } = 0.9;
    [Range(0, 10)]
    public double CostWeight { get; set; } = 0.6;
    [Range(1, 120)]
    public int DecisionCacheSeconds { get; set; } = 10;
    [Range(0, 100)]
    public decimal MaxEstimatedCostPerRequestUsd { get; set; } = 0.05m;
    public bool EnableFailureSimulation { get; set; } = false;
    [Range(0, 1)]
    public double FailureInjectionRate { get; set; } = 0.0;
    public bool EnableTrafficSimulation { get; set; } = false;
    [Range(1, 100_000)]
    public int SimulationRequestCount { get; set; } = 200;
    [Range(1, 3600)]
    public int AutoTuneIntervalSeconds { get; set; } = 30;
    [Range(50, 120_000)]
    public int MaxLatencyMs { get; set; } = 2500;
    [Range(0, 10_000)]
    public decimal MaxCostPerMinute { get; set; } = 0.2m;
    public bool EnableAutoTuning { get; set; } = false;
    [Range(0, 1)]
    public double DecisionAuditSampleRate { get; set; } = 0.1;
    public List<AiModelProfile> ModelProfiles { get; set; } = new();
}
public sealed class AiModelProfile
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Range(1, 1_000_000)]
    public int MaxTokens { get; set; } = 4096;
    [Range(0, 10_000)]
    public decimal CostWeight { get; set; } = 1.0m;
    [Range(0, 10_000)]
    public decimal LatencyWeight { get; set; } = 1.0m;
    [Range(0, 100)]
    public int Priority { get; set; } = 10;
    [Range(0, 1)]
    public double CapabilityScore { get; set; } = 0.5;
    [Range(0, 100)]
    public decimal InputCostPer1KTokensUsd { get; set; } = 0.001m;
    [Range(0, 100)]
    public decimal OutputCostPer1KTokensUsd { get; set; } = 0.002m;
    public bool Enabled { get; set; } = true;
}