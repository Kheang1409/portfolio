import { NextRequest, NextResponse } from "next/server";
import type { AssistantRequest } from "@/lib/api/types";

function backendBaseUrl(): string | null {
  const fromEnv = process.env.NEXT_PUBLIC_BACKEND_API_URL;
  if (fromEnv && fromEnv.trim()) {
    return fromEnv.trim().replace(/\/$/, "");
  }

  return null;
}

function pickForwardHeaders(request: NextRequest): HeadersInit {
  const headers = new Headers();
  const contentType = request.headers.get("content-type");

  if (contentType) {
    headers.set("content-type", contentType);
  }

  const apiKey = request.headers.get("x-api-key");
  if (apiKey) {
    headers.set("x-api-key", apiKey);
  }

  return headers;
}

function pickResponseHeaders(source: Headers): Headers {
  const headers = new Headers();
  const passThrough = [
    "content-type",
    "cache-control",
    "x-ai-model-used",
    "x-ai-latency-ms",
    "x-ai-fallback-used",
    "x-ai-context-used",
    "x-ai-cached-response",
  ];

  for (const key of passThrough) {
    const value = source.get(key);
    if (value) {
      headers.set(key, value);
    }
  }

  return headers;
}

export async function POST(request: NextRequest) {
  let payload: AssistantRequest;
  try {
    payload = await request.json();
    if (
      !payload ||
      typeof payload.message !== "string" ||
      !payload.message.trim() ||
      payload.message.length > 2000
    ) {
      return NextResponse.json(
        { message: "Please enter a question of up to 2,000 characters." },
        { status: 400 },
      );
    }
  } catch {
    return NextResponse.json({ message: "Invalid request." }, { status: 400 });
  }

  const history = Array.isArray(payload.history)
    ? payload.history
        .filter(
          (m) =>
            m &&
            (m.role === "user" || m.role === "assistant") &&
            typeof m.content === "string",
        )
        .slice(-20)
        .map((m) => ({ ...m, content: m.content.slice(0, 8000) }))
    : [];
  const baseUrl = backendBaseUrl();
  if (!baseUrl) return NextResponse.json({ message: "Assistant service is not configured." }, { status: 503 });
  try {
    const response = await fetch(`${baseUrl}/api/assistant`, {
      method: "POST",
      headers: pickForwardHeaders(request),
      body: JSON.stringify({
        ...payload,
        history,
        context: payload.context,
      }),
      cache: "no-store",
      signal: AbortSignal.any([request.signal, AbortSignal.timeout(40000)]),
    });
    if (!response.ok)
      return NextResponse.json(
        {
          message:
            response.status === 429
              ? "Please wait a moment before retrying."
              : "The assistant is temporarily unavailable.",
          errorCode:
            response.status === 429 ? "RATE_LIMITED" : "ASSISTANT_UNAVAILABLE",
        },
        { status: response.status },
      );
    return new NextResponse(response.body, {
      status: response.status,
      headers: pickResponseHeaders(response.headers),
    });
  } catch {
    return NextResponse.json(
      {
        message:
          "The assistant is temporarily unavailable. Please try again or use the contact form.",
        errorCode: "ASSISTANT_UNAVAILABLE",
      },
      { status: 503 },
    );
  }
}
