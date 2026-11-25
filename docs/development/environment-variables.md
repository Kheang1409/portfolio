# Environment Variables Reference

All configurable variables for the portfolio application. **Never commit `.env` to version control.** Use `.env.example` as template.

---

## Core Configuration

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Yes | `Production` | `Development` \| `Staging` \| `Production` |
| `ASPNETCORE_URLS` | Yes | `http://+:8080` | Backend listen address (container internal) |
| `NODE_ENV` | Yes | `production` | Frontend `development` \| `production` |

---

## Database (MongoDB Atlas) — **Required**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `MONGODB_CONNECTIONSTRING` | Yes | — | `mongodb+srv://user:pass@cluster.mongodb.net/db?retryWrites=true&w=majority` |
| `MONGODB_DATABASE` | Yes | `KaiAssistant` | Database name |

**Collections created automatically**: `resumes`, `donations`, `donationWebhookEvents`, `donationReceipts`, `visitors`, `conversations`, `rateLimitKeys`, `knowledgeChunks`

---

## Redis — **Required**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `REDIS__CONNECTIONSTRING` | Yes | `redis://redis:6379` | Dev: `redis://redis:6379` • Prod: `rediss://redis:6380` (TLS) |
| `REDIS__STRICTDISTRIBUTEDMODE` | No | `false` | `true` = enforce distributed rate limiting (requires reliable Redis) |

**Production TLS**: Certificates mounted via `redis-certs` volume at `/etc/redis/tls/`

---

## AI Assistant (Google Gemini) — **Required**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `GEMINI_API_KEY` | Yes | — | Google AI Studio API key |
| `GEMINI_MODEL_NAMES` | No | `gemini-3.1-flash-lite-preview:generateContent,gemini-3-flash-preview:generateContent,gemini-2.5-flash:generateContent,gemini-2.5-flash-lite:generateContent` | Comma-separated primary,fallback models |
| `GEMINI_ENDPOINT` | No | `https://generativelanguage.googleapis.com/v1beta/models/` | API base URL |
| `GEMINI_SYSTEM_PROMPT` | No | (built-in) | System prompt for assistant |
| `GEMINI_PROMPT_MAX_CHARS` | No | `10000` | Max prompt characters |
| `GEMINI_INCLUDE_PERSONAL_DETAILS` | No | `true` | Include personal details in context |
| `GEMINI_TEMPERATURE` | No | `0.7` | Generation temperature (0-2) |
| `GEMINI_TOPK` | No | `40` | Top-K sampling |
| `GEMINI_TOPP` | No | `0.95` | Top-P (nucleus) sampling |
| `GEMINI_MAX_OUTPUT_TOKENS` | No | `8192` | Max output tokens |
| `GEMINI_CANDIDATE_COUNT` | No | `1` | Number of candidates |

---

## PayPal Donations — **Required for Donations**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `PAYPAL_CLIENT_ID` | Yes | — | PayPal App Client ID |
| `PAYPAL_CLIENT_SECRET` | Yes | — | PayPal App Secret |
| `PAYPAL_ENVIRONMENT` | Yes | `live` | `sandbox` \| `live` |
| `PAYPAL_WEBHOOK_ID` | Yes | — | PayPal Webhook ID (not URL) |
| `PAYPAL_MERCHANT_ID` | No | — | Merchant ID for multi-account |
| `PAYPAL_EMAIL_RETENTION_DAYS` | No | `30` | Donor email retention (GDPR) |

**.NET-style equivalents** (take precedence when both set):
- `PayPal__ClientId`, `PayPal__ClientSecret`, `PayPal__Environment`, `PayPal__WebhookId`, `PayPal__MerchantId`, `PayPal__EmailRetentionDays`

---

## SMTP / Email — **Required for Contact & Receipts**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `SMTP_SERVER` | Yes | `smtp.gmail.com` | SMTP host |
| `SMTP_PORT` | Yes | `587` | SMTP port (587 STARTTLS, 465 SSL) |
| `SMTP_SENDER_EMAIL` | Yes | — | From address |
| `SMTP_SENDER_PASSWORD` | Yes | — | App password or API key |
| `SMTP_RECEIVER_EMAIL` | Yes | — | Contact form destination |
| `SMTP_ENABLED` | No | `true` | `true` \| `false` |

---

## CORS & Security — **Required**

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ALLOWED_ORIGINS` | Yes | — | Comma-separated: `https://domain.com,https://www.domain.com` |

---

## Frontend Configuration

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `BACKEND_API_URL` | Yes | `http://backend:8080` | Server-to-server backend URL (Docker: `http://backend:8080`, Local: `http://localhost:5000`) |
| `GITHUB_USER` | No | — | Override GitHub username (inferred from resume if empty) |
| `GITHUB_TOKEN` | No | — | Server-side GitHub token (rate limit increase) |
| `GITHUB_USER_AGENT` | No | `kai-portfolio-site` | GitHub API User-Agent |
| `NEXT_PUBLIC_SITE_URL` | Yes | — | Canonical URL for SEO/social (`https://yourdomain.com`) |
| `GOOGLE_SITE_VERIFICATION` | No | — | Search Console verification meta tag |
| `NEXT_PUBLIC_DEBUG_AI` | No | — | Client-side assistant debug logging |

> **Important**: `BACKEND_API_URL` is **server-only**. Never use `NEXT_PUBLIC_` prefix for backend addresses or secrets.

---

## Observability (Optional)

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | — | OpenTelemetry collector endpoint (e.g., `https://otel-collector:4317`) |
| `OTEL_SERVICE_NAME` | No | `kai-assistant-api` | Service name for traces/metrics |

---

## Redis TLS Certificates (Production)

These are **file paths inside the Redis container** — do not change. Certificates are mounted via `redis-certs` volume.

| Variable | Path |
|----------|------|
| `REDIS_TLS_CERT_FILE` | `/etc/redis/tls/redis.crt` |
| `REDIS_TLS_KEY_FILE` | `/etc/redis/tls/redis.key` |
| `REDIS_TLS_CA_CERT_FILE` | `/etc/redis/tls/ca.crt` |

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
- [ ] `REDIS__CONNECTIONSTRING` — TLS in prod (`rediss://`), certs mounted
- [ ] `GEMINI_API_KEY` — Valid, quota available
- [ ] `PAYPAL_*` — Sandbox tested, webhook registered, `PAYPAL_ENVIRONMENT=live` for prod
- [ ] `SMTP_*` — Test email sends successfully
- [ ] `ALLOWED_ORIGINS` — Exact production domains, no trailing slashes
- [ ] `NEXT_PUBLIC_SITE_URL` — Matches production domain
- [ ] `BACKEND_API_URL` — Internal Docker network address for prod

---

## Example `.env` (Development)

```bash
# Core
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
NODE_ENV=development

# Database (local MongoDB or Atlas dev cluster)
MONGODB_CONNECTIONSTRING=mongodb://localhost:27017
MONGODB_DATABASE=KaiAssistant

# Redis (local Docker)
REDIS__CONNECTIONSTRING=redis://redis:6379
REDIS__STRICTDISTRIBUTEDMODE=false

# AI (your Gemini key)
GEMINI_API_KEY=your-dev-key
GEMINI_MODEL_NAMES=gemini-2.5-flash:generateContent

# PayPal (sandbox)
PAYPAL_CLIENT_ID=sandbox-client-id
PAYPAL_CLIENT_SECRET=sandbox-secret
PAYPAL_ENVIRONMENT=sandbox
PAYPAL_WEBHOOK_ID=wh_sandbox_xxx
PAYPAL_MERCHANT_ID=
PAYPAL_EMAIL_RETENTION_DAYS=30

# SMTP (Gmail app password or dev service)
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_SENDER_EMAIL=dev@example.com
SMTP_SENDER_PASSWORD=app-password
SMTP_RECEIVER_EMAIL=dev@example.com
SMTP_ENABLED=true

# CORS (include localhost)
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5000

# Frontend
BACKEND_API_URL=http://localhost:5000
NEXT_PUBLIC_SITE_URL=http://localhost:3000
GITHUB_USER=your-github-username
```