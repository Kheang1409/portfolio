import { NextRequest, NextResponse } from "next/server";

const backend = (process.env.BACKEND_API_URL ?? process.env.NEXT_PUBLIC_BACKEND_API_URL)?.replace(/\/$/, "");

async function forward(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  if (!backend) return NextResponse.json({ message: "Donation service is not configured." }, { status: 503 });
  const { path } = await context.params;
  const url = `${backend}/api/donations/${path.map(encodeURIComponent).join("/")}${request.nextUrl.search}`;
  try {
    const response = await fetch(url, {
      method: request.method,
      headers: request.headers.get("content-type") ? { "Content-Type": request.headers.get("content-type")! } : undefined,
      body: request.method === "GET" ? undefined : await request.text(),
      cache: "no-store",
      signal: AbortSignal.any([request.signal, AbortSignal.timeout(15000)]),
    });
    return new NextResponse(response.body, { status: response.status, headers: { "Cache-Control": "no-store", "Content-Type": response.headers.get("content-type") ?? "application/json" } });
  } catch {
    return NextResponse.json({ message: "Donation service is temporarily unavailable." }, { status: 503 });
  }
}

export const GET = forward;
export const POST = forward;
