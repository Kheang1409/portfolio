/**
 * Session management utility for persisting conversation across browser sessions.
 * Stores a stable sessionId in localStorage for consistent conversation memory.
 */

const SESSION_ID_STORAGE_KEY = "kai_assistant_session_id";

/**
 * Generate a new unique session ID
 */
function generateSessionId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

/**
 * Get or create a persistent session ID for this browser
 * Session ID is stored in localStorage and reused across page refreshes
 * @returns Stable session ID
 */
export function getOrCreateSessionId(): string {
  // Only run on client side
  if (typeof window === "undefined") {
    return generateSessionId();
  }

  try {
    // Try to retrieve existing session ID
    const existing = localStorage.getItem(SESSION_ID_STORAGE_KEY);
    if (existing && existing.trim().length > 0) {
      return existing;
    }

    // Generate and store new session ID
    const newSessionId = generateSessionId();
    localStorage.setItem(SESSION_ID_STORAGE_KEY, newSessionId);
    return newSessionId;
  } catch {
    // Fallback if localStorage is unavailable
    return generateSessionId();
  }
}

/**
 * Clear the stored session ID (e.g., on logout)
 */
export function clearSessionId(): void {
  try {
    localStorage.removeItem(SESSION_ID_STORAGE_KEY);
  } catch {
    // Silently fail if localStorage is unavailable
  }
}
