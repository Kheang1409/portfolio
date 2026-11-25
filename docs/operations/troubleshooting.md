# Troubleshooting Guide

## Quick Diagnosis

| Symptom | Likely Cause | First Check |
|---------|--------------|-------------|
| Profile not loading | MongoDB connectivity | `curl /api/resumes/latest` |
| Assistant fails | Gemini / Redis / MongoDB | Backend logs, `/health/ready` |
| GitHub projects empty | Resume GitHub URL / `GITHUB_USER` | Resume data, env var |
| Donations broken | PayPal config / webhook | PayPal dashboard, backend logs |
| 503 page shows | Backend unhealthy | `/health/ready`, container logs |
| High latency | Redis / MongoDB slow | `redis-cli INFO`, Atlas metrics |

---

## Backend Issues

### Profile / Resume Not Loading

**Symptoms**: Home page shows skeleton, resume page empty, assistant says "no data"

**Checks**:
```bash
# 1. Direct API call
curl http://localhost:5000/api/resumes/latest

# 2. Check MongoDB connectivity
docker exec backend mongosh "mongodb+srv://..." --eval 'db.resumes.findOne()'

# 3. Verify environment
docker exec backend env | grep MONGODB
```

**Common Causes**:
- `MONGODB_CONNECTIONSTRING` wrong (check Atlas IP whitelist)
- No resume document in database
- MongoDB Atlas cluster paused (M0 free tier)

**Fix**: Insert resume document, verify connection string, resume cluster.

---

### Assistant Not Working

**Symptoms**: "I'm having trouble connecting" fallback, empty responses, timeouts

**Checks**:
```bash
# 1. Backend health
curl http://localhost:5000/health/ready

# 2. Check Gemini config
docker exec backend env | grep GEMINI

# 3. Check Redis (rate limiting, circuit breaker)
docker exec redis redis-cli -u rediss://redis:6380 --tls INFO

# 4. Backend logs
docker compose logs backend | grep -i gemini
```

**Common Causes**:
| Issue | Indicator | Fix |
|-------|-----------|-----|
| Invalid API key | 401 in logs | Update `GEMINI_API_KEY` |
| Quota exceeded | 429 in logs | Wait / request quota increase |
| Redis down | Circuit breaker open | Check Redis container |
| MongoDB down | Retrieval fails | Check `/health/ready` |

**Degraded Mode**: Keyword-only retrieval works without embeddings.

---

### Donations Broken

**Symptoms**: Config not loading, order creation fails, capture fails, webhook not processed

**Checks**:
```bash
# 1. Config endpoint
curl http://localhost:5000/api/donations/config

# 2. PayPal dashboard
# Check: App credentials, webhook registration, webhook events

# 3. Backend logs
docker compose logs backend | grep -i paypal
```

**Common Causes**:
| Issue | Indicator | Fix |
|-------|-----------|-----|
| Sandbox vs Live mismatch | `PAYPAL_ENVIRONMENT` wrong | Set to `live` for production |
| Webhook ID wrong | 400 on webhook | Use Webhook ID (not URL) |
| Credentials invalid | 401 from PayPal | Verify Client ID/Secret |
| Webhook not registered | No events in dashboard | Register webhook URL |

**Webhook Debug**:
```bash
# Check webhook events in MongoDB
docker exec backend mongosh KaiAssistant --eval 'db.donationWebhookEvents.find().sort({receivedAt: -1}).limit(5).pretty()'
```

---

### Rate Limiting Issues

**Symptoms**: 429 responses, "Too many requests" errors

**Checks**:
```bash
# 1. Redis connectivity
docker exec redis redis-cli -u rediss://redis:6380 --tls PING

# 2. Rate limit keys
docker exec redis redis-cli -u rediss://redis:6380 --tls KEYS "ratelimit:*"

# 3. Backend logs
docker compose logs backend | grep -i ratelimit
```

**Fixes**:
- Increase limits in code if legitimate traffic
- Check `REDIS__STRICTDISTRIBUTEDMODE=false` for dev
- Verify Redis memory not full (`INFO memory`)

---

## Frontend Issues

### 503 Page Showing (Backend Unavailable)

**Symptoms**: Illustrated 503 page, auto-retry every 30s

**Checks**:
```bash
# 1. Backend health
curl http://localhost:5000/health/ready

# 2. Frontend health proxy
curl http://localhost:3000/api/health/live

# 3. Container status
docker compose ps
```

**Root Causes**:
- Backend container crashed (check `docker compose logs backend`)
- MongoDB/Redis down (backend `/health/ready` returns 503)
- Network issue between frontend/backend containers

---

### Assistant UI Not Loading

**Symptoms**: Empty chat, send button disabled, no suggestions

**Checks**:
1. Browser console for JS errors
2. Network tab: `/api/assistant` request/response
3. `NEXT_PUBLIC_DEBUG_AI=true` for verbose logging

**Common Causes**:
- Backend `/api/assistant` returning error (check backend logs)
- CORS misconfiguration (`ALLOWED_ORIGINS`)
- TypeScript build error (run `npm run type-check`)

---

### GitHub Projects Empty

**Symptoms**: "No projects found" on home page

**Checks**:
```bash
# 1. Resume has GitHub URL
curl http://localhost:5000/api/resumes/latest | jq '.profile.githubUrl'

# 2. Frontend proxy
curl http://localhost:3000/api/github/projects

# 3. GitHub API rate limit
curl -H "Authorization: token $GITHUB_TOKEN" https://api.github.com/rate_limit
```

**Fixes**:
- Add GitHub URL to resume document
- Set `GITHUB_USER` env var as override
- Add `GITHUB_TOKEN` for higher rate limit

---

### Resume PDF Not Downloading

**Symptoms**: 404 on `/resume.pdf` or downloads wrong file

**Checks**:
```bash
# 1. File exists
ls -la frontend/public/resume.pdf

# 2. Next.js serves static files
curl -I http://localhost:3000/resume.pdf
```

**Fix**: Regenerate PDF and commit to `frontend/public/resume.pdf`

---

## Docker/Infrastructure Issues

### Container Won't Start

**Checks**:
```bash
# 1. Container logs
docker compose logs backend
docker compose logs frontend

# 2. Resource limits
docker stats

# 3. Port conflicts
netstat -tulpn | grep -E '3000|5000|6379|6380'
```

**Common Causes**:
- Port already in use
- Memory limit too low (OOM kill)
- Missing environment variables
- Health check failing (increase `start_period`)

---

### Redis TLS Handshake Failure

**Symptoms**: Backend logs show TLS errors, Redis connection fails

**Checks**:
```bash
# 1. Certificates in volume
docker run --rm -v redis-certs:/certs alpine ls -la /certs/

# 2. Test TLS connection
docker exec redis redis-cli -u rediss://redis:6380 --tls --cert /etc/redis/tls/redis.crt --key /etc/redis/tls/redis.key --cacert /etc/redis/tls/ca.crt PING
```

**Fix**: Re-create `redis-certs` volume with correct CA-signed certificates.

---

### MongoDB Connection Timeout

**Symptoms**: Backend logs show `TimeoutException` on MongoDB operations

**Checks**:
```bash
# 1. Atlas cluster status
# Check Atlas UI: Cluster running, not paused

# 2. Network access
# Atlas → Network Access → IP whitelist includes server IP

# 3. Connection string format
# Must be SRV: mongodb+srv://user:pass@cluster.mongodb.net/db
```

**Fix**: Whitelist IP, resume cluster, verify connection string.

---

## Performance Issues

### High Latency

**Checks**:
```bash
# 1. Backend health latency
time curl http://localhost:5000/health/ready

# 2. Redis latency
docker exec redis redis-cli -u rediss://redis:6380 --tls --latency-history

# 3. MongoDB Atlas metrics
# Atlas UI → Metrics → Query latency
```

**Optimizations**:
- Enable output caching for `/api/resumes/latest` (30s)
- Check MongoDB indexes
- Increase Redis memory if evicting
- Use read replicas for analytics queries

---

### High Memory Usage

**Checks**:
```bash
docker stats --no-stream
```

**Limits** (in `docker-compose.prod.yml`):
| Service | Memory Limit | CPU Limit |
|---------|-------------|-----------|
| Backend | 512M | 0.5 |
| Frontend | 256M | 0.25 |
| Redis | 256M | 0.25 |

**Fix**: Increase limits if consistently near capacity.

---

## Logging & Debugging

### Enable Debug Logging

**Backend** (Development):
```bash
# In docker-compose.override.yml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - Logging__LogLevel__Default=Debug
```

**Frontend**:
```bash
# Browser console
localStorage.setItem('debug', 'true')
# Or
NEXT_PUBLIC_DEBUG_AI=true
```

### Structured Logs (Production)

```bash
# Backend logs (JSON)
docker compose logs backend | jq '.'

# Filter by correlation ID
docker compose logs backend | jq 'select(.correlationId == "abc-123")'
```

---

## Escalation

| Severity | Response Time | Escalation |
|----------|---------------|------------|
| **Critical** (Site down, data loss) | 15 min | Page primary on-call |
| **High** (Feature broken, payments failing) | 1 hour | Page secondary on-call |
| **Medium** (Performance, minor bugs) | 4 hours | Assign to sprint |
| **Low** (Cosmetic, docs) | Next sprint | Backlog |

---

## Related Documents

- [Monitoring](monitoring.md) — Metrics, alerts, dashboards
- [Production Runbook](../deployment/production-runbook.md) — Deployment procedures
- [Health Checks](../api/health-checks.md) — Health endpoint details
- [Data Protection](../deployment/data-protection.md) — Backup/recovery