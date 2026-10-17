import CheckoutForm from "@/components/checkout/CheckoutForm";
import { mockCardsByCustomerId, mockCustomers, mockProducts } from "@/lib/mock-data";

export default function CheckoutPage() {
  return (
    <main className="mx-auto w-full max-w-xl px-4 py-8 sm:py-12">
      <div className="rounded-lg border border-gray-200 bg-white p-6 sm:p-8">
        <h1 className="text-lg font-semibold text-gray-900">Checkout</h1>
        <p className="mt-1 text-sm text-gray-500">Select a customer and product to pay for.</p>
        <div className="mt-6">
          <CheckoutForm
            customers={mockCustomers}
            products={mockProducts}
            cardsByCustomerId={mockCardsByCustomerId}
          />
        </div>
      </div>
    </main>
  );
}
