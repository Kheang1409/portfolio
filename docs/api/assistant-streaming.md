# Assistant Streaming Protocol

## Overview

The `/api/assistant` endpoint returns a **streaming NDJSON** (Newline Delimited JSON) response. This document details the protocol for frontend integration.

---

## Request

```http
POST /api/assistant
Content-Type: application/json

{
  "query": "What is your experience with .NET?",
  "history": [
    {"role": "user", "content": "Hi"},
    {"role": "assistant", "content": "Hello! How can I help?"}
  ]
}
```

---

## Response Stream

**Content-Type**: `application/x-ndjson`  
**Transfer-Encoding**: `chunked`

Each line is a complete JSON object. Parse line-by-line.

### Chunk Types

#### 1. Content Chunk (Partial Response)

```json
{"type":"content","text":"I have","index":0}
```

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | Always `"content"` |
| `text` | string | Partial text (append to accumulated response) |
| `index` | integer | Monotonically increasing sequence number |

#### 2. Done Chunk (Stream Complete)

```json
{"type":"done","finishReason":"STOP","index":3}
```

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | Always `"done"` |
| `finishReason` | string | `"STOP"` (normal), `"LENGTH"` (max tokens), `"SAFETY"` (safety filter) |
| `index` | integer | Final sequence number |

#### 3. Error Chunk (Stream Error)

```json
{"type":"error","message":"Upstream service unavailable","code":"ServiceUnavailable","index":0}
```

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | Always `"error"` |
| `message` | string | Human-readable error |
| `code` | string | Error code (see below) |
| `index` | integer | Sequence number (0 for first error) |

#### 4. Fallback Chunk (Graceful Degradation)

```json
{"type":"fallback","text":"I'm having trouble connecting to the AI service. Please try again in a moment.","index":0}
```

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | Always `"fallback"` |
| `text` | string | Complete fallback message |
| `index` | integer | Always 0 |

---

## Error Codes

| Code | HTTP Status | Description | Retryable |
|------|-------------|-------------|-----------|
| `ValidationError` | 400 | Query empty or exceeds max chars | No |
| `RateLimited` | 429 | Too many requests | Yes (after `Retry-After`) |
| `ServiceUnavailable` | 503 | AI service down (fallback returned) | Yes |
| `InternalError` | 500 | Unexpected server error | Yes |

---

## Frontend Implementation (TypeScript)

### Stream Consumer

```typescript
interface AssistantChunk {
  type: 'content' | 'done' | 'error' | 'fallback';
  text?: string;
  message?: string;
  code?: string;
  finishReason?: 'STOP' | 'LENGTH' | 'SAFETY';
  index: number;
}

async function* streamAssistant(
  query: string, 
  history: Message[],
  signal: AbortSignal
): AsyncGenerator<AssistantChunk> {
  const response = await fetch('/api/assistant', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ query, history }),
    signal
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error?.message ?? 'Request failed');
  }

  const reader = response.body?.getReader();
  if (!reader) throw new Error('No response body');

  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() ?? ''; // Keep incomplete line

      for (const line of lines) {
        if (!line.trim()) continue;
        try {
          yield JSON.parse(line) as AssistantChunk;
        } catch {
          console.warn('Failed to parse chunk:', line);
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}
```

### React Hook

```typescript
function useAssistantStream() {
  const [chunks, setChunks] = useState<AssistantChunk[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController>();

  const sendQuery = useCallback(async (query: string, history: Message[]) => {
    setChunks([]);
    setError(null);
    setIsStreaming(true);
    
    abortRef.current = new AbortController();
    
    try {
      for await (const chunk of streamAssistant(query, history, abortRef.current.signal)) {
        setChunks(prev => [...prev, chunk]);
        
        if (chunk.type === 'error') {
          setError(chunk.message);
          break;
        }
        if (chunk.type === 'fallback') {
          setError('Using fallback response');
        }
      }
    } catch (err) {
      if (err instanceof DOMException && err.name === 'AbortError') return;
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsStreaming(false);
    }
  }, []);

  const stop = useCallback(() => {
    abortRef.current?.abort();
    setIsStreaming(false);
  }, []);

  const retry = useCallback(() => {
    // Re-send last query from history
  }, []);

  return { chunks, isStreaming, error, sendQuery, stop, retry };
}
```

### Accumulating Response

```typescript
function useAccumulatedResponse(chunks: AssistantChunk[]): string {
  return useMemo(() => {
    return chunks
      .filter(c => c.type === 'content')
      .sort((a, b) => a.index - b.index)
      .map(c => c.text ?? '')
      .join('');
  }, [chunks]);
}
```

---

## Conversation History Format

```typescript
interface Message {
  role: 'user' | 'assistant';
  content: string;
}
```

**Constraints**:
- Max 6 messages (3 exchanges) sent with request
- Stored in browser `localStorage` (frontend)
- Not persisted on backend

---

## Retry Logic

```typescript
const MAX_RETRIES = 2;
const RETRY_DELAYS = [1000, 3000]; // ms

async function sendWithRetry(query: string, history: Message[], attempt = 0): Promise<void> {
  try {
    for await (const chunk of streamAssistant(query, history)) {
      // Handle chunks
    }
  } catch (err) {
    if (attempt < MAX_RETRIES && isRetryable(err)) {
      await sleep(RETRY_DELAYS[attempt]);
      return sendWithRetry(query, history, attempt + 1);
    }
    throw err;
  }
}

function isRetryable(err: Error): boolean {
  // Network error, 5xx, 429
  return err instanceof TypeError || // Network error
    (err as any).status >= 500 ||
    (err as any).status === 429;
}
```

---

## Cancellation

```typescript
// User clicks "Stop"
abortController.abort();

// Cleanup
reader.releaseLock();
setIsStreaming(false);
```

---

## Testing

### Mock Stream (Development)

```typescript
async function* mockStream(query: string): AsyncGenerator<AssistantChunk> {
  const responses = [
    'I have ', '8 years ', 'of experience ', 'with .NET.'
  ];
  
  for (let i = 0; i < responses.length; i++) {
    await new Promise(r => setTimeout(r, 100));
    yield { type: 'content', text: responses[i], index: i };
  }
  yield { type: 'done', finishReason: 'STOP', index: responses.length };
}
```

### Unit Test

```typescript
it('accumulates content chunks correctly', async () => {
  const chunks: AssistantChunk[] = [];
  for await (const chunk of mockStream('test')) {
    chunks.push(chunk);
  }
  
  const accumulated = chunks
    .filter(c => c.type === 'content')
    .sort((a, b) => a.index - b.index)
    .map(c => c.text).join('');
    
  expect(accumulated).toBe('I have 8 years of experience with .NET.');
});
```

---

## Related Documents

- [API Endpoints](endpoints.md) — Full endpoint reference
- [Frontend Assistant UI](../frontend/assistant-ui.md) — UI implementation
- [AI Pipeline](../architecture/ai-pipeline.md) — Backend streaming logic