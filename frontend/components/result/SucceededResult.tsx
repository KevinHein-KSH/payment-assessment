import Link from "next/link";
import { formatAmount, formatPaymentChannel } from "@/lib/format";
import type { TransactionDetails } from "@/lib/types";

interface SucceededResultProps {
  transaction: TransactionDetails;
}

export default function SucceededResult({ transaction }: SucceededResultProps) {
  const rows: [string, string][] = [
    ["Product", transaction.productName],
    ["Amount", formatAmount(transaction.amount, transaction.currency)],
    ["Invoice number", transaction.invoiceNo],
    ["Payment method", formatPaymentChannel(transaction.paymentChannel)],
    ["Card / account", transaction.maskedCardNo ?? "-"],
  ];

  return (
    <div>
      <h1 className="text-lg font-semibold text-gray-900">Payment succeeded</h1>
      <p className="mt-1 text-sm text-gray-500">Thank you. Your payment has been received.</p>
      <dl className="mt-6 divide-y divide-gray-200 rounded-md border border-gray-200 text-sm">
        {rows.map(([label, value]) => (
          <div key={label} className="flex justify-between gap-4 px-4 py-2.5">
            <dt className="text-gray-500">{label}</dt>
            <dd className="break-all text-right text-gray-900">{value}</dd>
          </div>
        ))}
      </dl>
      {/* Keep the customer selected so a card saved by this payment shows up straight away */}
      <Link
        href={`/?customerId=${transaction.customerId}`}
        className="mt-6 block w-full rounded-md bg-blue-600 px-4 py-2.5 text-center text-sm font-medium text-white hover:bg-blue-700"
      >
        Back to checkout
      </Link>
    </div>
  );
}
