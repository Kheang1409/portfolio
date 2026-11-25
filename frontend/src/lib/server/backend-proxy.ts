import "server-only";
import { getBackendBaseUrl } from "@/lib/server/backend";

export async function forwardBackendRequest(
  request: Request,
  path: string,
  serviceName: string,
): Promise<Response> {
  try {
    const requestHeaders = new Headers();
    const contentType = request.headers.get("content-type");
    if (contentType) requestHeaders.set("content-type", contentType);

    const response = await fetch(`${getBackendBaseUrl()}${path}`, {
      method: request.method,
      headers: requestHeaders,
      body:
        request.method === "GET" || request.method === "HEAD"
          ? undefined
          : await request.arrayBuffer(),
      cache: "no-store",
      signal: AbortSignal.any([
        request.signal,
        AbortSignal.timeout(20_000),
      ]),
    });

    const headers = new Headers({ "Cache-Control": "no-store" });
    const responseType = response.headers.get("content-type");
    const retryAfter = response.headers.get("retry-after");
    if (responseType) headers.set("Content-Type", responseType);
    if (retryAfter) headers.set("Retry-After", retryAfter);

    return new Response(response.body, {
      status: response.status,
      headers,
    });
  } catch (error) {
    console.error(`${serviceName} backend request failed.`, error);
    return Response.json(
      { message: `${serviceName} is temporarily unavailable. Please try again.` },
      { status: 503, headers: { "Cache-Control": "no-store" } },
    );
  }
}
