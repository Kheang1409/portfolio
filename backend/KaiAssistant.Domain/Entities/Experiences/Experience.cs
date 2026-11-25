namespace KaiAssistant.Domain.Entities.Experiences;

public class Experience
{
    public string Role { get; internal set; } = string.Empty;
    public string Company { get; internal set; } = string.Empty;
    public string? Location { get; internal set; }
    public DateTime? StartDates { get; internal set; }
    public DateTime? EndDate { get; internal set; }
    public List<string> BulletPoints { get; internal set; } = new();
    public Experience() { }
    public static Experience Create(string role, string company, string? location = null,
        DateTime? startDate = null, DateTime? endDate = null, IEnumerable<string>? bulletPoints = null)
    {
        return new Experience
        {
            Role = role,
            Company = company,
            Location = location,
            StartDates = startDate,
            EndDate = endDate,
            BulletPoints = bulletPoints != null ? new List<string>(bulletPoints) : new()
        };
    }
    public void AddBulletPoint(string point)
    {
        if (!string.IsNullOrWhiteSpace(point))
            BulletPoints.Add(point);
    }
}