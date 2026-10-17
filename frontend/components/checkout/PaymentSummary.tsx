import { formatAmount } from "@/lib/format";
import type { Customer, Product } from "@/lib/types";

interface PaymentSummaryProps {
  customer: Customer;
  product: Product;
}

export default function PaymentSummary({ customer, product }: PaymentSummaryProps) {
  const rows: [string, string][] = [
    ["Customer", `${customer.firstName} ${customer.lastName}`],
    ["Product", product.name],
    ["Payment type", product.paymentType],
  ];

  return (
    <section>
      <h2 className="text-sm font-semibold text-gray-900">Payment summary</h2>
      <dl className="mt-3 divide-y divide-gray-200 rounded-md border border-gray-200 text-sm">
        {rows.map(([label, value]) => (
          <div key={label} className="flex justify-between gap-4 px-4 py-2.5">
            <dt className="text-gray-500">{label}</dt>
            <dd className="text-right text-gray-900">{value}</dd>
          </div>
        ))}
        <div className="flex justify-between gap-4 bg-gray-50 px-4 py-3">
          <dt className="font-medium text-gray-900">Total</dt>
          <dd className="text-right font-semibold text-gray-900">
            {formatAmount(product.price, product.currency)}
          </dd>
        </div>
      </dl>
    </section>
  );
}
