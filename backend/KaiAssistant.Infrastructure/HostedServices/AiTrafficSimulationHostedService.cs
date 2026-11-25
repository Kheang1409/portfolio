using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class AiTrafficSimulationHostedService : BackgroundService
{
    private readonly IHostEnvironment _environment;
    private readonly IAiTrafficSimulationService _simulationService;
    private readonly IOptionsMonitor<AiModelOrchestrationOptions> _options;
    private readonly ILogger<AiTrafficSimulationHostedService> _logger;
    private bool _ran;
    public AiTrafficSimulationHostedService(
        IHostEnvironment environment,
        IAiTrafficSimulationService simulationService,
        IOptionsMonitor<AiModelOrchestrationOptions> options,
        ILogger<AiTrafficSimulationHostedService> logger)
    {
        _environment = environment;
        _simulationService = simulationService;
        _options = options;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_ran)
        {
            return;
        }
        _ran = true;
        var config = _options.CurrentValue;
        if (!config.EnableTrafficSimulation || _environment.IsProduction())
        {
            return;
        }
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        try
        {
            await _simulationService.RunOnceAsync(config.SimulationRequestCount, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI traffic simulation startup run failed.");
        }
    }
}