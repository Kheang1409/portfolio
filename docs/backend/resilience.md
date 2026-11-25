# Backend Resilience Patterns

## Overview

Resilience implementation using **Polly** policies for retry, circuit breaker, timeout, fallback, and bulkhead.

---

## Policy Registry

```csharp
// Program.cs / ServiceCollectionExtensions.cs
services.AddHttpClient<IGeminiClient, GeminiClient>(client =>
{
    client.BaseAddress = new Uri(settings.Endpoint);
    client.DefaultRequestHeaders.Add("x-goog-api-key", settings.ApiKey);
})
.AddPolicyHandler(GetGeminiResiliencePolicy());

services.AddHttpClient<IPayPalClient, PayPalClient>(client =>
{
    client.BaseAddress = new Uri(settings.BaseUrl);
})
.AddPolicyHandler(GetPayPalResiliencePolicy());
```

---

## Gemini AI Policies

```csharp
private static IAsyncPolicy<HttpResponseMessage> GetGeminiResiliencePolicy()
{
    // 1. Timeout (per attempt)
    var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
        TimeSpan.FromSeconds(30),
        onTimeoutAsync: async (ctx, ct) => 
            _logger.LogWarning("Gemini request timed out"));

    // 2. Retry (transient failures)
    var retry = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetryAsync: async (outcome, delay, attempt, ctx) =>
            {
                _logger.LogWarning("Gemini retry {Attempt} after {Delay}ms: {Reason}", 
                    attempt, delay.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            });

    // 3. Circuit Breaker
    var circuitBreaker = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .OrResult(r => (int)r.StatusCode >= 500)
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) => 
                _logger.LogError("Gemini circuit OPEN for {Duration}s", duration.TotalSeconds),
            onReset: () => _logger.LogInformation("Gemini circuit CLOSED"),
            onHalfOpen: () => _logger.LogInformation("Gemini circuit HALF-OPEN"));

    // 4. Fallback
    var fallback = Policy<HttpResponseMessage>
        .Handle<Exception>()
        .FallbackAsync(
            fallbackAction: async (ctx, ct) =>
            {
                _logger.LogWarning("Gemini fallback triggered");
                return CreateFallbackResponse();
            },
            onFallbackAsync: async (ex, ctx) =>
            {
                _logger.LogWarning(ex, "Gemini fallback executed");
            });

    // 5. Bulkhead (streaming concurrency limit)
    var bulkhead = Policy.BulkheadAsync<HttpResponseMessage>(
        maxParallelization: 10,
        maxQueuingActions: 20,
        onBulkheadRejectedAsync: ctx =>
        {
            _logger.LogWarning("Gemini bulkhead rejected");
            return Task.FromResult(CreateQueueFullResponse());
        });

    // Composition: Fallback(CircuitBreaker(Retry(Timeout(Bulkhead(Inner)))))
    return Policy.WrapAsync(fallback, circuitBreaker, retry, timeout, bulkhead);
}
```

---

## PayPal Policies

```csharp
private static IAsyncPolicy<HttpResponseMessage> GetPayPalResiliencePolicy()
{
    var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

    var retry = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .OrResult(r => (int)r.StatusCode >= 500)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt) * 2));

    var circuitBreaker = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .OrResult(r => (int)r.StatusCode >= 500)
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(60));

    var fallback = Policy<HttpResponseMessage>
        .Handle<Exception>()
        .FallbackAsync(
            fallbackAction: async (ctx, ct) => CreatePayPalFallbackResponse(),
            onFallbackAsync: async (ex, ctx) => _logger.LogWarning(ex, "PayPal fallback"));

    return Policy.WrapAsync(fallback, circuitBreaker, retry, timeout);
}
```

---

## MongoDB Policies

```csharp
// In MongoDB repository base
private static readonly AsyncRetryPolicy MongoRetryPolicy = Policy
    .Handle<MongoException>(ex => ex.IsTransient)
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        3,
        attempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)),
        onRetry: (ex, delay, attempt, ctx) => 
            _logger.LogWarning(ex, "MongoDB retry {Attempt} after {Delay}ms", attempt, delay.TotalMilliseconds));

// Usage
await MongoRetryPolicy.ExecuteAsync(async ct => 
    await _collection.Find(filter).FirstOrDefaultAsync(ct));
```

---

## Redis Policies

```csharp
private static readonly AsyncRetryPolicy RedisRetryPolicy = Policy
    .Handle<RedisException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        2,
        attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)),
        onRetry: (ex, delay, attempt, ctx) => 
            _logger.LogWarning(ex, "Redis retry {Attempt} after {Delay}ms", attempt, delay.TotalMilliseconds));

// Fallback: In-memory rate limiting
public async Task<RateLimitResult> CheckLimitAsync(string key, int limit, TimeSpan window)
{
    try
    {
        return await RedisRetryPolicy.ExecuteAsync(async ct => 
            await _distributedRateLimit.CheckLimitAsync(key, limit, window, ct));
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Redis unavailable, using in-memory fallback");
        return _inMemoryRateLimit.CheckLimit(key, limit, window);
    }
}
```

---

## SMTP Policies

```csharp
private static readonly AsyncRetryPolicy SmtpRetryPolicy = Policy
    .Handle<SmtpException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        3,
        attempt => TimeSpan.FromSeconds(5 * Math.Pow(2, attempt)),
        onRetry: (ex, delay, attempt, ctx) => 
            _logger.LogWarning(ex, "SMTP retry {Attempt} after {Delay}s", attempt, delay.TotalSeconds));

// Outbox processor uses this
public async Task ProcessOutboxAsync(CancellationToken ct)
{
    var messages = await _outboxRepository.GetPendingAsync(ct);
    
    foreach (var msg in messages)
    {
        try
        {
            await SmtpRetryPolicy.ExecuteAsync(async ct => 
                await _smtpClient.SendAsync(msg, ct));
            
            await _outboxRepository.MarkSentAsync(msg.Id, ct);
        }
        catch (Exception ex)
        {
            await _outboxRepository.IncrementAttemptsAsync(msg.Id, ct);
            _logger.LogError(ex, "Failed to send email {MessageId}", msg.Id);
        }
    }
}
```

---

## Circuit Breaker State (Redis)

```csharp
public class DistributedCircuitBreaker
{
    private readonly IDatabase _redis;
    private readonly string _serviceName;

    public async Task<CircuitState> GetStateAsync()
    {
        var failures = await _redis.StringGetAsync($"circuitbreaker:{_serviceName}:failures");
        var lastFailure = await _redis.StringGetAsync($"circuitbreaker:{_serviceName}:lastFailure");
        
        if (!failures.HasValue) return CircuitState.Closed;
        
        var count = (int)failures;
        if (count >= 5)
        {
            if (lastFailure.HasValue && DateTime.UtcNow - (DateTime)lastFailure > TimeSpan.FromSeconds(30))
                return CircuitState.HalfOpen;
            return CircuitState.Open;
        }
        return CircuitState.Closed;
    }

    public async Task RecordSuccessAsync()
    {
        await _redis.KeyDeleteAsync($"circuitbreaker:{_serviceName}:failures");
        await _redis.KeyDeleteAsync($"circuitbreaker:{_serviceName}:lastFailure");
    }

    public async Task RecordFailureAsync()
    {
        var count = await _redis.StringIncrementAsync($"circuitbreaker:{_serviceName}:failures");
        await _redis.StringSetAsync($"circuitbreaker:{_serviceName}:lastFailure", DateTime.UtcNow);
        await _redis.KeyExpireAsync($"circuitbreaker:{_serviceName}:failures", TimeSpan.FromMinutes(5));
    }
}
```

---

## Degraded Mode Behavior

| Dependency Down | Fallback Behavior |
|-----------------|-------------------|
| **MongoDB** | `/health/ready` → 503; All data endpoints fail fast |
| **Redis** | Rate limiting → in-memory (per-instance); Circuit breaker state lost |
| **Gemini** | Assistant → fallback message; Embeddings → keyword-only retrieval |
| **PayPal** | Donations → config returns error; Orders fail gracefully |
| **SMTP** | Emails → outbox queue (retry background); Donations still complete |

---

## Health Checks

```csharp
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IMongoClient _client;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            await _client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), ct);
            return HealthCheckResult.Healthy("MongoDB ping successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB ping failed", ex);
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync(ct);
            return HealthCheckResult.Healthy("Redis ping successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed", ex);
        }
    }
}

// Registration
services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb")
    .AddCheck<RedisHealthCheck>("redis");
```

---

## Metrics Emitted

| Metric | Type | Labels |
|--------|------|--------|
| `resilience.retry.attempts` | Counter | `service`, `attempt` |
| `resilience.circuitbreaker.state` | Gauge | `service` (0=closed, 1=open, 2=half-open) |
| `resilience.fallback.triggered` | Counter | `service`, `reason` |
| `resilience.timeout.exceeded` | Counter | `service` |
| `resilience.bulkhead.rejected` | Counter | `service` |
| `resilience.redis.fallback` | Counter | `operation` |

---

## Testing Resilience

### Unit Tests

```csharp
[Fact]
public async Task GeminiClient_ReturnsFallback_WhenCircuitOpen()
{
    // Arrange
    var handler = new MockHttpMessageHandler();
    handler.When("*/models/*:generateContent").Respond(500);
    
    var client = new GeminiClient(handler.ToHttpClient(), settings, logger);

    // Act - trigger circuit breaker
    for (int i = 0; i < 5; i++)
    {
        await client.GenerateContentAsync("test");
    }

    // Act - next call should fallback
    var result = await client.GenerateContentAsync("test");

    // Assert
    result.Should().NotBeNull();
    result.IsFallback.Should().BeTrue();
}
```

### Chaos Testing (Manual)

```bash
# Simulate MongoDB down
docker compose stop mongodb
curl http://localhost:5000/health/ready  # 503

# Simulate Redis down
docker compose stop redis
# Rate limiting falls back to in-memory

# Simulate Gemini down
# Set invalid GEMINI_API_KEY, restart backend
curl -X POST http://localhost:5000/api/assistant -d '{"query":"test"}'
# Returns fallback message
```

---

## Related Documents

- [Scalability Patterns](../architecture/scalability.md) — Horizontal scaling
- [AI Pipeline](../architecture/ai-pipeline.md) — AI-specific fallbacks
- [Knowledge Retrieval](knowledge-retrieval.md) — Retrieval fallbacks
- [Production Runbook](../deployment/production-runbook.md) — Operational procedures