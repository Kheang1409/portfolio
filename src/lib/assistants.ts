import type {
  AssistantContext,
  AssistantProblem,
  AssistantRequest,
  AssistantResponseMeta,
  AssistantStreamEvent,
  ConversationMessage,
} from "./types";

export type AssistantMsg = { sender: "user" | "bot"; text: string };

export type AssistantStreamHandlers = {
  onDelta?: (event: Extract<AssistantStreamEvent, { type: "delta" }>) => void;
  onCompleted?: (
    event: Extract<AssistantStreamEvent, { type: "completed" }>,
  ) => void;
  onError?: (event: Extract<AssistantStreamEvent, { type: "error" }>) => void;
};

function createMessageId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function resolveBackendUrl(path: string): string {
  const envBackend = process.env.NEXT_PUBLIC_BACKEND_API_URL;
  const fallbackDefault = "http://localhost:5000";
  const backend =
    envBackend && envBackend.trim() !== "" ? envBackend : fallbackDefault;

  return backend ? `${backend.replace(/\/$/, "")}${path}` : path;
}

async function readProblem(res: Response): Promise<AssistantProblem> {
  const contentType = res.headers.get("content-type") ?? "";
  try {
    if (contentType.includes("application/json")) {
      const json = (await res.json()) as Record<string, unknown>;
      const ext = (json.extensions ?? json) as Record<string, unknown>;
      return {
        title: typeof json.title === "string" ? json.title : undefined,
        detail: typeof json.detail === "string" ? json.detail : undefined,
        status: typeof json.status === "number" ? json.status : res.status,
        errorCode:
          typeof ext.errorCode === "string"
            ? ext.errorCode
            : typeof json.errorCode === "string"
              ? json.errorCode
              : undefined,
        retryable:
          typeof ext.retryable === "boolean"
            ? ext.retryable
            : typeof json.retryable === "boolean"
              ? json.retryable
              : undefined,
      };
    }

    const text = await res.text();
    return {
      status: res.status,
      detail: text,
    };
  } catch {
    return { status: res.status };
  }
}

export async function askAssistant(
  message: string,
  history: ConversationMessage[] = [],
  context?: AssistantContext,
): Promise<AssistantResponseMeta> {
  const payload: AssistantRequest = { message, history, context };
  const url = resolveBackendUrl("/api/assistants/ask");

  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => null);
    throw new Error(text ?? `Request failed with status ${res.status}`);
  }

  const text = await res.text();
  const modelUsed = res.headers.get("X-AI-Model-Used") ?? undefined;
  const latencyRaw = res.headers.get("X-AI-Latency-Ms");
  const fallbackRaw = res.headers.get("X-AI-Fallback-Used");

  const latencyMs = latencyRaw ? Number(latencyRaw) : undefined;
  const fallbackUsed = fallbackRaw
    ? fallbackRaw.toLowerCase() === "true"
    : undefined;

  return {
    text,
    modelUsed,
    latencyMs: Number.isFinite(latencyMs) ? latencyMs : undefined,
    fallbackUsed,
  };
}

export async function streamAssistant(
  message: string,
  history: ConversationMessage[] = [],
  context: AssistantContext | undefined,
  handlers: AssistantStreamHandlers,
  signal?: AbortSignal,
): Promise<void> {
  const payload: AssistantRequest = { message, history, context };
  const url = resolveBackendUrl("/api/assistants/stream");

  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
    signal,
  });

  if (!res.ok) {
    const problem = await readProblem(res);
    const shouldFallback =
      res.status === 404 ||
      res.status === 409 ||
      problem.errorCode === "STREAMING_DISABLED";

    if (shouldFallback) {
      const buffered = await askAssistant(message, history, context);
      const messageId = createMessageId();
      handlers.onDelta?.({
        type: "delta",
        messageId,
        text: buffered.text,
        modelUsed: buffered.modelUsed,
        fallbackUsed: buffered.fallbackUsed,
      });
      handlers.onCompleted?.({
        type: "completed",
        messageId,
        modelUsed: buffered.modelUsed,
        fallbackUsed: buffered.fallbackUsed,
        latencyMs: buffered.latencyMs,
      });
      return;
    }

    handlers.onError?.({
      type: "error",
      messageId: createMessageId(),
      errorCode: problem.errorCode ?? `HTTP_${res.status}`,
      retryable: problem.retryable ?? (res.status === 429 || res.status >= 500),
      errorMessage:
        problem.detail ??
        problem.title ??
        `Request failed with status ${res.status}`,
    });
    return;
  }

  if (!res.body) {
    handlers.onError?.({
      type: "error",
      messageId: createMessageId(),
      errorCode: "STREAM_UNAVAILABLE",
      retryable: true,
      errorMessage: "Streaming response body is unavailable.",
    });
    return;
  }

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  try {
    while (true) {
      const { value, done } = await reader.read();
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      let lineBreakIndex = buffer.indexOf("\n");

      while (lineBreakIndex >= 0) {
        const line = buffer.slice(0, lineBreakIndex).trim();
        buffer = buffer.slice(lineBreakIndex + 1);
        lineBreakIndex = buffer.indexOf("\n");

        if (!line) {
          continue;
        }

        try {
          const event = JSON.parse(line) as AssistantStreamEvent;
          if (event.type === "delta") {
            handlers.onDelta?.(event);
            continue;
          }

          if (event.type === "completed") {
            handlers.onCompleted?.(event);
            continue;
          }

          if (event.type === "error") {
            handlers.onError?.(event);
          }
        } catch {
          handlers.onError?.({
            type: "error",
            messageId: createMessageId(),
            errorCode: "STREAM_PARSE_ERROR",
            retryable: true,
            errorMessage: "Failed to parse streaming payload.",
          });
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}
