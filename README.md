# Kai's Portfolio

A full-stack portfolio application built with **Next.js 16** and **.NET 10**. The frontend loads profile data from the backend at runtime; MongoDB, Redis, and Google Gemini are required for persisted profile data, assistant features, and rate limiting.

---

## Features

### Frontend (Next.js 16, App Router)
- Responsive portfolio with sections for profile, skills, experience, projects, education, assistant, donations, and contact
- Custom illustrated 404, server-error, and maintenance experiences
- Data-driven `/resume` page with section navigation, print styling, and bundled PDF download
- GitHub project feed at `/api/github/projects`
- Portfolio assistant with suggested questions, streamed responses, local conversation history, retry/stop, and copy controls
- Assistant proxy at `/api/assistant`, preserving streaming responses and conversation context
- Contact and visitor-tracking integrations with the backend
- PayPal donation flow with backend order creation and capture

### Backend (.NET 10, Clean Architecture)
- `POST /api/assistant` — streaming portfolio assistant grounded in the latest resume
- `POST /api/contacts` and `POST /api/visits` — contact and visitor requests
- `GET /api/resumes/latest` — current resume data used by the home page and resume page
- `GET /api/donations/config`, `GET /api/donations/leaderboard`, and donation order/capture endpoints — PayPal checkout integration
- `GET /api/health`, `/health/live`, and `/health/ready` — API health and readiness
- `/ops/*` — operational endpoints (disabled by default)

The frontend maintenance page at `/maintenance` is informational, not a global maintenance switch. The resume page, home-page profile sections, and footer retrieve the latest resume from the backend at request time. The resume PDF in `frontend/public/resume.pdf` is a separate bundled file — refresh it whenever the stored resume is updated.

---

## Technology Stack

| Category | Technologies |
|----------|--------------|
| **Frontend** | Next.js 16 (App Router), React 18, TypeScript, Tailwind CSS, Framer Motion |
| **Backend** | .NET 10, ASP.NET Core, MediatR, Polly (resilience) |
| **Database** | MongoDB (Atlas) |
| **Cache** | Redis 7.0+ (TLS in production) |
| **AI** | Google Gemini API (streaming, embeddings) |
| **Payments** | PayPal Checkout SDK (Orders v2, Webhooks) |
| **Infrastructure** | AWS Lambda (backend), Docker, AWS SAM |
| **Containerization** | Docker Compose (dev + prod override) |
| **CI/CD** | GitHub Actions (security gates, quality checks, Lambda deploy) |

---

## Requirements

| Requirement | Version | Notes |
|-------------|---------|-------|
| Node.js | 20+ | Required for frontend development |
| .NET SDK | 10 | Required for backend development |
| Docker Desktop | Latest | For Compose workflow |
| MongoDB | Atlas (recommended) | Profile, assistant knowledge, contacts, visits, donations |
| Redis | 7.0+ | Distributed rate limiting and caching |
| Google Gemini API Key | — | Assistant generation and semantic retrieval |

---

## Getting Started

### 1. Clone the Repository

```powershell
git clone <repository-url>
cd portfolio
```

### 2. Configure Environment Variables

```powershell
Copy-Item .env.example .env
# Edit .env with your MongoDB, Redis, Gemini, PayPal, and SMTP values
```

Required variables are documented in [.env.example](.env.example) and [Environment Variables](docs/development/environment-variables.md).

### 3. Start with Docker Compose

**Development** (with hot reload override):
```powershell
docker compose up -d --build
```

**Production** (with production overrides):
```powershell
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

**Ports**: Frontend `3000` → Backend `5000` (maps to container `8080`) → Redis `6379`

### 4. Verify Health

```powershell
curl http://localhost:5000/health/ready     # Backend
curl http://localhost:3000/api/health/live  # Frontend
```

---

## Development & Validation

### Frontend
```powershell
cd frontend
npm run lint
npm run type-check
npm test           # Playwright E2E
npm run build      # Production build verification
```

### Backend
```powershell
cd backend
dotnet build KaiAssistant.sln
dotnet test KaiAssistant.Tests/KaiAssistant.Tests.csproj
dotnet test KaiAssistant.RagEvaluation/KaiAssistant.RagEvaluation.csproj
```

---

## Docker Deployment

| Environment | Command |
|-------------|---------|
| Development | `docker compose up -d --build` |
| Production | `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build` |

- `docker-compose.yml` — Base configuration (services, networks, dev defaults)
- `docker-compose.prod.yml` — Production overrides (TLS Redis, resource limits, healthchecks, persistent volumes)

---

## CI/CD

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| [`.github/workflows/ci-security.yml`](.github/workflows/ci-security.yml) | Push/PR to `main`, `develop` | Security gates: NuGet/npm audit, Trivy container scan, CodeQL, secret scanning, dependency review; Quality: build, test, lint, type-check |
| [`.github/workflows/deploy-lambda.yml`](.github/workflows/deploy-lambda.yml) | Push to `production` branch, manual dispatch | Deploys backend to AWS Lambda via SAM; validates PayPal secrets, audits dependencies |

**Note**: Tests run in CI as part of the quality gates (see `ci-security.yml`).

---

## Documentation

All technical documentation lives in [`docs/`](docs/) — this is the single source of truth.

| Area | Entry Points |
|------|--------------|
| **Development** | [Local Development](docs/development/local-development.md) • [Docker Compose](docs/development/docker-compose.md) • [Environment Variables](docs/development/environment-variables.md) |
| **Architecture** | [System Overview](docs/architecture/system-overview.md) • [Clean Architecture](docs/architecture/clean-architecture.md) • [AI Pipeline](docs/architecture/ai-pipeline.md) • [RAG Architecture](docs/architecture/rag-architecture.md) |
| **API Reference** | [Endpoints](docs/api/endpoints.md) • [Assistant Streaming](docs/api/assistant-streaming.md) • [PayPal Donations](docs/api/paypal-donations.md) • [Health Checks](docs/api/health-checks.md) |
| **Frontend** | [Pages & Routing](docs/frontend/pages-routing.md) • [Assistant UI](docs/frontend/assistant-ui.md) • [Resume Page](docs/frontend/resume-page.md) • [Status Pages](docs/frontend/status-pages.md) • [Accessibility](docs/frontend/accessibility.md) |
| **Backend** | [Knowledge & Retrieval](docs/backend/knowledge-retrieval.md) • [Resilience Patterns](docs/backend/resilience.md) • [Dependency Security](docs/backend/dependency-security.md) • [Testing](docs/backend/testing.md) |
| **Deployment** | [Production Runbook](docs/deployment/production-runbook.md) • [Production Env Vars](docs/deployment/production-env-vars.md) • [Data Protection](docs/deployment/data-protection.md) • [AWS Lambda Deploy](docs/deployment/aws-lambda-deploy.md) |
| **Diagrams** | [System Diagram](docs/diagrams/system-diagram.md) • [AI Flow Diagram](docs/diagrams/ai-flow-diagram.md) |
| **Operations** | [Troubleshooting](docs/operations/troubleshooting.md) • [Monitoring](docs/operations/monitoring.md) |

---

## Repository Structure

```
portfolio/
├── .github/workflows/       # CI/CD pipelines
├── backend/                 # .NET 10 API (KaiAssistant.sln)
│   ├── KaiAssistant.API/
│   ├── KaiAssistant.Application/
│   ├── KaiAssistant.Domain/
│   ├── KaiAssistant.Infrastructure/
│   ├── KaiAssistant.Tests/
│   ├── KaiAssistant.RagEvaluation/
│   ├── Dockerfile
│   └── infra/               # AWS SAM template
├── frontend/                # Next.js 16 App Router
│   ├── src/app/             # Routes, API proxies, global styles
│   ├── src/components/      # UI components
│   ├── src/lib/             # Server/client utilities
│   ├── public/              # Static assets (images, resume.pdf)
│   └── Dockerfile
├── deploy/                  # PowerShell deployment scripts
├── docs/                    # Unified documentation
├── docker-compose.yml       # Base compose (dev + prod override)
├── docker-compose.prod.yml  # Production overrides
├── .env.example             # Environment template
├── DATA_PROTECTION_STRATEGY.md
└── README.md                # This file
```

