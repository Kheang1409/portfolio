# Portfolio Frontend

A Next.js portfolio frontend with an engineering-inspired interface. Profile and experience content comes from the backend resume API.

## Experience

The home page presents identity, system metrics, engineering profile, capability matrix, career mission log, GitHub-backed projects, education, an AI assistant showcase, and contact terminal. `/resume` provides a recruiter-friendly engineering dossier with dedicated print styling.

Six persisted suit configurations change the full material system—not only the accent color:

- MK II: prototype titanium and cool-white energy
- MK III: restrained red, gold, and white alloy
- MK 45: dark crimson systems and electric-blue seams
- MK 46: segmented tactical surfaces and distributed light nodes
- MK 50: energetic nanotech red, gold, and cyan
- MK 85: polished advanced nanotech, the default

The selected configuration is stored as `kai-armor-theme` in `localStorage` and applied through semantic CSS tokens by `Providers` and `useArmorTheme`.

## Architecture

```text
src/
  app/                 routes, metadata, API proxies, global design system
  components/
    hud/               section shell and theme selector
    sections/          accessible home-page content
    visuals/           optional WebGL reactor
  config/              armor theme definitions
  hooks/               armor theme context API
  lib/                 assistant, contact, session, SEO, and tracking clients
public/images/
  kai/ armor/ projects/ backgrounds/ textures/
```

Professional profile content comes from the backend resume endpoint. Three.js is decorative and is not required to understand or navigate the portfolio.

## Three.js and performance

`ArcReactorScene` renders one hero-only WebGL context with concentric reactor rings and a restrained nanotech particle field. It caps device pixel ratio, lowers mobile complexity, pauses rendering outside the viewport, honors reduced-motion, resizes through `ResizeObserver`, and disposes geometries, materials, and the renderer on unmount. Other visual effects use CSS rather than additional WebGL contexts.

The site uses dynamic imports for the scene and lower-page sections, system fonts, semantic HTML fallbacks, dynamic viewport sizing for the assistant, and mobile-specific simplification.

## Accessibility

- Semantic landmarks and ordered headings
- Keyboard-operable navigation, controls, forms, and assistant
- Visible focus behavior and high-contrast text
- Decorative canvas excluded from the accessibility tree
- `prefers-reduced-motion` respected by Framer Motion and CSS
- Forms retain labels, validation, and status announcements
- Print styles remove navigation, effects, and unnecessary controls

## Integrations

- `POST /api/assistant`: Next.js proxy preserving streaming NDJSON, session metadata, history, retry, cancellation, and errors
- `POST /api/contacts`: contact submission through the configured backend
- `POST /api/visits`: best-effort visitor tracking that never blocks rendering
- GitHub repository feed with loading, empty, and failure states

The home page and `/resume` retrieve profile content from `GET /api/resumes/latest`. `NEXT_PUBLIC_BACKEND_API_URL` must point to the backend for resume, contact, visit tracking, and assistant responses. GitHub projects are fetched live from GitHub.

## Environment variables

```text
NEXT_PUBLIC_BACKEND_API_URL  Backend base URL; required for profile data
BACKEND_API_URL              Optional server-side backend URL for donation proxy (use backend service URL in containers)
NEXT_PUBLIC_SITE_URL         Canonical and social metadata origin
GITHUB_TOKEN                 Optional server-side GitHub authentication
GITHUB_USER                  GitHub account whose repositories are displayed
GITHUB_USER_AGENT            Optional GitHub request user agent
GOOGLE_SITE_VERIFICATION     Optional search-console verification
NEXT_PUBLIC_DEBUG_AI         Optional assistant debug logging
```

## Donations

Donations use PayPal Checkout. Set backend secrets `PayPal__ClientId` and `PayPal__ClientSecret`, plus `PayPal__Environment=sandbox` while testing. Set `PayPal__Environment=live` only after sandbox verification and PayPal account approval. Credentials belong only in the backend/deployment secret store; never use a `NEXT_PUBLIC_` prefix. The browser receives only the PayPal client ID, which is designed to be public.

The checkout amount is validated and created by the backend; capture is performed by the backend and verified against the PayPal response before showing the donation publicly. Card entry is handled by PayPal. Available card options vary by PayPal eligibility, buyer country, and merchant account. Donor email is optional and used only to attempt a receipt; it is removed after the receipt attempt. Donor name, amount, and remark are public only when the donor opts in.

Donation receipts use the existing SMTP configuration and a backend outbox with retries; SMTP failures do not reverse or fail a confirmed payment. Receipt email is optional. The public leaderboard shows up to 50 opted-in successful donations; aggregate totals include successful donations only. PayPal webhook events are verified using the configured backend-only `PayPal__WebhookId` before updating refund/reversal state. See the backend README for Sandbox and deployment configuration.

## Local development

```bash
npm install
npm run dev
```

Quality checks:

```bash
npm run lint
npm run type-check
npm run build
```

## Future image assets

The current avatar and generated CSS visuals are safe fallbacks. These optional replacement files are already routed into the image structure:

```text
public/images/kai/kai-og.webp
public/images/kai-classic-stance.webp
public/images/armor/nanotech-system.webp
public/images/backgrounds/reactor-core.webp
public/images/backgrounds/mark2-blueprint.webp
public/images/backgrounds/software-core.webp
```

Keep all meaningful names, labels, and technical content in HTML rather than baking text into artwork.
