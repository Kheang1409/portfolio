# Health Checks

## Overview

Three-tier health check system for different consumers.

---

## Endpoints

| Endpoint | Purpose | Consumer | Dependencies Checked |
|----------|---------|----------|---------------------|
| `GET /api/health` | Basic process health | Load balancer (basic) | None |
| `GET /health/live` | Liveness probe | Kubernetes, Docker | None (process only) |
| `GET /health/ready` | Readiness probe | Kubernetes, Docker, Load balancer | MongoDB, Redis |

---

## `/api/health` (Basic)

**Response** (200):
```json
{ "status": "Healthy" }
```

**Use**: Simple uptime monitoring, basic load balancer checks.

---

## `/health/live` (Liveness)

**Purpose**: Process is running and not deadlocked.

**Response** (200):
```json
{ "status": "Alive" }
```

**Response** (500 - if implemented with self-check):
```json
{ "status": "Dead", "reason": "Thread pool exhausted" }
```

**Configuration** (Dockerfile):
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1
```

**Behavior**:
- Failure → Container restart (Docker) / Pod restart (Kubernetes)
- No dependency checks — only process health
- Fast (< 100ms)

---

## `/health/ready` (Readiness)

**Purpose**: Service ready to handle requests.

**Response** (200):
```json
{
  "status": "Ready",
  "checks": {
    "mongodb": {
      "status": "Healthy",
      "latencyMs": 12,
      "description": "MongoDB ping successful"
    },
    "redis": {
      "status": "Healthy",
      "latencyMs": 3,
      "description": "Redis ping successful"
    }
  },
  "timestamp": "2026-11-24T10:30:00Z"
}
```

**Response** (503):
```json
{
  "status": "NotReady",
  "checks": {
    "mongodb": {
      "status": "Unhealthy",
      "latencyMs": 5000,
      "description": "MongoDB connection timeout",
      "exception": "TimeoutException"
    },
    "redis": {
      "status": "Healthy",
      "latencyMs": 3,
      "description": "Redis ping successful"
    }
  },
  "timestamp": "2026-11-24T10:30:00Z"
}
```

**Checks Performed**:

| Check | Implementation | Timeout |
|-------|----------------|---------|
| MongoDB | `db.RunCommandAsync((Command<BsonDocument>)"{ ping: 1 }")` | 5s |
| Redis | `await _database.PingAsync()` | 3s |

**Configuration** (Dockerfile):
```dockerfile
# Same as liveness but used by orchestration for traffic routing
```

**Docker Compose**:
```yaml
services:
  backend:
    depends_on:
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 30s
      timeout: 10s
      start_period: 15s
      retries: 3
```

---

## Frontend Health Proxy

The frontend exposes proxied health endpoints for browser access:

| Frontend Path | Proxies To |
|---------------|------------|
| `/api/health/live` | `BACKEND_API_URL/health/live` |
| `/api/health/ready` | `BACKEND_API_URL/health/ready` |

**Used by**: Root layout for 503 page auto-retry logic.

---

## Kubernetes Probe Configuration

```yaml
# deployment.yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 15
  periodSeconds: 30
  timeoutSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

---

## Monitoring Integration

### Prometheus Metrics

```prometheus
# HELP health_check_duration_seconds Health check latency
# TYPE health_check_duration_seconds histogram
health_check_duration_seconds_bucket{endpoint="ready",check="mongodb",le="0.1"} 1234
health_check_duration_seconds_bucket{endpoint="ready",check="mongodb",le="0.5"} 1450
health_check_duration_seconds_bucket{endpoint="ready",check="mongodb",le="1.0"} 1490
health_check_duration_seconds_bucket{endpoint="ready",check="mongodb",le="+Inf"} 1500

# HELP health_check_status Health check status (1=healthy, 0=unhealthy)
# TYPE health_check_status gauge
health_check_status{endpoint="ready",check="mongodb"} 1
health_check_status{endpoint="ready",check="redis"} 1
```

### Grafana Alerts

```yaml
- alert: BackendNotReady
  expr: health_check_status{endpoint="ready"} == 0
  for: 2m
  labels:
    severity: critical
  annotations:
    summary: "Backend readiness check failing"
    description: "{{ $labels.check }} unhealthy for 2 minutes"

- alert: BackendHighLatency
  expr: histogram_quantile(0.95, health_check_duration_seconds_bucket{endpoint="ready"}) > 1
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "Backend health check latency high"
```

---

## Troubleshooting

| Symptom | Likely Cause | Action |
|---------|--------------|--------|
| `/health/ready` returns 503 | MongoDB/Redis down | Check dependency logs, network |
| `/health/ready` slow (>5s) | MongoDB connection pool exhausted | Check pool size, query load |
| `/health/live` fails | Process deadlocked | Check thread pool, memory |
| Frontend shows 503 page | Backend readiness failing | Check backend `/health/ready` directly |
| Docker restart loop | Health check failing | Increase `start_period`, check logs |

---

## Custom Health Checks (Adding New)

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            // Your check logic
            var isHealthy = await CheckDependencyAsync(ct);
            
            return isHealthy 
                ? HealthCheckResult.Healthy("Dependency OK")
                : HealthCheckResult.Unhealthy("Dependency degraded");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Check failed", ex);
        }
    }
}

// Registration in Program.cs
services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom-dependency");
```

---

## Related Documents

- [Docker Compose](../development/docker-compose.md) — Health check configuration
- [Deployment Runbook](../deployment/production-runbook.md) — Production health monitoring
- [Resilience Patterns](../architecture/resilience.md) — Degraded mode behavior