# Monitoring & Observability

## Overview

Three pillars: **Metrics**, **Logs**, **Traces** — implemented via OpenTelemetry, Serilog, and standard Docker/CloudWatch integration.

---

## Metrics

### Application Metrics (OpenTelemetry → Prometheus/OTLP)

| Metric | Type | Description | Labels |
|--------|------|-------------|--------|
| `http_requests_total` | Counter | Total HTTP requests | `method`, `path`, `status` |
| `http_request_duration_seconds` | Histogram | Request latency | `method`, `path` |
| `health_check_status` | Gauge | Health check result (1=healthy) | `endpoint`, `check` |
| `health_check_duration_seconds` | Histogram | Health check latency | `endpoint`, `check` |
| `assistant_requests_total` | Counter | Assistant queries | `status` (success, fallback, error) |
| `assistant_stream_duration_seconds` | Histogram | Stream time to first chunk / complete | — |
| `donation_orders_total` | Counter | Donation orders created | `status` (created, recovered) |
| `donation_captures_total` | Counter | Donation captures | `status` (completed, failed) |
| `rate_limit_checks_total` | Counter | Rate limit checks | `endpoint`, `result` (allowed, limited) |
| `circuit_breaker_state` | Gauge | Circuit breaker (0=closed, 1=open, 2=half-open) | `service` |
| `retry_attempts_total` | Counter | Retry attempts | `service`, `attempt` |
| `fallback_triggered_total` | Counter | Fallback executions | `service`, `reason` |
| `knowledge_chunks_total` | Gauge | Indexed knowledge chunks | — |
| `knowledge_embedding_coverage` | Gauge | Fraction with embeddings (0-1) | — |
| `mongodb_connections_active` | Gauge | Active MongoDB connections | — |
| `redis_memory_usage_bytes` | Gauge | Redis memory usage | — |
| `redis_connected_clients` | Gauge | Redis client connections | — |

### Infrastructure Metrics

| Source | Metrics |
|--------|---------|
| **Docker** | `container_cpu_usage`, `container_memory_usage`, `container_network_io`, `container_restart_count` |
| **MongoDB Atlas** | `oplog_window`, `connections`, `query_latency`, `disk_usage`, `backup_status` |
| **Redis** | `used_memory`, `connected_clients`, `keyspace_hits/misses`, `replication_lag` |
| **Node.js (Frontend)** | `event_loop_lag`, `heap_used`, `http_requests` |

---

## Logs

### Structured Logging (Serilog → JSON)

```json
{
  "Timestamp": "2026-11-24T10:30:00.123Z",
  "Level": "Information",
  "MessageTemplate": "Assistant query processed",
  "Properties": {
    "query": "What is your experience?",
    "latencyMs": 1250,
    "chunksReturned": 3,
    "evidenceStrength": "Strong",
    "fallback": false,
    "correlationId": "abc-123",
    "userAgent": "Mozilla/5.0...",
    "ip": "192.168.1.1"
  }
}
```

### Log Levels

| Level | Usage |
|-------|-------|
| **Debug** | Detailed flow, payloads (dev only) |
| **Information** | Key operations, requests, responses |
| **Warning** | Degraded mode, retries, fallbacks, circuit breaker |
| **Error** | Failed operations, exceptions, 5xx responses |
| **Fatal** | Process crash, unrecoverable state |

### Log Retention

| Environment | Retention | Storage |
|-------------|-----------|---------|
| **Development** | 1 day | Docker JSON file (10MB max) |
| **Staging** | 7 days | Loki / CloudWatch Logs |
| **Production** | 30 days | Loki / Datadog / Elasticsearch |

### Key Log Queries

```bash
# All errors in last hour
docker compose logs --since=1h backend | jq 'select(.Level == "Error")'

# Assistant fallbacks
docker compose logs backend | jq 'select(.Properties.fallback == true)'

# Circuit breaker state changes
docker compose logs backend | jq 'select(.MessageTemplate | contains("circuit"))'

# Donation failures
docker compose logs backend | jq 'select(.MessageTemplate | contains("donation") and .Level == "Error")'

# By correlation ID
docker compose logs backend | jq 'select(.Properties.correlationId == "abc-123")'
```

---

## Traces (OpenTelemetry)

### Enabled Instrumentations

| Library | Spans |
|---------|-------|
| `AspNetCore` | HTTP request/response |
| `HttpClient` | Outbound HTTP (Gemini, PayPal, SMTP) |
| `MongoDB` | Database operations |
| `Redis` | Cache/rate limit operations |
| `EntityFrameworkCore` | N/A (using raw driver) |

### Trace Context Propagation

```
W3C TraceContext headers:
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
tracestate: vendor=value
```

### Key Spans

| Operation | Span Name | Attributes |
|-----------|-----------|------------|
| Assistant request | `POST /api/assistant` | `query.length`, `history.count` |
| Knowledge retrieval | `retrieve.knowledge` | `query`, `results.count`, `evidence.type` |
| Gemini streaming | `gemini.generate` | `model`, `tokens.in`, `tokens.out` |
| PayPal order create | `paypal.create_order` | `amount`, `currency` |
| PayPal capture | `paypal.capture` | `orderId`, `status` |
| MongoDB query | `mongodb.find` | `collection`, `filter` |
| Redis rate limit | `redis.ratelimit` | `key`, `limit`, `result` |

### Sampling

| Environment | Sampler | Rate |
|-------------|---------|------|
| Development | AlwaysOn | 100% |
| Staging | ParentBased(TraceIdRatio) | 10% |
| Production | ParentBased(TraceIdRatio) | 1% |

---

## Dashboards (Grafana)

### 1. System Overview
- Request rate (req/s) by endpoint
- Error rate (%) by endpoint
- Latency p50/p95/p99
- Container CPU/Memory
- Active connections

### 2. Assistant Performance
- Queries per minute
- Fallback rate (%)
- Time to first token (p50/p95)
- Stream duration
- Evidence distribution (Strong/Weak)

### 3. Donation Funnel
- Config requests → Order creates → Captures → Completed
- Conversion rates at each step
- Webhook processing latency
- Refund/reversal rate

### 4. Infrastructure Health
- MongoDB: connections, oplog, disk, backup status
- Redis: memory, hit rate, clients, persistence
- Containers: restart count, OOM kills, CPU throttle

### 5. Rate Limiting & Resilience
- Rate limit checks (allowed/limited)
- Circuit breaker state (per service)
- Retry attempts
- Fallback triggers

---

## Alerts

### Critical (Page Immediately)

| Alert | Condition | Runbook |
|-------|-----------|---------|
| `BackendDown` | `/health/ready` 503 for 2m | [Troubleshooting](../operations/troubleshooting.md#backend-issues) |
| `MongoDBUnreachable` | Health check fails 3x | Check Atlas, connection string |
| `RedisUnreachable` | Health check fails 3x | Check container, TLS certs |
| `DonationCaptureFailing` | Capture error rate > 5% | Check PayPal, webhook |
| `DiskSpaceCritical` | Container disk > 90% | Clean logs, increase volume |

### Warning (Notify Within 1 Hour)

| Alert | Condition | Runbook |
|-------|-----------|---------|
| `HighErrorRate` | 5xx > 1% for 5m | Check logs, recent deploy |
| `HighLatency` | p99 > 5s for 10m | Check DB, external APIs |
| `CircuitBreakerOpen` | Any service open > 5m | Check Gemini/PayPal status |
| `RateLimitFallback` | In-memory mode active > 10m | Redis connectivity |
| `BackupFailed` | Atlas backup job failed | Atlas UI → Backup |
| `CertificateExpiring` | TLS cert < 30 days | Renew certs |

### Info (Daily Digest)

| Alert | Condition |
|-------|-----------|
| `DailyTrafficSummary` | Requests, errors, latency summary |
| `DonationSummary` | Orders, captures, revenue |
| `AssistantUsage` | Queries, fallback rate, top questions |

---

## Alert Configuration (Prometheus/Grafana)

```yaml
# Critical alerts
- alert: BackendDown
  expr: health_check_status{endpoint="ready"} == 0
  for: 2m
  labels:
    severity: critical
  annotations:
    summary: "Backend readiness check failing"
    runbook_url: "https://github.com/yourorg/portfolio/docs/operations/troubleshooting.md"

- alert: CircuitBreakerOpen
  expr: circuit_breaker_state == 1
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "Circuit breaker open for {{ $labels.service }}"
    description: "{{ $labels.service }} circuit breaker has been open for 5 minutes"

# Warning alerts
- alert: HighErrorRate
  expr: |
    sum(rate(http_requests_total{status=~"5.."}[5m])) 
    / sum(rate(http_requests_total[5m])) > 0.01
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "High error rate: {{ $value | humanizePercentage }}"

- alert: HighLatencyP99
  expr: |
    histogram_quantile(0.99, sum(rate(http_request_duration_seconds_bucket[5m])) by (le)) > 5
  for: 10m
  labels:
    severity: warning
  annotations:
    summary: "P99 latency {{ $value }}s exceeds 5s threshold"
```

---

## Synthetic Monitoring

### External Checks (Pingdom / Datadog / UptimeRobot)

| Check | Frequency | Target |
|-------|-----------|--------|
| Homepage load | 1 min | `https://yourdomain.com` |
| Resume page | 5 min | `https://yourdomain.com/resume` |
| Assistant API | 1 min | `POST /api/assistant` (smoke query) |
| Health ready | 30 sec | `https://api.yourdomain.com/health/ready` |
| Donation config | 5 min | `GET /api/donations/config` |

### Smoke Test Script

```bash
#!/bin/bash
# smoke-tests.sh

BASE="https://yourdomain.com"
API="https://api.yourdomain.com"

# Homepage
curl -sf "$BASE" > /dev/null || exit 1

# Resume
curl -sf "$BASE/resume" > /dev/null || exit 1

# Assistant (smoke query)
curl -sf -X POST "$API/api/assistant" \
  -H "Content-Type: application/json" \
  -d '{"query":"test","history":[]}' | grep -q "content" || exit 1

# Health
curl -sf "$API/health/ready" | grep -q "Ready" || exit 1

# Donations
curl -sf "$API/api/donations/config" | grep -q "clientId" || exit 1

echo "All smoke tests passed"
```

---

## Log Aggregation Stack (Recommended)

| Component | Tool | Purpose |
|-----------|------|---------|
| **Collection** | Promtail / Fluent Bit | Ship Docker logs |
| **Storage** | Loki / Elasticsearch | Indexed log storage |
| **Query** | LogQL / KQL | Fast log search |
| **Visualization** | Grafana / Kibana | Log dashboards |
| **Alerting** | Grafana Alerting / ElastAlert | Log-based alerts |

### Loki Labels (Recommended)

```yaml
# promtail-config.yml
clients:
  - url: http://loki:3100/loki/api/v1/push

scrape_configs:
  - job_name: docker
    static_configs:
      - labels:
          job: portfolio
          environment: production
          container: __meta_docker_container_name
          service: __meta_docker_container_label_com_docker_compose_service
```

---

## Cost Monitoring

| Resource | Monthly Cost (Est.) | Alert Threshold |
|----------|---------------------|-----------------|
| MongoDB Atlas (M10) | $57 | > $100 |
| ElastiCache Redis (cache.t3.micro) | $15 | > $30 |
| Docker Host (t3.medium) | $30 | > $50 |
| Data Transfer | $5-20 | > $50 |
| Log Storage (Loki/CloudWatch) | $10-50 | > $100 |

---

## Related Documents

- [Troubleshooting](../operations/troubleshooting.md) — Debugging procedures
- [Production Runbook](../deployment/production-runbook.md) — Deployment monitoring
- [Health Checks](../api/health-checks.md) — Health endpoint details
- [Resilience Patterns](../architecture/resilience.md) — Circuit breaker metrics