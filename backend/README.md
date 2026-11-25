# Backend (KaiAssistant API)

The .NET 10 backend for Kai's portfolio. It serves resume data, streams assistant responses, accepts contact and visitor requests, and processes PayPal donations.

**All technical documentation has moved to the unified [`docs/`](../docs/) folder at the repository root.**

---

## Quick Links

| Topic | Document |
|-------|----------|
| **API Surface** | [API Endpoints](../docs/api/endpoints.md) |
| **Assistant Streaming** | [Assistant Streaming Protocol](../docs/api/assistant-streaming.md) |
| **PayPal Donations** | [PayPal Integration](../docs/api/paypal-donations.md) |
| **Health Checks** | [Health Check Endpoints](../docs/api/health-checks.md) |
| **Architecture** | [System Overview](../docs/architecture/system-overview.md) • [Clean Architecture](../docs/architecture/clean-architecture.md) |
| **AI Pipeline** | [AI Pipeline](../docs/architecture/ai-pipeline.md) • [RAG Architecture](../docs/architecture/rag-architecture.md) • [Embedding Strategy](../docs/architecture/embedding-strategy.md) |
| **Knowledge & Retrieval** | [Knowledge Retrieval](../docs/backend/knowledge-retrieval.md) • [Corpus Management](../docs/architecture/corpus-management.md) |
| **Resilience** | [Resilience Patterns](../docs/backend/resilience.md) • [Scalability](../docs/architecture/scalability.md) |
| **Security** | [Dependency Security](../docs/backend/dependency-security.md) |
| **Testing** | [Testing Strategy](../docs/backend/testing.md) |
| **Deployment** | [Production Runbook](../docs/deployment/production-runbook.md) • [Production Env Vars](../docs/deployment/production-env-vars.md) • [Data Protection](../docs/deployment/data-protection.md) • [AWS Lambda Deploy](../docs/deployment/aws-lambda-deploy.md) |
| **Diagrams** | [System Diagram](../docs/diagrams/system-diagram.md) • [AI Flow Diagram](../docs/diagrams/ai-flow-diagram.md) |
| **Operations** | [Troubleshooting](../docs/operations/troubleshooting.md) • [Monitoring](../docs/operations/monitoring.md) |

---

## Project Structure

```
backend/
├── KaiAssistant.API/              # Web API entry point, controllers, middleware
├── KaiAssistant.Application/      # Use cases, DTOs, interfaces, MediatR handlers
├── KaiAssistant.Domain/           # Entities, value objects, domain events, specifications
├── KaiAssistant.Infrastructure/   # MongoDB, Redis, Gemini, PayPal, SMTP implementations
├── KaiAssistant.Tests/            # Unit & integration tests (xUnit, Moq, FluentAssertions)
├── KaiAssistant.RagEvaluation/    # RAG evaluation harness
├── Dockerfile                     # Multi-stage build (SDK → ASP.NET runtime)
├── infra/template.yaml            # AWS SAM template for Lambda deployment
└── README.md                      # This file
```

---

## Local Development

```powershell
cd backend
dotnet restore KaiAssistant.sln
dotnet run --project KaiAssistant.API
```

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger` (Development only)
- Health: `http://localhost:5000/health/ready`

**Required environment variables** (set in shell or `.env`):
```powershell
$env:MONGODB_CONNECTIONSTRING="mongodb://localhost:27017"
$env:MONGODB_DATABASE="KaiAssistant"
$env:GEMINI_API_KEY="your-key"
$env:REDIS__CONNECTIONSTRING="redis://localhost:6379"
$env:ASPNETCORE_ENVIRONMENT="Development"
```

---

## Docker

```powershell
# From repository root
docker compose up -d --build backend
```

Production override: `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build backend`

---

## Testing

```powershell
dotnet build KaiAssistant.sln
dotnet test KaiAssistant.Tests/KaiAssistant.Tests.csproj
dotnet test KaiAssistant.RagEvaluation/KaiAssistant.RagEvaluation.csproj
```

---

## Key Concepts

### Clean Architecture
Dependencies point inward: **Domain** ← **Application** ← **Infrastructure** ← **API**

### MediatR (CQRS-lite)
Commands/Queries for use cases, decoupled from controllers.

### Result Pattern
`Result<T>` for explicit success/failure handling — no exceptions for control flow.

### Knowledge Bootstrap
`KnowledgeBootstrapHostedService` runs at startup: hashes latest resume, chunks content, writes keyword index to MongoDB, optionally generates Gemini embeddings. Idempotent via content hash.

### Resilience (Polly)
Retry, circuit breaker, timeout, fallback, and bulkhead policies for all external calls (Gemini, PayPal, MongoDB, Redis, SMTP).

---

## Related Root-Level Docs

- [Root README](../README.md) — Project overview, quick start, full docker-compose
- [Frontend README](../frontend/README.md) — Next.js frontend documentation
- [Environment Template](../.env.example) — All configurable variables
- [Data Protection Strategy](../DATA_PROTECTION_STRATEGY.md) — MongoDB backup, Redis persistence, DR procedures
- [Deployment Scripts](../deploy/) — `deploy.ps1`, `verify-deployment.ps1`, `rollback.ps1`, `fetch-secrets.ps1`