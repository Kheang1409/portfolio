# Resilience Patterns

## Overview

The portfolio assistant implements multiple resilience patterns to handle partial failures gracefully.

---

## Patterns Implemented

### 1. Retry with Exponential Backoff

**Used for**: HTTP calls (Gemini, PayPal, SMTP), MongoDB operations, Redis operations

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .Or<MongoException>()
    .Or<RedisException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (ex, delay, attempt, ctx) => 
            _logger.LogWarning(ex, "Retry {Attempt} after {Delay}ms", attempt, delay.TotalMilliseconds)
    );
```

**Configuration**:
| Operation | Max Retries | Base Delay | Max Delay |
|-----------|-------------|------------|-----------|
| Gemini API | 3 | 1s | 4s |
| PayPal API | 3 | 2s | 8s |
| MongoDB | 3 | 0.5s | 2s |
| Redis | 2 | 0.1s | 0.2s |
| SMTP | 3 | 5s | 20s |

---

### 2. Circuit Breaker

**Used for**: Gemini AI calls, PayPal API

```csharp
var circuitBreaker = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (ex, duration) => 
            _logger.LogError(ex, "Circuit opened for {Duration}s", duration.TotalSeconds),
        onReset: () => _logger.LogInformation("Circuit closed"),
        onHalfOpen: () => _logger.LogInformation("Circuit half-open")
    );
```

**State Machine**:
```
CLOSED (normal) ──5 failures──▶ OPEN (fast-fail) ──30s──▶ HALF-OPEN (1 test) ──success──▶ CLOSED
                              ▲                                                          │
                              └────────────────────── failure ─────────────────────────┘
```

**Per-Service Keys**: `circuitbreaker:gemini`, `circuitbreaker:paypal`

---

### 3. Timeout

**Used for**: All external HTTP calls

```csharp
var timeoutPolicy = Policy.TimeoutAsync(
    timeout: TimeSpan.FromSeconds(30),
    onTimeoutAsync: (ctx, token) => 
    {
        _logger.LogError("Request timed out after 30s");
        return Task.CompletedTask;
    }
);
```

| Service | Timeout |
|---------|---------|
| Gemini (streaming) | 60s (stream), 10s (connect) |
| Gemini (embedding) | 30s |
| PayPal | 30s |
| MongoDB | 10s (default) |
| Redis | 5s |
| SMTP | 30s |

---

### 4. Fallback

**Used for**: AI Assistant, Embeddings, Non-critical features

```csharp
var fallbackPolicy = Policy
    .Handle<Exception>()
    .FallbackAsync(
        fallbackAction: async (ctx, ct) => 
        {
            _logger.LogWarning("Fallback triggered for {Operation}", ctx.OperationKey);
            return GetFallbackResponse(ctx.OperationKey);
        },
        onFallbackAsync: (ex, ctx) => 
        {
            _logger.LogWarning(ex, "Fallback executed for {Operation}", ctx.OperationKey);
            return Task.CompletedTask;
        }
    );
```

**Fallback Responses**:
| Operation | Fallback |
|-----------|----------|
| Assistant Stream | `"I'm having trouble connecting to the AI service. Please try again in a moment."` |
| Embedding Generation | Skip semantic retrieval; keyword-only |
| PayPal Config | Return cached config (5min TTL) |
| Donation Leaderboard | Return empty array |
| Visitor Tracking | Fire-and-forget (no blocking) |

---

### 5. Bulkhead (Concurrency Limit)

**Used for**: AI Streaming (prevent resource exhaustion)

```csharp
var bulkhead = Policy.BulkheadAsync(
    maxParallelization: 10,
    maxQueuingActions: 20,
    onBulkheadRejectedAsync: ctx => 
    {
        _logger.LogWarning("AI bulkhead rejected: {Queued} queued", ctx.QueuedCount);
        return Task.FromResult(CreateQueueFullResponse());
    }
);
```

---

## Policy Composition (Wrap Order Matters)

```csharp
// OUTERMOST: Logging/Metrics
var loggingPolicy = Policy.WrapAsync(loggingPolicy);

// Fallback (catches everything)
var fallbackPolicy = Policy.WrapAsync(fallbackPolicy);

// Circuit Breaker (stops calling failing service)
var circuitBreakerPolicy = Policy.WrapAsync(circuitBreakerPolicy);

// Retry (retries transient failures)
var retryPolicy = Policy.WrapAsync(retryPolicy);

// Timeout (innermost - applies to single attempt)
var timeoutPolicy = Policy.WrapAsync(timeoutPolicy);

// Final composed policy
var resilientPolicy = Policy.WrapAsync(
    loggingPolicy,
    fallbackPolicy,
    circuitBreakerPolicy,
    retryPolicy,
    timeoutPolicy
);
```

**Execution Flow**:
```
Request
  │
  ▼
┌─────────────────────┐
│  LOGGING            │  ◀── Logs start/end, duration, outcome
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│  FALLBACK           │  ◀── Catches ANY exception, returns friendly response
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│  CIRCUIT BREAKER    │  ◀── If OPEN, throws immediately (caught by Fallback)
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│  RETRY              │  ◀── Retries up to 3x on transient failures
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│  TIMEOUT            │  ◀── Enforced per ATTEMPT (not total)
└─────────┬───────────┘
          │
          ▼
   ACTUAL CALL
```

---

## Health Checks

### Liveness (`/health/live`)
- **Purpose**: Process is running
- **Checks**: None (always returns 200 if process alive)
- **Used by**: Kubernetes/Docker restart policy

### Readiness (`/health/ready`)
- **Purpose**: Ready to serve traffic
- **Checks**: MongoDB ping, Redis ping
- **Used by**: Load balancer, Docker Compose `depends_on: condition: service_healthy`

```csharp
public class ReadinessHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        var checks = new List<Task<HealthCheckResult>>
        {
            CheckMongoDBAsync(ct),
            CheckRedisAsync(ct)
        };

        var results = await Task.WhenAll(checks);
        var unhealthy = results.Where(r => r.Status == HealthStatus.Unhealthy).ToList();

        if (unhealthy.Any())
            return HealthCheckResult.Unhealthy("Dependencies unhealthy", data: results.ToDictionary(r => r.Description));

        return HealthCheckResult.Healthy("All dependencies healthy");
    }
}
```

---

## Degraded Mode Behavior

| Dependency Down | User Impact | System Behavior |
|-----------------|-------------|-----------------|
| **MongoDB** | Resume, Assistant, Donations, Contacts fail | `/health/ready` → 503; Frontend shows 503 page |
| **Redis** | Rate limiting → in-memory fallback; Circuit breaker state lost | Log warning; In-memory rate limit (per-instance) |
| **Gemini** | Assistant → fallback message; Embeddings → keyword-only | Circuit breaker opens; Fallback response streamed |
| **PayPal** | Donations unavailable | Config returns error; Order creation fails gracefully |
| **SMTP** | Contact/Receipt emails queued | Outbox processor retries; Donations still complete |

---

## Observability for Resilience

### Metrics Emitted

| Metric | Type | Labels |
|--------|------|--------|
| `resilience.retry.attempts` | Counter | `operation`, `attempt` |
| `resilience.circuitbreaker.state` | Gauge | `service` (closed=0, open=1, half-open=2) |
| `resilience.fallback.triggered` | Counter | `operation`, `reason` |
| `resilience.timeout.exceeded` | Counter | `operation` |
| `resilience.bulkhead.rejected` | Counter | `operation` |

### Logs (Structured)

```json
{
  "timestamp": "2026-11-24T10:30:00.123Z",
  "level": "WARNING",
  "message": "Circuit opened for gemini",
  "service": "gemini",
  "durationSeconds": 30,
  "failureCount": 5,
  "correlationId": "abc-123"
}
```

---

## Testing Resilience

### Chaos Testing (Manual)

```bash
# Simulate MongoDB down
docker compose stop mongodb
curl http://localhost:5000/health/ready  # Should return 503

# Simulate Redis down
docker compose stop redis
# Rate limiting falls back to in-memory

# Simulate Gemini down
# Set invalid GEMINI_API_KEY, restart backend
curl -X POST http://localhost:5000/api/assistant -d '{"query":"test"}'
# Should return fallback message
```

### Unit Tests

```csharp
[Fact]
public async Task Assistant_ReturnsFallback_WhenGeminiFails()
{
    // Arrange
    var handler = new MockHttpMessageHandler();
    handler.When("*/models/*:generateContent").Respond(500);
    
    var service = new GeminiAssistantService(handler.ToHttpClient(), settings, logger);

    // Act
    var chunks = new List<AssistantResponseChunk>();
    await foreach (var chunk in service.StreamResponseAsync(request, CancellationToken.None))
        chunks.Add(chunk);

    // Assert
    chunks.Should().ContainSingle(c => c.Type == "fallback");
    chunks.First(c => c.Type == "fallback").Text.Should().Contain("trouble connecting");
}
```

---

## Related Documents

- [Scalability Patterns](scalability.md) — Horizontal scaling, caching
- [AI Pipeline](ai-pipeline.md) — AI-specific fallbacks
- [System Overview](system-overview.md) — Architecture
- [Production Runbook](../deployment/production-runbook.md) — Operational procedures