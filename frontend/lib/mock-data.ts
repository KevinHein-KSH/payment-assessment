import type {
  Customer,
  CustomerCard,
  Product,
  TransactionDetails,
  TransactionStatus,
} from "./types";

export const mockCustomers: Customer[] = [
  { id: 1, firstName: "Alice", lastName: "Tan", email: "alice.tan@example.com" },
  { id: 2, firstName: "Ben", lastName: "Lim", email: "ben.lim@example.com" },
  { id: 3, firstName: "Chloe", lastName: "Ng", email: "chloe.ng@example.com" },
];

export const mockProducts: Product[] = [
  {
    id: 1,
    name: "Single Class Pass",
    description: "One drop-in class",
    price: 25.0,
    currency: "SGD",
    paymentType: "OneTime",
  },
  {
    id: 2,
    name: "10-Class Pack",
    description: "Ten classes, valid for 3 months",
    price: 220.0,
    currency: "SGD",
    paymentType: "OneTime",
  },
  {
    id: 3,
    name: "Monthly Membership",
    description: "Unlimited classes, billed monthly",
    price: 149.0,
    currency: "SGD",
    paymentType: "Recurring",
  },
  {
    id: 4,
    name: "Annual Membership",
    description: "Unlimited classes, billed yearly",
    price: 1490.0,
    currency: "SGD",
    paymentType: "Recurring",
  },
];

// Only customer 1 has saved cards; the others show just "Pay with a new payment method".
export const mockCardsByCustomerId: Record<number, CustomerCard[]> = {
  1: [
    { id: 1, maskedCardNo: "411111XXXXXX1111" },
    { id: 2, maskedCardNo: "512345XXXXXX2346" },
  ],
};

export const mockTransactions: Record<TransactionStatus, TransactionDetails> = {
  Succeeded: {
    invoiceNo: "INV20261017000101",
    status: "Succeeded",
    customerId: 1,
    productId: 3,
    productName: "Monthly Membership",
    amount: 149.0,
    currency: "SGD",
    paymentChannel: "CC",
    maskedCardNo: "411111XXXXXX1111",
    respCode: "0000",
    respDesc: "Success",
  },
  Failed: {
    invoiceNo: "INV20261017000102",
    status: "Failed",
    customerId: 2,
    productId: 1,
    productName: "Single Class Pass",
    amount: 25.0,
    currency: "SGD",
    paymentChannel: null,
    maskedCardNo: null,
    respCode: "4005",
    respDesc: "Do not honour",
  },
  Pending: {
    invoiceNo: "INV20261017000103",
    status: "Pending",
    customerId: 3,
    productId: 2,
    productName: "10-Class Pack",
    amount: 220.0,
    currency: "SGD",
    paymentChannel: null,
    maskedCardNo: null,
    respCode: null,
    respDesc: null,
  },
};

// Change this to preview another result state on /result/[invoiceNo].
export const MOCK_RESULT_STATE: TransactionStatus = "Succeeded";
