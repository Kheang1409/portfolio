# Resume Page (`/resume`)

## Overview

Recruiter-focused, printable resume page with section navigation, compact skill tags, framed cards, and PDF download.

---

## Features

### 1. Section Navigation (Sticky Sidebar)

```tsx
<nav className="sticky top-20 hidden lg:block">
  <ul className="space-y-2">
    {SECTIONS.map(section => (
      <li key={section.id}>
        <a 
          href={`#${section.id}`}
          className={cn(
            'block px-3 py-2 text-sm font-medium rounded transition-colors',
            activeSection === section.id 
              ? 'bg-black text-white' 
              : 'text-gray-700 hover:bg-gray-100'
          )}
        >
          {section.label}
        </a>
      </li>
    ))}
  </ul>
</nav>
```

- **Sticky** — Stays visible while scrolling
- **Scroll Spy** — Highlights current section via `IntersectionObserver`
- **Smooth Scroll** — `scroll-behavior: smooth` on html
- **Mobile** — Hidden on `< lg`, replaced by bottom sheet

### 2. Section Components

| Section | Component | Key Features |
|---------|-----------|--------------|
| Profile | `ProfileSection` | Name, headline, summary, contact links |
| Skills | `SkillsSection` | Categorized tags, color-coded by level |
| Experience | `ExperienceSection` | Framed cards, timeline, highlights |
| Projects | `ProjectsSection` | Case study cards, tech stack, links |
| Education | `EducationSection` | Degree cards, honors |
| Certifications | `CertificationsSection` | Badge-style cards, credential links |

### 3. Skills Display

```tsx
// Grouped by category
const skillsByCategory = skills.reduce((acc, skill) => {
  (acc[skill.category] ??= []).push(skill);
  return acc;
}, {} as Record<string, Skill[]>);

// Level colors
const levelColors = {
  Expert: 'bg-black text-white',
  Advanced: 'bg-gray-800 text-white',
  Intermediate: 'bg-gray-200 text-gray-900',
  Beginner: 'bg-gray-100 text-gray-700'
};
```

### 4. Experience Cards

```tsx
<article className="border border-gray-200 rounded-lg p-6">
  <div className="flex items-start gap-4">
    <time className="text-sm text-gray-500 whitespace-nowrap">
      {formatDate(exp.startDate)} – {exp.endDate ? formatDate(exp.endDate) : 'Present'}
    </time>
    <div className="flex-1">
      <h3 className="font-semibold">{exp.role}</h3>
      <p className="text-gray-600">{exp.company}</p>
      <p className="mt-2 text-sm">{exp.description}</p>
      <div className="mt-3 flex flex-wrap gap-1">
        {exp.technologies.map(tech => (
          <span key={tech} className="px-2 py-0.5 text-xs bg-gray-100 rounded">
            {tech}
          </span>
        ))}
      </div>
      {exp.highlights && (
        <ul className="mt-3 space-y-1 list-disc list-inside text-sm">
          {exp.highlights.map(h => <li key={h}>{h}</li>)}
        </ul>
      )}
    </div>
  </div>
</article>
```

---

## Print Styles (`@media print`)

```css
@media print {
  /* Hide navigation and interactive elements */
  .no-print,
  nav,
  button,
  [data-no-print] {
    display: none !important;
  }

  /* Reset layout */
  .resume-container {
    display: block !important;
    max-width: none !important;
    padding: 0 !important;
  }

  /* Section breaks */
  section {
    break-inside: avoid;
    page-break-inside: avoid;
  }

  /* Links */
  a {
    text-decoration: none !important;
    color: inherit !important;
  }

  /* Colors */
  .bg-black { background: #000 !important; -webkit-print-color-adjust: exact; }
  .text-white { color: #fff !important; -webkit-print-color-adjust: exact; }

  /* Margins */
  @page { margin: 1cm; }
}
```

### Print Controls (UI)

```tsx
<div className="no-print flex gap-2 mb-6">
  <button onClick={() => window.print()} className="btn-secondary">
    <PrinterIcon /> Print / Save as PDF
  </button>
  <a href="/resume.pdf" download className="btn-secondary">
    <DownloadIcon /> Download PDF
  </a>
</div>
```

---

## PDF Download (`/resume.pdf`)

- **Static file** in `frontend/public/resume.pdf`
- **Separate from live data** — must be regenerated when resume updates
- **Generation**: Manual (Designer/CLI) or automated (CI with headless Chrome)

### Regeneration Checklist

When resume content changes:
1. Update MongoDB resume document
2. Verify `/resume` page renders correctly
3. Regenerate PDF:
   ```bash
   # Option 1: Chrome headless
   npx @puppeteer/puppeteer --no-sandbox --print-to-pdf=public/resume.pdf http://localhost:3000/resume
   
   # Option 2: Manual (browser) — Print → Save as PDF
   ```
4. Commit new `public/resume.pdf`
5. Deploy

---

## Data Fetching

```typescript
// src/app/resume/page.tsx (Server Component)
import { getResume } from '@/lib/server/resume';

export default async function ResumePage() {
  const resume = await getResume(); // Fetches from backend /api/resumes/latest
  
  return <ResumeContent resume={resume} />;
}
```

**Caching**: None — always fresh (request-time fetch)

---

## Responsive Behavior

| Breakpoint | Behavior |
|------------|----------|
| `< 640px` (mobile) | Single column, bottom sheet navigation |
| `640px - 1024px` (tablet) | Single column, sticky top nav |
| `> 1024px` (desktop) | Two-column: sidebar + content |

---

## Accessibility

| Feature | Implementation |
|---------|----------------|
| **Semantic HTML** | `<section>`, `<article>`, `<header>`, `<time>`, `<address>` |
| **Heading Hierarchy** | h1 (name) → h2 (section titles) → h3 (role/degree) |
| **Skip Link** | "Skip to main content" for keyboard users |
| **Focus Visible** | All interactive elements have visible focus |
| **Print Accessibility** | Content fully readable when printed |
| **Color Contrast** | Skill tags meet 4.5:1 ratio |

---

## SEO

```typescript
export async function generateMetadata(): Promise<Metadata> {
  const resume = await getResume();
  return {
    title: `${resume.profile.fullName} — Resume`,
    description: resume.profile.summary.slice(0, 160),
    openGraph: {
      title: `${resume.profile.fullName} — Resume`,
      description: resume.profile.summary.slice(0, 160),
      type: 'profile',
      profile: {
        firstName: resume.profile.fullName.split(' ')[0],
        lastName: resume.profile.fullName.split(' ').slice(1).join(' '),
        username: resume.profile.githubUrl?.split('/').pop()
      }
    }
  };
}
```

---

## Testing (Playwright)

```typescript
// tests/resume.spec.ts
test('resume page loads with data', async ({ page }) => {
  await page.goto('/resume');
  await expect(page.locator('h1')).toContainText('Kai Assistant');
  await expect(page.locator('section#experience')).toBeVisible();
  await expect(page.locator('section#skills')).toBeVisible();
});

test('section navigation works', async ({ page }) => {
  await page.goto('/resume');
  await page.locator('nav a[href="#skills"]').click();
  await expect(page.locator('section#skills')).toBeInViewport();
});

test('print styles hide navigation', async ({ page }) => {
  await page.goto('/resume');
  await page.emulateMedia({ media: 'print' });
  await expect(page.locator('nav')).toBeHidden();
});

test('PDF download link works', async ({ page }) => {
  await page.goto('/resume');
  const download = await page.waitForEvent('download');
  await page.locator('a[href="/resume.pdf"]').click();
  expect(download.suggestedFilename()).toBe('resume.pdf');
});
```

---

## Related Documents

- [Pages & Routing](pages-routing.md) — Route structure
- [Accessibility](accessibility.md) — A11y standards
- [API Endpoints](../api/endpoints.md) — `/api/resumes/latest`
- [Data Flow](../architecture/system-overview.md) — Resume display flow