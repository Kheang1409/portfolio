namespace KaiAssistant.Domain.Entities.Certifications;

public class Certification
{
    public string Title { get; internal set; } = string.Empty;
    public string Issuer { get; internal set; } = string.Empty;
    public DateTime? Date { get; internal set; }
    // Parameterless constructor for MongoDB deserialization
    public Certification() { }
    // Factory method for controlled creation
    public static Certification Create(string title, string issuer, DateTime? date = null)
    {
        return new Certification
        {
            Title = title,
            Issuer = issuer,
            Date = date
        };
    }
}