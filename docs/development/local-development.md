# Local Development Guide

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Node.js | 20+ | Frontend development |
| .NET SDK | 10.0 | Backend development |
| Docker Desktop | Latest | Redis, optional MongoDB |
| MongoDB | Atlas or local | Database (required for data-backed features) |
| Redis | 7.0+ | Rate limiting, caching (Docker provided) |

## Quick Start (Docker Compose — Recommended)

```powershell
# 1. Clone and configure
cd portfolio
Copy-Item .env.example .env
# Edit .env with your values (MongoDB, Gemini, PayPal, SMTP)

# 2. Start all services
docker compose up -d --build

# 3. Verify
curl http://localhost:5000/health/ready   # Backend
curl http://localhost:3000/api/health/live # Frontend
```

**Ports**: Frontend `3000` → Backend `5000` (maps to container `8080`) → Redis `6379`

---

## Manual Backend Development

```powershell
cd backend
dotnet restore KaiAssistant.sln
dotnet run --project KaiAssistant.API
```

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger` (Development only)
- Health: `http://localhost:5000/health/ready`

**Required env vars** (set in shell or `.env`):
```powershell
$env:MONGODB_CONNECTIONSTRING="mongodb://localhost:27017"
$env:MONGODB_DATABASE="KaiAssistant"
$env:GEMINI_API_KEY="your-key"
$env:REDIS__CONNECTIONSTRING="redis://localhost:6379"
$env:ASPNETCORE_ENVIRONMENT="Development"
```

---

## Manual Frontend Development

```powershell
cd frontend
npm install
$env:BACKEND_API_URL="http://localhost:5000"
npm run dev
```

- App: `http://localhost:3000`
- Backend proxy: `http://localhost:3000/api/*` → `http://localhost:5000/api/*`

**Frontend-only env vars** (`.env.local` or shell):
```bash
NEXT_PUBLIC_SITE_URL=http://localhost:3000
GITHUB_USER=your-github-username  # Optional override
GITHUB_TOKEN=ghp_xxx              # Optional, server-side only
NEXT_PUBLIC_DEBUG_AI=true         # Optional client debug logging
```

---

## Running Tests

### Backend
```powershell
cd backend
dotnet build KaiAssistant.sln
dotnet test KaiAssistant.Tests/KaiAssistant.Tests.csproj
dotnet test KaiAssistant.RagEvaluation/KaiAssistant.RagEvaluation.csproj
```

### Frontend
```powershell
cd frontend
npm run lint
npm run type-check
npm test           # Playwright E2E
npm run build      # Production build verification
```

---

## Common Workflows

### Add a New Backend Endpoint
1. Define DTO in `KaiAssistant.Application/Contracts/`
2. Add interface in `KaiAssistant.Application/Interfaces/`
3. Implement in `KaiAssistant.Infrastructure/Services/`
4. Register in `KaiAssistant.Infrastructure/DependencyInjection.cs`
5. Add controller in `KaiAssistant.API/Controllers/`
6. Write tests in `KaiAssistant.Tests/`

### Add a New Frontend Page
1. Create route in `src/app/(new-route)/page.tsx`
2. Add components in `src/components/sections/` or `src/components/hud/`
3. Server data fetching via `src/lib/server/backend-client.ts`
4. Client interactivity in `src/components/` with `'use client'`

### Update Resume Data
1. Insert/update document in MongoDB `resumes` collection
2. Backend `KnowledgeBootstrapHostedService` re-indexes at startup
3. Frontend fetches latest via `GET /api/resumes/latest` at request time
4. Regenerate `frontend/public/resume.pdf` for print download

---

## Debugging Tips

| Issue | Check |
|-------|-------|
| Profile not loading | `GET /api/resumes/latest` returns 200 with data |
| Assistant fails | `GEMINI_API_KEY` valid, MongoDB reachable, Redis reachable |
| GitHub projects empty | Resume has GitHub URL or `GITHUB_USER` set |
| Donations broken | PayPal env vars, webhook URL accessible |
| CORS errors | `ALLOWED_ORIGINS` includes `http://localhost:3000` |
| Rate limited | Redis connection, `REDIS__STRICTDISTRIBUTEDMODE=false` for dev |

---

## Hot Reload in Docker

The base `docker-compose.yml` supports hot reload for development:

```powershell
docker compose up -d --build
# Backend: dotnet watch (configured in Dockerfile.dev if created)
# Frontend: next dev with webpack HMR
```

For a dedicated dev override, create `docker-compose.override.yml` (auto-loaded):

```yaml
# docker-compose.override.yml (gitignored)
services:
  backend:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./backend:/src
    command: dotnet watch --project KaiAssistant.API run --no-launch-profile
  frontend:
    volumes:
      - ./frontend:/app
      - /app/node_modules
      - /app/.next
    command: npm run dev
```

---

## Useful Commands

```powershell
# View logs
docker compose logs -f backend
docker compose logs -f frontend

# Restart single service
docker compose restart backend

# Rebuild after dependency changes
docker compose up -d --build backend

# Clean rebuild
docker compose down -v
docker compose up -d --build

# Execute in container
docker compose exec backend bash
docker compose exec frontend sh
```