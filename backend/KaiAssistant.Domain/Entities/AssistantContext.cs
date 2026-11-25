namespace KaiAssistant.Domain.Entities;

public sealed class AssistantContext
{
    public AssistantUserProfile? UserProfile { get; set; }
    public string? SystemPersona { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
public sealed class AssistantUserProfile
{
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Locale { get; set; }
}