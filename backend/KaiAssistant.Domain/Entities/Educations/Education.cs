namespace KaiAssistant.Domain.Entities.Educations;

public class Education
{
    public string Degree { get; internal set; } = string.Empty;
    public string Institution { get; internal set; } = string.Empty;
    public string? Status { get; internal set; }
    public string? Location { get; internal set; }
    public DateTime? StartDates { get; internal set; }
    public DateTime? EndDate { get; internal set; }
    public Education() { }
    public static Education Create(string degree, string institution, string? location = null,
        DateTime? startDate = null, DateTime? endDate = null, string? status = null)
    {
        return new Education
        {
            Degree = degree,
            Institution = institution,
            Status = status,
            Location = location,
            StartDates = startDate,
            EndDate = endDate
        };
    }
}