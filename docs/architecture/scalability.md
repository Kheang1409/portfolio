# Scalability Patterns

## Current Architecture (Single-Instance)

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Frontend   │────▶│  Backend    │────▶│  MongoDB    │
│  (1 pod)    │     │  (1 pod)    │     │  (Atlas)    │
└─────────────┘     └──────┬──────┘     └─────────────┘
                           │
                    ┌──────┴──────┐
                    │   Redis     │
                    │  (1 pod)    │
                    └─────────────┘
```

**Capacity**: ~1000 req/min, 100 concurrent users

---

## Horizontal Scaling

### Frontend (Stateless)
```yaml
# docker-compose.scale.yml
services:
  frontend:
    deploy:
      replicas: 3
      resources:
        limits:
          memory: 256M
          cpus: '0.25'
```
- **No session affinity needed** (stateless, JWT in cookies)
- **CDN** for static assets (`.next/static/`, `public/`)
- **Health check**: `/api/health/live`

### Backend (Stateless)
```yaml
services:
  backend:
    deploy:
      replicas: 3
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
```
- **Connection pooling**: MongoDB (max 100), Redis (max 50)
- **No in-memory state** (rate limit in Redis, cache in Redis)
- **Health check**: `/health/live` + `/health/ready`

### Redis (Single → Cluster)
| Stage | Configuration | Capacity |
|-------|---------------|----------|
| Dev | Single, no persistence | 10K ops/sec |
| Prod | Single, RDB+AOF, TLS | 50K ops/sec |
| Scale | Redis Cluster (3+ masters) | 200K+ ops/sec |

**Migration Path**:
```yaml
# docker-compose.redis-cluster.yml
services:
  redis:
    image: redis:7.0-cluster
    command: redis-server --cluster-enabled yes --cluster-config-file nodes.conf
    deploy:
      replicas: 6  # 3 masters + 3 replicas
```

### MongoDB (Atlas Auto-Scale)
- **M0/M2**: Shared, no auto-scale
- **M10+**: Auto-scale storage, manual tier upgrade
- **Sharding**: For > 100GB or > 10K ops/sec

---

## Performance Optimization

### Backend

| Optimization | Implementation |
|--------------|----------------|
| **Response Compression** | `AddResponseCompression()` (Brotli/Gzip) |
| **Output Caching** | `AddOutputCache()` for `GET /api/resumes/latest` (30s) |
| **Connection Pooling** | `MongoClientSettings.MaxConnectionPoolSize = 100` |
| **Async All The Way** | No `.Result`, `.Wait()` — full async pipeline |
| **Pagination** | Cursor-based for large collections |
| **Projection** | Fetch only needed fields |

### Frontend

| Optimization | Implementation |
|--------------|----------------|
| **Standalone Output** | `output: 'standalone'` in `next.config.js` |
| **Dynamic Imports** | `dynamic(() => import(...), { ssr: false })` for heavy sections |
| **Image Optimization** | `next/image` with `loader: 'custom'` for CDN |
| **Font Optimization** | `next/font` with `display: 'swap'` |
| **Bundle Analysis** | `@next/bundle-analyzer` in CI |

### Database

| Optimization | Implementation |
|--------------|----------------|
| **Indexes** | Compound indexes on query patterns |
| **Projection** | `Find().Project()` to reduce payload |
| **Aggregation** | Server-side filtering vs client-side |
| **Read Replicas** | Atlas `readPreference: 'secondaryPreferred'` for analytics |

---

## Caching Strategy

| Data | Cache Layer | TTL | Invalidation |
|------|-------------|-----|--------------|
| Resume (latest) | Output Cache (backend) | 30s | Auto (time) |
| Resume (latest) | Redis (backend) | 5m | Resume update event |
| Assistant Knowledge | MongoDB (persistent) | N/A | Resume hash change |
| Rate Limit Counters | Redis | 60s-1h | Auto (TTL) |
| AI Circuit Breaker | Redis | 30s-5m | Auto (TTL) |
| Static Assets | CDN / Browser | 1yr | Content hash in filename |

---

## Rate Limiting

### Distributed (Redis-backed)

```csharp
public class DistributedRateLimitService : IRateLimitService
{
    public async Task<RateLimitResult> CheckLimitAsync(string key, int limit, TimeSpan window)
    {
        var redisKey = $"ratelimit:{key}";
        var count = await _database.StringIncrementAsync(redisKey);
        
        if (count == 1)
            await _database.KeyExpireAsync(redisKey, window);
            
        return new RateLimitResult
        {
            Allowed = count <= limit,
            Remaining = Math.Max(0, limit - (int)count),
            ResetAt = DateTimeOffset.UtcNow.Add(window)
        };
    }
}
```

**Policies**:
| Endpoint | Limit | Window | Key |
|----------|-------|--------|-----|
| `/api/assistant` | 20 | 1 min | IP + UserAgent |
| `/api/contacts` | 5 | 1 hour | IP |
| `/api/donations/orders` | 10 | 1 hour | IP |
| `/api/visits` | 100 | 1 min | IP |

**Fallback**: In-memory sliding window if Redis unavailable (logged warning)

---

## Circuit Breaker (AI Calls)

```csharp
public class GeminiCircuitBreaker
{
    private readonly IDatabase _redis;
    private const string Key = "circuitbreaker:gemini";
    
    public async Task<bool> IsOpenAsync()
    {
        var failures = await _database.StringGetAsync(Key);
        return failures.HasValue && (int)failures >= 5;
    }
    
    public async Task RecordSuccessAsync()
    {
        await _database.KeyDeleteAsync(Key);
    }
    
    public async Task RecordFailureAsync()
    {
        await _database.StringIncrementAsync(Key);
        await _database.KeyExpireAsync(Key, TimeSpan.FromMinutes(5));
    }
}
```

**Behavior**:
- **Closed** (normal): Calls proceed
- **Open** (5 failures): Fast-fail, return cached/error response
- **Half-Open** (after 30s): Allow 1 test call

---

## Load Testing

### Tool: k6

```javascript
// load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '2m', target: 50 },   // Ramp up
    { duration: '5m', target: 100 },  // Sustained
    { duration: '2m', target: 200 },  // Stress
    { duration: '2m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function() {
  const base = 'http://localhost:5000';
  
  // Health check
  check(http.get(`${base}/health/ready`), { 'ready 200': r => r.status === 200 });
  
  // Resume
  check(http.get(`${base}/api/resumes/latest`), { 'resume 200': r => r.status === 200 });
  
  // Assistant (sample query)
  const payload = JSON.stringify({ query: "What is your experience with .NET?", history: [] });
  check(http.post(`${base}/api/assistant`, payload, { headers: { 'Content-Type': 'application/json' } }), {
    'assistant 200': r => r.status === 200,
    'streamed': r => r.body.includes('data:'),
  });
  
  sleep(1);
}
```

**Run**: `k6 run load-test.js`

---

## Capacity Planning

| Metric | Current | 10x Target | Action |
|--------|---------|------------|--------|
| Backend CPU | 10% | 50% | Add replicas |
| Backend Memory | 100MB | 400MB | Increase limit |
| Redis Memory | 50MB | 200MB | Enable Cluster |
| MongoDB Connections | 20 | 150 | Increase pool, read replicas |
| Gemini API Quota | 1500/day | 15000/day | Request quota increase |

---

## Related Documents

- [Resilience Patterns](resilience.md) — Circuit breakers, retries, fallbacks
- [System Overview](system-overview.md) — Architecture diagram
- [Data Protection](../deployment/data-protection.md) — Backup strategy
- [Production Runbook](../deployment/production-runbook.md) — Operational procedures