using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Domain.Entities.Visitors;

public class VisitorEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public DateTimeOffset VisitedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string SessionId { get; set; } = string.Empty;
    public string Path { get; set; } = "/";
    public string? Referrer { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Timezone { get; set; }
    public string? Language { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
    public int? ViewportWidth { get; set; }
    public int? ViewportHeight { get; set; }
    public string? Platform { get; set; }
    public string? NetworkType { get; set; }
    public string? IpAddress { get; set; }
    public bool IsUniqueVisit { get; set; }
}