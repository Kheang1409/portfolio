# Portfolio Frontend

Production-oriented Next.js portfolio UI with integrated AI assistant.

## Stack

- Next.js 16 (App Router)
- React 18 + TypeScript
- Tailwind CSS
- Framer Motion
- React Markdown + syntax highlighting (assistant message rendering)

## Current Features

- Multi-section portfolio landing page:
  - Hero, About, Skills, Experience, Projects, Education, Contact
- Floating AI assistant chat widget:
  - Streaming-first conversation flow
  - Automatic fallback to buffered assistant responses
  - Local chat history persistence
- GitHub projects integration via server route
- SEO metadata, robots, and sitemap support
- Dark/light mode support via theme provider

## API Integration Design

### Client -> Backend (direct)

The frontend directly calls backend APIs using `NEXT_PUBLIC_BACKEND_API_URL` (fallback: `http://localhost:5000`):

- `POST /api/assistants/ask`
- `POST /api/assistants/stream`
- `POST /api/contacts`

### Frontend Server Routes (BFF style)

Implemented Next.js API routes:

- `GET /api/github/projects`
  - Fetches and filters repositories from GitHub API.

## Environment Variables

### Public / Client-Visible

- `NEXT_PUBLIC_BACKEND_API_URL`
- `NEXT_PUBLIC_SITE_URL`

### Server-Side (Next runtime)

- `GITHUB_TOKEN` (optional, increases GitHub API reliability)
- `GITHUB_USER_AGENT` (optional)
- `GOOGLE_SITE_VERIFICATION` (optional)

## Scripts

```bash
npm run dev
npm run build
npm run start
npm run lint
npm run type-check
```

## Run Locally

### Frontend only

From `frontend`:

```bash
npm install
npm run dev
```

Open `http://localhost:3000`.

### Full stack with Docker Compose

From repository root:

```bash
docker-compose up -d --build
```

Default local ports:

- Frontend: `http://localhost:3000`
- Backend: `http://localhost:5000`

## Folder Map (Key Areas)

- `src/app/layout.tsx`: global layout + metadata + assistant
- `src/app/page.tsx`: home page section composition
- `src/app/api/github/projects/route.ts`: GitHub project data endpoint
- `src/components/sections/Assistant.tsx`: assistant chat UI
- `src/lib/assistants.ts`: ask/stream integration and fallback logic
- `src/lib/contacts.ts`: contact API posting

## Notes

- `next.config.js` is configured for standalone output and optimized image formats.
- Production Docker image runs the standalone Next server (`server.js`) as non-root user.
