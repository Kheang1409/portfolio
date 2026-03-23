export type VisitorTrackPayload = {
  sessionId: string;
  path: string;
  referrer?: string;
  userAgent?: string;
  deviceType?: string;
  browser?: string;
  operatingSystem?: string;
  timezone?: string;
  language?: string;
  screenWidth?: number;
  screenHeight?: number;
  viewportWidth?: number;
  viewportHeight?: number;
  platform?: string;
  networkType?: string;
};

const SESSION_KEY = "visitor_session_id";

function getBackendBaseUrl(): string {
  const envBackend = process.env.NEXT_PUBLIC_BACKEND_API_URL;
  return envBackend && envBackend.trim().length > 0
    ? envBackend.replace(/\/$/, "")
    : "http://localhost:5000";
}

function getOrCreateSessionId(): string {
  if (typeof window === "undefined") {
    return "server";
  }

  const existing = window.localStorage.getItem(SESSION_KEY);
  if (existing && existing.trim().length > 0) {
    return existing;
  }

  const generated = crypto.randomUUID().replace(/-/g, "");
  window.localStorage.setItem(SESSION_KEY, generated);
  return generated;
}

function inferDeviceType(userAgent: string): string {
  const ua = userAgent.toLowerCase();
  if (ua.includes("tablet") || ua.includes("ipad")) return "tablet";
  if (ua.includes("mobi") || ua.includes("android")) return "mobile";
  return "desktop";
}

function inferBrowser(userAgent: string): string {
  const ua = userAgent.toLowerCase();
  if (ua.includes("edg/")) return "Edge";
  if (ua.includes("chrome/")) return "Chrome";
  if (ua.includes("safari/") && !ua.includes("chrome/")) return "Safari";
  if (ua.includes("firefox/")) return "Firefox";
  if (ua.includes("opr/") || ua.includes("opera")) return "Opera";
  return "unknown";
}

function inferOs(userAgent: string): string {
  const ua = userAgent.toLowerCase();
  if (ua.includes("windows")) return "Windows";
  if (ua.includes("mac os") || ua.includes("macintosh")) return "macOS";
  if (ua.includes("android")) return "Android";
  if (ua.includes("iphone") || ua.includes("ipad") || ua.includes("ios"))
    return "iOS";
  if (ua.includes("linux")) return "Linux";
  return "unknown";
}

export function buildVisitorPayload(): VisitorTrackPayload {
  if (typeof window === "undefined") {
    return {
      sessionId: "server",
      path: "/",
    };
  }

  const userAgent = window.navigator.userAgent;
  const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;

  return {
    sessionId: getOrCreateSessionId(),
    path: `${window.location.pathname}${window.location.search}`,
    referrer: document.referrer || undefined,
    userAgent,
    deviceType: inferDeviceType(userAgent),
    browser: inferBrowser(userAgent),
    operatingSystem: inferOs(userAgent),
    timezone,
    language: window.navigator.language,
    screenWidth: window.screen.width,
    screenHeight: window.screen.height,
    viewportWidth: window.innerWidth,
    viewportHeight: window.innerHeight,
    platform: window.navigator.platform,
    networkType: (
      window.navigator as Navigator & {
        connection?: { effectiveType?: string };
      }
    ).connection?.effectiveType,
  };
}

export async function trackVisit(payload: VisitorTrackPayload): Promise<void> {
  const url = `${getBackendBaseUrl()}/api/visits`;

  try {
    await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
      keepalive: true,
      cache: "no-store",
    });
  } catch {
    // Best-effort telemetry should never break user navigation.
  }
}
