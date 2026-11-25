namespace KaiAssistant.Application.Interfaces;

public interface IRateLimitingService
{
    Task<(bool IsAllowed, int Remaining, DateTimeOffset ResetAt)> CheckRateLimitAsync(
        string identifier,
        int maxRequests,
        TimeSpan window,
        CancellationToken cancellationToken = default);
    Task ResetAsync(string identifier, CancellationToken cancellationToken = default);
}