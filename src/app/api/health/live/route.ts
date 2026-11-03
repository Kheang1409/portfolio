import { NextResponse } from "next/server";
import { getBackendBaseUrl } from "@/lib/server/backend";

const HEALTH_CHECK_TIMEOUT_MS = 5000;

export async function GET() {
  try {
    const response = await fetch(`${getBackendBaseUrl()}/health/live`, {
      cache: "no-store",
      signal: AbortSignal.timeout(HEALTH_CHECK_TIMEOUT_MS),
    });

    if (!response.ok) {
      return NextResponse.json(
        { status: "unavailable" },
        { status: 503, headers: { "Cache-Control": "no-store" } },
      );
    }

    return NextResponse.json(
      { status: "healthy" },
      { headers: { "Cache-Control": "no-store" } },
    );
  } catch (error) {
    console.error("Backend liveness check failed.", error);
    return NextResponse.json(
      { status: "unavailable" },
      { status: 503, headers: { "Cache-Control": "no-store" } },
    );
  }
}
