using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Services;
using KaiAssistant.Domain.Entities.Donations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KaiAssistant.Infrastructure.Payments;

public sealed class DonationReceiptOutboxWorker(
    IMongoDatabase database,
    IEmailService emailService,
    ILogger<DonationReceiptOutboxWorker> logger) : BackgroundService
{
    private readonly IMongoCollection<DonationReceiptOutbox> _outbox = database.GetCollection<DonationReceiptOutbox>("donation_receipt_outbox");
    private readonly IMongoCollection<Donation> _donations = database.GetCollection<Donation>("donations");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessOneAsync(stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "Donation receipt outbox processing failed."); }
            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task ProcessOneAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var leaseUntil = now.AddMinutes(10);
        var job = await _outbox.FindOneAndUpdateAsync(
            x => x.SentAtUtc == null && x.NextAttemptAtUtc <= now && (x.LockExpiresAtUtc == null || x.LockExpiresAtUtc <= now),
            Builders<DonationReceiptOutbox>.Update.Inc(x => x.Attempts, 1).Set(x => x.LastAttemptAtUtc, now).Set(x => x.LockExpiresAtUtc, leaseUntil),
            new FindOneAndUpdateOptions<DonationReceiptOutbox> { ReturnDocument = ReturnDocument.After, Sort = Builders<DonationReceiptOutbox>.Sort.Ascending(x => x.NextAttemptAtUtc) },
            cancellationToken).ConfigureAwait(false);
        if (job is null) return;
        try
        {
            var sent = await emailService.SendDonationReceiptAsync(job.Name, job.Email, job.Amount, job.Currency, cancellationToken).ConfigureAwait(false);
            if (!sent) throw new InvalidOperationException("Email provider did not accept the donation receipt.");
            await _outbox.UpdateOneAsync(x => x.Id == job.Id && x.SentAtUtc == null,
                Builders<DonationReceiptOutbox>.Update.Set(x => x.SentAtUtc, DateTimeOffset.UtcNow).Set(x => x.Email, string.Empty).Set(x => x.LockExpiresAtUtc, (DateTimeOffset?)null).Unset(x => x.LastError), cancellationToken: cancellationToken).ConfigureAwait(false);
            await _donations.UpdateOneAsync(x => x.Id == job.DonationId,
                Builders<Donation>.Update.Set(x => x.ReceiptSent, true).Unset(x => x.DonorEmail), cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var nextAttempt = DateTimeOffset.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(job.Attempts, 6))));
            await _outbox.UpdateOneAsync(x => x.Id == job.Id && x.SentAtUtc == null,
                Builders<DonationReceiptOutbox>.Update.Set(x => x.NextAttemptAtUtc, nextAttempt).Set(x => x.LockExpiresAtUtc, (DateTimeOffset?)null).Set(x => x.LastError, "Temporary receipt delivery failure."), cancellationToken: cancellationToken).ConfigureAwait(false);
            logger.LogWarning("Donation receipt delivery attempt {Attempt} failed for internal donation {DonationId}.", job.Attempts, job.DonationId);
        }
    }
}
