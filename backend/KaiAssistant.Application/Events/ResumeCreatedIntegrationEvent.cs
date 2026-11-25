namespace KaiAssistant.Application.Events;

public sealed record ResumeCreatedIntegrationEvent(string? ResumeId) : IntegrationEvent;