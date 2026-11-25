using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.Application.Options;

public sealed class AiGovernanceOptions
{
    public const string SectionName = "AiGovernance";
    public bool Enabled { get; set; } = true;
    [Range(1, 64_000)]
    public int MaxTokensPerRequest { get; set; } = 1200;
    [Range(1, 10_000)]
    public int MaxRequestsPerMinutePerIp { get; set; } = 20;
    [Range(1, 1_000_000)]
    public int MaxInputChars { get; set; } = 3000;
    [Range(1, 100)]
    public int MaxHistoryMessages { get; set; } = 8;
    [Range(1, 50_000)]
    public int MaxHistoryMessageChars { get; set; } = 1200;
    [Range(1, 32)]
    public int EstimatedCharsPerToken { get; set; } = 4;
    public bool TruncateOversizedInput { get; set; } = true;
    public bool EnableResponseCache { get; set; } = false;
    [Range(5, 3600)]
    public int ResponseCacheTtlSeconds { get; set; } = 45;
    [Required]
    public string CacheKeyVersion { get; set; } = "v1";
    public bool EnableResponseTruncation { get; set; } = false;
    [Range(200, 100_000)]
    public int MaxResponseChars { get; set; } = 2200;
    [Range(0, 100)]
    public decimal MaxCostPerRequestUsd { get; set; } = 0.02m;
    [Range(0, 1000)]
    public decimal SoftBudgetPerMinuteUsdPerIp { get; set; } = 0.20m;
    [Range(0, 1000)]
    public decimal SoftBudgetPerHourUsdPerSession { get; set; } = 2.00m;
    [Range(1, 500)]
    public int MinimumResponseChars { get; set; } = 8;
    public bool EnableUnsafeOutputFiltering { get; set; } = true;
    public string[] UnsafeTerms { get; set; } = [];
}