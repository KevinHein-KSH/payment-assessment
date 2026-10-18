export function formatAmount(amount: number, currency: string): string {
  return `${amount.toFixed(2)} ${currency}`;
}

// 2C2P channel codes likely for a Singapore merchant (developer.2c2p.com/docs/reference-codes-payment-scheme).
// shortcut: unlisted codes show as-is; add entries when a new channel shows up.
const PAYMENT_CHANNELS: Record<string, string> = {
  VI: "Visa",
  MA: "Mastercard",
  AM: "American Express",
  JC: "JCB",
  UP: "UnionPay",
  CP: "UnionPay",
  DI: "Discover",
  DN: "Diners",
  PN: "PayNow",
  GP: "GrabPay",
  AL: "Alipay",
  WC: "WeChat Pay",
  SH: "ShopeePay",
  AO: "Atome",
};

export function formatPaymentChannel(code: string | null): string {
  if (!code) return "-";
  return PAYMENT_CHANNELS[code] ?? code;
}
