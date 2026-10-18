"use client";

import { useState, useEffect } from "react";
import type {
  CheckoutRequest,
  CheckoutResponse,
  Customer,
  CustomerCard,
  Product,
} from "@/lib/types";
import { idempotencyKeyFor } from "@/lib/idempotency";
import PaymentMethodOptions from "./PaymentMethodOptions";
import PaymentSummary from "./PaymentSummary";

interface CheckoutFormProps {
  customers: Customer[];
  products: Product[];
  // From the URL; NaN or an unknown id falls back to the first entry
  initialCustomerId?: number;
  initialProductId?: number;
}

const selectClass =
  "mt-1 block w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600";

export default function CheckoutForm({
  customers,
  products,
  initialCustomerId,
  initialProductId,
}: CheckoutFormProps) {
  const [customerId, setCustomerId] = useState(
    customers.find((c) => c.id === initialCustomerId)?.id ?? customers[0].id,
  );
  const [productId, setProductId] = useState(
    products.find((p) => p.id === initialProductId)?.id ?? products[0].id,
  );
  const [cards, setCards] = useState<CustomerCard[]>([]);
  // null means "Pay with a new payment method"
  const [selectedCardId, setSelectedCardId] = useState<number | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const customer = customers.find((c) => c.id === customerId) ?? customers[0];
  const product = products.find((p) => p.id === productId) ?? products[0];

  // docs/01 F1: the key exists from page load (and per purchase); handleCheckout reads the same one
  useEffect(() => {
    idempotencyKeyFor(customerId, productId);
  }, [customerId, productId]);

  async function handleCheckout() {
    setSubmitting(true);
    setError(null);
    try {
      const body: CheckoutRequest = {
        customerId,
        productId,
        customerCardId: selectedCardId ?? undefined,
      };
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/api/checkout`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Idempotency-Key": idempotencyKeyFor(customerId, productId),
        },
        body: JSON.stringify(body),
      });
      if (res.status === 409) {
        // Another request with this key is still running (double click, second tab): don't start a new one
        setError("This payment is already being processed. Please wait a moment and try again.");
        setSubmitting(false);
        return;
      }
      if (!res.ok) throw new Error(`Checkout returned ${res.status}`);
      const data: CheckoutResponse = await res.json();
      // Pending → the hosted page (or 3DS later); a replayed purchase that already finished → straight to its result
      // (docs/03). The button stays disabled while the browser leaves.
      window.location.href =
        data.status === "Pending" && data.redirectUrl ? data.redirectUrl : `/result/${data.invoiceNo}`;
    } catch {
      setError("Checkout failed. Please try again.");
      setSubmitting(false);
    }
  }

  function handleCustomerChange(id: number) {
    setCustomerId(id);
    setSelectedCardId(null);
  }

  useEffect(() => {
    async function fetchCards() {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL}/api/customers/${customerId}/cards`,
      );
      if (!res.ok) return;
      setCards(await res.json());
    }
    fetchCards();
  }, [customerId]);

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

      {error && (
        <p role="alert" className="text-sm text-red-600">
          {error}
        </p>
      )}

      <button
        type="button"
        onClick={handleCheckout}
        disabled={submitting}
        className="w-full rounded-md bg-blue-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-gray-300 disabled:text-gray-600"
      >
        {submitting ? "Processing..." : "Checkout"}
      </button>
    </div>
  );
}
