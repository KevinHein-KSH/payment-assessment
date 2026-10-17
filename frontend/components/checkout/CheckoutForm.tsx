"use client";

import { useState } from "react";
import type { Customer, CustomerCard, Product } from "@/lib/types";
import PaymentMethodOptions from "./PaymentMethodOptions";
import PaymentSummary from "./PaymentSummary";

interface CheckoutFormProps {
  customers: Customer[];
  products: Product[];
  cardsByCustomerId: Record<number, CustomerCard[]>;
}

const selectClass =
  "mt-1 block w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600";

export default function CheckoutForm({ customers, products, cardsByCustomerId }: CheckoutFormProps) {
  const [customerId, setCustomerId] = useState(customers[0].id);
  const [productId, setProductId] = useState(products[0].id);
  // null means "Pay with a new payment method"
  const [selectedCardId, setSelectedCardId] = useState<number | null>(null);

  const customer = customers.find((c) => c.id === customerId) ?? customers[0];
  const product = products.find((p) => p.id === productId) ?? products[0];
  const cards = cardsByCustomerId[customerId] ?? [];

  function handleCustomerChange(id: number) {
    setCustomerId(id);
    setSelectedCardId(null);
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <label className="block text-sm font-medium text-gray-700">
          Customer
          <select
            value={customerId}
            onChange={(e) => handleCustomerChange(Number(e.target.value))}
            className={selectClass}
          >
            {customers.map((c) => (
              <option key={c.id} value={c.id}>
                {c.firstName} {c.lastName}
              </option>
            ))}
          </select>
        </label>

        <label className="block text-sm font-medium text-gray-700">
          Product
          <select
            value={productId}
            onChange={(e) => setProductId(Number(e.target.value))}
            className={selectClass}
          >
            {products.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <PaymentSummary customer={customer} product={product} />

      <PaymentMethodOptions
        cards={cards}
        selectedCardId={selectedCardId}
        onChange={setSelectedCardId}
      />

      <button
        type="button"
        className="w-full rounded-md bg-blue-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-blue-700"
      >
        Checkout
      </button>

      <div className="rounded-md border border-dashed border-gray-300 p-4">
        <p className="mb-3 text-xs font-medium uppercase tracking-wide text-gray-500">
          Preview: disabled state while the request is in flight
        </p>
        <button
          type="button"
          disabled
          className="w-full cursor-not-allowed rounded-md bg-gray-300 px-4 py-2.5 text-sm font-medium text-gray-600"
        >
          Processing...
        </button>
      </div>
    </div>
  );
}
