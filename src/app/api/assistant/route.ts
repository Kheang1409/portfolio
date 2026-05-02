import { NextRequest, NextResponse } from "next/server";

const FALLBACK_BACKEND_URL = "http://localhost:5000";

function backendBaseUrl(): string {
  const fromEnv = process.env.NEXT_PUBLIC_BACKEND_API_URL;
  if (fromEnv && fromEnv.trim()) {
    return fromEnv.trim().replace(/\/$/, "");
  }

  return FALLBACK_BACKEND_URL;
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
  const targetUrl = `${backendBaseUrl()}/api/assistant`;

  const response = await fetch(targetUrl, {
    method: "POST",
    headers: pickForwardHeaders(request),
    body: await request.text(),
    cache: "no-store",
  });

  return new NextResponse(response.body, {
    status: response.status,
    headers: pickResponseHeaders(response.headers),
  });
}
