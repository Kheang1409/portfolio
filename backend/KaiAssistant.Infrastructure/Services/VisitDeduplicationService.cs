using System.Security.Cryptography;
using System.Text;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Infrastructure.Cache;
using StackExchange.Redis;
namespace KaiAssistant.Infrastructure.Services;

public class VisitDeduplicationService : IVisitDeduplicationService
{
    private readonly IRedisConnectionFactory _redisFactory;
    private const string FINGERPRINT_PREFIX = "visit:fingerprint:";
    private const string DAILY_COUNT_KEY = "visit:daily:";
    public VisitDeduplicationService(IRedisConnectionFactory redisFactory)
    {
        _redisFactory = redisFactory;
    }
    public async Task<bool> ShouldCountVisitAsync(
        string ipAddress,
        string userAgent,
        TimeSpan? debounceWindow = null,
        CancellationToken cancellationToken = default)
    {
        debounceWindow ??= TimeSpan.FromSeconds(30);
        var fingerprint = GenerateFingerprint(ipAddress, userAgent);
        var key = $"{FINGERPRINT_PREFIX}{fingerprint}";
        var redis = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (redis is null)
        {
            return true;
        }
        var db = redis.GetDatabase();
        var lastVisit = await db.StringGetAsync(key).ConfigureAwait(false);
        if (lastVisit.IsNullOrEmpty)
        {
            // New fingerprint, count it
            await db.StringSetAsync(
                key,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                TimeSpan.FromHours(24))
                .ConfigureAwait(false);
            // Increment daily unique count
            var dailyKey = $"{DAILY_COUNT_KEY}{DateOnly.FromDateTime(DateTime.UtcNow)}";
            await db.StringIncrementAsync(dailyKey).ConfigureAwait(false);
            await db.KeyExpireAsync(dailyKey, TimeSpan.FromDays(7)).ConfigureAwait(false);
            return true;
        }
        // Check debounce window
        var lastVisitSeconds = long.Parse(lastVisit.ToString());
        var lastVisitTime = DateTimeOffset.FromUnixTimeSeconds(lastVisitSeconds);
        var timeSinceLastVisit = DateTimeOffset.UtcNow - lastVisitTime;
        if (timeSinceLastVisit >= debounceWindow)
        {
            // Outside debounce window, allow and update
            await db.StringSetAsync(
                key,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                TimeSpan.FromHours(24))
                .ConfigureAwait(false);
            return true;
        }
        // Within debounce window, reject
        return false;
    }
    public async Task<long> GetUniqueVisitCountAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var redis = await _redisFactory.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (redis is null)
        {
            return 0;
        }
        var db = redis.GetDatabase();
        var dailyKey = $"{DAILY_COUNT_KEY}{date}";
        var count = await db.StringGetAsync(dailyKey).ConfigureAwait(false);
        return count.IsNullOrEmpty ? 0 : long.Parse(count.ToString());
    }
    private static string GenerateFingerprint(string ipAddress, string userAgent)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var input = $"{ipAddress}:{userAgent}:{today}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}
