using KaiAssistant.Application.Events;
namespace KaiAssistant.Application.Interfaces;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}