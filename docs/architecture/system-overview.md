# System Overview

## High-Level Architecture

```
┌────────────────────────────────────────────────────────────────────────────┐
│                            KAI ASSISTANT PORTFOLIO                         │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  ┌──────────────┐       ┌──────────────┐       ┌──────────────┐            │
│  │   Browser    │────▶▶│   Frontend   │────▶▶│   Backend    │            │
│  │  (Client)    │       │  (Next.js 16)│       │  (.NET 10)   │            │
│  └──────────────┘       └──────┬───────┘       └──────┬───────┘            │
│                                │                      │                    │
│                       ┌────────┴────────┐      ┌──────┴───────┐            │
│                       │  Same-Origin    │      │   MongoDB    │            │
│                       │  API Proxies    │      │   (Atlas)    │            │
│                       └─────────────────┘      └──────┬───────┘            │
│                                                       │                    │
│                                              ┌────────┴────────┐           │
│                                              │     Redis       │           │
│                                              │  (Rate Limit,   │           │
│                                              │   Cache, State) │           │
│                                              └────────┬────────┘           │
│                                                       │                    │
│                                              ┌────────┴────────┐           │
│                                              │   Google Gemini │           │
│                                              │  (Embeddings +  │           │
│                                              │   Generation)   │           │
│                                              └────────┬────────┘           │
│                                                       │                    │
│                                              ┌────────┴────────┐           │
│                                              │     PayPal      │           │
│                                              │  (Donations)    │           │
│                                              └─────────────────┘           │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Responsibilities

### Frontend (Next.js 16, App Router)

| Layer                      | Responsibility                                                                            |
| -------------------------- | ----------------------------------------------------------------------------------------- |
| `src/app/`                 | Routes, metadata, layout, same-origin API proxies (`/api/*`)                              |
| `src/components/sections/` | Home page sections (Profile, Skills, Experience, Projects, Assistant, Donations, Contact) |
| `src/components/hud/`      | Reusable section shell (header, divider, container)                                       |
| `src/lib/server/`          | Backend URL resolution, request proxy, cookie handling                                    |
| `src/lib/`                 | Assistant state, contact forms, resume parsing, SEO, analytics                            |
| `public/`                  | Static assets (images, `resume.pdf`, `robots.txt`)                                        |

**Output**: Standalone Next.js server (`node server.js`) for Docker deployment

### Backend (.NET 10, Clean Architecture)

| Project                       | Responsibility                                                   |
| ----------------------------- | ---------------------------------------------------------------- |
| `KaiAssistant.API`            | Controllers, middleware, DI registration, OpenAPI, health checks |
| `KaiAssistant.Application`    | Use cases, DTOs, interfaces, validators, MediatR handlers        |
| `KaiAssistant.Domain`         | Entities, value objects, domain events, specifications           |
| `KaiAssistant.Infrastructure` | MongoDB, Redis, Gemini, PayPal, SMTP implementations             |
| `KaiAssistant.Tests`          | Unit & integration tests (xUnit, Moq, FluentAssertions)          |
| `KaiAssistant.RagEvaluation`  | RAG evaluation harness (separate project)                        |

**Key Patterns**:

- **Clean Architecture**: Dependencies point inward (Domain ← Application ← Infrastructure ← API)
- **MediatR**: Commands/Queries for use cases, decoupled from controllers
- **Result Pattern**: `Result<T>` for explicit success/failure handling
- **Options Pattern**: Strongly-typed configuration sections

---

## Data Flow

### 1. Resume Display (Home & Resume Page)

```
Browser → GET / → Frontend (server) → GET /api/resumes/latest → Backend → MongoDB
                                                                    ↓
Browser ← HTML (streamed) ← Frontend ← JSON ← Backend ← Resume Document
```

### 2. Assistant Query

```
Browser → POST /api/assistant → Frontend Proxy → POST /api/assistant → Backend
                                                              ↓
                                              KnowledgeBootstrapService (startup)
                                                              ↓
                                              Keyword chunks + Gemini embeddings → MongoDB
                                                              ↓
                                              Retrieval: keyword → strong evidence
                                                            semantic → weak evidence
                                                              ↓
                                              Context bounded → Gemini Stream (NDJSON)
                                                              ↓
Browser ← Streamed NDJSON ← Frontend Proxy ← Streamed NDJSON ← Backend
```

### 3. Donation Flow

```
Browser → POST /api/donations/orders → Frontend Proxy → Backend → PayPal Create Order
                                                                    ↓
Browser ← Order ID ← Frontend ← Order ID ← Backend ← PayPal Response
      ↓
Browser → PayPal Checkout (client SDK) → PayPal → Webhook → Backend
                                                            ↓
                                              Capture & Verify → MongoDB
                                                            ↓
                                              Receipt Email (SMTP outbox)
```

---

## Technology Stack

| Layer              | Technology                   | Version                                    |
| ------------------ | ---------------------------- | ------------------------------------------ |
| Frontend Framework | Next.js                      | 16.2.1 (App Router)                        |
| Frontend Language  | TypeScript                   | 5.2+                                       |
| UI Library         | React                        | 18.2                                       |
| Styling            | Tailwind CSS                 | 3.3.5                                      |
| Animation          | Framer Motion                | 10.16.4                                    |
| Icons              | FontAwesome, Lucide          | 7.3, 0.292                                 |
| Backend Framework  | ASP.NET Core                 | 10.0                                       |
| Backend Language   | C#                           | 13 (.NET 10)                               |
| Database           | MongoDB                      | Atlas (Driver 3.x)                         |
| Cache/Rate Limit   | Redis                        | 7.0 (StackExchange.Redis)                  |
| AI/Embeddings      | Google Gemini                | `gemini-embedding-001`, `gemini-2.5-flash` |
| Payments           | PayPal Checkout              | .NET SDK                                   |
| Email              | SMTP (Gmail/Transactional)   | MailKit                                    |
| Logging            | Serilog                      | 4.x                                        |
| Observability      | OpenTelemetry                | 1.9+                                       |
| Testing (Backend)  | xUnit, Moq, FluentAssertions | Latest                                     |
| Testing (Frontend) | Playwright                   | 1.63                                       |
| Containerization   | Docker                       | Multi-stage builds                         |
| Orchestration      | Docker Compose               | v2                                         |

---

## Deployment Topology

### Development

- Docker Compose (base + override)
- Hot reload via bind mounts
- Non-TLS Redis, Development env

### Production (Docker Compose)

- Base + `docker-compose.prod.yml`
- TLS Redis (`rediss://`), resource limits
- Named volumes for Redis persistence
- External `redis-certs` volume for TLS

### Production (AWS Lambda)

- SAM template (`backend/infra/template.yaml`)
- API Gateway + Lambda (.NET 10)
- MongoDB Atlas, ElastiCache Redis
- Secrets via Parameter Store / Secrets Manager
- Deployed via GitHub Actions (`.github/workflows/deploy-lambda.yml`)

---

## Security Boundaries

| Boundary           | Protection                                                        |
| ------------------ | ----------------------------------------------------------------- |
| Browser ↔ Frontend | Same-origin, HTTPS, CSP headers                                   |
| Frontend ↔ Backend | Internal Docker network, CORS (`ALLOWED_ORIGINS`)                 |
| Backend ↔ MongoDB  | Atlas TLS, connection string auth, IP whitelist                   |
| Backend ↔ Redis    | TLS (prod), password (if configured)                              |
| Backend ↔ Gemini   | API key in header, HTTPS                                          |
| Backend ↔ PayPal   | Webhook signature verification, client secret server-only         |
| Secrets            | `.env` (dev), GitHub Actions Secrets / AWS Parameter Store (prod) |

---

## Observability

| Signal  | Implementation                                                    |
| ------- | ----------------------------------------------------------------- |
| Logs    | Serilog → Console (Docker) → Aggregation (Loki/Datadog)           |
| Metrics | OpenTelemetry → Prometheus / OTLP                                 |
| Traces  | OpenTelemetry → Jaeger / Tempo / Datadog                          |
| Health  | `/health/live` (liveness), `/health/ready` (readiness + deps)     |
| Alerts  | GitHub Actions security gates, Atlas alerts, custom metric alerts |

---

## Scaling Considerations

| Component | Scaling Strategy                                      |
| --------- | ----------------------------------------------------- |
| Frontend  | Horizontal (stateless), CDN for static assets         |
| Backend   | Horizontal (stateless), connection pooling            |
| MongoDB   | Atlas auto-scaling, read replicas                     |
| Redis     | Single instance (rate limit tolerant), Cluster for HA |
| Gemini    | API quota limits, fallback models configured          |

---

## Related Documents

- [Clean Architecture](clean-architecture.md) — Layer responsibilities, dependency rules
- [AI Pipeline](ai-pipeline.md) — Model orchestration, Gemini gateway
- [RAG Architecture](rag-architecture.md) — Retrieval-augmented generation details
- [Embedding Strategy](embedding-strategy.md) — Vector embeddings, Gemini embedding-001
- [Resilience Patterns](resilience.md) — Circuit breakers, retries, fallbacks
- [System Diagram](../diagrams/system-diagram.md) — Visual architecture
- [AI Flow Diagram](../diagrams/ai-flow-diagram.md) — Visual AI pipeline
