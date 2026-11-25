using KaiAssistant.Domain.Interfaces.Repositories;
using KaiAssistant.Infrastructure.Cache;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class ResumeCacheWarmupHostedService : IHostedService
{
    private const string WarmupLockKey = "startup:warmup:resume:lock";
    private readonly IResumeRepository _resumeRepository;
    private readonly IRedisConnectionFactory _redisFactory;
    private readonly ILogger<ResumeCacheWarmupHostedService> _logger;
    public ResumeCacheWarmupHostedService(
        IResumeRepository resumeRepository,
        IRedisConnectionFactory redisFactory,
        ILogger<ResumeCacheWarmupHostedService> logger)
    {
        _resumeRepository = resumeRepository;
        _redisFactory = redisFactory;
        _logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var redis = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (redis is not null)
            {
                var db = redis.GetDatabase();
                var token = $"{Environment.MachineName}:{Guid.NewGuid():N}";
                var acquired = await db.StringSetAsync(WarmupLockKey, token, TimeSpan.FromMinutes(2), when: When.NotExists)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (!acquired)
                {
                    _logger.LogInformation("Resume cache warmup skipped; lock is owned by another instance.");
                    return;
                }
            }
            await _resumeRepository.GetLatestAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Resume cache warmup completed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resume cache warmup failed.");
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}