# KAI // Engineering Portfolio

A Next.js portfolio for Hang Kheang Taing, built as a professional engineering command interface. The visual system draws from advanced aerospace, powered-exoskeleton, nanotechnology, and HUD design without using copyrighted characters, marks, or artwork.

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

Static professional facts remain in HTML. Three.js is decorative and is not required to understand or navigate the portfolio.

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
- `GET /api/github/projects`: server-side GitHub repository feed with loading, empty, and failure states

## Environment variables

```text
NEXT_PUBLIC_BACKEND_API_URL  Backend base URL; localhost fallback in development
NEXT_PUBLIC_SITE_URL         Canonical and social metadata origin
GITHUB_TOKEN                 Optional server-side GitHub authentication
GITHUB_USER_AGENT            Optional GitHub request user agent
GOOGLE_SITE_VERIFICATION     Optional search-console verification
NEXT_PUBLIC_DEBUG_AI         Optional assistant debug logging
```

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
