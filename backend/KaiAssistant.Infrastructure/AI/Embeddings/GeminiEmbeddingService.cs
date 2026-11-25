using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KaiAssistant.Application.Interfaces;
using KaiAssistant.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KaiAssistant.Infrastructure.AI.Embeddings;

public sealed class EmbeddingProviderException(string message, bool retryable, HttpStatusCode? statusCode = null, Exception? inner = null) : Exception(message, inner)
{
    public bool Retryable { get; } = retryable;
    public HttpStatusCode? StatusCode { get; } = statusCode;
}

public sealed class GeminiEmbeddingService : IEmbeddingService, IDisposable
{
    private static readonly Meter Meter = new("KaiAssistant.Embeddings", "1.0.0");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("embedding_request_duration", "ms");
    private static readonly Histogram<long> BatchSize = Meter.CreateHistogram<long>("embedding_batch_size");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("embedding_requests_total");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("embedding_failures_total");
    private static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("embedding_cache_hits");
    private static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>("embedding_cache_misses");
    private static readonly Counter<long> RateLimits = Meter.CreateCounter<long>("embedding_rate_limit_total");
    private readonly IHttpClientFactory _clients;
    private readonly IOptionsMonitor<EmbeddingOptions> _options;
    private readonly IMemoryCache _cache;
    private readonly string _apiKey;
    private readonly SemaphoreSlim _concurrency;

    public GeminiEmbeddingService(IHttpClientFactory clients, IOptionsMonitor<EmbeddingOptions> options, IMemoryCache cache, IConfiguration configuration)
    {
        _clients = clients; _options = options; _cache = cache;
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? configuration["GeminiSettings:ApiKey"] ?? string.Empty;
        _concurrency = new SemaphoreSlim(options.CurrentValue.MaxConcurrency, options.CurrentValue.MaxConcurrency);
    }

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        GenerateEmbeddingAsync(text, EmbeddingPurpose.RetrievalQuery, null, cancellationToken);

    public async Task<float[]> GenerateEmbeddingAsync(string text, EmbeddingPurpose purpose, string? title = null, CancellationToken cancellationToken = default) =>
        (await GenerateEmbeddingsAsync([text], purpose, title is null ? null : [title], cancellationToken).ConfigureAwait(false))[0];

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, EmbeddingPurpose purpose, IReadOnlyList<string?>? titles = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count == 0) return [];
        var options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(_apiKey)) throw new EmbeddingProviderException("GEMINI_API_KEY is required for live embeddings.", false);
        if (texts.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > options.MaxInputChars)) throw new EmbeddingProviderException("Embedding input is empty or exceeds the configured size limit.", false);
        if (titles is not null && titles.Count != texts.Count) throw new ArgumentException("Titles must map one-to-one with texts.", nameof(titles));
        var output = new float[texts.Count][];
        var missing = new List<int>();
        for (var i = 0; i < texts.Count; i++)
        {
            var key = CreateCacheKey(options, purpose, texts[i]);
            if (options.CacheEnabled && _cache.TryGetValue<float[]>(key, out var cached)) { output[i] = cached!; CacheHits.Add(1); }
            else { missing.Add(i); CacheMisses.Add(1); }
        }
        foreach (var batch in missing.Chunk(options.BatchSize))
        {
            var values = await SendBatchAsync(batch.Select(i => texts[i]).ToArray(), purpose,
                titles is null ? null : batch.Select(i => titles[i]).ToArray(), cancellationToken).ConfigureAwait(false);
            for (var j = 0; j < batch.Length; j++)
            {
                output[batch[j]] = values[j];
                if (options.CacheEnabled) _cache.Set(CreateCacheKey(options, purpose, texts[batch[j]]), values[j],
                    TimeSpan.FromSeconds(purpose == EmbeddingPurpose.RetrievalQuery ? options.QueryCacheTtlSeconds : options.DocumentCacheTtlSeconds));
            }
        }
        return output;
    }

    private async Task<IReadOnlyList<float[]>> SendBatchAsync(string[] texts, EmbeddingPurpose purpose, string?[]? titles, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        await _concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
                var started = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    Requests.Add(1); BatchSize.Record(texts.Length);
                    using var request = new HttpRequestMessage(HttpMethod.Post, $"models/{options.Model}:batchEmbedContents");
                    request.Headers.Add("x-goog-api-key", _apiKey);
                    request.Content = JsonContent.Create(new
                    {
                        requests = texts.Select((text, i) => new
                        {
                            model = $"models/{options.Model}",
                            taskType = purpose == EmbeddingPurpose.RetrievalQuery ? "RETRIEVAL_QUERY" : "RETRIEVAL_DOCUMENT",
                            title = purpose == EmbeddingPurpose.RetrievalDocument ? titles?[i] : null,
                            outputDimensionality = options.Dimensions,
                            content = new { parts = new[] { new { text } } }
                        }).ToArray()
                    });
                    using var response = await _clients.CreateClient("GeminiEmbeddings").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        var retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
                        if (response.StatusCode == HttpStatusCode.TooManyRequests) RateLimits.Add(1);
                        if (!retryable || attempt >= options.MaxRetries) throw new EmbeddingProviderException($"Gemini embedding request failed with HTTP {(int)response.StatusCode}.", retryable, response.StatusCode);
                        await DelayAsync(response, attempt, cancellationToken).ConfigureAwait(false); continue;
                    }
                    await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                    var payload = await JsonSerializer.DeserializeAsync<BatchResponse>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, timeout.Token).ConfigureAwait(false);
                    var embeddings = payload?.Embeddings?.Select(x => x.Values ?? []).ToArray() ?? [];
                    if (embeddings.Length != texts.Length) throw new EmbeddingProviderException("Gemini returned an incomplete embedding batch.", false);
                    foreach (var vector in embeddings) { ValidateDimensions(vector, options.Dimensions); Normalize(vector); }
                    Duration.Record(started.Elapsed.TotalMilliseconds); return embeddings;
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                { Failures.Add(1); throw new EmbeddingProviderException("Gemini embedding request timed out.", true, inner: ex); }
                catch (HttpRequestException) when (attempt < options.MaxRetries)
                { await Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt) + Random.Shared.Next(25, 125)), cancellationToken).ConfigureAwait(false); }
                catch { Failures.Add(1); throw; }
            }
        }
        finally { _concurrency.Release(); }
    }

    private static async Task DelayAsync(HttpResponseMessage response, int attempt, CancellationToken cancellationToken)
    {
        var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt) + Random.Shared.Next(25, 150));
        await Task.Delay(delay > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : delay, cancellationToken).ConfigureAwait(false);
    }
    public static string CreateCacheKey(EmbeddingOptions options, EmbeddingPurpose purpose, string text)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim().ToLowerInvariant()))).ToLowerInvariant();
        return $"rag:embedding:{options.Provider}:{options.Model}:{options.Version}:{purpose}:{hash}";
    }
    internal static void ValidateDimensions(float[] vector, int expected) { if (vector.Length != expected) throw new EmbeddingProviderException($"Embedding dimension mismatch: expected {expected}, received {vector.Length}.", false); }
    internal static void Normalize(float[] vector) { double sum = vector.Sum(x => x * x); if (sum <= 0) throw new EmbeddingProviderException("Embedding vector has zero magnitude.", false); var norm = (float)Math.Sqrt(sum); for (var i = 0; i < vector.Length; i++) vector[i] /= norm; }
    public void Dispose() => _concurrency.Dispose();
    private sealed class BatchResponse { public EmbeddingDto[]? Embeddings { get; set; } }
    private sealed class EmbeddingDto { public float[]? Values { get; set; } }
}
