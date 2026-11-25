import { NextRequest, NextResponse } from "next/server";
import { getBackendBaseUrl } from "@/lib/server/backend";

async function forward(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  try {
    const url = `${getBackendBaseUrl()}/api/donations/${path.map(encodeURIComponent).join("/")}${request.nextUrl.search}`;
    const response = await fetch(url, {
      method: request.method,
      headers: request.headers.get("content-type")
        ? { "Content-Type": request.headers.get("content-type")! }
        : undefined,
      body: request.method === "GET" || request.method === "HEAD"
        ? undefined
        : await request.arrayBuffer(),
      cache: "no-store",
      signal: AbortSignal.any([request.signal, AbortSignal.timeout(15000)]),
    });
    return new NextResponse(response.body, { status: response.status, headers: { "Cache-Control": "no-store", "Content-Type": response.headers.get("content-type") ?? "application/json" } });
  } catch (error) {
    console.error("Donation backend request failed.", error);
    return NextResponse.json({ message: "Donation service is temporarily unavailable." }, { status: 503 });
  }
}

export const GET = forward;
export const POST = forward;
