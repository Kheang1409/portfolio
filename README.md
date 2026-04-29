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

### Frontend Only (Quick Dev)

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:3000` and start building!

**Note**: Features requiring backend (assistant chat, contact form) will use fallback to `http://localhost:5000` or the value of `NEXT_PUBLIC_BACKEND_API_URL`.

### Full Stack with Docker Compose

From repository root:

```bash
docker-compose up -d --build
```

Services:
- Frontend: `http://localhost:3000`
- Backend: `http://localhost:5000` (proxied) and `http://localhost:8080` (direct)
- MongoDB and Redis for backend dependencies

## Development Workflow

### Hot Reload

The dev server watches for changes automatically:

```bash
npm run dev
```

Changes to components, pages, and styles appear instantly.

### Type Checking

Ensure TypeScript has no errors:

```bash
npm run type-check
```

### Linting

Check code quality:

```bash
npm run lint

# Auto-fix fixable issues
npm run lint -- --fix
```

### Build for Production

```bash
npm run build
```

This creates an optimized standalone build in `.next/`.

## Building and Deploying

### Docker Build

The included `Dockerfile` creates a minimal Node.js runtime image:

```bash
# Build locally
docker build -t portfolio-frontend .

# Run
docker run -p 3000:3000 portfolio-frontend
```

### Vercel / Netlify

This Next.js app is optimized for Vercel:

1. Push to GitHub
2. Connect repository to Vercel
3. Environment variables are automatically configured from `.env.local` or secrets
4. Deploy on push

## Troubleshooting

### Port 3000 Already In Use

```bash
# Use a different port
npm run dev -- -p 3001

# Or kill the process
lsof -ti:3000 | xargs kill -9  # macOS/Linux
netstat -ano | findstr :3000   # Windows
```

### Backend API Not Reachable

**Issue**: Assistant chat shows "Connection failed"

**Solution**: Verify backend is running and accessible

```bash
# Check backend health
curl http://localhost:8080/api/health

# Check NEXT_PUBLIC_BACKEND_API_URL env var
echo $NEXT_PUBLIC_BACKEND_API_URL
```

If needed, set it in `.env.local`:

```bash
NEXT_PUBLIC_BACKEND_API_URL=http://localhost:8080
```

### GitHub Projects Not Loading

**Issue**: Projects section is empty

**Possible causes**:
1. `GITHUB_TOKEN` not set (optional but improves rate limits)
2. GitHub API rate limited (public requests limited to 60/hour)

**Solution**:
```bash
# Set your GitHub token in .env.local
GITHUB_TOKEN=your-github-token-here

# Restart dev server
npm run dev
```

### Build Fails with Memory Issues

**Solution**: Increase Node.js memory

```bash
# macOS/Linux
NODE_OPTIONS=--max_old_space_size=4096 npm run build

# Windows (PowerShell)
$env:NODE_OPTIONS="--max_old_space_size=4096"; npm run build
```

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
