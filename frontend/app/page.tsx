import CheckoutForm from "@/components/checkout/CheckoutForm";
import { Customer, Product } from "@/lib/types";

const API = process.env.NEXT_PUBLIC_API_BASE_URL;

export default async function CheckoutPage() {
  const [customers, products] = await Promise.all([
    fetch(`${API}/api/customers`, { cache: "no-cache" }).then((res) => res.json() as Promise<Customer[]>),
    fetch(`${API}/api/products`, { cache: "no-cache" }).then((res) => res.json() as Promise<Product[]>),
  ]);

  return (
    <main className="mx-auto w-full max-w-xl px-4 py-8 sm:py-12">
      <div className="rounded-lg border border-gray-200 bg-white p-6 sm:p-8">
        <h1 className="text-lg font-semibold text-gray-900">Checkout</h1>
        <p className="mt-1 text-sm text-gray-500">Select a customer and product to pay for.</p>
        <div className="mt-6">
          <CheckoutForm
            customers={customers}
            products={products}
          />
        </div>
      </div>
    </main>
  );
}
