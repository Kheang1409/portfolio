# Frontend Accessibility

## Compliance Target

**WCAG 2.1 Level AA** — All pages and components meet AA criteria.

---

## Semantic HTML

### Landmarks
```tsx
// layout.tsx
<header role="banner">...</header>
<nav role="navigation" aria-label="Main navigation">...</nav>
<main role="main">...</main>
<footer role="contentinfo">...</footer>
```

### Heading Hierarchy
| Page | h1 | h2 | h3 |
|------|-----|-----|-----|
| Home | Profile name | Section titles | Role/Project names |
| Resume | Name | Section titles | Role/Degree |
| Assistant | "Portfolio Assistant" | — | — |

---

## Keyboard Navigation

### Focus Management
```typescript
// Focus input on mount (Assistant)
useEffect(() => {
  inputRef.current?.focus();
}, []);

// Focus management on route change
useEffect(() => {
  const main = document.getElementById('main-content');
  main?.focus(); // Skip link target
}, [pathname]);
```

### Skip Link
```tsx
// layout.tsx
<a 
  href="#main-content" 
  className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 z-50 btn-primary"
>
  Skip to main content
</a>

<main id="main-content" tabIndex={-1}>
  {children}
</main>
```

### Tab Order
- Logical: Header → Nav → Main → Footer
- No `tabIndex` > 0 (except skip link target)
- Modal dialogs trap focus

---

## Focus Visible

```css
/* Global focus styles */
*:focus-visible {
  outline: 2px solid black;
  outline-offset: 2px;
}

/* Custom focus for specific components */
.btn:focus-visible {
  outline: 2px solid black;
  outline-offset: 2px;
}

.input:focus-visible {
  outline: 2px solid black;
  outline-offset: 1px;
}
```

---

## ARIA

### Live Regions
```tsx
// Streaming announcement (polite)
<div aria-live="polite" aria-atomic="true" className="sr-only">
  {isStreaming ? 'Receiving response' : 'Response complete'}
</div>

// Error announcement (assertive)
<div aria-live="assertive" aria-atomic="true" className="sr-only">
  {error}
</div>

// Status updates
<div aria-live="polite" aria-atomic="true" className="sr-only">
  {statusMessage}
</div>
```

### Labels & Descriptions
```tsx
// Icon buttons
<button aria-label="Copy response" onClick={copy}>
  <CopyIcon />
</button>

// Form inputs
<label htmlFor="email">Email</label>
<input 
  id="email" 
  type="email" 
  aria-describedby="email-hint"
  aria-invalid={!!errors.email}
/>
<p id="email-hint" className="sr-only">Enter your email address</p>

// Dynamic content
<div role="status" aria-live="polite">
  {messages.length} messages in conversation
</div>
```

### Roles
```tsx
// Assistant messages
<article role="log" aria-live="polite" aria-label="Conversation">
  {messages.map(msg => (
    <div key={msg.id} role={msg.role === 'user' ? 'none' : 'article'}>
      ...
    </div>
  ))}
</article>

// Navigation
<nav role="navigation" aria-label="Resume sections">
  <ul>
    <li><a href="#skills">Skills</a></li>
  </ul>
</nav>
```

---

## Color & Contrast

### Palette (Monochrome)
| Usage | Color | Contrast (AA) |
|-------|-------|---------------|
| Primary text | `#000000` | 21:1 on white |
| Secondary text | `#374151` (gray-700) | 12.6:1 on white |
| Muted text | `#6B7280` (gray-500) | 7:1 on white |
| Borders | `#E5E7EB` (gray-200) | 4.5:1 on white |
| Primary button | `#000000` bg / `#FFFFFF` text | 21:1 |
| Secondary button | `#FFFFFF` bg / `#000000` text + border | 21:1 |
| Skill tags (Expert) | `#000000` bg / `#FFFFFF` text | 21:1 |
| Skill tags (Beginner) | `#F3F4F6` bg / `#1F2937` text | 12.6:1 |

### Reduced Motion
```css
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

**Component Implementation**:
```tsx
// Framer Motion respects automatically
<motion.div 
  initial={{ opacity: 0, y: 20 }}
  animate={{ opacity: 1, y: 0 }}
  transition={{ duration: 0.3 }}
/>

// Custom typewriter effect
const [showCursor, setShowCursor] = useState(true);
useEffect(() => {
  if (prefersReducedMotion) return;
  const id = setInterval(() => setShowCursor(c => !c), 530);
  return () => clearInterval(id);
}, [prefersReducedMotion]);
```

---

## Forms

### Validation
```tsx
<form onSubmit={handleSubmit} noValidate>
  <div>
    <label htmlFor="name">Name *</label>
    <input
      id="name"
      required
      aria-required="true"
      aria-invalid={!!errors.name}
      aria-describedby={errors.name ? 'name-error' : 'name-hint'}
    />
    {errors.name && (
      <p id="name-error" role="alert" className="text-red-600 text-sm">
        {errors.name}
      </p>
    )}
    <p id="name-hint" className="sr-only">Enter your full name</p>
  </div>
  
  <button type="submit" disabled={isSubmitting}>
    {isSubmitting ? 'Sending...' : 'Send'}
  </button>
</form>
```

### Status Announcements
```tsx
// Toast / status message
<div role="status" aria-live="polite" aria-atomic="true">
  Message sent successfully!
</div>

// Error summary
<div role="alert" aria-live="assertive">
  <h2>Form has errors</h2>
  <ul>
    <li><a href="#name">Name is required</a></li>
  </ul>
</div>
```

---

## Images & Media

### Alt Text Strategy
```tsx
// Decorative
<Image src="/images/greeting.jfif" alt="" role="presentation" />

// Informational
<Image 
  src="/images/project-ecommerce.jpg" 
  alt="E-Commerce Platform dashboard showing product grid and cart"
/>

// Complex (long description)
<Image 
  src="/images/architecture-diagram.png" 
  alt="System architecture diagram"
  longDesc="/architecture-diagram-description"
/>
```

### Illustrations (Status Pages)
```tsx
// 404
<Image src="/images/404.jfif" alt="Astronaut floating in space with '404' on suit" />

// Error
<Image src="/images/error.jfif" alt="Robot with confused expression holding broken gear" />

// Maintenance
<Image src="/images/maintenance.jfif" alt="Construction site with tools and blueprint" />

// 503
<Image src="/images/503.jfif" alt="Server rack with warning lights and '503' display" />
```

---

## Responsive & Touch

### Touch Targets
```css
/* Minimum 44x44px */
.btn, .input, [role="button"], a {
  min-height: 44px;
  min-width: 44px;
}

/* Mobile nav */
.mobile-nav-button {
  padding: 12px 16px;
}
```

### Viewport
```html
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=5">
```
- No `user-scalable=no`
- Maximum scale 5x for zoom accessibility

---

## Testing Checklist

### Automated (CI)
```bash
npm run test:a11y  # axe-core + Playwright
```

### Manual
| Test | Method |
|------|--------|
| Keyboard only | Tab through entire site, no mouse |
| Screen reader | NVDA (Windows) / VoiceOver (Mac) |
| Zoom 200% | Browser zoom, check layout |
| High contrast | OS high contrast mode |
| Reduced motion | OS setting, verify animations disabled |
| Color blindness | Simulate (Stark, Color Oracle) |

### axe-core Rules (Configured)
```json
// .github/workflows/ci-security.yml or playwright.config.ts
{
  "rules": {
    "color-contrast": { "enabled": true },
    "keyboard": { "enabled": true },
    "aria-allowed-attr": { "enabled": true },
    "aria-required-attr": { "enabled": true },
    "image-alt": { "enabled": true },
    "label": { "enabled": true },
    "heading-order": { "enabled": true },
    "landmark-one-main": { "enabled": true },
    "region": { "enabled": true }
  }
}
```

---

## Component A11y Checklist

| Component | Requirements |
|-----------|--------------|
| **Button** | `type="button"`, accessible name, focus visible, disabled state |
| **Link** | Meaningful text, `href`, external links `rel="noopener noreferrer"` |
| **Input** | Associated `<label>`, `id`/`htmlFor`, `aria-invalid`, `aria-describedby` |
| **Select** | `<label>`, `<option>`, keyboard navigation |
| **Dialog/Modal** | `role="dialog"`, `aria-modal="true"`, `aria-labelledby`, focus trap, ESC close |
| **Tooltip** | `role="tooltip"`, `aria-describedby`, hover/focus trigger |
| **Table** | `<caption>`, `<th scope="col/row">`, keyboard navigation |
| **List** | `<ul>/<ol>`, `<li>`, nested properly |
| **Image** | `alt` text (empty for decorative), `width`/`height` for CLS |

---

## Related Documents

- [Pages & Routing](pages-routing.md) — Page structure
- [Assistant UI](assistant-ui.md) — Chat accessibility
- [Resume Page](resume-page.md) — Print accessibility
- [Status Pages](status-pages.md) — Error page accessibility
- [WCAG 2.1 Quick Reference](https://www.w3.org/WAI/WCAG21/quickref/)