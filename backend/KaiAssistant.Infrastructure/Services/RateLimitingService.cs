using KaiAssistant.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace KaiAssistant.Infrastructure.Services;

public class RateLimitingService : IRateLimitingService
{
    public async Task<(bool IsAllowed, int Remaining, DateTimeOffset ResetAt)> CheckRateLimitAsync(
        string identifier,
        int maxRequests,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var resetAt = DateTimeOffset.UtcNow.Add(window);
        return (true, maxRequests, resetAt);
    }
    public async Task ResetAsync(string identifier, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}