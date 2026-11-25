# Frontend Pages & Routing

## Route Structure (Next.js 16 App Router)

```
src/app/
├── layout.tsx              # Root layout (providers, global styles, health check)
├── page.tsx                # Home page (/)
├── resume/
│   └── page.tsx            # Resume page (/resume)
├── api/                    # Same-origin API proxies
│   ├── assistant/route.ts  # POST → backend /api/assistant
│   ├── contacts/route.ts   # POST → backend /api/contacts
│   ├── visits/route.ts     # POST → backend /api/visits
│   ├── donations/
│   │   ├── config/route.ts
│   │   ├── leaderboard/route.ts
│   │   ├── orders/route.ts
│   │   └── capture/route.ts
│   ├── github/projects/route.ts
│   └── health/
│       ├── live/route.ts
│       └── ready/route.ts
├── not-found.tsx           # 404 page
├── error.tsx               # Route error boundary
├── global-error.tsx        # Root layout error boundary
└── maintenance/
    └── page.tsx            # /maintenance page
```

---

## Home Page (`/`)

### Sections (in order)
1. **Greeting** — Illustrated hero with name, headline, CTA to resume
2. **Profile** — Summary, location, email, social links
3. **Skills** — Categorized tags (Language, Framework, Cloud, Tool, Database)
4. **Experience** — Timeline cards with company, role, dates, highlights
5. **Projects** — Case study cards with image, tech stack, links
6. **Education** — Degree cards with institution, field, honors
7. **GitHub Projects** — Live feed from `/api/github/projects`
8. **Assistant** — Portfolio assistant chat interface
9. **Donations** — PayPal checkout button
10. **Contact** — Form with name, email, subject, message
11. **Footer** — Links, copyright, resume data attribution

### Data Fetching
- **Server Components**: Profile, Skills, Experience, Projects, Education, GitHub — fetched at request time via `GET /api/resumes/latest`
- **Client Components**: Assistant, Donations, Contact — interactive, hydrated

### Dynamic Imports (Code Splitting)
```typescript
// Heavy sections loaded on demand
const Experience = dynamic(() => import('@/components/sections/Experience'), { ssr: false });
const Projects = dynamic(() => import('@/components/sections/Projects'), { ssr: false });
const Assistant = dynamic(() => import('@/components/sections/Assistant'), { ssr: false });
const Donations = dynamic(() => import('@/components/sections/Donations'), { ssr: false });
```

---

## Resume Page (`/resume`)

### Features
- **Section Navigation** — Sticky sidebar with scroll-spy
- **Compact Skill Tags** — Grouped by category, color-coded by level
- **Framed Cards** — Experience, Projects, Education, Certifications
- **Print Styles** — `@media print` hides nav, shows full content
- **PDF Download** — Links to `/resume.pdf` (static file in `public/`)

### Data Flow
```
GET /resume (server)
  → GET /api/resumes/latest (backend)
  → Render ResumePage with data
  → Client: PDF download button → /resume.pdf
```

### Print Optimization
```css
@media print {
  .no-print { display: none !important; }
  .resume-content { padding: 0; }
  .section { break-inside: avoid; }
  a { text-decoration: none; color: inherit; }
}
```

---

## API Proxies (`/api/*`)

All browser API calls go through **same-origin Next.js routes** — never direct to backend.

### Proxy Pattern
```typescript
// src/app/api/assistant/route.ts
import { createBackendClient } from '@/lib/server/backend-client';

export async function POST(request: Request) {
  const backend = createBackendClient();
  const body = await request.json();
  
  const response = await backend.post('/api/assistant', body);
  
  // Stream response back to browser
  return new Response(response.body, {
    headers: {
      'Content-Type': 'application/x-ndjson',
      'Cache-Control': 'no-cache',
      'Connection': 'keep-alive'
    }
  });
}
```

### Backend Client (`src/lib/server/backend-client.ts`)
- Resolves `BACKEND_API_URL` from env
- Adds correlation ID header
- Handles timeouts, retries
- Streams responses for `/api/assistant`

---

## Status Pages

### 404 (`not-found.tsx`)
- Branded illustration (`/images/404.jfif`)
- Friendly message, link to home
- No server details exposed

### Route Error (`error.tsx`)
- Catches rendering errors in route segment
- Logs to console (dev) / monitoring (prod)
- Retry button → `window.location.reload()`

### Global Error (`global-error.tsx`)
- Catches root layout errors
- Full page fallback
- Same retry behavior

### Maintenance (`/maintenance/page.tsx`)
- Informational page (not a kill switch)
- Branded illustration (`/images/maintenance.jfif`)
- Does not affect backend health or other routes

### 503 (Backend Unavailable)
- Detected via `/api/health/live` proxy in root layout
- Shows illustrated 503 page with auto-retry (30s)
- Manual retry button
- Individual features (assistant, donations) show own error states

---

## Navigation

### Mobile
- Hamburger menu → slide-over drawer
- Sections: Home, Resume, GitHub, Assistant, Donate, Contact
- Smooth scroll to sections on home page

### Desktop
- Sticky header with anchor links
- Resume page has separate navigation

---

## SEO & Metadata

```typescript
// layout.tsx
export const metadata: Metadata = {
  title: { default: 'Kai Assistant | Portfolio', template: '%s | Kai Assistant' },
  description: 'Senior Software Engineer — .NET, Azure, React, AI',
  openGraph: {
    type: 'website',
    locale: 'en_US',
    siteName: 'Kai Assistant Portfolio'
  },
  robots: { index: true, follow: true },
  verification: { google: process.env.GOOGLE_SITE_VERIFICATION }
};
```

### Dynamic Metadata (Resume Page)
```typescript
// resume/page.tsx
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const resume = await getResume();
  return {
    title: `${resume.profile.fullName} — Resume`,
    description: resume.profile.summary.slice(0, 160)
  };
}
```

---

## Environment Variables (Frontend)

| Variable | Required | Description |
|----------|----------|-------------|
| `NEXT_PUBLIC_SITE_URL` | Yes | Canonical URL for SEO |
| `BACKEND_API_URL` | Yes | Server-only backend URL |
| `GITHUB_USER` | No | GitHub username override |
| `GITHUB_TOKEN` | No | Server-side GitHub token |
| `GITHUB_USER_AGENT` | No | GitHub API User-Agent |
| `GOOGLE_SITE_VERIFICATION` | No | Search Console verification |
| `NEXT_PUBLIC_DEBUG_AI` | No | Client-side assistant debug logging |

---

## Build Output

```javascript
// next.config.js
module.exports = {
  output: 'standalone',  // For Docker deployment
  images: {
    remotePatterns: []   // Only local images used
  },
  experimental: {
    optimizePackageImports: ['lucide-react', '@fortawesome/react-fontawesome']
  }
};
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

## Related Documents

- [Assistant UI](assistant-ui.md) — Chat implementation
- [Resume Page](resume-page.md) — Resume page details
- [Status Pages](status-pages.md) — Error/maintenance pages
- [Accessibility](accessibility.md) — A11y implementation
- [API Proxies](../api/endpoints.md) — Backend endpoint reference