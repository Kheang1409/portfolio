using KaiAssistant.Domain.Entities.Outbox;
using KaiAssistant.Domain.Entities.Resumes;
using KaiAssistant.Domain.Entities.Visitors;
using KaiAssistant.Domain.Entities.Donations;
using KaiAssistant.Domain.Entities.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
namespace KaiAssistant.Infrastructure.Mongo;

public sealed class MongoIndexInitializerHostedService : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoIndexInitializerHostedService> _logger;
    public MongoIndexInitializerHostedService(
        IMongoDatabase database,
        ILogger<MongoIndexInitializerHostedService> logger)
    {
        _database = database;
        _logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var resumeCollection = _database.GetCollection<Resume>("resumes");
        var outboxCollection = _database.GetCollection<OutboxMessage>("outbox_messages");
        var visitorCollection = _database.GetCollection<VisitorEvent>("visitorevents");
        var conversationCollection = _database.GetCollection<Conversation>("ai_conversations");
        var donationCollection = _database.GetCollection<Donation>("donations");
        var donationEventCollection = _database.GetCollection<DonationWebhookEvent>("donation_webhook_events");
        var donationReceiptCollection = _database.GetCollection<DonationReceiptOutbox>("donation_receipt_outbox");
        var resumeIndexes = new[]
        {
            new CreateIndexModel<Resume>(
                Builders<Resume>.IndexKeys
                    .Descending(x => x.CreatedAtUtc)
                    .Descending(x => x.Id),
                new CreateIndexOptions { Name = "ix_resume_created_desc" })
        };
        var outboxIndexes = new[]
        {
            new CreateIndexModel<OutboxMessage>(
                Builders<OutboxMessage>.IndexKeys.Ascending(x => x.EventId),
                new CreateIndexOptions { Name = "ix_outbox_event_id", Unique = true }),
            new CreateIndexModel<OutboxMessage>(
                Builders<OutboxMessage>.IndexKeys
                    .Ascending(x => x.ProcessedAtUtc)
                    .Ascending(x => x.NextAttemptAtUtc),
                new CreateIndexOptions { Name = "ix_outbox_pending" }),
            new CreateIndexModel<OutboxMessage>(
                Builders<OutboxMessage>.IndexKeys
                    .Ascending(x => x.ProcessedAtUtc)
                    .Ascending(x => x.LockExpiresAtUtc)
                    .Ascending(x => x.NextAttemptAtUtc),
                new CreateIndexOptions { Name = "ix_outbox_leasing" }),
            new CreateIndexModel<OutboxMessage>(
                Builders<OutboxMessage>.IndexKeys.Ascending(x => x.IdempotencyKey),
                new CreateIndexOptions { Name = "ix_outbox_idempotency", Unique = true }),
            new CreateIndexModel<OutboxMessage>(
                Builders<OutboxMessage>.IndexKeys
                    .Ascending(x => x.ProcessedAtUtc)
                    .Ascending(x => x.AttemptCount)
                    .Ascending(x => x.NextAttemptAtUtc),
                new CreateIndexOptions { Name = "ix_outbox_retry_scheduling" })
        };
        var conversationIndexes = new[]
        {
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Ascending(x => x.ConversationId),
                new CreateIndexOptions { Name = "ix_conversation_id", Unique = true }),
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Descending(x => x.UpdatedAt),
                new CreateIndexOptions { Name = "ix_conversation_updated_desc" })
        };
        var visitorIndexes = new[]
        {
            new CreateIndexModel<VisitorEvent>(
                Builders<VisitorEvent>.IndexKeys.Descending(x => x.VisitedAtUtc),
                new CreateIndexOptions { Name = "ix_visits_visited_at_desc" }),
            new CreateIndexModel<VisitorEvent>(
                Builders<VisitorEvent>.IndexKeys.Ascending(x => x.SessionId),
                new CreateIndexOptions { Name = "ix_visits_session_id" }),
            new CreateIndexModel<VisitorEvent>(
                Builders<VisitorEvent>.IndexKeys.Ascending(x => x.Path),
                new CreateIndexOptions { Name = "ix_visits_path" })
        };
        await resumeCollection.Indexes.CreateManyAsync(resumeIndexes, cancellationToken).ConfigureAwait(false);
        await outboxCollection.Indexes.CreateManyAsync(outboxIndexes, cancellationToken).ConfigureAwait(false);
        await conversationCollection.Indexes.CreateManyAsync(conversationIndexes, cancellationToken).ConfigureAwait(false);
        await visitorCollection.Indexes.CreateManyAsync(visitorIndexes, cancellationToken).ConfigureAwait(false);
        await donationCollection.Indexes.CreateManyAsync([
            new CreateIndexModel<Donation>(Builders<Donation>.IndexKeys.Ascending(x => x.PayPalOrderId), new CreateIndexOptions { Name = "ux_donations_paypal_order", Unique = true }),
            new CreateIndexModel<Donation>(Builders<Donation>.IndexKeys.Ascending(x => x.IdempotencyKey), new CreateIndexOptions { Name = "ux_donations_idempotency", Unique = true, Sparse = true }),
            new CreateIndexModel<Donation>(Builders<Donation>.IndexKeys.Descending(x => x.Status).Descending(x => x.ShowOnLeaderboard).Descending(x => x.Amount).Ascending(x => x.PaidAtUtc), new CreateIndexOptions { Name = "ix_donations_leaderboard" })
        ], cancellationToken).ConfigureAwait(false);
        await donationEventCollection.Indexes.CreateOneAsync(new CreateIndexModel<DonationWebhookEvent>(Builders<DonationWebhookEvent>.IndexKeys.Ascending(x => x.EventId), new CreateIndexOptions { Name = "ux_donation_webhook_event", Unique = true }), cancellationToken: cancellationToken).ConfigureAwait(false);
        await donationReceiptCollection.Indexes.CreateManyAsync([
            new CreateIndexModel<DonationReceiptOutbox>(Builders<DonationReceiptOutbox>.IndexKeys.Ascending(x => x.DonationId), new CreateIndexOptions { Name = "ux_donation_receipt_donation", Unique = true }),
            new CreateIndexModel<DonationReceiptOutbox>(Builders<DonationReceiptOutbox>.IndexKeys.Ascending(x => x.SentAtUtc).Ascending(x => x.NextAttemptAtUtc), new CreateIndexOptions { Name = "ix_donation_receipt_pending" }),
            new CreateIndexModel<DonationReceiptOutbox>(Builders<DonationReceiptOutbox>.IndexKeys.Ascending(x => x.RetainUntilUtc), new CreateIndexOptions { Name = "ttl_donation_receipt_retention", ExpireAfter = TimeSpan.Zero })
        ], cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Mongo indexes initialized.");
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
