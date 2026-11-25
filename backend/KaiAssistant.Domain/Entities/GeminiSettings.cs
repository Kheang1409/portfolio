namespace KaiAssistant.Domain.Entities;

public class GeminiSettings : AssistantBehaviorSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public List<string> ModelNames { get; set; } = new();
    public string Endpoint { get; set; } = string.Empty;
    public int PromptMaxChars { get; set; } = 10000;
    public bool IncludePersonalDetails { get; set; } = true;
    public double? Temperature { get; set; }
    public int? TopK { get; set; }
    public double? TopP { get; set; }
    public int? MaxOutputTokens { get; set; }
    public int? CandidateCount { get; set; }
    // How long to consider the last successful model as preferred (seconds)
    public int LastSuccessfulModelCacheTtlSeconds { get; set; } = 300;
    // If the service returns a RetryInfo.retryDelay greater than this (seconds), skip waiting and try the next model
    public int SkipRetryDelayThresholdSeconds { get; set; } = 10;
    // If a model's 429 counter reaches this value, deprioritize or skip it
    public int DeprioritizeOn429Count { get; set; } = 3;
    // If true, models with 429 count >= DeprioritizeOn429Count will be skipped entirely (not retried)
    // If false, they will be moved to the end of the candidate list (deprioritized)
    public bool DeprioritizeSkip { get; set; } = false;
}