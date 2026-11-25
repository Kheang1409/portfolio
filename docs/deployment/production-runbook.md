# Production Deployment Runbook

## Overview

This runbook covers deploying the KaiAssistant Portfolio to production using Docker Compose.

---

## Prerequisites

| Tool | Version |
|------|---------|
| Docker Engine | 24+ |
| Docker Compose | v2 (plugin) |
| Git | 2.40+ |

---

## Pre-Deployment Checklist

- [ ] **Secrets populated** in `.env` (from secret store)
- [ ] **MongoDB Atlas** cluster running, IP whitelisted
- [ ] **Redis TLS certificates** in `redis-certs` volume
- [ ] **PayPal** live credentials configured, webhook registered
- [ ] **SMTP** credentials tested
- [ ] **Gemini API key** valid with quota
- [ ] **Domain DNS** pointing to server
- [ ] **SSL certificates** configured (reverse proxy)

---

## Environment Variables (Production)

```bash
# Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
NODE_ENV=production

# Database
MONGODB_CONNECTIONSTRING=mongodb+srv://...
MONGODB_DATABASE=KaiAssistant

# Redis (TLS required)
REDIS__CONNECTIONSTRING=rediss://redis:6380
REDIS__STRICTDISTRIBUTEDMODE=true

# AI
GEMINI_API_KEY=...
GEMINI_MODEL_NAMES=gemini-3.1-flash-lite-preview:generateContent,...

# PayPal (LIVE)
PAYPAL_CLIENT_ID=...
PAYPAL_CLIENT_SECRET=...
PAYPAL_ENVIRONMENT=live
PAYPAL_WEBHOOK_ID=...
PAYPAL_MERCHANT_ID=...

# Email
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_SENDER_EMAIL=...
SMTP_SENDER_PASSWORD=...
SMTP_RECEIVER_EMAIL=...
SMTP_ENABLED=true

# CORS
ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com

# Frontend
BACKEND_API_URL=http://backend:8080
NEXT_PUBLIC_SITE_URL=https://yourdomain.com
```

---

## Deployment Steps

### 1. Prepare Server

```bash
# Clone repository
git clone https://github.com/yourorg/portfolio.git
cd portfolio

# Create .env from secret store (or fetch-secrets.ps1)
cp .env.example .env
# Edit .env with production values

# Create redis-certs volume and copy certificates
docker volume create redis-certs
docker run --rm -v redis-certs:/certs -v /path/to/certs:/source alpine \
  cp /source/*.crt /source/*.key /certs/
```

### 2. Build & Deploy

```bash
# Build images
docker compose -f docker-compose.yml -f docker-compose.prod.yml build

# Deploy (initial)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Deploy (update)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps backend frontend
```

### 3. Verify Deployment

```bash
# Check container status
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps

# Check logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f backend
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f frontend

# Health checks
curl -f http://localhost:5000/health/ready
curl -f http://localhost:3000/api/health/live

# Full verification script
./deploy/verify-deployment.ps1
```

### 4. Post-Deployment

- [ ] Verify frontend loads at `https://yourdomain.com`
- [ ] Test assistant query
- [ ] Test contact form
- [ ] Test donation flow (small amount)
- [ ] Check MongoDB collections populated
- [ ] Check Redis connectivity
- [ ] Monitor logs for 10 minutes

---

## Rollback Procedure

```bash
# Quick rollback (previous images)
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Full rollback (specific commit)
git checkout <previous-tag>
docker compose -f docker-compose.yml -f docker-compose.prod.yml build
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## Monitoring Commands

```bash
# Container stats
docker stats

# Backend logs (last 100 lines)
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs --tail=100 backend

# Frontend logs
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs --tail=100 frontend

# Redis info
docker exec redis redis-cli -u rediss://redis:6380 --tls INFO memory

# MongoDB status (via Atlas UI or mongosh)
```

---

## Troubleshooting

| Issue | Check | Fix |
|-------|-------|-----|
| Backend 503 | `/health/ready` fails | Check MongoDB/Redis connectivity |
| Assistant fallback | Logs show Gemini errors | Verify `GEMINI_API_KEY`, quota |
| Donations fail | PayPal webhook not received | Check webhook URL, `PAYPAL_WEBHOOK_ID` |
| SSL errors | Browser shows insecure | Configure reverse proxy (nginx/traefik) |
| High memory | `docker stats` shows >80% | Increase limits in `docker-compose.prod.yml` |

---

## Maintenance Windows

| Task | Frequency | Procedure |
|------|-----------|-----------|
| Base image updates | Weekly | `docker compose pull && docker compose up -d --build` |
| Redis RDB backup | Daily (auto) | Verify `redis-data` volume |
| MongoDB backup | Continuous (Atlas) | Verify Atlas backup status |
| Log rotation | Automatic | Docker JSON file driver (max-size: 10m, max-file: 3) |
| Certificate renewal | 90 days (Let's Encrypt) | Update reverse proxy certs |

---

## Emergency Contacts

| Role | Contact |
|------|---------|
| Primary On-Call | [Name] - [Phone] - [Email] |
| Secondary On-Call | [Name] - [Phone] - [Email] |
| MongoDB Atlas Support | [Support URL] |
| PayPal Developer Support | [Support URL] |

---

## Related Documents

- [Production Environment Variables](production-env-vars.md)
- [Data Protection Strategy](../DATA_PROTECTION_STRATEGY.md)
- [Docker Compose Reference](../development/docker-compose.md)
- [Deploy Scripts](../deploy/)