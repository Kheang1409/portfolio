using MongoDB.Bson.Serialization.Attributes;
namespace KaiAssistant.Domain.Entities.Resumes;

[BsonIgnoreExtraElements]
public record Personals
{
    public required string LegalName { get; init; }
    public string PreferredName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Linkedin { get; init; } = string.Empty;
    public string Github { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Portfolio { get; init; } = string.Empty;
    public string WorkAuthorization { get; init; } = string.Empty;
}