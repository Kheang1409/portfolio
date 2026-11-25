namespace KaiAssistant.Application.Interfaces;

public interface IVisitTrackingService
{
    Task RecordVisitAsync(
        string fingerprint,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);
    Task<long> GetUniqueVisitorCountAsync(
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default);
    Task<long> GetTotalVisitCountAsync(
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default);
    Task<VisitStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
public class VisitStatistics
{
    public long UniqueVisitorsToday { get; set; }
    public long UniqueVisitorsWeek { get; set; }
    public long UniqueVisitorsMonth { get; set; }
    public long TotalVisitsToday { get; set; }
    public long TotalVisitsWeek { get; set; }
    public long TotalVisitsMonth { get; set; }
    public double AvgRequestsPerVisitorToday => UniqueVisitorsToday > 0
        ? (double)TotalVisitsToday / UniqueVisitorsToday
        : 0;
    public int PeakHourUtc { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}