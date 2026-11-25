namespace KaiAssistant.Application.Events;

public sealed record ContactMessageSubmittedIntegrationEvent(
    string Name,
    string Email,
    string MessageHash) : IntegrationEvent;