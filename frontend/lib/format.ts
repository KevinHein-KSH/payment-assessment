export function formatAmount(amount: number, currency: string): string {
  return `${amount.toFixed(2)} ${currency}`;
}
