# Portfolio Assistant UI

## Component Structure

```
src/components/sections/Assistant/
├── Assistant.tsx           # Main container (client component)
├── AssistantHeader.tsx     # Title, suggested questions
├── AssistantMessages.tsx   # Message list + streaming display
├── AssistantInput.tsx      # Textarea + send/stop controls
├── AssistantHistory.tsx    # localStorage persistence
└── useAssistantStream.ts   # Streaming hook
```

---

## Features

### 1. Suggested Questions
Pre-defined prompts to guide users:
```typescript
const SUGGESTED_QUESTIONS = [
  "What is your current role?",
  "What technologies do you specialize in?",
  "Tell me about your e-commerce project",
  "What cloud platforms have you used?",
  "How do you approach system design?"
];
```
- Click to auto-fill input and send
- Hidden after first user message

### 2. Streaming Response
- **NDJSON parsing** via `useAssistantStream` hook
- **Typewriter effect** — characters appear as chunks arrive
- **Accumulated state** — `content` chunks appended in order
- **Done handling** — marks message complete, enables new input

### 3. Conversation History
- **Persisted in `localStorage`** under key `assistant-history`
- **Max 20 messages** (10 exchanges)
- **Loaded on mount** — restores previous session
- **Cleared on "New Chat"** button

### 4. Controls
| Control | Action |
|---------|--------|
| **Send** | Submits query, starts stream |
| **Stop** | Aborts fetch, keeps partial response |
| **Retry** | Re-sends last query (uses history) |
| **Copy** | Copies assistant message to clipboard |
| **New Chat** | Clears history, resets to suggestions |

### 5. Error States
| Error | UI Behavior |
|-------|-------------|
| Network error | Inline error message, "Retry" button |
| Rate limited | Shows `Retry-After` countdown, auto-retry |
| Fallback response | Yellow banner: "AI service unavailable, showing fallback" |
| Empty query | Inline validation, disabled send |

---

## Streaming Hook (`useAssistantStream`)

```typescript
interface UseAssistantStreamResult {
  messages: Message[];
  isStreaming: boolean;
  error: string | null;
  sendQuery: (query: string) => Promise<void>;
  stopStream: () => void;
  retryLast: () => Promise<void>;
  clearHistory: () => void;
  copyMessage: (index: number) => Promise<void>;
}

function useAssistantStream(): UseAssistantStreamResult {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController>();
  const lastQueryRef = useRef<string>();

  // Load history from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem('assistant-history');
    if (stored) setMessages(JSON.parse(stored));
  }, []);

  // Persist to localStorage on change
  useEffect(() => {
    localStorage.setItem('assistant-history', JSON.stringify(messages.slice(-20)));
  }, [messages]);

  const sendQuery = useCallback(async (query: string) => {
    setError(null);
    setIsStreaming(true);
    lastQueryRef.current = query;
    abortRef.current = new AbortController();

    // Add user message
    setMessages(prev => [...prev, { role: 'user', content: query }]);
    
    // Add placeholder assistant message
    const assistantIndex = messages.length;
    setMessages(prev => [...prev, { role: 'assistant', content: '', streaming: true }]);

    try {
      const response = await fetch('/api/assistant', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          query, 
          history: messages.slice(-6).map(m => ({ role: m.role, content: m.content })) 
        }),
        signal: abortRef.current.signal
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error?.message ?? 'Request failed');
      }

      const reader = response.body?.getReader();
      if (!reader) throw new Error('No response body');

      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';

        for (const line of lines) {
          if (!line.trim()) continue;
          const chunk = JSON.parse(line);
          
          if (chunk.type === 'content') {
            setMessages(prev => prev.map((m, i) => 
              i === assistantIndex ? { ...m, content: m.content + chunk.text } : m
            ));
          } else if (chunk.type === 'done') {
            setMessages(prev => prev.map((m, i) => 
              i === assistantIndex ? { ...m, streaming: false } : m
            ));
          } else if (chunk.type === 'error') {
            setError(chunk.message);
            setMessages(prev => prev.map((m, i) => 
              i === assistantIndex ? { ...m, streaming: false, error: chunk.message } : m
            ));
          } else if (chunk.type === 'fallback') {
            setMessages(prev => prev.map((m, i) => 
              i === assistantIndex ? { ...m, content: chunk.text, streaming: false, fallback: true } : m
            ));
          }
        }
      }
    } catch (err) {
      if (err instanceof DOMException && err.name === 'AbortError') return;
      setError(err instanceof Error ? err.message : 'Unknown error');
      setMessages(prev => prev.map((m, i) => 
        i === assistantIndex ? { ...m, streaming: false, error: err instanceof Error ? err.message : 'Unknown error' } : m
      ));
    } finally {
      setIsStreaming(false);
    }
  }, [messages]);

  const stopStream = useCallback(() => {
    abortRef.current?.abort();
    setIsStreaming(false);
  }, []);

  const retryLast = useCallback(() => {
    if (lastQueryRef.current) sendQuery(lastQueryRef.current);
  }, [sendQuery]);

  const clearHistory = useCallback(() => {
    setMessages([]);
    localStorage.removeItem('assistant-history');
  }, []);

  const copyMessage = useCallback(async (index: number) => {
    const message = messages[index];
    if (message?.role === 'assistant' && message.content) {
      await navigator.clipboard.writeText(message.content);
    }
  }, [messages]);

  return { messages, isStreaming, error, sendQuery, stopStream, retryLast, clearHistory, copyMessage };
}
```

---

## Message Type

```typescript
interface Message {
  role: 'user' | 'assistant';
  content: string;
  streaming?: boolean;    // Currently receiving chunks
  error?: string;         // Error message if failed
  fallback?: boolean;     // Fallback response shown
}
```

---

## Accessibility

| Feature | Implementation |
|---------|----------------|
| **Keyboard Navigation** | Tab through suggestions, input, buttons |
| **Focus Management** | Focus input on mount, after send, after clear |
| **ARIA Live Region** | Streaming content announced via `aria-live="polite"` |
| **Status Announcements** | "Message sent", "Response complete", "Error" via `aria-live="assertive"` |
| **Button Labels** | `aria-label` for icon-only buttons (copy, stop, retry) |
| **Reduced Motion** | Respects `prefers-reduced-motion` for typewriter effect |

```tsx
// Streaming announcement
<div aria-live="polite" aria-atomic="true" className="sr-only">
  {isStreaming ? 'Receiving response' : 'Response complete'}
</div>

// Error announcement
<div aria-live="assertive" className="sr-only">
  {error}
</div>
```

---

## Styling

- **Monochrome theme** — black text, white surfaces, gray borders
- **Message bubbles** — User: black bg/white text; Assistant: white bg/black text
- **Streaming cursor** — Blinking block during streaming
- **Code blocks** — Syntax highlighted (via `rehype-highlight`)
- **Markdown** — Rendered via `react-markdown` + `remark-gfm`

---

## Testing (Playwright)

```typescript
// tests/assistant.spec.ts
test('assistant streams response', async ({ page }) => {
  await page.goto('/');
  await page.locator('[data-testid="assistant-input"]').fill('What is your role?');
  await page.locator('[data-testid="assistant-send"]').click();
  
  // Wait for streaming to start
  await expect(page.locator('[data-testid="assistant-message"]')).toContainText('I');
  
  // Wait for completion
  await expect(page.locator('[data-testid="assistant-message"]')).toContainText('Software Engineer');
});

test('assistant history persists', async ({ page }) => {
  await page.goto('/');
  await page.locator('[data-testid="assistant-input"]').fill('Test');
  await page.locator('[data-testid="assistant-send"]').click();
  await expect(page.locator('[data-testid="assistant-message"]')).toBeVisible();
  
  await page.reload();
  await expect(page.locator('[data-testid="assistant-message"]')).toBeVisible();
});

test('assistant copy button works', async ({ page }) => {
  await page.goto('/');
  await page.locator('[data-testid="assistant-input"]').fill('Test');
  await page.locator('[data-testid="assistant-send"]').click();
  await expect(page.locator('[data-testid="assistant-message"]')).toBeVisible();
  
  await page.locator('[data-testid="assistant-copy"]').click();
  // Verify clipboard (requires context grant)
});
```

---

## Related Documents

- [Assistant Streaming Protocol](../api/assistant-streaming.md) — Backend protocol
- [AI Pipeline](../architecture/ai-pipeline.md) — Backend logic
- [Pages & Routing](pages-routing.md) — Page structure
- [Accessibility](accessibility.md) — A11y standards