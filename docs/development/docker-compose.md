# Docker Compose Reference

## Files

| File | Purpose | Loaded |
|------|---------|--------|
| `docker-compose.yml` | Base configuration (dev defaults) | Always |
| `docker-compose.override.yml` | Local dev overrides (hot reload, `Development` env) | Auto (gitignored) |
| `docker-compose.prod.yml` | Production overrides (TLS Redis, resource limits, `Production` env) | Explicit `-f` |

---

## Base: `docker-compose.yml`

```yaml
services:
  redis:
    image: redis:7.0
    container_name: redis
    restart: unless-stopped

  backend:
    build:
      context: ./backend
    env_file:
      - .env
    environment:
      - SMTP_SERVER=${SMTP_SERVER}
      - SMTP_PORT=${SMTP_PORT}
      # ... all backend env vars mapped from ${VAR}
      - ASPNETCORE_URLS=http://+:8080
      - MONGODB_CONNECTIONSTRING=${MONGODB_CONNECTIONSTRING}
      - MONGODB_DATABASE=${MONGODB_DATABASE}
      - GEMINI_API_KEY=${GEMINI_API_KEY}
      - GEMINI_MODEL_NAMES=${GEMINI_MODEL_NAMES}
      # ... all Gemini vars
    ports:
      - 5000:8080
    depends_on:
      - redis
    restart: unless-stopped

  frontend:
    build:
      context: ./frontend
    environment:
      - NODE_ENV=production
      - BACKEND_API_URL=http://backend:8080
      - GITHUB_USER=${GITHUB_USER:-}
      - GITHUB_TOKEN=${GITHUB_TOKEN:-}
      - GITHUB_USER_AGENT=${GITHUB_USER_AGENT:-kai-portfolio-site}
    depends_on:
      - backend
    ports:
      - 3000:3000
    restart: unless-stopped
```

**Key points**:
- No `ASPNETCORE_ENVIRONMENT` hardcoded — set via override
- `env_file: .env` loads all variables; individual `environment:` maps them to container
- Frontend `BACKEND_API_URL` uses Docker service name `backend:8080`
- MongoDB **not** provisioned — configure `MONGODB_CONNECTIONSTRING` to external instance

---

## Development Override: `docker-compose.override.yml`

Create this file locally (gitignored via `.gitignore`):

```yaml
services:
  redis:
    command: redis-server --save 60 1 --loglevel debug
    ports:
      - 6379:6379

  backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - REDIS__CONNECTIONSTRING=redis://redis:6379
      - REDIS__STRICTDISTRIBUTEDMODE=false
    volumes:
      - ./backend:/src
    command: dotnet watch --project KaiAssistant.API run --no-launch-profile

  frontend:
    environment:
      - NODE_ENV=development
    volumes:
      - ./frontend:/app
      - /app/node_modules
      - /app/.next
    command: npm run dev
    ports:
      - 3000:3000
      - 9229:9229  # Node inspector
```

**Usage**: `docker compose up -d --build` (auto-loads override)

---

## Production: `docker-compose.prod.yml`

```yaml
services:
  redis:
    image: redis:7.0
    container_name: redis-prod
    command: >
      redis-server
      --save 300 10
      --appendonly yes
      --appendfsync everysec
      --maxmemory 256mb
      --maxmemory-policy allkeys-lru
      --tls-port 6380
      --port 0
      --tls-cert-file /etc/redis/tls/redis.crt
      --tls-key-file /etc/redis/tls/redis.key
      --tls-ca-cert-file /etc/redis/tls/ca.crt
      --tls-auth-clients no
    volumes:
      - redis-data:/data
      - redis-certs:/etc/redis/tls:ro
    deploy:
      resources:
        limits:
          memory: 256M
          cpus: '0.25'
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "redis-cli", "-u", "rediss://localhost:6380", "--tls", "--cert", "/etc/redis/tls/redis.crt", "--key", "/etc/redis/tls/redis.key", "--cacert", "/etc/redis/tls/ca.crt", "PING"]
      interval: 30s
      timeout: 10s
      start_period: 15s
      retries: 3

  backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - REDIS__CONNECTIONSTRING=rediss://redis:6380
      - REDIS__STRICTDISTRIBUTEDMODE=true
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'
    restart: unless-stopped

  frontend:
    environment:
      - NODE_ENV=production
      - BACKEND_API_URL=http://backend:8080
    deploy:
      resources:
        limits:
          memory: 256M
          cpus: '0.25'
        reservations:
          memory: 128M
          cpus: '0.1'
    restart: unless-stopped

volumes:
  redis-data:
  redis-certs:
    external: true
```

**Usage**: `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build`

---

## Environment Variable Precedence

```
Highest: docker-compose.prod.yml environment:
         docker-compose.override.yml environment:
         docker-compose.yml environment:
         .env file (env_file:)
Lowest:  Dockerfile ENV defaults
```

---

## Service Communication

| From | To | Address |
|------|-----|---------|
| Frontend (browser) | Frontend (server) | `http://localhost:3000` |
| Frontend (server) | Backend | `http://backend:8080` |
| Backend | Redis (dev) | `redis://redis:6379` |
| Backend | Redis (prod) | `rediss://redis:6380` (TLS) |
| Backend | MongoDB | `MONGODB_CONNECTIONSTRING` (external) |

---

## Volumes

| Volume | Type | Purpose |
|--------|------|---------|
| `redis-data` | Named (prod) | Redis RDB/AOF persistence |
| `redis-certs` | External (prod) | TLS certificates (CA-signed) |
| `./backend:/src` | Bind (dev) | Hot reload source |
| `./frontend:/app` | Bind (dev) | Hot reload source |

---

## Health Checks

```yaml
# Backend (in Dockerfile)
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

# Frontend (in Dockerfile)
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:3000/api/health/live || exit 1

# Redis (prod override)
healthcheck:
  test: ["CMD", "redis-cli", "-u", "rediss://localhost:6380", "--tls", ...]
```

---

## Common Commands

```bash
# Development (with override)
docker compose up -d --build
docker compose logs -f backend
docker compose exec backend dotnet test

# Production
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f

# Maintenance
docker compose down                    # Stop, keep volumes
docker compose down -v                 # Stop, remove volumes
docker compose pull                    # Update base images
docker compose up -d --build --force-recreate  # Full rebuild
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Backend can't connect to Redis | Wrong connection string | Check `REDIS__CONNECTIONSTRING` matches override (dev: `redis://`, prod: `rediss://`) |
| Frontend 503 on load | Backend not ready | Increase `start_period` or check backend logs |
| TLS handshake fail (prod) | Certs missing in `redis-certs` volume | `docker run --rm -v redis-certs:/certs -v /path/to/certs:/source alpine cp /source/* /certs/` |
| MongoDB timeout | `MONGODB_CONNECTIONSTRING` wrong | Verify Atlas IP whitelist, credentials, SRV format |
| Port 3000/5000 in use | Conflict | `docker compose down` first; check `netstat -ano | findstr :3000` |