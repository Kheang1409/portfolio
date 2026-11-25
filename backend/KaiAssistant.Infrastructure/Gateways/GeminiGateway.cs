using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Diagnostics;
using KaiAssistant.Application.Options;
using KaiAssistant.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
namespace KaiAssistant.Infrastructure.Gateways;

public class GeminiGateway : IGeminiGateway
{
    private static readonly Meter Meter = new("KaiAssistant.AiModels", "1.0.0");
    private static readonly Counter<long> ModelRequests = Meter.CreateCounter<long>("ai_model_requests_total");
    private static readonly Counter<long> ModelFailures = Meter.CreateCounter<long>("ai_model_failures_total");
    private static readonly Counter<long> FallbackCount = Meter.CreateCounter<long>("ai_fallback_count");
    private static readonly Histogram<double> ModelLatency = Meter.CreateHistogram<double>("ai_model_latency_ms", "ms");
    private readonly IHttpClientFactory _factory;
    private readonly IOptions<GeminiSettings> _settings;
    private readonly IOptionsMonitor<AiModelOrchestrationOptions> _orchestrationOptions;
    private readonly IModelHealthService _modelHealth;
    private readonly IResilienceStatusProvider _resilience;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GeminiGateway> _logger;
    private readonly object _cacheLock = new();
    private string? _lastSuccessfulModel;
    private DateTime _lastSuccessfulAt = DateTime.MinValue;
    private readonly ConcurrentDictionary<string, int> _model429Counts = new();
    public GeminiGateway(
        IHttpClientFactory factory,
        IOptions<GeminiSettings> settings,
        IOptionsMonitor<AiModelOrchestrationOptions> orchestrationOptions,
        IModelHealthService modelHealth,
        IResilienceStatusProvider resilience,
        IHostEnvironment environment,
        ILogger<GeminiGateway> logger)
    {
        _factory = factory;
        _settings = settings;
        _orchestrationOptions = orchestrationOptions;
        _modelHealth = modelHealth;
        _resilience = resilience;
        _environment = environment;
        _logger = logger;
    }
    public async Task<(string? Body, string? UsedModel)> SendGenerationRequestAsync(string payloadJson, IEnumerable<string> models, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("Gemini");
        var modelList = models.ToList();
        _modelHealth.EnsureModelsRegistered(modelList, DateTimeOffset.UtcNow);
        var deprioritized = new List<string>();
        string? preferred = null;
        lock (_cacheLock)
        {
            if (!string.IsNullOrWhiteSpace(_lastSuccessfulModel))
            {
                var ttl = TimeSpan.FromSeconds(_settings.Value.LastSuccessfulModelCacheTtlSeconds);
                if (DateTime.UtcNow - _lastSuccessfulAt <= ttl && modelList.Contains(_lastSuccessfulModel))
                {
                    preferred = _lastSuccessfulModel;
                    modelList.Remove(_lastSuccessfulModel);
                    modelList.Insert(0, _lastSuccessfulModel);
                    _logger.LogDebug("Preferring last successful model {Model}", _lastSuccessfulModel);
                }
            }
        }
        var threshold = _settings.Value.DeprioritizeOn429Count;
        var skipMode = _settings.Value.DeprioritizeSkip;
        var filtered = new List<string>();
        foreach (var m in modelList)
        {
            _model429Counts.TryGetValue(m, out var cnt);
            if (cnt >= threshold)
            {
                if (skipMode)
                {
                    _logger.LogInformation("Skipping model {Model} because 429 count {Count} >= threshold {Threshold}", m, cnt, threshold);
                    continue;
                }
                _logger.LogInformation("Deprioritizing model {Model} because 429 count {Count} >= threshold {Threshold}", m, cnt, threshold);
                deprioritized.Add(m);
                continue;
            }
            filtered.Add(m);
        }
        modelList = filtered.Concat(deprioritized).ToList();
        for (var modelIndex = 0; modelIndex < modelList.Count; modelIndex++)
        {
            var model = modelList[modelIndex];
            if (!_modelHealth.CanAttempt(model, DateTimeOffset.UtcNow))
            {
                _logger.LogInformation("Skipping model {Model} due to open circuit/health state.", model);
                continue;
            }
            var attempt = 0;
            var maxAttempts = 3;
            var backoffMs = 1000;
            while (attempt < maxAttempts)
            {
                attempt++;
                ModelRequests.Add(1, KeyValuePair.Create<string, object?>("model", model));
                var callSw = Stopwatch.StartNew();
                try
                {
                    if (ShouldInjectFailure(out var failureType))
                    {
                        if (failureType == "429")
                        {
                            _model429Counts.AddOrUpdate(model, 1, (_, old) => old + 1);
                            _modelHealth.MarkRateLimited(model, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(10, 45)), "simulated_429");
                            _resilience.RecordFailure("ai", "simulated_429");
                            ModelFailures.Add(1,
                                KeyValuePair.Create<string, object?>("model", model),
                                KeyValuePair.Create<string, object?>("status", "simulated_429"));
                            _logger.LogInformation("Failure simulation injected 429 for model {Model}", model);
                            break;
                        }
                        var spikeMs = Random.Shared.Next(800, 2200);
                        _logger.LogInformation("Failure simulation injected latency spike={LatencyMs} for model {Model}", spikeMs, model);
                        await Task.Delay(spikeMs, cancellationToken).ConfigureAwait(false);
                    }
                    var url = $"{_settings.Value.Endpoint}{model}";
                    using var req = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                    };
                    req.Headers.TryAddWithoutValidation("x-goog-api-key", _settings.Value.ApiKey);
                    var resp = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
                    callSw.Stop();
                    ModelLatency.Record(callSw.Elapsed.TotalMilliseconds, KeyValuePair.Create<string, object?>("model", model));
                    _modelHealth.RecordLatency(model, DateTimeOffset.UtcNow, callSw.Elapsed.TotalMilliseconds);
                    if (resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        lock (_cacheLock)
                        {
                            _lastSuccessfulModel = model;
                            _lastSuccessfulAt = DateTime.UtcNow;
                        }
                        _model429Counts.AddOrUpdate(model, 0, (_, __) => 0);
                        _resilience.RecordSuccess("ai");
                        _modelHealth.RecordSuccess(model, DateTimeOffset.UtcNow, fallbackUsed: modelIndex > 0);
                        _modelHealth.SetActiveModel(model, DateTimeOffset.UtcNow);
                        if (modelIndex > 0)
                        {
                            FallbackCount.Add(1,
                                KeyValuePair.Create<string, object?>("to_model", model),
                                KeyValuePair.Create<string, object?>("from_model", modelList[0]));
                        }
                        return (body, model);
                    }
                    var bodyErr = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogWarning("Gemini model {Model} returned {Status} (attempt {Attempt}/{Max}): {Body}", model, resp.StatusCode, attempt, maxAttempts, bodyErr);
                    _resilience.RecordFailure("ai", $"{resp.StatusCode}");
                    ModelFailures.Add(1,
                        KeyValuePair.Create<string, object?>("model", model),
                        KeyValuePair.Create<string, object?>("status", (int)resp.StatusCode));
                    if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (int)resp.StatusCode == 429)
                    {
                        _model429Counts.AddOrUpdate(model, 1, (_, old) => old + 1);
                        var isQuotaExhausted = bodyErr.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase) ||
                                               bodyErr.Contains("limit: 0", StringComparison.OrdinalIgnoreCase);
                        if (isQuotaExhausted)
                        {
                            _modelHealth.MarkRateLimited(model, DateTimeOffset.UtcNow, retryAtUtc: null, reason: "quota_exhausted");
                            _logger.LogWarning("Quota exhausted for model {Model}; skipping retries for this model.", model);
                            break;
                        }
                        TimeSpan wait = TimeSpan.FromMilliseconds(backoffMs);
                        if (resp.Headers.RetryAfter != null)
                        {
                            if (resp.Headers.RetryAfter.Delta.HasValue)
                                wait = resp.Headers.RetryAfter.Delta.Value;
                            else if (resp.Headers.RetryAfter.Date.HasValue)
                                wait = resp.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                        }
                        else
                        {
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(bodyErr);
                                if (doc.RootElement.TryGetProperty("details", out var details) && details.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var d in details.EnumerateArray())
                                    {
                                        if (d.TryGetProperty("@type", out var t) && t.GetString()?.Contains("RetryInfo") == true)
                                        {
                                            if (d.TryGetProperty("retryDelay", out var rd) && rd.ValueKind == System.Text.Json.JsonValueKind.String)
                                            {
                                                if (System.TimeSpan.TryParse(rd.GetString(), out var parsed))
                                                {
                                                    wait = parsed;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                        var skipThreshold = TimeSpan.FromSeconds(_settings.Value.SkipRetryDelayThresholdSeconds);
                        if (wait > skipThreshold)
                        {
                            _modelHealth.MarkRateLimited(model, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.Add(wait), "retry_delay_too_large");
                            _logger.LogInformation("Skipping model {Model} due to large retryDelay {RetryDelay}", model, wait);
                            break;
                        }
                        _modelHealth.MarkRateLimited(model, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.Add(wait), "http_429");
                        if (attempt >= maxAttempts)
                        {
                            _logger.LogWarning("Exceeded retry attempts for model {Model} after receiving 429.", model);
                            break;
                        }
                        _logger.LogInformation("Waiting {Wait} before retrying model {Model} (attempt {Attempt}/{Max})", wait, model, attempt + 1, maxAttempts);
                        try { await Task.Delay(wait, cancellationToken).ConfigureAwait(false); } catch (OperationCanceledException) { throw; }
                        backoffMs *= 2;
                        continue;
                    }
                    _modelHealth.RecordFailure(model, DateTimeOffset.UtcNow, $"{resp.StatusCode}");
                    if ((int)resp.StatusCode >= 500 && (int)resp.StatusCode <= 599)
                    {
                        if (attempt >= maxAttempts)
                        {
                            break;
                        }
                        var jitter = Random.Shared.Next(0, 250);
                        var wait = TimeSpan.FromMilliseconds(backoffMs + jitter);
                        try { await Task.Delay(wait, cancellationToken).ConfigureAwait(false); } catch (OperationCanceledException) { throw; }
                        backoffMs *= 2;
                        continue;
                    }
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Gemini request cancelled for model {Model}", model);
                    throw;
                }
                catch (System.Exception ex)
                {
                    callSw.Stop();
                    _modelHealth.RecordLatency(model, DateTimeOffset.UtcNow, callSw.Elapsed.TotalMilliseconds);
                    _logger.LogError(ex, "Exception calling Gemini model {Model} (attempt {Attempt}/{Max})", model, attempt, maxAttempts);
                    _resilience.RecordFailure("ai", ex.Message);
                    ModelFailures.Add(1,
                        KeyValuePair.Create<string, object?>("model", model),
                        KeyValuePair.Create<string, object?>("status", "exception"));
                    _modelHealth.RecordFailure(model, DateTimeOffset.UtcNow, ex.Message);
                    if (attempt >= maxAttempts) break;
                    try { await Task.Delay(backoffMs, cancellationToken).ConfigureAwait(false); } catch (OperationCanceledException) { throw; }
                    backoffMs *= 2;
                }
            }
        }
        return (null, null);
    }
    private bool ShouldInjectFailure(out string failureType)
    {
        failureType = "none";
        if (_environment.IsProduction())
        {
            return false;
        }
        var options = _orchestrationOptions.CurrentValue;
        if (!options.EnableFailureSimulation)
        {
            return false;
        }
        var rate = Math.Clamp(options.FailureInjectionRate, 0d, 1d);
        if (rate <= 0 || Random.Shared.NextDouble() > rate)
        {
            return false;
        }
        failureType = Random.Shared.NextDouble() < 0.5 ? "429" : "latency_spike";
        return true;
    }
    public async IAsyncEnumerable<AiGatewayStreamChunk> StreamGenerationRequestAsync(
        string payloadJson,
        IEnumerable<string> models,
        int maxDurationSeconds,
        int maxTokens,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelList = models
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _modelHealth.EnsureModelsRegistered(modelList, DateTimeOffset.UtcNow);
        if (modelList.Count == 0)
        {
            yield break;
        }
        var durationCap = TimeSpan.FromSeconds(Math.Clamp(maxDurationSeconds, 5, 600));
        var tokenCap = Math.Clamp(maxTokens, 32, 16384);
        for (var modelIndex = 0; modelIndex < modelList.Count; modelIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var model = modelList[modelIndex];
            if (!_modelHealth.CanAttempt(model, DateTimeOffset.UtcNow))
            {
                continue;
            }
            var producedAny = false;
            var emittedTokens = 0;
            var sw = Stopwatch.StartNew();
            if (ShouldInjectFailure(out var streamFailureType))
            {
                if (streamFailureType == "429")
                {
                    _model429Counts.AddOrUpdate(model, 1, (_, old) => old + 1);
                    _modelHealth.MarkRateLimited(model, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(10, 45)), "simulated_429_stream");
                    _resilience.RecordFailure("ai", "simulated_429_stream");
                    ModelFailures.Add(1,
                        KeyValuePair.Create<string, object?>("model", model),
                        KeyValuePair.Create<string, object?>("status", "simulated_429_stream"));
                    continue;
                }
                var spikeMs = Random.Shared.Next(800, 2200);
                await Task.Delay(spikeMs, cancellationToken).ConfigureAwait(false);
            }
            var capture = await TryCaptureModelStreamAsync(payloadJson, model, durationCap, tokenCap, cancellationToken).ConfigureAwait(false);
            if (capture.Error is not null)
            {
                _logger.LogWarning("Streaming request failed for model {Model}; trying next candidate. {Error}", model, capture.Error);
                _resilience.RecordFailure("ai", capture.Error);
                _modelHealth.RecordFailure(model, DateTimeOffset.UtcNow, capture.Error);
                if (TryParse429Error(capture.Error, out var isQuotaExhausted, out var retryAfter))
                {
                    _model429Counts.AddOrUpdate(model, 1, (_, old) => old + 1);
                    if (isQuotaExhausted)
                    {
                        _modelHealth.MarkRateLimited(model, DateTimeOffset.UtcNow, retryAtUtc: null, reason: "quota_exhausted_stream");
                        _logger.LogWarning("Quota exhausted for model {Model} in streaming path; switching to next model.", model);
                    }
                    else
                    {
                        var skipThreshold = TimeSpan.FromSeconds(_settings.Value.SkipRetryDelayThresholdSeconds);
                        var retryAt = retryAfter.HasValue ? DateTimeOffset.UtcNow.Add(retryAfter.Value) : (DateTimeOffset?)null;
                        var reason = retryAfter.HasValue && retryAfter.Value > skipThreshold
                            ? "retry_delay_too_large_stream"
                            : "http_429_stream";
                        _modelHealth.MarkRateLimited(model, DateTimeOffset.UtcNow, retryAt, reason);
                    }
                }
                continue;
            }
            foreach (var delta in capture.Deltas)
            {
                if (string.IsNullOrWhiteSpace(delta))
                {
                    continue;
                }
                producedAny = true;
                emittedTokens += EstimateTokens(delta);
                yield return new AiGatewayStreamChunk
                {
                    DeltaText = delta,
                    UsedModel = model,
                    FallbackUsed = modelIndex > 0,
                    IsCompleted = false
                };
                if (sw.Elapsed >= durationCap || emittedTokens >= tokenCap)
                {
                    break;
                }
            }
            if (producedAny)
            {
                _resilience.RecordSuccess("ai");
                _modelHealth.RecordSuccess(model, DateTimeOffset.UtcNow, fallbackUsed: modelIndex > 0);
                _modelHealth.SetActiveModel(model, DateTimeOffset.UtcNow);
                _model429Counts.AddOrUpdate(model, 0, (_, __) => 0);
                if (modelIndex > 0)
                {
                    FallbackCount.Add(1,
                        KeyValuePair.Create<string, object?>("to_model", model),
                        KeyValuePair.Create<string, object?>("from_model", modelList[0]));
                }
                yield return new AiGatewayStreamChunk
                {
                    UsedModel = model,
                    FallbackUsed = modelIndex > 0,
                    IsCompleted = true
                };
                yield break;
            }
        }
        var buffered = await SendGenerationRequestAsync(payloadJson, modelList, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(buffered.Body))
        {
            yield break;
        }
        var bufferedText = ExtractTextFromResponseJson(buffered.Body);
        if (string.IsNullOrWhiteSpace(bufferedText))
        {
            yield break;
        }
        var bufferedFallbackUsed = modelList.Count > 1
            && !string.Equals(buffered.UsedModel, modelList[0], StringComparison.OrdinalIgnoreCase);
        foreach (var chunk in ChunkText(bufferedText, 64))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new AiGatewayStreamChunk
            {
                DeltaText = chunk,
                UsedModel = buffered.UsedModel,
                FallbackUsed = bufferedFallbackUsed,
                IsCompleted = false
            };
        }
        yield return new AiGatewayStreamChunk
        {
            UsedModel = buffered.UsedModel,
            FallbackUsed = bufferedFallbackUsed,
            IsCompleted = true
        };
    }
    private async IAsyncEnumerable<string> StreamModelAsync(
        string payloadJson,
        string model,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var client = _factory.CreateClient("Gemini");
        var streamModel = model;
        var methodSeparator = streamModel.IndexOf(':');
        if (methodSeparator > 0)
        {
            streamModel = streamModel[..methodSeparator];
        }
        var url = $"{_settings.Value.Endpoint}{streamModel}:streamGenerateContent?alt=sse";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("x-goog-api-key", _settings.Value.ApiKey);
        using var resp = await client
            .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"Streaming call failed: {(int)resp.StatusCode} {resp.StatusCode} {err}");
        }
        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        var lastCombined = string.Empty;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var payload = line[5..].Trim();
            if (string.IsNullOrWhiteSpace(payload) || string.Equals(payload, "[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var current = ExtractTextFromResponseJson(payload);
            if (string.IsNullOrWhiteSpace(current))
            {
                continue;
            }
            string delta;
            if (!string.IsNullOrEmpty(lastCombined) && current.StartsWith(lastCombined, StringComparison.Ordinal))
            {
                delta = current[lastCombined.Length..];
            }
            else
            {
                delta = current;
            }
            lastCombined = current;
            if (!string.IsNullOrWhiteSpace(delta))
            {
                yield return delta;
            }
        }
    }
    private async Task<(List<string> Deltas, string? Error)> TryCaptureModelStreamAsync(
        string payloadJson,
        string model,
        TimeSpan durationCap,
        int tokenCap,
        CancellationToken cancellationToken)
    {
        try
        {
            var deltas = new List<string>();
            var sw = Stopwatch.StartNew();
            var emittedTokens = 0;
            await foreach (var delta in StreamModelAsync(payloadJson, model, cancellationToken).ConfigureAwait(false))
            {
                deltas.Add(delta);
                emittedTokens += EstimateTokens(delta);
                if (sw.Elapsed >= durationCap || emittedTokens >= tokenCap)
                {
                    break;
                }
            }
            return (deltas, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (new List<string>(), ex.Message);
        }
    }
    private static string ExtractTextFromResponseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        static string? TryExtractText(JsonElement el, int depth = 0)
        {
            if (depth > 10)
            {
                return null;
            }
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    if (el.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                    {
                        return t.GetString();
                    }
                    if (el.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
                    {
                        return ot.GetString();
                    }
                    if (el.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in parts.EnumerateArray())
                        {
                            var nested = TryExtractText(p, depth + 1);
                            if (!string.IsNullOrWhiteSpace(nested))
                            {
                                return nested;
                            }
                        }
                    }
                    if (el.TryGetProperty("content", out var content))
                    {
                        var nested = TryExtractText(content, depth + 1);
                        if (!string.IsNullOrWhiteSpace(nested))
                        {
                            return nested;
                        }
                    }
                    if (el.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in output.EnumerateArray())
                        {
                            var nested = TryExtractText(item, depth + 1);
                            if (!string.IsNullOrWhiteSpace(nested))
                            {
                                return nested;
                            }
                        }
                    }
                    if (el.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in candidates.EnumerateArray())
                        {
                            var nested = TryExtractText(item, depth + 1);
                            if (!string.IsNullOrWhiteSpace(nested))
                            {
                                return nested;
                            }
                        }
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray())
                    {
                        var nested = TryExtractText(item, depth + 1);
                        if (!string.IsNullOrWhiteSpace(nested))
                        {
                            return nested;
                        }
                    }
                    break;
            }
            return null;
        }
        return TryExtractText(doc.RootElement) ?? string.Empty;
    }
    private static IEnumerable<string> ChunkText(string text, int chunkSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }
        var size = Math.Max(1, chunkSize);
        for (var i = 0; i < text.Length; i += size)
        {
            var len = Math.Min(size, text.Length - i);
            yield return text.Substring(i, len);
        }
    }
    private static int EstimateTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }
        return (int)Math.Ceiling(value.Length / 4d);
    }
    private static bool TryParse429Error(string error, out bool isQuotaExhausted, out TimeSpan? retryAfter)
    {
        isQuotaExhausted = false;
        retryAfter = null;
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }
        var is429 = error.Contains(" 429 ", StringComparison.OrdinalIgnoreCase)
            || error.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
            || error.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase);
        if (!is429)
        {
            return false;
        }
        isQuotaExhausted = error.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase)
            || error.Contains("GenerateRequestsPerDay", StringComparison.OrdinalIgnoreCase)
            || error.Contains("free_tier", StringComparison.OrdinalIgnoreCase)
            || error.Contains("limit: 0", StringComparison.OrdinalIgnoreCase);
        try
        {
            var jsonStart = error.IndexOf('{');
            if (jsonStart >= 0)
            {
                var json = error[jsonStart..];
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var errorNode)
                    && errorNode.TryGetProperty("details", out var details)
                    && details.ValueKind == JsonValueKind.Array)
                {
                    foreach (var detail in details.EnumerateArray())
                    {
                        if (detail.TryGetProperty("@type", out var typeNode)
                            && typeNode.GetString()?.Contains("RetryInfo", StringComparison.OrdinalIgnoreCase) == true
                            && detail.TryGetProperty("retryDelay", out var retryDelayNode)
                            && retryDelayNode.ValueKind == JsonValueKind.String)
                        {
                            if (TimeSpan.TryParse(retryDelayNode.GetString(), out var parsed))
                            {
                                retryAfter = parsed;
                                return true;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
        }
        var regex = new Regex(@"retry\s+in\s+(\d+(?:\.\d+)?)s", RegexOptions.IgnoreCase);
        var match = regex.Match(error);
        if (match.Success && double.TryParse(match.Groups[1].Value, out var seconds))
        {
            retryAfter = TimeSpan.FromSeconds(seconds);
        }
        return true;
    }
}