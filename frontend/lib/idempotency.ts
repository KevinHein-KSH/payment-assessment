// Idempotency-Key lifecycle (docs/01 F1). The key lives in sessionStorage with the purchase it belongs to:
// a refresh or a second click for the same customer + product reuses it, so the backend replays instead of charging twice;
// a different customer or product is a new purchase and gets a new key; a finished payment clears it.
const STORAGE_KEY = "checkout:idempotency";

export function idempotencyKeyFor(customerId: number, productId: number): string {
  const purchase = `${customerId}:${productId}`;
  try {
    const stored = JSON.parse(sessionStorage.getItem(STORAGE_KEY) ?? "null") as { purchase: string; key: string } | null;
    if (stored?.purchase === purchase) return stored.key;
    const key = crypto.randomUUID();
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ purchase, key }));
    return key;
  } catch {
    // sessionStorage blocked (e.g. some private modes): a fresh key still protects this one request
    return crypto.randomUUID();
  }
}

export function clearIdempotencyKey(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // nothing stored, nothing to clear
  }
}
