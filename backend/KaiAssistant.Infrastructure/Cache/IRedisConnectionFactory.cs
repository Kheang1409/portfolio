using StackExchange.Redis;
namespace KaiAssistant.Infrastructure.Cache;

public interface IRedisConnectionFactory
{
    bool IsConfigured { get; }
    bool IsConnected { get; }
    ValueTask<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default);
}