using System.ComponentModel.DataAnnotations;
namespace KaiAssistant.API.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 120;
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
    public bool UseSlidingWindow { get; set; } = true;
    [Range(1, 10)]
    public int BurstMultiplier { get; set; } = 1;
    [Range(1, 100_000)]
    public int? GlobalPermitLimit { get; set; }
    public bool StrictDistributedMode { get; set; } = false;
    public string[] PathPrefixes { get; set; } = ["/api"];
    public EndpointRateLimitRule[] EndpointRules { get; set; } =
    [
        new EndpointRateLimitRule { Name = "ask", PathContains = "/ask", PermitLimit = 120, WindowSeconds = 60 },
        new EndpointRateLimitRule { Name = "stream", PathContains = "/stream", PermitLimit = 60, WindowSeconds = 60 },
        new EndpointRateLimitRule { Name = "batch", PathContains = "/batch", PermitLimit = 30, WindowSeconds = 60 },
        new EndpointRateLimitRule { Name = "sse", PathContains = "/sse", PermitLimit = 45, WindowSeconds = 60 }
    ];
}
public sealed class EndpointRateLimitRule
{
    public string Name { get; set; } = "default";
    public string PathContains { get; set; } = string.Empty;
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 120;
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}