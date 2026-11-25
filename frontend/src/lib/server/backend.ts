import "server-only";

export function getBackendBaseUrl(): string {
  const configuredUrl =
    process.env.BACKEND_API_URL?.trim() ||
    process.env.NEXT_PUBLIC_BACKEND_API_URL?.trim();

  if (!configuredUrl) {
    throw new Error("BACKEND_API_URL is required for server-side API requests.");
  }

  let url: URL;
  try {
    url = new URL(configuredUrl);
  } catch {
    throw new Error("BACKEND_API_URL must be an absolute HTTP or HTTPS URL.");
  }

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error("BACKEND_API_URL must use HTTP or HTTPS.");
  }

  return configuredUrl.replace(/\/+$/, "");
}
