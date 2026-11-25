# Frontend Status Pages

## Overview

Branded error and status pages using provided illustrations. All pages are **informational** — they do not disable site functionality.

---

## Page Inventory

| Page | Route | Trigger | Illustration |
|------|-------|---------|--------------|
| **404 Not Found** | `not-found.tsx` | Unknown route | `/images/404.jfif` |
| **Route Error** | `error.tsx` | Route rendering failure | `/images/error.jfif` |
| **Global Error** | `global-error.tsx` | Root layout failure | `/images/error.jfif` |
| **Maintenance** | `/maintenance/page.tsx` | Manual visit | `/images/maintenance.jfif` |
| **503 Backend Down** | Root layout state | `/api/health/live` fails | `/images/503.jfif` |

---

## 404 Page (`not-found.tsx`)

```tsx
export default function NotFound() {
  return (
    <main className="min-h-screen flex items-center justify-center px-4">
      <div className="text-center max-w-md">
        <Image 
          src="/images/404.jfif" 
          alt="Page not found illustration" 
          width={300} 
          height={300}
          className="mx-auto mb-6"
        />
        <h1 className="text-4xl font-bold mb-2">Page Not Found</h1>
        <p className="text-gray-600 mb-6">
          The page you're looking for doesn't exist or has been moved.
        </p>
        <Link href="/" className="btn-primary">
          <HomeIcon className="inline mr-2" /> Back to Home
        </Link>
      </div>
    </main>
  );
}
```

**Behavior**:
- Matches any unmatched route
- No server details exposed
- Links to home page

---

## Route Error (`error.tsx`)

```tsx
'use client';

export default function Error({ error, reset }: { error: Error; reset: () => void }) {
  useEffect(() => {
    console.error('Route error:', error); // Dev logging
    // In prod: send to error tracking (Sentry, etc.)
  }, [error]);

  return (
    <main className="min-h-screen flex items-center justify-center px-4">
      <div className="text-center max-w-md">
        <Image 
          src="/images/error.jfif" 
          alt="Error illustration" 
          width={300} 
          height={300}
          className="mx-auto mb-6"
        />
        <h1 className="text-4xl font-bold mb-2">Something Went Wrong</h1>
        <p className="text-gray-600 mb-6">
          We encountered an error loading this page. Please try again.
        </p>
        <button onClick={reset} className="btn-primary">
          <RefreshCwIcon className="inline mr-2" /> Try Again
        </button>
      </div>
    </main>
  );
}
```

**Behavior**:
- Catches errors in **route segment** (page, layout, component)
- `reset()` re-renders the route segment
- Error logged to console (dev) / monitoring (prod)

---

## Global Error (`global-error.tsx`)

```tsx
'use client';

export default function GlobalError({ error, reset }: { error: Error; reset: () => void }) {
  useEffect(() => {
    console.error('Global error:', error);
  }, [error]);

  return (
    <html>
      <head>
        <title>Error | Kai Assistant</title>
      </head>
      <body className="min-h-screen flex items-center justify-center px-4">
        <div className="text-center max-w-md">
          <Image 
            src="/images/error.jfif" 
            alt="Critical error illustration" 
            width={300} 
            height={300}
            className="mx-auto mb-6"
          />
          <h1 className="text-4xl font-bold mb-2">Critical Error</h1>
          <p className="text-gray-600 mb-6">
            A critical error occurred. The application cannot recover automatically.
          </p>
          <button onClick={reset} className="btn-primary">
            <RefreshCwIcon className="inline mr-2" /> Refresh Page
          </button>
        </div>
      </body>
    </html>
  );
}
```

**Behavior**:
- Catches errors in **root layout** (providers, global styles)
- Full HTML document — must include `<html>`, `<head>`, `<body>`
- `reset()` triggers full page reload

---

## Maintenance Page (`/maintenance/page.tsx`)

```tsx
export default function Maintenance() {
  return (
    <main className="min-h-screen flex items-center justify-center px-4">
      <div className="text-center max-w-md">
        <Image 
          src="/images/maintenance.jfif" 
          alt="Maintenance illustration" 
          width={300} 
          height={300}
          className="mx-auto mb-6"
        />
        <h1 className="text-4xl font-bold mb-2">Maintenance</h1>
        <p className="text-gray-600 mb-6">
          This page is for informational purposes only. The site remains fully operational.
        </p>
        <p className="text-sm text-gray-500">
          If you're seeing this, someone manually navigated to /maintenance.
        </p>
        <Link href="/" className="btn-primary">
          <HomeIcon className="inline mr-2" /> Back to Home
        </Link>
      </div>
    </main>
  );
}
```

**Key Points**:
- **NOT a maintenance mode** — does not disable other routes
- **No backend coordination** — purely informational
- **Robots meta**: `<meta name="robots" content="noindex" />`
- **Use case**: Stakeholder communication, scheduled maintenance notice

---

## 503 Backend Unavailable (Root Layout)

### Detection Logic

```tsx
// src/app/layout.tsx
'use client';

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const [backendStatus, setBackendStatus] = useState<'checking' | 'healthy' | 'unhealthy'>('checking');

  useEffect(() => {
    let mounted = true;
    let interval: NodeJS.Timeout;

    const checkHealth = async () => {
      try {
        const res = await fetch('/api/health/live', { 
          cache: 'no-store',
          signal: AbortSignal.timeout(5000)
        });
        if (mounted) setBackendStatus(res.ok ? 'healthy' : 'unhealthy');
      } catch {
        if (mounted) setBackendStatus('unhealthy');
      }
    };

    checkHealth();
    interval = setInterval(checkHealth, 30000); // Retry every 30s
    
    return () => { mounted = false; clearInterval(interval); };
  }, []);

  if (backendStatus === 'checking') {
    return <LoadingSkeleton />; // Or show page with loading state
  }

  if (backendStatus === 'unhealthy') {
    return <BackendUnavailable onRetry={checkHealth} />;
  }

  return (
    <html lang="en">
      <head>...</head>
      <body>{children}</body>
    </html>
  );
}
```

### 503 Component

```tsx
function BackendUnavailable({ onRetry }: { onRetry: () => void }) {
  const [retryCount, setRetryCount] = useState(0);

  return (
    <main className="min-h-screen flex items-center justify-center px-4">
      <div className="text-center max-w-md">
        <Image 
          src="/images/503.jfif" 
          alt="Service unavailable illustration" 
          width={300} 
          height={300}
          className="mx-auto mb-6"
        />
        <h1 className="text-4xl font-bold mb-2">Service Temporarily Unavailable</h1>
        <p className="text-gray-600 mb-6">
          The backend service is currently unreachable. We're automatically retrying every 30 seconds.
        </p>
        <p className="text-sm text-gray-500 mb-4">
          Automatic retry in {30 - retryCount} seconds... (Attempt #{retryCount + 1})
        </p>
        <div className="flex gap-2 justify-center">
          <button onClick={onRetry} className="btn-primary">
            <RefreshCwIcon className="inline mr-2" /> Retry Now
          </button>
          <Link href="/maintenance" className="btn-secondary">
            Maintenance Info
          </Link>
        </div>
      </div>
    </main>
  );
}
```

**Behavior**:
- Checks `/api/health/live` on mount + every 30s
- Shows 503 page when backend unhealthy
- Auto-retries every 30s (configurable)
- Manual "Retry Now" button
- Individual features (assistant, donations) show **own** error states — not blocked by 503

---

## Illustration Assets

All illustrations in `frontend/public/images/`:

| File | Used By | Description |
|------|---------|-------------|
| `greeting.jfif` | Home page hero | Professional greeting |
| `404.jfif` | 404 page | "Lost in space" theme |
| `error.jfif` | Error pages | "Something broke" theme |
| `maintenance.jfif` | Maintenance page | "Under construction" theme |
| `503.jfif` | 503 page | "Service down" theme |

**Optimization**:
- Next.js `<Image />` with `priority` for above-fold
- `width`/`height` explicit for CLS prevention
- `alt` text descriptive for accessibility

---

## Error Logging (Production)

```typescript
// lib/error-logging.ts
export function logError(error: Error, context?: Record<string, unknown>) {
  if (process.env.NODE_ENV === 'production') {
    // Send to monitoring service (Sentry, Datadog, etc.)
    // Example:
    // Sentry.captureException(error, { extra: context });
  } else {
    console.error('[Error]', error, context);
  }
}
```

**Integration**:
```tsx
// error.tsx
useEffect(() => {
  logError(error, { route: 'current', timestamp: new Date().toISOString() });
}, [error]);
```

---

## Testing (Playwright)

```typescript
// tests/status-pages.spec.ts
test('404 page shows for unknown route', async ({ page }) => {
  await page.goto('/nonexistent-page');
  await expect(page.locator('h1')).toContainText('Page Not Found');
  await expect(page.locator('img[alt*="not found"]')).toBeVisible();
});

test('error boundary catches route error', async ({ page }) => {
  await page.goto('/trigger-error'); // Page that throws
  await expect(page.locator('h1')).toContainText('Something Went Wrong');
  await page.locator('button:has-text("Try Again")').click();
  // Should recover
});

test('503 page shows when backend down', async ({ page }) => {
  await page.route('/api/health/live', route => route.abort('failed'));
  await page.goto('/');
  await expect(page.locator('h1')).toContainText('Service Temporarily Unavailable');
});

test('maintenance page is informational', async ({ page }) => {
  await page.goto('/maintenance');
  await expect(page.locator('h1')).toContainText('Maintenance');
  // Other routes still work
  await page.goto('/');
  await expect(page.locator('h1')).toContainText('Kai Assistant');
});
```

---

## Related Documents

- [Pages & Routing](pages-routing.md) — Route structure
- [Health Checks](../api/health-checks.md) — Backend health endpoints
- [Accessibility](accessibility.md) — A11y for error pages
- [Troubleshooting](../operations/troubleshooting.md) — Debugging status pages