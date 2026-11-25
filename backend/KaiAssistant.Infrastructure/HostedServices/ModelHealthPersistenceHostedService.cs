using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class ModelHealthPersistenceHostedService : BackgroundService
{
    private readonly IModelHealthService _modelHealth;
    private readonly IOptionsMonitor<AiModelOrchestrationOptions> _options;
    private readonly ILogger<ModelHealthPersistenceHostedService> _logger;
    public ModelHealthPersistenceHostedService(
        IModelHealthService modelHealth,
        IOptionsMonitor<AiModelOrchestrationOptions> options,
        ILogger<ModelHealthPersistenceHostedService> logger)
    {
        _modelHealth = modelHealth;
        _options = options;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _modelHealth.FlushPendingChanges(DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed persisting buffered model health state.");
            }
            var delaySeconds = Math.Clamp(_options.CurrentValue.MetricsPersistIntervalSeconds, 1, 300);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken).ConfigureAwait(false);
        }
    }
}