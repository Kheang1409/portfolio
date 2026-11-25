using KaiAssistant.Domain.Entities;
namespace KaiAssistant.Application.DTOs;

public sealed class AssistantContextDto
{
    public AssistantUserProfileDto? UserProfile { get; set; }
    public string? SystemPersona { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public AssistantContext ToDomain()
    {
        return new AssistantContext
        {
            UserProfile = UserProfile is null
                ? null
                : new AssistantUserProfile
                {
                    UserId = UserProfile.UserId,
                    DisplayName = UserProfile.DisplayName,
                    Locale = UserProfile.Locale
                },
            SystemPersona = SystemPersona,
            Metadata = Metadata
        };
    }
}
public sealed class AssistantUserProfileDto
{
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Locale { get; set; }
}