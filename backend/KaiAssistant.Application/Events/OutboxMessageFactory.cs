using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using KaiAssistant.Domain.Entities.Outbox;
namespace KaiAssistant.Application.Events;

public static class OutboxMessageFactory
{
    public static OutboxMessage Create(IntegrationEvent integrationEvent)
    {
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());
        var idempotencySeed = $"{integrationEvent.EventId:N}:{integrationEvent.GetType().FullName}:{payload}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencySeed));
        var eventIdempotencyKey = string.IsNullOrWhiteSpace(integrationEvent.IdempotencyKey)
            ? integrationEvent.EventId.ToString("N")
            : integrationEvent.IdempotencyKey;
        return new OutboxMessage
        {
            EventId = integrationEvent.EventId,
            EventType = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name,
            Payload = payload,
            IdempotencyKey = $"{eventIdempotencyKey}:{Convert.ToHexString(hashBytes)}",
            OccurredAtUtc = integrationEvent.OccurredAtUtc,
            NextAttemptAtUtc = DateTimeOffset.UtcNow,
            AttemptCount = 0,
            TraceParent = Activity.Current?.Id,
            TraceState = Activity.Current?.TraceStateString
        };
    }
}