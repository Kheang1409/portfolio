namespace KaiAssistant.Application.Interfaces;

public interface IVisitDeduplicationService
{
    Task<bool> ShouldCountVisitAsync(
        string ipAddress,
        string userAgent,
        TimeSpan? debounceWindow = null,
        CancellationToken cancellationToken = default);
    Task<long> GetUniqueVisitCountAsync(DateOnly date, CancellationToken cancellationToken = default);
}