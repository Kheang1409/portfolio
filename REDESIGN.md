# Cambodian portfolio redesign

## Architecture and content
- Next.js App Router, Tailwind and Framer Motion remain in place.
- Home, resume, downloadable resumes, SEO image routes, assistant proxy and GitHub API routes are preserved.
- Semantic palette tokens: `src/app/globals.css`; Tailwind consumes those tokens in both themes.
- Cultural/journey/values content: `src/lib/story.ts`; reusable editorial sections and reveal utility: `src/components/sections/Story.tsx`.
- Existing professional experience, education, skills and project summaries remain in their original components.
- Generated media prompts and provenance: `ASSET_PROMPTS.md`. Hero and cultural images total approximately 343 KB before responsive Next image optimization.
- Cinematic CSS depth, atmospheric mist, sparse particles and Framer Motion replace the old game WebGL scenes. Mobile and reduced-motion users receive static atmosphere. Content is visible without waiting for a reveal.
- The original portrait is preserved. Farming content is a metaphor, not a claim of personal farming experience. No testimonials, unsupported proficiency claims, or private project links have been invented. Repository filters cover the requested categories using supplied technologies; categories without matching repositories show an explicit empty state.

## Run and verify
`npm run dev`, `npm run build`, `npm run type-check`, `npm run lint`, `npm test`.

The legacy `next lint` command has been replaced with ESLint flat configuration. Four Playwright scenarios cover desktop, mobile, tablet, accessibility and contact states.

Browser tests use installed Chrome and start the dev server if needed. They mock GitHub and contact responses, prevent visit tracking, and never send a real contact message. Coverage includes navigation, filtering, dialog focus restoration, assistant keyboard access, image loading, desktop/mobile/tablet overflow, dark/light WCAG A/AA checks, reduced motion, resume routing, native form validation, persistent errors and successful retry.

## Backend integration
The original contact and visitor tracking integrations use `NEXT_PUBLIC_BACKEND_API_URL`, defaulting to `http://localhost:5000`. Assistant proxy configuration is unchanged. That backend must be running/configured for live delivery, AI replies and analytics. Local browser inspection found an unavailable-backend network error from visitor tracking; no application JavaScript exceptions were observed. Contact delivery was validated with mocked responses rather than a live message. GitHub repositories loaded from the real API.

## Content additions
Add real testimonials only with supplied quotes and attribution. Add private project screenshots, verified case-study URLs, roles and additional categories when the owner supplies them. The featured interface is an illustrative composition, not a claimed production screenshot.

## Navigation and assistant refinement
Header links follow page order: Journey, About, Skills, Work, Culture, Experience, Education, Contact. The hero footer uses normal document flow to avoid overlap on wide/short screens. The local Next developer indicator is disabled so it cannot cover the journey link.

Kai now includes suggested questions, readable Markdown, copy actions, multiline input, persistent history, deliberate conversation reset, a single retry panel and direct portfolio/contact links when unavailable. New conversations reset the backend session identifier. Retrying replaces the failed exchange; cancelled or incomplete replies are excluded from model history. Stream cancellation and timeout cover the response body, and request guards prevent late responses from modifying newer conversations.

The server supplies public resume facts and a concise portfolio-specific persona. Raw backend errors and developer metrics are kept out of the main conversation. Live model quality still depends on the configured backend; streaming/error behavior and grounding were verified with test fixtures.
