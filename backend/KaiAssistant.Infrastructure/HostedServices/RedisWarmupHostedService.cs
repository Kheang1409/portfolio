using KaiAssistant.Infrastructure.Cache;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class RedisWarmupHostedService : BackgroundService
{
    private readonly IRedisConnectionFactory _factory;
    private readonly ILogger<RedisWarmupHostedService> _logger;
    public RedisWarmupHostedService(IRedisConnectionFactory factory, ILogger<RedisWarmupHostedService> logger)
    {
        _factory = factory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_factory.IsConfigured)
        {
            return;
        }
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            var connection = await _factory.GetConnectionAsync(stoppingToken).ConfigureAwait(false);
            _logger.LogInformation("Redis warmup completed. Connected={Connected}", connection?.IsConnected == true);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is stopping.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis warmup failed; application will continue with fallback paths.");
        }
    }
}