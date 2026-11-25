using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
namespace KaiAssistant.API.HealthChecks;

public sealed class AiProviderHealthCheck : IHealthCheck
{
    private readonly IOptions<GeminiSettings> _settings;
    public AiProviderHealthCheck(IOptions<GeminiSettings> settings)
    {
        _settings = settings;
    }
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cfg = _settings.Value;
        if (string.IsNullOrWhiteSpace(cfg.Endpoint) || cfg.ModelNames.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("AI provider configuration is incomplete."));
        }
        if (string.IsNullOrWhiteSpace(cfg.ApiKey))
        {
            return Task.FromResult(HealthCheckResult.Degraded("AI provider key is not configured in this environment."));
        }
        return Task.FromResult(HealthCheckResult.Healthy("AI provider configuration is healthy."));
    }
}