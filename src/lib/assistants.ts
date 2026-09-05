import { ApiError, requestStream } from "@/lib/api/client";
import type { AssistantRequest, StreamChunk } from "@/lib/api/types";

const DEBUG_AI = process.env.NEXT_PUBLIC_DEBUG_AI === "true";

export type AssistantStreamHandlers = {
  onDelta?: (event: Extract<StreamChunk, { type: "delta" }>) => void;
  onCompleted?: (event: Extract<StreamChunk, { type: "completed" }>) => void;
  onError?: (event: Extract<StreamChunk, { type: "error" }>) => void;
};

export type AssistantRequestOptions = {
  signal?: AbortSignal;
  timeoutMs?: number;
  retries?: number;
};

function createMessageId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function debugLog(scope: string, payload?: unknown) {
  if (!DEBUG_AI) {
    return;
  }

  if (payload === undefined) {
    console.info(`[AI:${scope}]`);
    return;
  }

  console.info(`[AI:${scope}]`, payload);
}

function createErrorChunk(
  error: unknown,
): Extract<StreamChunk, { type: "error" }> {
  if (error instanceof ApiError) {
    return {
      type: "error",
      messageId: createMessageId(),
      errorCode:
        error.code ?? (error.status ? `HTTP_${error.status}` : "API_ERROR"),
      retryable: error.retryable,
      errorMessage: error.message,
    };
  }

  if (error instanceof DOMException && error.name === "AbortError") {
    return {
      type: "error",
      messageId: createMessageId(),
      errorCode: "REQUEST_ABORTED",
      retryable: false,
      errorMessage: "Request cancelled.",
    };
  }

  return {
    type: "error",
    messageId: createMessageId(),
    errorCode: "NETWORK_ERROR",
    retryable: true,
    errorMessage: "Network request failed.",
  };
}

async function parseNdjsonStream(
  response: Response,
  handlers: AssistantStreamHandlers,
  signal?: AbortSignal,
): Promise<void> {
  if (!response.body) {
    throw new ApiError("Streaming response body is unavailable.", {
      code: "STREAM_UNAVAILABLE",
      retryable: true,
    });
  }

  const reader = response.body.getReader();
  const cancel = () => {
    void reader.cancel().catch(() => undefined);
  };
  signal?.addEventListener("abort", cancel, { once: true });
  if (signal?.aborted) cancel();
  const decoder = new TextDecoder();
  let buffer = "";

  try {
    while (true) {
      if (signal?.aborted)
        throw new DOMException("Request cancelled.", "AbortError");
      const { value, done } = await reader.read();
      if (signal?.aborted)
        throw new DOMException("Request cancelled.", "AbortError");
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });

      let lineEnd = buffer.indexOf("\n");
      while (lineEnd >= 0) {
        const rawLine = buffer.slice(0, lineEnd);
        buffer = buffer.slice(lineEnd + 1);
        lineEnd = buffer.indexOf("\n");

        const line = rawLine.trim();
        if (!line) {
          continue;
        }

        try {
          const chunk = JSON.parse(line) as StreamChunk;
          debugLog(`ndjson:${chunk.type}`, chunk);

          if (chunk.type === "delta") {
            handlers.onDelta?.(chunk);
          } else if (chunk.type === "completed") {
            handlers.onCompleted?.(chunk);
          } else {
            handlers.onError?.(chunk);
          }
        } catch {
          handlers.onError?.({
            type: "error",
            messageId: createMessageId(),
            errorCode: "NDJSON_PARSE_ERROR",
            retryable: true,
            errorMessage: "Failed to parse assistant stream.",
          });
        }
      }
    }

    const tail = buffer.trim();
    if (tail) {
      try {
        const chunk = JSON.parse(tail) as StreamChunk;
        debugLog(`ndjson:${chunk.type}`, chunk);
        if (chunk.type === "delta") {
          handlers.onDelta?.(chunk);
        } else if (chunk.type === "completed") {
          handlers.onCompleted?.(chunk);
        } else {
          handlers.onError?.(chunk);
        }
      } catch {
        handlers.onError?.({
          type: "error",
          messageId: createMessageId(),
          errorCode: "NDJSON_PARSE_ERROR",
          retryable: true,
          errorMessage: "Failed to parse assistant stream.",
        });
      }
    }
  } finally {
    signal?.removeEventListener("abort", cancel);
    reader.releaseLock();
  }
}

export async function streamAssistant(
  payload: AssistantRequest,
  handlers: AssistantStreamHandlers,
  options: AssistantRequestOptions = {},
): Promise<void> {
  debugLog("assistant:request", payload);
  const controller = new AbortController();
  let timedOut = false;
  const cancel = () => controller.abort();
  options.signal?.addEventListener("abort", cancel, { once: true });
  if (options.signal?.aborted) cancel();
  const timer = setTimeout(() => {
    timedOut = true;
    controller.abort();
  }, options.timeoutMs ?? 45000);
  try {
    const response = await requestStream({
      path: "/api/assistant",
      body: payload,
      signal: controller.signal,
      timeoutMs: options.timeoutMs,
      retries: options.retries,
      useAssistantProxy: true,
    });
    await parseNdjsonStream(response, handlers, controller.signal);
  } catch (error) {
    if (options.signal?.aborted)
      throw new DOMException("Request cancelled.", "AbortError");
    handlers.onError?.(
      createErrorChunk(
        timedOut
          ? new ApiError("Request timed out.", {
              code: "TIMEOUT",
              retryable: true,
            })
          : error,
      ),
    );
  } finally {
    clearTimeout(timer);
    options.signal?.removeEventListener("abort", cancel);
  }
}
