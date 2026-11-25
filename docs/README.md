# Documentation Index

Welcome to the KaiAssistant Portfolio documentation. This is a mono-repo containing both the **Next.js 16 frontend** and **.NET 10 backend**.

## Quick Navigation

| Area | Document |
|------|----------|
| **Getting Started** | [Local Development](development/local-development.md) • [Docker Compose](development/docker-compose.md) • [Environment Variables](development/environment-variables.md) |
| **Architecture** | [System Overview](architecture/system-overview.md) • [Clean Architecture](architecture/clean-architecture.md) • [AI Pipeline](architecture/ai-pipeline.md) • [RAG Architecture](architecture/rag-architecture.md) |
| **API Reference** | [Endpoints](api/endpoints.md) • [Assistant Streaming](api/assistant-streaming.md) • [PayPal Donations](api/paypal-donations.md) • [Health Checks](api/health-checks.md) |
| **Frontend** | [Pages & Routing](frontend/pages-routing.md) • [Assistant UI](frontend/assistant-ui.md) • [Resume Page](frontend/resume-page.md) • [Status Pages](frontend/status-pages.md) • [Accessibility](frontend/accessibility.md) |
| **Backend** | [Knowledge & Retrieval](backend/knowledge-retrieval.md) • [Embedding Strategy](backend/embedding-strategy.md) • [Resilience Patterns](backend/resilience.md) • [Dependency Security](backend/dependency-security.md) • [Testing](backend/testing.md) |
| **Deployment** | [Production Runbook](deployment/production-runbook.md) • [Environment Variables](deployment/production-env-vars.md) • [Data Protection](deployment/data-protection.md) • [AWS Lambda Deploy](deployment/aws-lambda-deploy.md) |
| **Diagrams** | [System Diagram](diagrams/system-diagram.md) • [AI Flow Diagram](diagrams/ai-flow-diagram.md) |
| **Operations** | [Troubleshooting](operations/troubleshooting.md) • [Monitoring](operations/monitoring.md) |

---

## Repository Structure

```
portfolio/
├── .github/workflows/       # CI/CD pipelines (security, deploy)
├── backend/                 # .NET 10 API (KaiAssistant.sln)
│   ├── KaiAssistant.API/    # Web API entry point
│   ├── KaiAssistant.Application/   # Use cases, DTOs, interfaces
│   ├── KaiAssistant.Domain/        # Entities, value objects, domain logic
│   ├── KaiAssistant.Infrastructure/ # MongoDB, Redis, Gemini, PayPal implementations
│   ├── KaiAssistant.Tests/         # Unit & integration tests
│   ├── KaiAssistant.RagEvaluation/ # RAG evaluation harness
│   ├── Dockerfile
│   └── infra/               # AWS SAM template for Lambda deployment
├── frontend/                # Next.js 16 App Router
│   ├── src/app/             # Routes, API proxies, global styles
│   ├── src/components/      # UI components (sections, HUD)
│   ├── src/lib/             # Server utilities, client helpers
│   ├── public/              # Static assets (images, resume.pdf)
│   └── Dockerfile
├── deploy/                  # PowerShell deployment scripts
├── docs/                    # Unified documentation (this folder)
├── docker-compose.yml       # Base compose (dev + prod override)
├── docker-compose.prod.yml  # Production overrides
├── .env.example             # Environment template
└── README.md                # Root overview (this file)
```

---

## Key Principles

1. **Single source of truth** — All technical documentation lives in `docs/`. Root `README.md` and folder `README.md` files are thin entry points linking here.
2. **No duplication** — Each concept documented once. Cross-reference with links.
3. **Runnable examples** — Code snippets are copy-pasteable and tested in CI.
4. **Version-aware** — Docs reflect `.NET 10` and `Next.js 16` (App Router, standalone output).

---

## Contributing to Documentation

- Edit files in `docs/` — they are the source of truth
- Keep Markdown lint-clean (`markdownlint` rules)
- Update this index when adding new documents
- Diagrams use Mermaid (rendered natively on GitHub)

---

## Related Root-Level Files

- [Root README](../README.md) — Project overview, quick start, troubleshooting
- [Environment Template](../.env.example) — All configurable variables with descriptions
- [Deployment Scripts](../deploy/) — `deploy.ps1`, `verify-deployment.ps1`, `rollback.ps1`, `fetch-secrets.ps1`
- [Data Protection Strategy](../DATA_PROTECTION_STRATEGY.md) — MongoDB backup, Redis persistence, DR procedures