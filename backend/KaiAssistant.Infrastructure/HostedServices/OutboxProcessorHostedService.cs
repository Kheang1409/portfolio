using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using KaiAssistant.Application.Events;
using KaiAssistant.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace KaiAssistant.Infrastructure.HostedServices;

public sealed class OutboxProcessorHostedService : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("KaiAssistant.OutboxProcessor", "1.0.0");
    private static readonly Meter Meter = new("KaiAssistant.OutboxProcessor", "1.0.0");
    private static readonly Histogram<double> QueueLagSeconds = Meter.CreateHistogram<double>("outbox_queue_lag_seconds", "s");
    private static readonly Counter<long> MessagesProcessed = Meter.CreateCounter<long>("outbox_messages_processed_total");
    private static readonly Counter<long> MessagesFailed = Meter.CreateCounter<long>("outbox_messages_failed_total");
    private static readonly Counter<long> IdempotencySkipped = Meter.CreateCounter<long>("outbox_idempotency_skipped_total");
    private static readonly Counter<long> LeaseCycles = Meter.CreateCounter<long>("outbox_lease_cycle_total");
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxProcessorOptions _options;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IOutboxProcessorState _state;
    private readonly IInstanceIdentity _instanceIdentity;
    private readonly ILogger<OutboxProcessorHostedService> _logger;
    public OutboxProcessorHostedService(
        IServiceProvider serviceProvider,
        IOptions<OutboxProcessorOptions> options,
        IFeatureFlagService featureFlags,
        IOutboxProcessorState state,
        IInstanceIdentity instanceIdentity,
        ILogger<OutboxProcessorHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _featureFlags = featureFlags;
        _state = state;
        _instanceIdentity = instanceIdentity;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox processor is disabled.");
            return;
        }
        var pollDelay = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_featureFlags.EnableOutboxProcessing)
            {
                await Task.Delay(pollDelay, stoppingToken).ConfigureAwait(false);
                continue;
            }
            try
            {
                _state.MarkCycleStarted();
                using var scope = _serviceProvider.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
                var idempotencyStore = scope.ServiceProvider.GetRequiredService<IEventIdempotencyStore>();
                var simulation = scope.ServiceProvider.GetService<IOperationalSimulationState>();
                var artificialDelayMs = simulation is null
                    ? 0
                    : await simulation.GetOutboxArtificialDelayMsAsync(stoppingToken).ConfigureAwait(false);
                if (artificialDelayMs > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(artificialDelayMs), stoppingToken).ConfigureAwait(false);
                }
                var now = DateTimeOffset.UtcNow;
                var leaseDuration = TimeSpan.FromSeconds(Math.Max(5, _options.LeaseDurationSeconds));
                var partitionCount = Math.Max(1, _options.PartitionCount);
                var partitionIndex = Math.Clamp(_options.PartitionIndex, 0, partitionCount - 1);
                var messages = await outboxRepository
                    .LeasePendingAsync(
                        _options.BatchSize,
                        now,
                        leaseDuration,
                        _instanceIdentity.InstanceId,
                        partitionCount,
                        partitionIndex,
                        stoppingToken)
                    .ConfigureAwait(false);
                LeaseCycles.Add(1,
                    KeyValuePair.Create<string, object?>("instance_id", _instanceIdentity.InstanceId),
                    KeyValuePair.Create<string, object?>("leased_count", messages.Count));
                if (messages.Count == 0)
                {
                    _state.MarkCycleSucceeded();
                    await Task.Delay(pollDelay, stoppingToken).ConfigureAwait(false);
                    continue;
                }
                var maxConcurrency = Math.Max(1, _options.MaxConcurrency);
                var gate = new SemaphoreSlim(maxConcurrency);
                var tasks = new List<Task>(messages.Count);
                foreach (var message in messages)
                {
                    await gate.WaitAsync(stoppingToken).ConfigureAwait(false);
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessMessageIdempotentAsync(message, outboxRepository, publisher, idempotencyStore, stoppingToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            gate.Release();
                        }
                    }, stoppingToken));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                if (messages.Count >= _options.BatchSize && _options.BackpressureDelayMs > 0)
                {
                    _logger.LogInformation(
                        "Outbox backlog pressure detected; delaying next cycle by {DelayMs}ms.",
                        _options.BackpressureDelayMs);
                    await Task.Delay(TimeSpan.FromMilliseconds(_options.BackpressureDelayMs), stoppingToken).ConfigureAwait(false);
                }
                _state.MarkCycleSucceeded();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _state.MarkCycleFailed(ex.Message);
                _logger.LogError(ex, "Outbox processor iteration failed.");
            }
            await Task.Delay(pollDelay, stoppingToken).ConfigureAwait(false);
        }
    }
    private async Task ProcessMessageAsync(
        KaiAssistant.Domain.Entities.Outbox.OutboxMessage message,
        IOutboxRepository outboxRepository,
        IIntegrationEventPublisher publisher,
        CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();
        ActivityContext parentContext = default;
        var hasParent = !string.IsNullOrWhiteSpace(message.TraceParent) &&
                        ActivityContext.TryParse(message.TraceParent, message.TraceState, out parentContext);
        using var activity = hasParent
            ? ActivitySource.StartActivity("outbox.process", ActivityKind.Consumer, parentContext)
            : ActivitySource.StartActivity("outbox.process", ActivityKind.Consumer);
        var lagSeconds = Math.Max(0, (DateTimeOffset.UtcNow - message.OccurredAtUtc).TotalSeconds);
        QueueLagSeconds.Record(lagSeconds);
        activity?.SetTag("outbox.message_id", message.Id);
        activity?.SetTag("outbox.event_type", message.EventType);
        activity?.SetTag("outbox.attempt_count", message.AttemptCount);
        activity?.SetTag("outbox.queue_lag_seconds", lagSeconds);
        try
        {
            var eventType = Type.GetType(message.EventType, throwOnError: false);
            if (eventType is null)
            {
                throw new InvalidOperationException($"Unknown outbox event type '{message.EventType}'.");
            }
            var deserialized = JsonSerializer.Deserialize(message.Payload, eventType) as IntegrationEvent;
            if (deserialized is null)
            {
                throw new InvalidOperationException($"Failed to deserialize outbox message '{message.Id}'.");
            }
            await publisher.PublishAsync(deserialized, stoppingToken).ConfigureAwait(false);
            await outboxRepository.MarkProcessedAsync(message.Id!, DateTimeOffset.UtcNow, _instanceIdentity.InstanceId, stoppingToken).ConfigureAwait(false);
            MessagesProcessed.Add(1, KeyValuePair.Create<string, object?>("event_type", eventType.Name));
            sw.Stop();
            _logger.LogInformation(
                "Outbox message processed: {MessageId} {EventType} attempts={AttemptCount} latencyMs={LatencyMs}",
                message.Id,
                message.EventType,
                message.AttemptCount,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            MessagesFailed.Add(1);
            var nextAttemptCount = message.AttemptCount + 1;
            if (nextAttemptCount >= Math.Max(1, _options.MaxAttempts))
            {
                await outboxRepository
                    .DeadLetterAsync(message.Id!, $"max-attempts:{nextAttemptCount}:{ex.Message}", DateTimeOffset.UtcNow, stoppingToken)
                    .ConfigureAwait(false);
                _logger.LogError(
                    ex,
                    "Outbox dead-lettered message after max attempts. MessageId={MessageId} EventType={EventType} Attempts={Attempts}",
                    message.Id,
                    message.EventType,
                    nextAttemptCount);
                return;
            }
            var baseBackoff = Math.Min(
                Math.Max(1, _options.MaxBackoffSeconds),
                (int)Math.Pow(2, message.AttemptCount + 1));
            var jitterMs = _options.RetryJitterMs > 0 ? Random.Shared.Next(0, _options.RetryJitterMs + 1) : 0;
            var nextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(baseBackoff).AddMilliseconds(jitterMs);
            await outboxRepository
                .MarkFailedAsync(message.Id!, ex.Message, nextAttemptAt, _instanceIdentity.InstanceId, stoppingToken)
                .ConfigureAwait(false);
            _state.MarkCycleFailed(ex.Message);
            if (message.AttemptCount >= 9)
            {
                _logger.LogError(
                    ex,
                    "Outbox permanent failure: {MessageId} {EventType} attempts={AttemptCount} nextAttemptIn={BackoffSeconds}s",
                    message.Id,
                    message.EventType,
                    message.AttemptCount + 1,
                    baseBackoff);
            }
            else
            {
                _logger.LogWarning(
                    ex,
                    "Outbox retry scheduled: {MessageId} {EventType} attempts={AttemptCount} nextAttemptIn={BackoffSeconds}s jitterMs={JitterMs} latencyMs={LatencyMs}",
                    message.Id,
                    message.EventType,
                    message.AttemptCount + 1,
                    baseBackoff,
                    jitterMs,
                    sw.ElapsedMilliseconds);
            }
        }
    }
    private async Task ProcessMessageIdempotentAsync(
        KaiAssistant.Domain.Entities.Outbox.OutboxMessage message,
        IOutboxRepository outboxRepository,
        IIntegrationEventPublisher publisher,
        IEventIdempotencyStore idempotencyStore,
        CancellationToken stoppingToken)
    {
        var processingTtl = TimeSpan.FromSeconds(Math.Max(5, _options.IdempotencyProcessingTtlSeconds));
        var processedTtl = TimeSpan.FromSeconds(Math.Max(30, _options.IdempotencyProcessedTtlSeconds));
        var acquireResult = await idempotencyStore
            .TryAcquireAsync(message.IdempotencyKey, processingTtl, stoppingToken)
            .ConfigureAwait(false);
        if (acquireResult == IdempotencyAcquireResult.AlreadyProcessed)
        {
            IdempotencySkipped.Add(1, KeyValuePair.Create<string, object?>("reason", "already_processed"));
            await outboxRepository
                .MarkProcessedAsync(message.Id!, DateTimeOffset.UtcNow, _instanceIdentity.InstanceId, stoppingToken)
                .ConfigureAwait(false);
            return;
        }
        if (acquireResult == IdempotencyAcquireResult.Busy)
        {
            IdempotencySkipped.Add(1, KeyValuePair.Create<string, object?>("reason", "busy"));
            return;
        }
        try
        {
            await ProcessMessageAsync(message, outboxRepository, publisher, stoppingToken).ConfigureAwait(false);
            await idempotencyStore
                .MarkProcessedAsync(message.IdempotencyKey, processedTtl, stoppingToken)
                .ConfigureAwait(false);
        }
        catch
        {
            await idempotencyStore.ReleaseAsync(message.IdempotencyKey, stoppingToken).ConfigureAwait(false);
            throw;
        }
    }
}