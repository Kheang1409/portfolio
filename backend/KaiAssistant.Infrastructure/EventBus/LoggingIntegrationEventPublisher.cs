using KaiAssistant.Application.Events;
using KaiAssistant.Application.Interfaces;
using Microsoft.Extensions.Logging;
namespace KaiAssistant.Infrastructure.EventBus;

public sealed class LoggingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ILogger<LoggingIntegrationEventPublisher> _logger;
    public LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
    {
        _logger = logger;
    }
    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Integration event published: {EventType} {EventId} at {OccurredAtUtc}",
            integrationEvent.GetType().Name,
            integrationEvent.EventId,
            integrationEvent.OccurredAtUtc);
        return Task.CompletedTask;
    }
}