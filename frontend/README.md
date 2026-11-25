# Frontend (Next.js 16)

The Next.js frontend for Kai's portfolio. It uses a responsive monochrome editorial design, with runtime profile content supplied by the backend resume API.

**All technical documentation has moved to the unified [`docs/`](../docs/) folder at the repository root.**

---

## Quick Links

| Topic | Document |
|-------|----------|
| **Pages & Routing** | [Pages & Routing](../docs/frontend/pages-routing.md) |
| **Assistant UI** | [Assistant UI](../docs/frontend/assistant-ui.md) |
| **Resume Page** | [Resume Page](../docs/frontend/resume-page.md) |
| **Status Pages** | [Status Pages](../docs/frontend/status-pages.md) |
| **Accessibility** | [Accessibility](../docs/frontend/accessibility.md) |
| **Architecture** | [System Overview](../docs/architecture/system-overview.md) |
| **API Reference** | [API Endpoints](../docs/api/endpoints.md) • [Assistant Streaming](../docs/api/assistant-streaming.md) |
| **Deployment** | [Production Runbook](../docs/deployment/production-runbook.md) • [Production Env Vars](../docs/deployment/production-env-vars.md) |

---

## Project Structure

```
frontend/
├── src/app/                 # Routes, metadata, same-origin API proxies, global styles
│   ├── api/                 # Assistant, contacts, visits, donations, GitHub, health proxies
│   ├── resume/              # Data-driven resume page
│   ├── layout.tsx           # Root layout (providers, health check)
│   ├── page.tsx             # Home page
│   ├── not-found.tsx        # 404 page
│   ├── error.tsx            # Route error boundary
│   ├── global-error.tsx     # Root layout error boundary
│   └── maintenance/page.tsx # Maintenance page
├── src/components/
│   ├── hud/                 # Reusable section shell
│   └── sections/            # Home page sections (Profile, Skills, Experience, Projects, Assistant, Donations, Contact)
├── src/lib/
│   ├── server/              # Backend URL resolver, request proxy
│   └── ...                  # Assistant, contact, resume, session, SEO, tracking
├── public/images/           # Greeting and status illustrations (*.jfif)
├── public/resume.pdf        # Bundled resume PDF (regenerate when resume updates)
├── Dockerfile               # Multi-stage build (builder → runner)
├── next.config.js           # Standalone output, image optimization
└── README.md                # This file
```

---

## Local Development

```powershell
cd frontend
npm install
$env:BACKEND_API_URL="http://localhost:5000"
npm run dev
```

- App: `http://localhost:3000`
- Backend proxy: `http://localhost:3000/api/*` → `http://localhost:5000/api/*`

**Frontend-only environment variables** (`.env.local` or shell):
```bash
NEXT_PUBLIC_SITE_URL=http://localhost:3000
GITHUB_USER=your-github-username      # Optional override
GITHUB_TOKEN=ghp_xxx                  # Optional, server-side only
NEXT_PUBLIC_DEBUG_AI=true             # Optional client-side assistant debug logging
```

> **Important**: `BACKEND_API_URL` is **server-only**. Never use `NEXT_PUBLIC_` prefix for backend addresses or secrets.

---

## Quality Checks

```powershell
npm run lint
npm run type-check
npm test           # Playwright E2E (resume navigation, mobile, print)
npm run build      # Production build verification
```

---

## Docker

```powershell
# From repository root
docker compose up -d --build frontend
```

Production: `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build frontend`

---

## Key Concepts

### Same-Origin API Proxies
All browser API calls go through `/api/*` routes in `src/app/api/` — they forward to the backend at `BACKEND_API_URL`. This keeps backend URLs and secrets off the client.

### Assistant Streaming
- Frontend POSTs to `/api/assistant` → proxies to backend `/api/assistant`
- Backend streams NDJSON → frontend streams to browser via `ReadableStream`
- Conversation history persisted in browser `localStorage` (max 20 messages)

### Resume Page
- Server Component fetches latest resume at request time via `GET /api/resumes/latest`
- Print styles hide navigation, optimize for PDF
- Download button links to static `/resume.pdf` (regenerate separately)

### Status Pages
- `not-found.tsx` — 404 with illustration
- `error.tsx` — Route rendering errors, retry button
- `global-error.tsx` — Root layout errors, full page fallback
- `/maintenance` — Informational only, not a maintenance switch
- Root layout checks `/api/health/live` — shows 503 page with auto-retry if backend down

### Accessibility (WCAG 2.1 AA)
- Semantic landmarks, ordered headings
- Keyboard-operable navigation, controls, forms, assistant
- Visible focus, high-contrast text
- `prefers-reduced-motion` respected
- Screen-reader announcements for streaming content and errors

---

## Build Output (Standalone)

```javascript
// next.config.js
output: 'standalone'  // For Docker deployment
```

**Docker Output**:
```
frontend/
├── .next/
│   ├── standalone/     # node server.js + dependencies
│   │   ├── server.js
│   │   └── node_modules/
│   └── static/         # Static assets (hashed filenames)
└── public/             # Public assets (copied to standalone)
```

---

## Related Root-Level Docs

- [Root README](../README.md) — Project overview, quick start, full docker-compose
- [Backend README](../backend/README.md) — .NET API documentation
- [Environment Template](../.env.example) — All configurable variables
- [Data Protection Strategy](../DATA_PROTECTION_STRATEGY.md) — MongoDB backup, Redis persistence, DR procedures
- [Deployment Scripts](../deploy/) — `deploy.ps1`, `verify-deployment.ps1`, `rollback.ps1`, `fetch-secrets.ps1`