# Production Environment Variables

## Complete Reference

All variables required for production deployment. **Never commit `.env` to version control.**

---

## Core

| Variable | Value | Notes |
|----------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Enables production middleware |
| `ASPNETCORE_URLS` | `http://+:8080` | Container internal port |
| `NODE_ENV` | `production` | Next.js production mode |

---

## Database (MongoDB Atlas)

| Variable | Value | Notes |
|----------|-------|-------|
| `MONGODB_CONNECTIONSTRING` | `mongodb+srv://user:pass@cluster.mongodb.net/db?retryWrites=true&w=majority` | **Required** — Atlas SRV format |
| `MONGODB_DATABASE` | `KaiAssistant` | Database name |

**Atlas Requirements**:
- Cluster tier M10+ for production
- Network access: Allow server IP or VPC peering
- Database user: Read/Write on `KaiAssistant` database

---

## Redis (Production TLS Required)

| Variable | Value | Notes |
|----------|-------|-------|
| `REDIS__CONNECTIONSTRING` | `rediss://redis:6380` | **Required** — `rediss://` for TLS |
| `REDIS__STRICTDISTRIBUTEDMODE` | `true` | Enforce distributed rate limiting |

**Certificate Setup**:
```bash
# Create volume
docker volume create redis-certs

# Copy CA-signed certificates
docker run --rm -v redis-certs:/certs -v /path/to/certs:/source alpine \
  cp /source/redis.crt /source/redis.key /source/ca.crt /certs/
```

**Redis Config** (in `docker-compose.prod.yml`):
- TLS port 6380 only
- RDB + AOF persistence
- `maxmemory 256mb`, `allkeys-lru`

---

## AI Assistant (Google Gemini)

| Variable | Value | Notes |
|----------|-------|-------|
| `GEMINI_API_KEY` | `your-api-key` | **Required** — Google AI Studio |
| `GEMINI_MODEL_NAMES` | `gemini-3.1-flash-lite-preview:generateContent,gemini-3-flash-preview:generateContent,gemini-2.5-flash:generateContent,gemini-2.5-flash-lite:generateContent` | Comma-separated fallbacks |
| `GEMINI_ENDPOINT` | `https://generativelanguage.googleapis.com/v1beta/models/` | Default |
| `GEMINI_SYSTEM_PROMPT` | (built-in) | Optional override |
| `GEMINI_PROMPT_MAX_CHARS` | `10000` | |
| `GEMINI_INCLUDE_PERSONAL_DETAILS` | `true` | |
| `GEMINI_TEMPERATURE` | `0.7` | |
| `GEMINI_TOPK` | `40` | |
| `GEMINI_TOPP` | `0.95` | |
| `GEMINI_MAX_OUTPUT_TOKENS` | `8192` | |
| `GEMINI_CANDIDATE_COUNT` | `1` | |

---

## PayPal Donations (Live Required)

| Variable | Value | Notes |
|----------|-------|-------|
| `PAYPAL_CLIENT_ID` | `live-client-id` | **Required** — Live app |
| `PAYPAL_CLIENT_SECRET` | `live-secret` | **Required** |
| `PAYPAL_ENVIRONMENT` | `live` | **Must be `live`** |
| `PAYPAL_WEBHOOK_ID` | `wh_live_xxx` | **Required** — Webhook ID (not URL) |
| `PAYPAL_MERCHANT_ID` | `merchant-id` | Optional |
| `PAYPAL_EMAIL_RETENTION_DAYS` | `30` | GDPR compliance |

**.NET-style equivalents** (take precedence):
- `PayPal__ClientId`, `PayPal__ClientSecret`, `PayPal__Environment`, `PayPal__WebhookId`, `PayPal__MerchantId`, `PayPal__EmailRetentionDays`

**Webhook Registration**:
- URL: `https://api.yourdomain.com/api/donations/webhook`
- Events: `PAYMENT.CAPTURE.COMPLETED`, `PAYMENT.CAPTURE.REFUNDED`, `PAYMENT.CAPTURE.REVERSED`
- Verify signature in PayPal dashboard

---

## SMTP / Email

| Variable | Value | Notes |
|----------|-------|-------|
| `SMTP_SERVER` | `smtp.gmail.com` | Or transactional service (SendGrid, etc.) |
| `SMTP_PORT` | `587` | STARTTLS (465 for SSL) |
| `SMTP_SENDER_EMAIL` | `noreply@yourdomain.com` | **Required** |
| `SMTP_SENDER_PASSWORD` | `app-password-or-api-key` | **Required** |
| `SMTP_RECEIVER_EMAIL` | `you@yourdomain.com` | Contact form destination |
| `SMTP_ENABLED` | `true` | |

**Gmail**: Use App Password (not account password)
**Transactional**: Use API key as password

---

## CORS & Security

| Variable | Value | Notes |
|----------|-------|-------|
| `ALLOWED_ORIGINS` | `https://yourdomain.com,https://www.yourdomain.com` | **Required** — Exact domains, no trailing slashes |

---

## Frontend Configuration

| Variable | Value | Notes |
|----------|-------|-------|
| `BACKEND_API_URL` | `http://backend:8080` | Internal Docker network |
| `GITHUB_USER` | `your-github-username` | Optional override |
| `GITHUB_TOKEN` | `ghp_xxx` | Optional, server-side only |
| `GITHUB_USER_AGENT` | `kai-portfolio-site` | |
| `NEXT_PUBLIC_SITE_URL` | `https://yourdomain.com` | **Required** — Canonical URL |
| `GOOGLE_SITE_VERIFICATION` | `verification-code` | Optional |
| `NEXT_PUBLIC_DEBUG_AI` | (unset) | Never enable in prod |

---

## Observability (Optional)

| Variable | Value | Notes |
|----------|-------|-------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `https://otel-collector:4317` | Or Grafana Cloud, etc. |
| `OTEL_SERVICE_NAME` | `kai-assistant-api` | |

---

## Redis TLS Certificates (Container Paths)

| Variable | Path | Notes |
|----------|------|-------|
| `REDIS_TLS_CERT_FILE` | `/etc/redis/tls/redis.crt` | Do not change |
| `REDIS_TLS_KEY_FILE` | `/etc/redis/tls/redis.key` | Do not change |
| `REDIS_TLS_CA_CERT_FILE` | `/etc/redis/tls/ca.crt` | Do not change |

Mounted via `redis-certs` volume in `docker-compose.prod.yml`.

---

## Variable Precedence (Backend)

```
1. Container environment (docker-compose environment:)
2. .env file (docker-compose env_file:)
3. appsettings.Production.json
4. appsettings.json
5. Environment variables (OS level)
6. .NET User Secrets (Development only)
```

---

## Validation Checklist

Before deploying, verify:

- [ ] `MONGODB_CONNECTIONSTRING` — Atlas cluster reachable, IP whitelisted
- [ ] `REDIS__CONNECTIONSTRING` — `rediss://` with certs mounted
- [ ] `GEMINI_API_KEY` — Valid, quota available
- [ ] `PAYPAL_*` — Live credentials, webhook registered, `ENVIRONMENT=live`
- [ ] `SMTP_*` — Test email sends successfully
- [ ] `ALLOWED_ORIGINS` — Exact production domains
- [ ] `NEXT_PUBLIC_SITE_URL` — Matches production domain
- [ ] `BACKEND_API_URL` — Internal Docker network address

---

## Example Production `.env`

```bash
# Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
NODE_ENV=production

# Database
MONGODB_CONNECTIONSTRING=mongodb+srv://user:pass@cluster.mongodb.net/KaiAssistant?retryWrites=true&w=majority
MONGODB_DATABASE=KaiAssistant

# Redis (TLS)
REDIS__CONNECTIONSTRING=rediss://redis:6380
REDIS__STRICTDISTRIBUTEDMODE=true

# AI
GEMINI_API_KEY=your-production-key
GEMINI_MODEL_NAMES=gemini-3.1-flash-lite-preview:generateContent,gemini-3-flash-preview:generateContent,gemini-2.5-flash:generateContent,gemini-2.5-flash-lite:generateContent

# PayPal (LIVE)
PAYPAL_CLIENT_ID=live-client-id
PAYPAL_CLIENT_SECRET=live-secret
PAYPAL_ENVIRONMENT=live
PAYPAL_WEBHOOK_ID=wh_live_xxx
PAYPAL_MERCHANT_ID=merchant-id
PAYPAL_EMAIL_RETENTION_DAYS=30

# SMTP
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_SENDER_EMAIL=noreply@yourdomain.com
SMTP_SENDER_PASSWORD=app-password
SMTP_RECEIVER_EMAIL=you@yourdomain.com
SMTP_ENABLED=true

# CORS
ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com

# Frontend
BACKEND_API_URL=http://backend:8080
NEXT_PUBLIC_SITE_URL=https://yourdomain.com
GITHUB_USER=your-github-username
GITHUB_TOKEN=ghp_xxx
```

---

## Related Documents

- [Production Runbook](production-runbook.md) — Deployment steps
- [Data Protection Strategy](../DATA_PROTECTION_STRATEGY.md) — Backup/DR
- [Docker Compose](../development/docker-compose.md) — Compose configuration
- [Environment Variables Template](../.env.example) — Template file