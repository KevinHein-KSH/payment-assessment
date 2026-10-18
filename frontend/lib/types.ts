// Shapes mirror the API in docs/02-technical.md.

export type PaymentType = "OneTime" | "Recurring";

export type TransactionStatus = "Pending" | "Succeeded" | "Failed";

// GET /api/customers
export interface Customer {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

// GET /api/products
export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  currency: string;
  paymentType: PaymentType;
}

// GET /api/customers/{id}/cards — masked only; the customerToken never reaches the browser
export interface CustomerCard {
  id: number;
  maskedCardNo: string;
}

// POST /api/checkout body (Idempotency-Key goes in the header)
export interface CheckoutRequest {
  customerId: number;
  productId: number;
  customerCardId?: number;
}

// POST /api/checkout response
export interface CheckoutResponse {
  invoiceNo: string;
  status: TransactionStatus;
  redirectUrl: string | null;
}

// GET /api/transactions/{invoiceNo}
export interface TransactionDetails {
  invoiceNo: string;
  status: TransactionStatus;
  customerId: number;
  productId: number;
  productName: string;
  amount: number;
  currency: string;
  paymentChannel: string | null;
  maskedCardNo: string | null;
  respCode: string | null;
  respDesc: string | null;
}
