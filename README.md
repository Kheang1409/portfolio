# Portfolio Frontend

Next.js 16 portfolio site with a live assistant widget and GitHub-backed project cards.

## Stack

- Next.js 16 App Router
- React 18 + TypeScript
- Tailwind CSS
- Framer Motion
- React Markdown with GitHub-flavored markdown support

## What The Frontend Actually Does

- Renders the home page sections in `src/app/page.tsx`: Hero, About, Skills, Experience, Projects, Education, Contact.
- Renders a dedicated resume page at `/resume`.
- Shows a floating assistant launcher from the root layout.
- Tracks visits with a best-effort POST to the backend.
- Loads GitHub project data from a Next.js server route.

## API Integration

### Assistant Chat

The browser talks to the Next.js proxy route at `/api/assistant`.

That route forwards requests to the backend assistant endpoint at `POST /api/assistant` and keeps the streaming NDJSON response intact.

The UI attaches a stable browser session id in metadata so the backend can persist conversation context across messages and refreshes.

Example request body sent through the proxy:

```json
{
	"message": "What is my name?",
	"history": [
		{
			"role": "user",
			"content": "My name is Alice"
		}
	],
	"context": {
		"systemPersona": "Hang Kheang Taing portfolio assistant",
		"metadata": {
			"sessionId": "browser-session-123",
			"uiSurface": "portfolio-assistant",
			"locale": "en-US"
		}
	}
}
```

### Contact Form

The contact form posts directly to the backend at `POST /api/contacts`.

### Visitor Tracking

The visitor tracker posts directly to the backend at `POST /api/visits`.

### GitHub Projects

The projects section calls the Next.js route at `GET /api/github/projects`, which fetches repositories from GitHub and filters them for the portfolio display.

## Environment Variables

### Public / Client Visible

- `NEXT_PUBLIC_BACKEND_API_URL` - backend base URL. Defaults to `http://localhost:5000`.
- `NEXT_PUBLIC_SITE_URL` - used for metadata and canonical URLs.

### Server Side

- `GITHUB_TOKEN` - optional GitHub API token.
- `GITHUB_USER_AGENT` - optional custom user agent for GitHub requests.
- `GOOGLE_SITE_VERIFICATION` - optional Google Search Console verification token.
- `NEXT_PUBLIC_DEBUG_AI` - optional debug flag for assistant request logging.

## Scripts

```bash
npm run dev
npm run build
npm run start
npm run lint
npm run type-check
```

## Run Locally

### Frontend Only

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:3000`.

If you are not using Docker Compose, make sure `NEXT_PUBLIC_BACKEND_API_URL` points to a running backend instance.

### Full Stack

From the repository root:

```bash
docker-compose up -d --build
```

That gives you the frontend at `http://localhost:3000` and the backend at `http://localhost:5000`.

## Key Files

- `src/app/layout.tsx` - global layout, metadata, navigation, footer, assistant launcher.
- `src/app/page.tsx` - landing page composition.
- `src/app/resume/page.tsx` - resume page.
- `src/app/api/assistant/route.ts` - assistant proxy.
- `src/app/api/github/projects/route.ts` - GitHub project feed.
- `src/lib/assistants.ts` - assistant streaming client.
- `src/lib/session.ts` - persistent browser session id utility.
- `src/lib/contacts.ts` - contact form client.
- `src/lib/visitor-tracking.ts` - visitor analytics client.

## Notes

- The app uses the shared backend URL fallback of `http://localhost:5000`.
- The backend route names in the docs should match the proxy route `POST /api/assistant` rather than older versioned assistant paths.
