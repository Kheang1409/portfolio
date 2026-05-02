import type { AiMetadata, ApiProblem } from "@/lib/api/types";

const DEFAULT_BACKEND_URL = "http://localhost:5000";
const DEFAULT_TIMEOUT_MS = 20_000;
const DEFAULT_RETRIES = 2;
const BASE_RETRY_DELAY_MS = 300;

const RETRYABLE_STATUS = new Set([408, 425, 429, 500, 502, 503, 504]);

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly retryable: boolean;
  readonly details?: ApiProblem;

  constructor(
    message: string,
    options?: {
      status?: number;
      code?: string;
      retryable?: boolean;
      details?: ApiProblem;
    },
  ) {
    super(message);
    this.name = "ApiError";
    this.status = options?.status ?? 0;
    this.code = options?.code;
    this.retryable = options?.retryable ?? false;
    this.details = options?.details;
  }
}

export type ApiRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  body?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
  timeoutMs?: number;
  retries?: number;
  useAssistantProxy?: boolean;
};

function getBackendBaseUrl(): string {
  const fromEnv = process.env.NEXT_PUBLIC_BACKEND_API_URL;
  return fromEnv && fromEnv.trim()
    ? fromEnv.trim().replace(/\/$/, "")
    : DEFAULT_BACKEND_URL;
}

function resolveUrl(path: string, useAssistantProxy: boolean): string {
  if (useAssistantProxy) {
    return path;
  }

  const cleanPath = path.startsWith("/") ? path : `/${path}`;
  return `${getBackendBaseUrl()}${cleanPath}`;
}

function toBooleanHeader(value: string | null): boolean | undefined {
  if (!value) {
    return undefined;
  }

  if (value.toLowerCase() === "true") {
    return true;
  }

  if (value.toLowerCase() === "false") {
    return false;
  }

  return undefined;
}

function toNumberHeader(value: string | null): number | undefined {
  if (!value) {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

export function readAiMetadata(headers: Headers): AiMetadata {
  return {
    modelUsed: headers.get("X-AI-Model-Used") ?? undefined,
    latencyMs: toNumberHeader(headers.get("X-AI-Latency-Ms")),
    fallbackUsed: toBooleanHeader(headers.get("X-AI-Fallback-Used")),
    contextEnhanced: toBooleanHeader(headers.get("X-AI-Context-Used")),
    cachedResponse: toBooleanHeader(headers.get("X-AI-Cached-Response")),
  };
}

function mergeSignals(externalSignal?: AbortSignal): {
  signal: AbortSignal;
  cleanup: () => void;
  abort: () => void;
} {
  const timeoutController = new AbortController();
  const linkedController = new AbortController();

  const onExternalAbort = () => {
    linkedController.abort(externalSignal?.reason);
  };

  const onTimeoutAbort = () => {
    linkedController.abort(timeoutController.signal.reason);
  };

  if (externalSignal) {
    if (externalSignal.aborted) {
      linkedController.abort(externalSignal.reason);
    } else {
      externalSignal.addEventListener("abort", onExternalAbort, { once: true });
    }
  }

  timeoutController.signal.addEventListener("abort", onTimeoutAbort, {
    once: true,
  });

  return {
    signal: linkedController.signal,
    cleanup: () => {
      if (externalSignal) {
        externalSignal.removeEventListener("abort", onExternalAbort);
      }
      timeoutController.signal.removeEventListener("abort", onTimeoutAbort);
    },
    abort: () => timeoutController.abort(),
  };
}

async function parseProblem(response: Response): Promise<ApiProblem> {
  const contentType = response.headers.get("content-type") ?? "";

  try {
    if (contentType.includes("application/json")) {
      const json = (await response.json()) as Record<string, unknown>;
      const ext = (json.extensions ?? json) as Record<string, unknown>;
      return {
        title: typeof json.title === "string" ? json.title : undefined,
        detail: typeof json.detail === "string" ? json.detail : undefined,
        status: typeof json.status === "number" ? json.status : response.status,
        message: typeof json.message === "string" ? json.message : undefined,
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

    const text = await response.text();
    return {
      status: response.status,
      detail: text || undefined,
    };
  } catch {
    return { status: response.status };
  }
}

function isRetryableStatus(status: number): boolean {
  return RETRYABLE_STATUS.has(status);
}

function isAbortError(error: unknown): error is DOMException {
  return error instanceof DOMException && error.name === "AbortError";
}

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

async function execute(options: ApiRequestOptions): Promise<Response> {
  const method = options.method ?? "POST";
  const timeoutMs = Math.max(1_000, options.timeoutMs ?? DEFAULT_TIMEOUT_MS);
  const retries = Math.max(0, options.retries ?? DEFAULT_RETRIES);
  const url = resolveUrl(options.path, options.useAssistantProxy ?? false);

  let attempt = 0;
  let lastError: unknown;

  while (attempt <= retries) {
    const { signal, cleanup, abort } = mergeSignals(options.signal);
    let timeoutId: ReturnType<typeof setTimeout> | null = null;
    let timedOut = false;

    try {
      timeoutId = setTimeout(() => {
        timedOut = true;
        abort();
      }, timeoutMs);

      const response = await fetch(url, {
        method,
        headers: {
          "Content-Type": "application/json",
          ...(options.headers ?? {}),
        },
        body:
          options.body !== undefined ? JSON.stringify(options.body) : undefined,
        signal,
      });

      if (!response.ok) {
        const problem = await parseProblem(response);
        const retryable =
          problem.retryable ?? isRetryableStatus(response.status);

        if (retryable && attempt < retries) {
          const delay = BASE_RETRY_DELAY_MS * 2 ** attempt;
          await wait(delay);
          attempt += 1;
          continue;
        }

        throw new ApiError(
          problem.detail ??
            problem.message ??
            problem.title ??
            `Request failed with status ${response.status}`,
          {
            status: response.status,
            code: problem.errorCode,
            retryable,
            details: problem,
          },
        );
      }

      return response;
    } catch (error) {
      lastError = error;

      if (isAbortError(error)) {
        if (timedOut) {
          throw new ApiError("Request timed out.", {
            code: "TIMEOUT",
            retryable: true,
          });
        }

        throw error;
      }

      if (attempt < retries) {
        const delay = BASE_RETRY_DELAY_MS * 2 ** attempt;
        await wait(delay);
        attempt += 1;
        continue;
      }
    } finally {
      if (timeoutId) {
        clearTimeout(timeoutId);
      }
      cleanup();
    }

    attempt += 1;
  }

  if (lastError instanceof ApiError) {
    throw lastError;
  }

  if (isAbortError(lastError)) {
    throw lastError;
  }

  throw new ApiError("Network request failed.", { retryable: true });
}

export async function requestText(
  options: ApiRequestOptions,
): Promise<{ text: string; metadata: AiMetadata; headers: Headers }> {
  const response = await execute(options);
  const text = await response.text();

  return {
    text,
    metadata: readAiMetadata(response.headers),
    headers: response.headers,
  };
}

export async function requestJson<T>(
  options: ApiRequestOptions,
): Promise<{ data: T; metadata: AiMetadata; headers: Headers }> {
  const response = await execute(options);
  const data = (await response.json()) as T;

  return {
    data,
    metadata: readAiMetadata(response.headers),
    headers: response.headers,
  };
}

export async function requestStream(
  options: ApiRequestOptions,
): Promise<Response> {
  return execute(options);
}
