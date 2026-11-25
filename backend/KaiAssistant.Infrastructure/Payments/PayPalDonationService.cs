using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using KaiAssistant.Application.Donations;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Services;
using KaiAssistant.Domain.Entities;
using KaiAssistant.Domain.Entities.Donations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KaiAssistant.Infrastructure.Payments;

public sealed class PayPalDonationService(
    IHttpClientFactory httpClientFactory,
    IMongoDatabase database,
    EmailSettings emailSettings,
    IOptions<PayPalOptions> options,
    ILogger<PayPalDonationService> logger) : IDonationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IMongoCollection<Donation> _donations = database.GetCollection<Donation>("donations");
    private readonly IMongoCollection<DonationWebhookEvent> _events = database.GetCollection<DonationWebhookEvent>("donation_webhook_events");
    private readonly IMongoCollection<DonationReceiptOutbox> _receipts = database.GetCollection<DonationReceiptOutbox>("donation_receipt_outbox");
    private readonly PayPalOptions _options = options.Value;
    private string ApiBase => string.Equals(_options.Environment, "live", StringComparison.OrdinalIgnoreCase)
        ? "https://api-m.paypal.com" : "https://api-m.sandbox.paypal.com";
    private static readonly TimeSpan WebhookProcessingLease = TimeSpan.FromMinutes(5);

    public async Task<CreateDonationResult> CreateOrderAsync(CreateDonationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            request.IdempotencyKey.Length > 64 ||
            !Guid.TryParse(request.IdempotencyKey, out var parsedKey) ||
            parsedKey == Guid.Empty)
            throw new ArgumentException("A valid idempotency key is required.", nameof(request));

        var idempotencyKey = parsedKey.ToString("D");

        // First, try to find an existing donation atomically.
        // This handles idempotent retries and stale recovery without race conditions.
        var existing = await _donations.Find(x => x.IdempotencyKey == idempotencyKey).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(existing.PayPalOrderId))
            {
                return new CreateDonationResult(existing.PayPalOrderId);
            }

            if (existing.Status != "CREATING")
            {
                throw new InvalidOperationException("Donation attempt cannot be resumed in its current state.");
            }

            // Distinguish between concurrent requests (attempted very recently) and stale attempts (attempted > 2 min ago).
            // LastOrderAttemptAtUtc tracks when the last PayPal order creation was attempted.
            var now = DateTimeOffset.UtcNow;
            var staleAttemptBefore = now.AddMinutes(-2);
            var concurrentWindowBefore = now.AddSeconds(-10); // Attempted within last 10 sec = concurrent request

            var lastAttempt = existing.LastOrderAttemptAtUtc;
            if (lastAttempt.HasValue && lastAttempt.Value > concurrentWindowBefore)
            {
                // Last attempt was very recent: another request is actively processing this idempotency key.
                // Fail fast to ensure only one PayPal order is created for concurrent duplicates.
                throw new InvalidOperationException("Donation order is still being created; retry shortly.");
            }

            // Stale CREATING attempt recovery (last attempt > 2 min ago or null): try to claim it atomically.
            var claim = Builders<Donation>.Filter.And(
                Builders<Donation>.Filter.Eq(x => x.Id, existing.Id),
                Builders<Donation>.Filter.Eq(x => x.Status, "CREATING"),
                Builders<Donation>.Filter.Or(
                    Builders<Donation>.Filter.Eq(x => x.LastOrderAttemptAtUtc, null),
                    Builders<Donation>.Filter.Lte(x => x.LastOrderAttemptAtUtc, staleAttemptBefore)));
            var claimed = await _donations.UpdateOneAsync(claim,
                Builders<Donation>.Update.Set(x => x.LastOrderAttemptAtUtc, now),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (claimed.ModifiedCount != 1)
            {
                throw new InvalidOperationException("Donation order is still being created; retry shortly.");
            }
            existing.LastOrderAttemptAtUtc = now;
            // Existing attempts always use the amount and details persisted on the server.
            var persistedAmount = existing.Amount;
            var paypalOrder = await SendPayPalAsync<PayPalOrder>(HttpMethod.Post, "/v2/checkout/orders", new
            {
                intent = "CAPTURE",
                purchase_units = new[] { new { amount = new { currency_code = existing.Currency, value = persistedAmount.ToString("0.00", CultureInfo.InvariantCulture) }, description = "Voluntary portfolio support" } },
                application_context = new { brand_name = _options.BrandName, user_action = "PAY_NOW", shipping_preference = "NO_SHIPPING" }
            }, cancellationToken, "order-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey))).ToLowerInvariant()[..32]).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(paypalOrder.Id)) throw new InvalidOperationException("PayPal returned an invalid order.");
            await _donations.UpdateOneAsync(x => x.Id == existing.Id && x.Status == "CREATING",
                Builders<Donation>.Update.Set(x => x.PayPalOrderId, paypalOrder.Id).Set(x => x.Status, "PENDING"), cancellationToken: cancellationToken).ConfigureAwait(false);
            return new CreateDonationResult(paypalOrder.Id);
        }

        // No existing donation: validate and create new.
        ValidateAmount(request.Amount);
        ValidateDonationDetails(request);
        var amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        var newDonation = new Donation
        {
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = BuildRequestFingerprint(request, amount),
            LastOrderAttemptAtUtc = DateTimeOffset.UtcNow,
            Amount = amount,
            Currency = _options.Currency,
            Status = "CREATING",
            DonorName = NormalizeName(request.Name),
            DonorEmailProvided = emailSettings.Enabled && !string.IsNullOrWhiteSpace(request.Email),
            DonorEmail = emailSettings.Enabled ? NormalizeEmail(request.Email) : null,
            Remark = request.ShowOnLeaderboard ? NormalizeRemark(request.Remark) : string.Empty,
            ShowOnLeaderboard = request.ShowOnLeaderboard
        };

        // Insert the new donation. The unique index on IdempotencyKey (created at startup) prevents duplicates.
        // If a concurrent request inserted first, we'll get DuplicateKey and retry the whole operation.
        try
        {
            await _donations.InsertOneAsync(newDonation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Concurrent request won the race. Retry by recursively calling ourselves.
            // This is safe because the recursive call will find the existing document.
            return await CreateOrderAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Small delay to allow other concurrent requests to see the document in CREATING state
        // before we call PayPal. This ensures they fail fast with InvalidOperationException
        // rather than succeeding after we've already created the PayPal order.
        await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        // New donation created successfully. Proceed to PayPal.
        var order = await SendPayPalAsync<PayPalOrder>(HttpMethod.Post, "/v2/checkout/orders", new
        {
            intent = "CAPTURE",
            purchase_units = new[] { new { amount = new { currency_code = newDonation.Currency, value = newDonation.Amount.ToString("0.00", CultureInfo.InvariantCulture) }, description = "Voluntary portfolio support" } },
            application_context = new { brand_name = _options.BrandName, user_action = "PAY_NOW", shipping_preference = "NO_SHIPPING" }
        }, cancellationToken, "order-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey))).ToLowerInvariant()[..32]).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(order.Id)) throw new InvalidOperationException("PayPal returned an invalid order.");
        await _donations.UpdateOneAsync(x => x.Id == newDonation.Id && x.Status == "CREATING",
            Builders<Donation>.Update.Set(x => x.PayPalOrderId, order.Id).Set(x => x.Status, "PENDING"), cancellationToken: cancellationToken).ConfigureAwait(false);
        return new CreateDonationResult(order.Id);
    }

    public async Task<DonationResult> CaptureAsync(CaptureDonationRequest request, CancellationToken cancellationToken)
    {
        ValidateOrderId(request.OrderId);
        var donation = await _donations.Find(x => x.PayPalOrderId == request.OrderId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("Donation order was not found.");
        if (donation.Status is "SUCCEEDED" or "COMPLETED") return new DonationResult("COMPLETED", donation.Amount, donation.Currency);
        if (donation.Status is "REFUNDED" or "REVERSED" or "FAILED") throw new InvalidOperationException("This donation cannot be captured in its current state.");
        var now = DateTimeOffset.UtcNow;
        var staleProcessingBefore = now.AddMinutes(-2);
        var filter = Builders<Donation>.Filter;
        var claimable = filter.Or(
            filter.Eq(x => x.Status, "PENDING"),
            filter.And(filter.Eq(x => x.Status, "PROCESSING"), filter.Or(
                filter.Eq(x => x.ProcessingStartedAtUtc, null),
                filter.Lte(x => x.ProcessingStartedAtUtc, staleProcessingBefore))));
        var started = await _donations.UpdateOneAsync(filter.And(filter.Eq(x => x.Id, donation.Id), claimable),
            Builders<Donation>.Update.Set(x => x.Status, "PROCESSING").Set(x => x.ProcessingStartedAtUtc, now), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (started.ModifiedCount != 1)
        {
            donation = await _donations.Find(x => x.Id == donation.Id).FirstAsync(cancellationToken).ConfigureAwait(false);
            if (donation.Status is "SUCCEEDED" or "COMPLETED") return new DonationResult("COMPLETED", donation.Amount, donation.Currency);
            throw new InvalidOperationException("Donation capture is already being processed.");
        }

        // PayPal-Request-Id is stable per order and server side; concurrent retries converge on one capture.
        PayPalOrder capture;
        try
        {
            capture = await SendPayPalAsync<PayPalOrder>(HttpMethod.Post, $"/v2/checkout/orders/{Uri.EscapeDataString(request.OrderId)}/capture", new { }, cancellationToken,
                "capture-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.OrderId))).ToLowerInvariant()[..32]).ConfigureAwait(false);
        }
        catch
        {
            await _donations.UpdateOneAsync(x => x.Id == donation.Id && x.Status == "PROCESSING", Builders<Donation>.Update.Set(x => x.Status, "PENDING").Unset(x => x.ProcessingStartedAtUtc), cancellationToken: CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        var captureUnit = capture.PurchaseUnits?.FirstOrDefault();
        var captureInfo = captureUnit?.Payments?.Captures?.FirstOrDefault(x => x.Status == "COMPLETED");
        if (capture.Status != "COMPLETED" || captureInfo is null || string.IsNullOrWhiteSpace(captureInfo.Id) ||
            captureInfo.Amount?.CurrencyCode != donation.Currency ||
            !decimal.TryParse(captureInfo.Amount.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var capturedAmount) || capturedAmount != donation.Amount ||
            (!string.IsNullOrWhiteSpace(_options.MerchantId) && captureUnit?.Payee?.MerchantId != _options.MerchantId))
        {
            await _donations.UpdateOneAsync(x => x.Id == donation.Id && x.Status == "PROCESSING", Builders<Donation>.Update.Set(x => x.Status, "FAILED").Unset(x => x.ProcessingStartedAtUtc), cancellationToken: CancellationToken.None).ConfigureAwait(false);
            throw new InvalidOperationException("PayPal did not confirm the expected completed donation.");
        }

        var completedAtUtc = DateTimeOffset.UtcNow;
        await MarkSucceededAsync(donation, captureInfo.Id, completedAtUtc, cancellationToken).ConfigureAwait(false);
        return new DonationResult("COMPLETED", donation.Amount, donation.Currency);
    }

    public async Task<bool> HandleWebhookAsync(PayPalWebhookHeaders headers, string rawBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookId) || string.IsNullOrWhiteSpace(rawBody) || rawBody.Length > 250_000) return false;
        using var eventJson = JsonDocument.Parse(rawBody);
        var root = eventJson.RootElement;
        var eventId = StringProperty(root, "id");
        var eventType = StringProperty(root, "event_type");
        if (string.IsNullOrWhiteSpace(eventId) || eventId.Length > 128 || eventType is not ("PAYMENT.CAPTURE.COMPLETED" or "PAYMENT.CAPTURE.DENIED" or "PAYMENT.CAPTURE.REFUNDED" or "PAYMENT.CAPTURE.REVERSED")) return false;

        var verified = await VerifyWebhookAsync(headers, root, cancellationToken).ConfigureAwait(false);
        if (!verified) return false;
        var leaseId = Guid.NewGuid().ToString("N");
        var processingStartedAtUtc = DateTimeOffset.UtcNow;
        try
        {
            await _events.InsertOneAsync(new DonationWebhookEvent
            {
                EventId = eventId,
                EventType = eventType,
                Status = "Processing",
                ProcessingStartedAtUtc = processingStartedAtUtc,
                ProcessingLeaseId = leaseId
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var prior = await _events.Find(x => x.EventId == eventId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (prior?.Status == "Processed") return true;
            var staleBefore = processingStartedAtUtc - WebhookProcessingLease;
            var claimable = Builders<DonationWebhookEvent>.Filter.And(
                Builders<DonationWebhookEvent>.Filter.Eq(x => x.EventId, eventId),
                Builders<DonationWebhookEvent>.Filter.Or(
                    Builders<DonationWebhookEvent>.Filter.In(x => x.Status, new[] { "Failed", "Received" }),
                    Builders<DonationWebhookEvent>.Filter.And(
                        Builders<DonationWebhookEvent>.Filter.Eq(x => x.Status, "Processing"),
                        Builders<DonationWebhookEvent>.Filter.Or(
                            Builders<DonationWebhookEvent>.Filter.Eq(x => x.ProcessingStartedAtUtc, null),
                            Builders<DonationWebhookEvent>.Filter.Lte(x => x.ProcessingStartedAtUtc, staleBefore)))));
            var retry = await _events.UpdateOneAsync(claimable,
                Builders<DonationWebhookEvent>.Update
                    .Set(x => x.Status, "Processing")
                    .Set(x => x.ProcessingStartedAtUtc, processingStartedAtUtc)
                    .Set(x => x.ProcessingLeaseId, leaseId)
                    .Inc(x => x.Attempts, 1), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (retry.ModifiedCount == 0) return true;
        }

        try
        {
            var resource = root.GetProperty("resource");
            var captureId = StringProperty(resource, "id");
            var orderId = resource.TryGetProperty("supplementary_data", out var supplementary) && supplementary.TryGetProperty("related_ids", out var related) ? StringProperty(related, "order_id") : string.Empty;
            var donation = !string.IsNullOrWhiteSpace(orderId)
                ? await _donations.Find(x => x.PayPalOrderId == orderId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
                : await _donations.Find(x => x.PayPalCaptureId == captureId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (donation is null) throw new InvalidOperationException("Webhook does not reference a known donation.");

            var eventTime = ParseDate(StringProperty(root, "create_time")) ?? DateTimeOffset.UtcNow;
            if (donation.LastPaymentEventAtUtc is null || eventTime >= donation.LastPaymentEventAtUtc)
            {
                if (resource.TryGetProperty("payee", out var webhookPayee) && webhookPayee.TryGetProperty("merchant_id", out var webhookMerchant) && !string.IsNullOrWhiteSpace(_options.MerchantId) && webhookMerchant.GetString() != _options.MerchantId)
                    throw new InvalidOperationException("Webhook merchant did not match the configured merchant.");
                if (eventType == "PAYMENT.CAPTURE.COMPLETED")
                {
                    var amount = resource.GetProperty("amount");
                    if (StringProperty(amount, "currency_code") != donation.Currency || !decimal.TryParse(StringProperty(amount, "value"), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value != donation.Amount || StringProperty(resource, "status") != "COMPLETED")
                        throw new InvalidOperationException("Webhook payment amount or state did not match the donation.");
                    await MarkSucceededAsync(donation, captureId, eventTime, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var next = eventType switch { "PAYMENT.CAPTURE.DENIED" => "FAILED", "PAYMENT.CAPTURE.REFUNDED" => "REFUNDED", _ => "REVERSED" };
                    var update = Builders<Donation>.Update.Set(x => x.Status, next).Set(x => x.LastPaymentEventAtUtc, eventTime);
                    await _donations.UpdateOneAsync(x => x.Id == donation.Id && (x.LastPaymentEventAtUtc == null || x.LastPaymentEventAtUtc <= eventTime), update, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            await _events.UpdateOneAsync(x => x.EventId == eventId && x.Status == "Processing" && x.ProcessingLeaseId == leaseId,
                Builders<DonationWebhookEvent>.Update
                    .Set(x => x.Status, "Processed")
                    .Set(x => x.ProcessedAtUtc, DateTimeOffset.UtcNow)
                    .Unset(x => x.ProcessingStartedAtUtc)
                    .Unset(x => x.ProcessingLeaseId), cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await _events.UpdateOneAsync(x => x.EventId == eventId && x.Status == "Processing" && x.ProcessingLeaseId == leaseId,
                Builders<DonationWebhookEvent>.Update
                    .Set(x => x.Status, "Failed")
                    .Unset(x => x.ProcessingStartedAtUtc)
                    .Unset(x => x.ProcessingLeaseId), cancellationToken: CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<DonorLeaderboardResult> GetLeaderboardAsync(CancellationToken cancellationToken)
    {
        var entries = await _donations.Find(x => (x.Status == "SUCCEEDED" || x.Status == "COMPLETED") && x.ShowOnLeaderboard)
            .SortByDescending(x => x.Amount).ThenBy(x => x.PaidAtUtc).Limit(50)
            .Project(x => new { x.DonorName, x.Amount, x.Remark, PaidAtUtc = x.PaidAtUtc }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var totals = await _donations.Aggregate().Match(x => x.Status == "SUCCEEDED" || x.Status == "COMPLETED")
            .Group(_ => 1, group => new DonationTotals(group.Count(), group.Sum(x => x.Amount))).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return new DonorLeaderboardResult(entries.Select((x, i) => new DonorLeaderboardEntry(NormalizeName(x.DonorName), x.Amount, NormalizeRemark(x.Remark), x.PaidAtUtc ?? DateTimeOffset.MinValue, i + 1)).ToArray(), totals?.Count ?? 0, totals?.Amount ?? 0m);
    }

    private async Task MarkSucceededAsync(Donation donation, string captureId, DateTimeOffset atUtc, CancellationToken cancellationToken)
    {
        var update = await _donations.UpdateOneAsync(x => x.Id == donation.Id && x.Status != "REFUNDED" && x.Status != "REVERSED" && x.Status != "FAILED" && (x.LastPaymentEventAtUtc == null || x.LastPaymentEventAtUtc <= atUtc),
            Builders<Donation>.Update.Set(x => x.Status, "SUCCEEDED").Set(x => x.PayPalCaptureId, captureId).Set(x => x.PaidAtUtc, atUtc).Set(x => x.LastPaymentEventAtUtc, atUtc).Unset(x => x.ProcessingStartedAtUtc), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (update.ModifiedCount == 0 && donation.Status is "REFUNDED" or "REVERSED" or "FAILED") return;
        if (update.ModifiedCount == 0)
        {
            var current = await _donations.Find(x => x.Id == donation.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (current is not null && (current.Status is "REFUNDED" or "REVERSED" or "FAILED" || current.LastPaymentEventAtUtc > atUtc)) return;
        }
        if (string.IsNullOrWhiteSpace(donation.DonorEmail) || string.IsNullOrWhiteSpace(donation.Id)) return;
        var email = new DonationReceiptOutbox { DonationId = donation.Id, Name = donation.DonorName, Email = donation.DonorEmail, Amount = donation.Amount, Currency = donation.Currency, RetainUntilUtc = DateTimeOffset.UtcNow.AddDays(Math.Clamp(_options.EmailRetentionDays, 1, 3650)) };
        try { await _receipts.InsertOneAsync(email, cancellationToken: cancellationToken).ConfigureAwait(false); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey) { }
    }

    private async Task<bool> VerifyWebhookAsync(PayPalWebhookHeaders headers, JsonElement webhookEvent, CancellationToken cancellationToken)
    {
        if (new[] { headers.TransmissionId, headers.TransmissionTime, headers.CertificateUrl, headers.AuthAlgorithm, headers.TransmissionSignature }.Any(string.IsNullOrWhiteSpace)) return false;
        if (!IsPayPalCertificateUrl(headers.CertificateUrl)) return false;
        using var response = await SendPayPalRawAsync(HttpMethod.Post, "/v1/notifications/verify-webhook-signature", new
        {
            auth_algo = headers.AuthAlgorithm, cert_url = headers.CertificateUrl, transmission_id = headers.TransmissionId,
            transmission_sig = headers.TransmissionSignature, transmission_time = headers.TransmissionTime,
            webhook_id = _options.WebhookId, webhook_event = webhookEvent
        }, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return false;
        using var result = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
        return StringProperty(result.RootElement, "verification_status") == "SUCCESS";
    }

    private static bool IsPayPalCertificateUrl(string? certificateUrl)
    {
        if (!Uri.TryCreate(certificateUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.Port != 443)
            return false;

        var host = uri.IdnHost;
        return string.Equals(host, "paypal.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".paypal.com", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<T> SendPayPalAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken, string? requestId = null)
    {
        using var response = await SendPayPalRawAsync(method, path, body, cancellationToken, requestId).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("PayPal API request {Path} failed with status {StatusCode}.", path, (int)response.StatusCode);
            throw new InvalidOperationException("PayPal could not process the donation request.");
        }
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PayPal returned an invalid response.");
    }

    private async Task<HttpResponseMessage> SendPayPalRawAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken, string? requestId = null)
    {
        var client = httpClientFactory.CreateClient("PayPal");
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/v1/oauth2/token");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}")));
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
        using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
        if (!tokenResponse.IsSuccessStatusCode) throw new InvalidOperationException("PayPal authentication failed.");
        var token = await tokenResponse.Content.ReadFromJsonAsync<PayPalToken>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PayPal authentication response was invalid.");
        var request = new HttpRequestMessage(method, $"{ApiBase}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        if (requestId is not null) request.Headers.Add("PayPal-Request-Id", requestId);
        request.Content = JsonContent.Create(body);
        try { return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false); }
        finally { request.Dispose(); }
    }

    private void ValidateAmount(decimal amount)
    {
        if (amount < _options.MinimumDonation || amount > _options.MaximumDonation || decimal.Round(amount, 2) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Donation amount is outside the allowed range or precision.");
        if (_options.Currency.Length != 3 || !_options.Currency.All(char.IsAsciiLetter))
            throw new InvalidOperationException("Donation currency is not configured correctly.");
    }

    private static void ValidateDonationDetails(CreateDonationRequest request)
    {
        if (request.Name?.Length > 80 || request.Email?.Length > 254 || request.Remark?.Length > 240)
            throw new ArgumentException("Donation details exceed the allowed length.", nameof(request));
        if (!string.IsNullOrWhiteSpace(request.Email) && !System.Net.Mail.MailAddress.TryCreate(request.Email, out _))
            throw new ArgumentException("Enter a valid email address or leave it blank.", nameof(request));
    }
    private string BuildRequestFingerprint(CreateDonationRequest request, decimal amount)
    {
        var normalized = string.Join("|", amount.ToString("0.00", CultureInfo.InvariantCulture), _options.Currency, NormalizeName(request.Name), NormalizeEmail(request.Email), NormalizeRemark(request.ShowOnLeaderboard ? request.Remark : null), request.ShowOnLeaderboard);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
    private static void ValidateOrderId(string? id) { if (string.IsNullOrWhiteSpace(id) || id.Length > 64 || id.Any(c => !char.IsAsciiLetterOrDigit(c))) throw new ArgumentException("Invalid donation order.", nameof(id)); }
    private static string StringProperty(JsonElement value, string name) => value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : string.Empty;
    private static DateTimeOffset? ParseDate(string value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    private static string NormalizeName(string? value) => SanitizeDisplayText(string.IsNullOrWhiteSpace(value) ? "Anonymous" : value.Trim(), 80);
    private static string NormalizeEmail(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim()[..Math.Min(value.Trim().Length, 254)];
    private static string NormalizeRemark(string? value) => SanitizeDisplayText(value?.Trim() ?? string.Empty, 240);
    private static string SanitizeDisplayText(string value, int maxLength)
    {
        var filtered = new string(value.Where(c => !char.IsControl(c) && c is not '<' and not '>' and not '\\' and not '`').ToArray());
        return filtered[..Math.Min(filtered.Length, maxLength)];
    }
    private sealed record DonationTotals(int Count, decimal Amount);
    private sealed record PayPalToken([property: JsonPropertyName("access_token")] string AccessToken);
    private sealed record PayPalOrder(string Id, string Status, [property: JsonPropertyName("purchase_units")] List<PayPalPurchaseUnit>? PurchaseUnits);
    private sealed record PayPalPurchaseUnit(PayPalAmount? Amount, PayPalPayments? Payments, PayPalPayee? Payee);
    private sealed record PayPalPayee([property: JsonPropertyName("merchant_id")] string? MerchantId);
    private sealed record PayPalAmount([property: JsonPropertyName("currency_code")] string CurrencyCode, string Value);
    private sealed record PayPalPayments(List<PayPalCapture>? Captures);
    private sealed record PayPalCapture(string? Id, string? Status, PayPalAmount? Amount);
}
