const STORAGE_KEY = "portfolio.donation-checkout-attempt";
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function getOrCreateDonationAttemptId(): string {
  const saved = window.sessionStorage.getItem(STORAGE_KEY);
  if (saved && UUID_PATTERN.test(saved)) return saved;

  const attemptId = window.crypto.randomUUID();
  window.sessionStorage.setItem(STORAGE_KEY, attemptId);
  return attemptId;
}

export function createDonationAttemptId(): string {
  const attemptId = window.crypto.randomUUID();
  window.sessionStorage.setItem(STORAGE_KEY, attemptId);
  return attemptId;
}

export function clearDonationAttemptId(): void {
  window.sessionStorage.removeItem(STORAGE_KEY);
}
