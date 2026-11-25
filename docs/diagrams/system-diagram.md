# System Diagram

## Mermaid Diagram

```mermaid
flowchart TB
    subgraph Client["Client Layer"]
        Browser[("Browser\n(User)")]
    end

    subgraph Frontend["Frontend (Next.js 16)"]
        FE_Server["Next.js Server\n(Node.js 24)"]
        FE_Client["React Client\n(Hydrated)"]
        FE_API["API Proxies\n(/api/*)"]
        FE_Static["Static Assets\n(Images, PDF)"]
    end

    subgraph Backend["Backend (.NET 10)"]
        BE_API["ASP.NET Core API\n(Port 8080)"]
        BE_Assistant["Assistant Service\n(Streaming)"]
        BE_Resume["Resume Service"]
        BE_Donations["Donation Service\n(PayPal)"]
        BE_Contacts["Contact Service"]
        BE_Visits["Visit Service"]
        BE_Health["Health Checks"]
        BE_Knowledge["Knowledge Bootstrap\n(Startup)"]
    end

    subgraph Data["Data Layer"]
        MongoDB[("MongoDB Atlas\n(Resumes, Donations,\nConversations, Knowledge)")]
        Redis[("Redis 7.0\n(Rate Limit, Cache,\nCircuit Breaker)")]
    end

    subgraph External["External Services"]
        Gemini["Google Gemini\n(Embeddings + Generation)"]
        PayPal["PayPal Checkout\n(Orders, Capture, Webhooks)"]
        SMTP["SMTP Email\n(Receipts, Contact)"]
        GitHub["GitHub API\n(Project Feed)"]
    end

    %% Connections
    Browser -->|HTTPS| FE_Server
    FE_Server -->|SSR| FE_Client
    FE_Client -->|Same-Origin| FE_API
    FE_API -->|Internal Docker| BE_API

    BE_API -->|DI| BE_Assistant
    BE_API -->|DI| BE_Resume
    BE_API -->|DI| BE_Donations
    BE_API -->|DI| BE_Contacts
    BE_API -->|DI| BE_Visits
    BE_API -->|DI| BE_Health

    BE_Assistant -->|Retrieval| BE_Knowledge
    BE_Knowledge -->|Keyword Chunks| MongoDB
    BE_Knowledge -.->|Embeddings (optional)| MongoDB

    BE_Assistant -->|Generate| Gemini
    BE_Resume -->|CRUD| MongoDB
    BE_Donations -->|CRUD| MongoDB
    BE_Donations -->|Orders/Capture| PayPal
    BE_Donations -.->|Webhooks| PayPal
    BE_Contacts -->|CRUD| MongoDB
    BE_Visits -->|CRUD| MongoDB

    BE_Assistant -->|Rate Limit| Redis
    BE_Donations -->|Idempotency| Redis
    BE_Health -->|Ping| MongoDB
    BE_Health -->|Ping| Redis

    BE_Contacts -->|Email| SMTP
    BE_Donations -->|Receipts| SMTP

    FE_API -.->|GitHub Feed| GitHub

    %% Styling
    classDef client fill:#f5f5f5,stroke:#333,stroke-width:2px
    classDef frontend fill:#e3f2fd,stroke:#1976d2,stroke-width:2px
    classDef backend fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef data fill:#e8f5e9,stroke:#388e3c,stroke-width:2px
    classDef external fill:#fce4ec,stroke:#c2185b,stroke-width:2px

    class Browser client
    class FE_Server,FE_Client,FE_API,FE_Static frontend
    class BE_API,BE_Assistant,BE_Resume,BE_Donations,BE_Contacts,BE_Visits,BE_Health,BE_Knowledge backend
    class MongoDB,Redis data
    class Gemini,PayPal,SMTP,GitHub external
```

---

## Component Details

### Frontend (Next.js 16)
| Component | Technology | Purpose |
|-----------|------------|---------|
| Server | Node.js 24, Next.js 16 (App Router) | SSR, API proxies, static generation |
| Client | React 18, TypeScript | Interactive components (Assistant, Donations, Contact) |
| Styling | Tailwind CSS 3.3 | Responsive monochrome design |
| Animations | Framer Motion 10 | Page transitions, micro-interactions |
| Icons | FontAwesome 7, Lucide React | Consistent iconography |

### Backend (.NET 10)
| Component | Technology | Purpose |
|-----------|------------|---------|
| API | ASP.NET Core 10 | REST endpoints, OpenAPI, health checks |
| Architecture | Clean Architecture (Domain, Application, Infrastructure) | Separation of concerns |
| Mediator | MediatR 12 | CQRS-lite, decoupled handlers |
| Validation | FluentValidation 11 | Request validation |
| Database | MongoDB.Driver 3 | Document persistence |
| Cache | StackExchange.Redis 2.8 | Distributed cache, rate limiting |
| Resilience | Polly 8 | Retry, circuit breaker, timeout, fallback |
| Observability | OpenTelemetry, Serilog | Metrics, logs, traces |
| AI | Google Gemini SDK | Embeddings, streaming generation |
| Payments | PayPal .NET SDK | Checkout, capture, webhooks |

### Data Layer
| Component | Technology | Purpose |
|-----------|------------|---------|
| MongoDB | Atlas (M10+) | Primary persistence (resumes, donations, conversations, knowledge) |
| Redis | 7.0 (Docker) | Rate limiting, circuit breaker state, session cache |

### External Services
| Service | Purpose | Integration |
|---------|---------|-------------|
| Google Gemini | Embeddings (768-dim), streaming generation | REST API, API key auth |
| PayPal | Donation checkout, capture, webhooks | REST API, webhook verification |
| SMTP | Contact form emails, donation receipts | MailKit, outbox pattern |
| GitHub | Public repository feed | REST API, optional token |

---

## Network Topology

```
┌─────────────────────────────────────────────────────────────────┐
                        Docker Network (bridge)                    
└─────────────────────────────────────────────────────────────────┘
  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐        
  │  Frontend    │    │  Backend     │    │   Redis      │        
  │  :3000       │◄───│  :8080       │◄───│  :6379/6380  │        
  └──────────────┘    └──────┬───────┘    └──────────────┘        
                             │                                       
                    ┌────────┴────────┐                             
                    │   MongoDB       │                             
                    │   (External)    │                             
                    │   Atlas SRV     │                             
                    └─────────────────┘                             

Ports Exposed to Host:
  Frontend: 3000 → 3000
  Backend:  5000 → 8080
  Redis:    6379 (dev) / 6380 TLS (prod)
```

---

## Data Flow Summary

### 1. Page Load (Home / Resume)
```
Browser → Frontend (SSR) → Backend /api/resumes/latest → MongoDB → JSON → HTML → Browser
```

### 2. Assistant Query
```
Browser → Frontend /api/assistant → Backend /api/assistant 
  → Knowledge Retrieval (Keyword + Semantic) → MongoDB
  → Context Building → Gemini Streaming → NDJSON → Frontend → Browser
```

### 3. Donation Flow
```
Browser → Frontend PayPal SDK → Backend /api/donations/orders → PayPal Create Order
  → PayPal Checkout (Browser) → Backend /api/donations/capture → PayPal Capture
  → MongoDB (Donation) → Webhook (Async) → Email Receipt (SMTP)
```

---

## Security Boundaries

| Boundary | Protection |
|----------|------------|
| Browser ↔ Frontend | HTTPS, CSP, Same-Origin |
| Frontend ↔ Backend | Internal Docker network, CORS (`ALLOWED_ORIGINS`) |
| Backend ↔ MongoDB | Atlas TLS, connection string auth, IP whitelist |
| Backend ↔ Redis | TLS (prod), password (optional) |
| Backend ↔ Gemini | HTTPS, API key in header |
| Backend ↔ PayPal | Webhook signature verification, client secret server-only |
| Secrets | `.env` (dev), GitHub Actions Secrets / AWS Parameter Store (prod) |

---

## Deployment Variants

### Development
- `docker-compose.yml` + `docker-compose.override.yml`
- Hot reload via bind mounts
- Non-TLS Redis, Development environment

### Production (Docker Compose)
- `docker-compose.yml` + `docker-compose.prod.yml`
- TLS Redis, resource limits, named volumes
- External `redis-certs` volume

### Production (AWS Lambda)
- SAM template (`backend/infra/template.yaml`)
- API Gateway + Lambda (.NET 10)
- MongoDB Atlas, ElastiCache Redis
- Secrets via AWS Secrets Manager

---

## Related Documents

- [AI Flow Diagram](ai-flow-diagram.md) — AI pipeline visualization
- [System Overview](../architecture/system-overview.md) — Architecture details
- [Docker Compose](../development/docker-compose.md) — Deployment configuration
- [Production Runbook](../deployment/production-runbook.md) — Operational procedures