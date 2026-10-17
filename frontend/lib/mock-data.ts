import type { TransactionDetails, TransactionStatus } from "./types";

// Result page only; removed once /result reads GET /api/transactions/{invoiceNo} (Phase 3).
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
