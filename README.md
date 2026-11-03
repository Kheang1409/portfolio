# Portfolio Frontend

The Next.js frontend for Kai's portfolio. It uses a responsive monochrome editorial design, with runtime profile content supplied by the backend resume API.

## Pages and experience

- The home page presents the profile, skills, experience, case studies, GitHub projects, education, assistant, donations, and contact form.
- `/resume` is a recruiter-focused, printable resume. It includes section-jump navigation, compact skill tags, framed experience and project cards, education and certification details, PDF download, and print controls.
- The resume page retrieves current profile data at request time. The downloadable `/resume.pdf` is a bundled static file and should be refreshed separately whenever the canonical resume changes.
- The assistant has suggested questions, streamed replies, retry and stop controls, conversation history stored in browser local storage, and copy-reply actions.
- Unknown routes and rendering failures use branded error pages. `/maintenance` is an informational page, not a site-wide maintenance switch.

The visual system uses light neutral surfaces, black typography, restrained borders, and responsive layouts. The provided greeting and status illustrations are under `public/images/`; they are illustrations, not professional profile photography.

## Branded status pages

- Unknown URLs render the root `not-found.tsx` page with the supplied 404 illustration.
- `error.tsx` handles route rendering failures; `global-error.tsx` covers failures in the root layout. Both log the error in the browser console without displaying server details and provide a retry action.
- `/maintenance` is a public, non-indexable maintenance information page using the supplied maintenance illustration. It does not activate a site-wide maintenance mode or change backend health.
- The home greeting and all status pages use the existing illustrations in `public/images/`. They are loaded as local Next.js image assets and have descriptive alternative text.

Backend liveness and readiness remain available at `/health/live` and `/health/ready` on the .NET API. The frontend does not infer a site-wide maintenance state from these endpoints; API features retain their own loading and failure handling.

## Architecture

```text
src/
  app/                 routes, metadata, same-origin API proxies, global styles
    api/               assistant, contacts, visits, donations, GitHub routes
    resume/            data-driven resume page and print/download actions
  components/
    hud/               reusable section shell
    sections/          home page and portfolio assistant
  lib/
    server/            backend URL resolver and request proxy
    ...                assistant, contact, resume, session, SEO, tracking
public/images/
  *.jfif               greeting and branded status illustrations
```

## Responsive design and performance

The site uses semantic HTML, dynamic imports for lower-page sections, a compact mobile navigation, and responsive resume and assistant layouts. Decorative WebGL has been removed from the home-page experience. Reduced-motion preferences are respected.

## Accessibility

- Semantic landmarks and ordered headings
- Keyboard-operable navigation, controls, forms, and assistant
- Visible focus behavior and high-contrast text
- Decorative canvas excluded from the accessibility tree
- `prefers-reduced-motion` respected by Framer Motion and CSS
- Forms retain labels, validation, and status announcements
- Resume print styles remove site navigation and screen-only controls

## Integrations

- The home page, footer, and `/resume` load profile content from backend `GET /api/resumes/latest` at runtime.
- `POST /api/assistant` proxies streaming NDJSON and preserves session context, conversation history, cancellation, and errors.
- `POST /api/contacts`, `POST /api/visits`, and `/api/donations/*` forward browser requests through same-origin Next.js routes.
- `/api/github/projects` retrieves the public repository feed. It uses `GITHUB_USER` when set, otherwise it infers the account from the resume's GitHub profile URL.

`BACKEND_API_URL` is a server-only setting. Docker Compose sets it to `http://backend:8080`; local development should use `http://localhost:5000`. Browser code uses same-origin routes and never needs the Docker service hostname. Do not expose backend addresses or credentials through `NEXT_PUBLIC_` variables.

## Environment variables

```text
BACKEND_API_URL              Server-only backend base URL; Compose sets http://backend:8080
NEXT_PUBLIC_SITE_URL         Canonical and social metadata origin
GITHUB_TOKEN                 Optional server-side GitHub authentication/rate-limit increase
GITHUB_USER                  Optional GitHub account override; otherwise inferred from the backend resume profile
GITHUB_USER_AGENT            Optional GitHub request user agent
GOOGLE_SITE_VERIFICATION     Optional search-console verification
NEXT_PUBLIC_DEBUG_AI         Optional client-side assistant debug logging
```

## Donations

Donations use PayPal Checkout. Set backend secrets `PayPal__ClientId` and `PayPal__ClientSecret`, plus `PayPal__Environment=sandbox` while testing. Set `PayPal__Environment=live` only after sandbox verification and PayPal account approval. Credentials belong only in the backend/deployment secret store; never use a `NEXT_PUBLIC_` prefix. The browser receives only the PayPal client ID, which is designed to be public.

The checkout amount is validated and created by the backend; capture is performed by the backend and verified against the PayPal response before showing the donation publicly. The page shows one PayPal checkout button; PayPal may offer eligible card payment within its own checkout based on the buyer, merchant, and region. Donor email is optional and used only to attempt a receipt; it is removed after the receipt attempt. Donor name, amount, and remark are public only when the donor opts in.

An interrupted checkout reuses its attempt UUID from the current tab's `sessionStorage` after a retry or page refresh. Browser storage contains only this non-sensitive identifier—not donor details or payment data. The backend recovers the original order and uses its persisted amount and currency; use **Start a new donation attempt** to intentionally begin a separate donation. A confirmed capture clears the saved identifier.

Donation receipts use the existing SMTP configuration and a backend outbox with retries; SMTP failures do not reverse or fail a confirmed payment. Receipt email is optional. The public leaderboard shows up to 50 opted-in successful donations; aggregate totals include successful donations only. PayPal webhook events are verified using the configured backend-only `PayPal__WebhookId` before updating refund/reversal state. See the backend README for Sandbox and deployment configuration.

## Local development

Run the backend separately and set `BACKEND_API_URL` in the frontend process environment:

```powershell
cd frontend
npm install
$env:BACKEND_API_URL="http://localhost:5000"
npm run dev
```

Quality checks:

```powershell
npm run lint
npm run type-check
npm test
npm run build
```

`npm test` runs the Playwright browser checks, including resume section navigation, mobile overflow, and print mode.

## Docker

From the repository root, run `docker compose up -d --build`. The frontend container connects to the backend over the Compose network using `http://backend:8080`. The backend still needs a reachable MongoDB instance; this Compose file does not start MongoDB.

After changing frontend environment variables or code, rebuild/recreate the frontend container. See the [root README](../README.md) for the full-stack setup and [backend README](../backend/README.md) for API and backend configuration.
