using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KaiAssistant.Application.Donations;
using KaiAssistant.Application.Services;
using KaiAssistant.Domain.Entities;
using KaiAssistant.Infrastructure.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace KaiAssistant.Tests.Payments;

public sealed class DonationSecurityTests
{
    [Fact]
    public async Task CreateOrder_RejectsAmountWithUnsupportedPrecisionBeforePayPal()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var handler = new StubHandler(_ => throw new Xunit.Sdk.XunitException("PayPal must not be called for invalid amount."));
        var service = Create(db, handler);

        var act = () => service.CreateOrderAsync(new CreateDonationRequest(10.999m, "A", null, null, false, Guid.NewGuid().ToString()), default);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CreateOrder_ReusesExistingAttemptWithoutTrustingRetryPayload()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var attemptId = Guid.NewGuid().ToString();
        await db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations").InsertOneAsync(
            new KaiAssistant.Domain.Entities.Donations.Donation
            {
                IdempotencyKey = attemptId,
                PayPalOrderId = "ORDER-ORIGINAL",
                Amount = 12.34m,
                Currency = "USD",
                Status = "PENDING"
            });
        var service = Create(db, new StubHandler(_ => throw new Xunit.Sdk.XunitException("An existing order must not create another PayPal order.")));

        var result = await service.CreateOrderAsync(
            new CreateDonationRequest(-50m, "Changed name", "invalid email", null, false, attemptId), default);

        result.OrderId.Should().Be("ORDER-ORIGINAL");
        var saved = await db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations")
            .Find(x => x.IdempotencyKey == attemptId).FirstAsync();
        saved.Amount.Should().Be(12.34m);
    }

    [Fact]
    public async Task CreateOrder_RecoversStaleAttemptUsingPersistedAmountAndRequestId()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var attemptId = Guid.NewGuid().ToString();
        await db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations").InsertOneAsync(
            new KaiAssistant.Domain.Entities.Donations.Donation
            {
                IdempotencyKey = attemptId,
                Amount = 13.25m,
                Currency = "USD",
                Status = "CREATING",
                LastOrderAttemptAtUtc = DateTimeOffset.UtcNow.AddMinutes(-3)
            });
        string? orderPayload = null;
        string? paypalRequestId = null;
        var handler = new AsyncStubHandler(async request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal))
                return Json("{\"access_token\":\"test-token\"}");
            orderPayload = await request.Content!.ReadAsStringAsync();
            paypalRequestId = request.Headers.GetValues("PayPal-Request-Id").Single();
            return Json("{\"id\":\"ORDER-RECOVERED\",\"status\":\"CREATED\"}");
        });
        var service = Create(db, handler);

        var result = await service.CreateOrderAsync(
            new CreateDonationRequest(99m, "Changed", null, null, false, attemptId), default);

        result.OrderId.Should().Be("ORDER-RECOVERED");
        orderPayload.Should().Contain("\"value\":\"13.25\"");
        paypalRequestId.Should().Be("order-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(attemptId))).ToLowerInvariant()[..32]);
        var saved = await db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations")
            .Find(x => x.IdempotencyKey == attemptId).FirstAsync();
        saved.Amount.Should().Be(13.25m);
        saved.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task CreateOrder_ConcurrentDuplicateAttemptsCreateOnlyOnePayPalOrder()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        await db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations").Indexes.CreateOneAsync(
            new CreateIndexModel<KaiAssistant.Domain.Entities.Donations.Donation>(
                Builders<KaiAssistant.Domain.Entities.Donations.Donation>.IndexKeys.Ascending(x => x.IdempotencyKey),
                new CreateIndexOptions { Unique = true }));
        var orderRequests = 0;
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal))
                return Json("{\"access_token\":\"test-token\"}");
            Interlocked.Increment(ref orderRequests);
            return Json("{\"id\":\"ORDER-ONCE\",\"status\":\"CREATED\"}");
        });
        var attemptId = Guid.NewGuid().ToString();
        var request = new CreateDonationRequest(10m, "A", null, null, false, attemptId);

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            try { return await Create(db, handler).CreateOrderAsync(request, default); }
            catch (InvalidOperationException) { return null; }
        }));

        results.OfType<CreateDonationResult>().Should().ContainSingle().Which.OrderId.Should().Be("ORDER-ONCE");
        orderRequests.Should().Be(1);
        (await db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations")
            .CountDocumentsAsync(x => x.IdempotencyKey == attemptId)).Should().Be(1);
    }

    [Fact]
    public async Task Capture_RejectsWhenCaptureAmountDiffersFromServerOrder()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var donations = db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations");
        await donations.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.Donation { PayPalOrderId = "ORDER123", Amount = 10m, Currency = "USD", Status = "PENDING" });
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal)) return Json("{\"access_token\":\"test-token\"}");
            return Json("{\"id\":\"ORDER123\",\"status\":\"COMPLETED\",\"purchase_units\":[{\"payments\":{\"captures\":[{\"id\":\"CAPTURE123\",\"status\":\"COMPLETED\",\"amount\":{\"currency_code\":\"USD\",\"value\":\"9.00\"}}]}}]}");
        });
        var service = Create(db, handler);

        var act = () => service.CaptureAsync(new CaptureDonationRequest("ORDER123"), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Capture_RecoversStaleProcessingDonationUsingPayPalIdempotency()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var donations = db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations");
        await donations.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.Donation
        {
            PayPalOrderId = "ORDERRECOVER",
            Amount = 10m,
            Currency = "USD",
            Status = "PROCESSING",
            ProcessingStartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-3)
        });
        var captureRequestIds = new List<string>();
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal)) return Json("{\"access_token\":\"test-token\"}");
            captureRequestIds.Add(request.Headers.GetValues("PayPal-Request-Id").Single());
            return Json("{\"id\":\"ORDERRECOVER\",\"status\":\"COMPLETED\",\"purchase_units\":[{\"payments\":{\"captures\":[{\"id\":\"CAPTURE123\",\"status\":\"COMPLETED\",\"amount\":{\"currency_code\":\"USD\",\"value\":\"10.00\"}}]}}]}");
        });
        var service = Create(db, handler);

        var result = await service.CaptureAsync(new CaptureDonationRequest("ORDERRECOVER"), default);

        result.Status.Should().Be("COMPLETED");
        captureRequestIds.Should().ContainSingle();
        var saved = await donations.Find(x => x.PayPalOrderId == "ORDERRECOVER").FirstOrDefaultAsync();
        saved.Status.Should().Be("SUCCEEDED");
        saved.ProcessingStartedAtUtc.Should().BeNull();

        var repeated = await service.CaptureAsync(new CaptureDonationRequest("ORDERRECOVER"), default);
        repeated.Status.Should().Be("COMPLETED");
        captureRequestIds.Should().ContainSingle();
    }

    [Fact]
    public async Task Capture_RecoversInterruptedProviderRequestWithStableIdempotencyKey()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var donations = db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations");
        await donations.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.Donation
        {
            PayPalOrderId = "ORDERINTERRUPTED",
            Amount = 10m,
            Currency = "USD",
            Status = "PENDING"
        });
        var captureRequestIds = new List<string>();
        var captureAttempt = 0;
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal))
                return Json("{\"access_token\":\"test-token\"}");
            captureRequestIds.Add(request.Headers.GetValues("PayPal-Request-Id").Single());
            if (Interlocked.Increment(ref captureAttempt) == 1)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("{\"name\":\"INTERNAL_ERROR\"}", Encoding.UTF8, "application/json")
                };
            return Json("{\"id\":\"ORDERINTERRUPTED\",\"status\":\"COMPLETED\",\"purchase_units\":[{\"payments\":{\"captures\":[{\"id\":\"CAPTURE-INTERRUPTED\",\"status\":\"COMPLETED\",\"amount\":{\"currency_code\":\"USD\",\"value\":\"10.00\"}}]}}]}");
        });
        var service = Create(db, handler);

        var interrupted = () => service.CaptureAsync(new CaptureDonationRequest("ORDERINTERRUPTED"), default);
        await interrupted.Should().ThrowAsync<InvalidOperationException>();
        var afterInterruption = await donations.Find(x => x.PayPalOrderId == "ORDERINTERRUPTED").FirstAsync();
        afterInterruption.Status.Should().Be("PENDING");
        afterInterruption.ProcessingStartedAtUtc.Should().BeNull();

        var recovered = await service.CaptureAsync(new CaptureDonationRequest("ORDERINTERRUPTED"), default);

        recovered.Status.Should().Be("COMPLETED");
        captureRequestIds.Should().HaveCount(2);
        captureRequestIds[1].Should().Be(captureRequestIds[0]);
        (await donations.Find(x => x.PayPalOrderId == "ORDERINTERRUPTED").FirstAsync()).Status.Should().Be("SUCCEEDED");
    }

    [Fact]
    public async Task Webhook_RejectsInvalidSignatureWithoutPersistingEvent()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal)
            ? Json("{\"access_token\":\"test-token\"}") : Json("{\"verification_status\":\"FAILURE\"}"));
        var service = Create(db, handler, webhookId: "webhook-id");
        using var document = System.Text.Json.JsonDocument.Parse("{\"id\":\"EVENT1\",\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}");

        var result = await service.HandleWebhookAsync(new PayPalWebhookHeaders("tx", "time", "https://api.paypal.com/cert", "SHA256withRSA", "sig"), document.RootElement.GetRawText(), default);

        result.Should().BeFalse();
        (await db.GetCollection<KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent>("donation_webhook_events").CountDocumentsAsync(FilterDefinition<KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent>.Empty)).Should().Be(0);
    }

    [Theory]
    [InlineData("https://paypal.com/cert", true)]
    [InlineData("https://api.paypal.com/cert", true)]
    [InlineData("https://sandbox.api.paypal.com/cert", true)]
    [InlineData("https://evilpaypal.com/cert", false)]
    [InlineData("https://paypal.com.evil.test/cert", false)]
    [InlineData("http://api.paypal.com/cert", false)]
    [InlineData("https://user@api.paypal.com/cert", false)]
    [InlineData("https://api.paypal.com:444/cert", false)]
    [InlineData("not a valid URL", false)]
    public async Task Webhook_ValidatesCertificateUrlHostAndScheme(string certificateUrl, bool trustedHost)
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        var requests = 0;
        var handler = new StubHandler(_ =>
        {
            Interlocked.Increment(ref requests);
            return Json("{\"access_token\":\"test-token\"}");
        });
        var service = Create(db, handler, webhookId: "webhook-id");
        using var document = System.Text.Json.JsonDocument.Parse("{\"id\":\"EVENT-HOST\",\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\"}");

        var result = await service.HandleWebhookAsync(
            new PayPalWebhookHeaders("tx", "time", certificateUrl, "SHA256withRSA", "sig"),
            document.RootElement.GetRawText(), default);

        result.Should().BeFalse();
        requests.Should().Be(trustedHost ? 2 : 0);
    }

    [Fact]
    public async Task Webhook_ReclaimsStaleEventAndAcknowledgesDuplicateDelivery()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        await EnsureWebhookEventIndex(db);
        var donations = db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations");
        await donations.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.Donation
        {
            PayPalOrderId = "ORDERSTALE",
            Amount = 10m,
            Currency = "USD",
            Status = "PENDING"
        });
        var events = db.GetCollection<KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent>("donation_webhook_events");
        await events.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent
        {
            EventId = "EVENT-STALE",
            EventType = "PAYMENT.CAPTURE.COMPLETED",
            Status = "Processing",
            ProcessingStartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-6),
            ProcessingLeaseId = "expired-owner"
        });
        var service = Create(db, SuccessfulWebhookHandler(), webhookId: "webhook-id");
        var body = CompletedWebhook("EVENT-STALE", "ORDERSTALE", "CAPTURE-STALE");
        var headers = ValidWebhookHeaders();

        (await service.HandleWebhookAsync(headers, body, default)).Should().BeTrue();
        var savedEvent = await events.Find(x => x.EventId == "EVENT-STALE").FirstAsync();
        savedEvent.Status.Should().Be("Processed");
        savedEvent.Attempts.Should().Be(1);
        savedEvent.ProcessingStartedAtUtc.Should().BeNull();
        savedEvent.ProcessingLeaseId.Should().BeNull();
        (await donations.Find(x => x.PayPalOrderId == "ORDERSTALE").FirstAsync()).Status.Should().Be("SUCCEEDED");

        var processedAt = savedEvent.ProcessedAtUtc;
        (await service.HandleWebhookAsync(headers, body, default)).Should().BeTrue();
        var afterDuplicate = await events.Find(x => x.EventId == "EVENT-STALE").FirstAsync();
        afterDuplicate.Status.Should().Be("Processed");
        afterDuplicate.Attempts.Should().Be(1);
        afterDuplicate.ProcessedAtUtc.Should().Be(processedAt);
    }

    [Fact]
    public async Task Webhook_ConcurrentDeliveriesAtomicallyClaimStaleEventOnce()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        await EnsureWebhookEventIndex(db);
        var donations = db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations");
        await donations.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.Donation
        {
            PayPalOrderId = "ORDERCONCURRENT",
            Amount = 10m,
            Currency = "USD",
            Status = "PENDING"
        });
        var events = db.GetCollection<KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent>("donation_webhook_events");
        await events.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent
        {
            EventId = "EVENT-CONCURRENT",
            EventType = "PAYMENT.CAPTURE.COMPLETED",
            Status = "Processing",
            ProcessingStartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-6),
            ProcessingLeaseId = "expired-owner"
        });
        var handler = SuccessfulWebhookHandler();
        var body = CompletedWebhook("EVENT-CONCURRENT", "ORDERCONCURRENT", "CAPTURE-CONCURRENT");
        var headers = ValidWebhookHeaders();

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            Create(db, handler, webhookId: "webhook-id").HandleWebhookAsync(headers, body, default)));

        results.Should().OnlyContain(result => result);
        var savedEvent = await events.Find(x => x.EventId == "EVENT-CONCURRENT").FirstAsync();
        savedEvent.Status.Should().Be("Processed");
        savedEvent.Attempts.Should().Be(1);
        savedEvent.ProcessingLeaseId.Should().BeNull();
        (await donations.Find(x => x.PayPalOrderId == "ORDERCONCURRENT").FirstAsync()).Status.Should().Be("SUCCEEDED");
    }

    [Theory]
    [InlineData("PAYMENT.CAPTURE.REFUNDED", "REFUNDED")]
    [InlineData("PAYMENT.CAPTURE.REVERSED", "REVERSED")]
    public async Task Webhook_UpdatesRefundAndReversalPaymentStates(string eventType, string expectedStatus)
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        await EnsureWebhookEventIndex(db);
        var donations = db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations");
        await donations.InsertOneAsync(new KaiAssistant.Domain.Entities.Donations.Donation
        {
            PayPalOrderId = "ORDER-REFUND",
            PayPalCaptureId = "CAPTURE-REFUND",
            Amount = 10m,
            Currency = "USD",
            Status = "SUCCEEDED",
            PaidAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });
        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            id = "EVENT-" + expectedStatus,
            event_type = eventType,
            create_time = DateTimeOffset.UtcNow,
            resource = new
            {
                id = "CAPTURE-REFUND",
                status = expectedStatus,
                amount = new { currency_code = "USD", value = "10.00" },
                supplementary_data = new { related_ids = new { order_id = "ORDER-REFUND" } }
            }
        });
        var service = Create(db, SuccessfulWebhookHandler(), webhookId: "webhook-id");

        var result = await service.HandleWebhookAsync(ValidWebhookHeaders(), body, default);

        result.Should().BeTrue();
        (await donations.Find(x => x.PayPalOrderId == "ORDER-REFUND").FirstAsync()).Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Leaderboard_ExcludesUnconfirmedAndPrivateDonations()
    {
        using var runner = Integration.MongoTestRunner.Start();
        var db = new MongoClient(runner.ConnectionString).GetDatabase("donation-tests");
        await db.GetCollection<KaiAssistant.Domain.Entities.Donations.Donation>("donations").InsertManyAsync([
            new() { PayPalOrderId = "A", Amount = 8, Status = "SUCCEEDED", ShowOnLeaderboard = true, DonorName = "<script>x</script>", Remark = "<img src=x onerror=alert(1)>" },
            new() { PayPalOrderId = "B", Amount = 90, Status = "PENDING", ShowOnLeaderboard = true, DonorName = "Pending" },
            new() { PayPalOrderId = "C", Amount = 70, Status = "SUCCEEDED", ShowOnLeaderboard = false, DonorEmail = "private@example.test" }
        ]);
        var service = Create(db, new StubHandler(_ => Json("{}")));

        var result = await service.GetLeaderboardAsync(default);

        result.Entries.Should().ContainSingle();
        result.Entries[0].Name.Should().Be("scriptx/script");
        result.Entries[0].Remark.Should().Be("img src=x onerror=alert(1)");
        result.TotalDonations.Should().Be(2);
        result.TotalAmount.Should().Be(78);
    }

    private static PayPalDonationService Create(IMongoDatabase db, HttpMessageHandler handler, string webhookId = "")
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("PayPal")).Returns(() => new HttpClient(handler, disposeHandler: false));
        return new PayPalDonationService(factory.Object, db, new EmailSettings("", 25, "", "", "", false), Options.Create(new PayPalOptions
        {
            ClientId = "client",
            ClientSecret = "secret",
            Environment = "sandbox",
            Currency = "USD",
            MinimumDonation = 1,
            MaximumDonation = 1000,
            WebhookId = webhookId
        }), NullLogger<PayPalDonationService>.Instance);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static StubHandler SuccessfulWebhookHandler() => new(request =>
        request.RequestUri!.AbsolutePath.EndsWith("/v1/oauth2/token", StringComparison.Ordinal)
            ? Json("{\"access_token\":\"test-token\"}")
            : Json("{\"verification_status\":\"SUCCESS\"}"));

    private static PayPalWebhookHeaders ValidWebhookHeaders() =>
        new("tx", "time", "https://api.paypal.com/cert", "SHA256withRSA", "sig");

    private static string CompletedWebhook(string eventId, string orderId, string captureId) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            id = eventId,
            event_type = "PAYMENT.CAPTURE.COMPLETED",
            create_time = DateTimeOffset.UtcNow,
            resource = new
            {
                id = captureId,
                status = "COMPLETED",
                amount = new { currency_code = "USD", value = "10.00" },
                supplementary_data = new { related_ids = new { order_id = orderId } }
            }
        });

    private static Task EnsureWebhookEventIndex(IMongoDatabase db) =>
        db.GetCollection<KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent>("donation_webhook_events")
            .Indexes.CreateOneAsync(new CreateIndexModel<KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent>(
                Builders<KaiAssistant.Domain.Entities.Donations.DonationWebhookEvent>.IndexKeys.Ascending(x => x.EventId),
                new CreateIndexOptions { Unique = true }));

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    private sealed class AsyncStubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
