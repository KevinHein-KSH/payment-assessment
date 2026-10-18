import { Suspense } from "react";
import { connection } from "next/server";
import CheckoutForm from "@/components/checkout/CheckoutForm";
import { Customer, Product } from "@/lib/types";

const API = process.env.NEXT_PUBLIC_API_BASE_URL;

type SearchParams = Promise<{ customerId?: string; productId?: string }>;

// ?customerId=&productId= preselects the dropdowns (Go back from a failed payment, docs/03)
export default function CheckoutPage({ searchParams }: { searchParams: SearchParams }) {
  return (
    <main className="mx-auto w-full max-w-xl px-4 py-8 sm:py-12">
      <div className="rounded-lg border border-gray-200 bg-white p-6 sm:p-8">
        <h1 className="text-lg font-semibold text-gray-900">Checkout</h1>
        <p className="mt-1 text-sm text-gray-500">Select a customer and product to pay for.</p>
        <div className="mt-6">
          {/* Cache Components: request-time data must stream inside Suspense; the heading above is the static shell */}
          <Suspense fallback={<p className="text-sm text-gray-500">Loading...</p>}>
            <CheckoutData searchParams={searchParams} />
          </Suspense>
        </div>
      </div>
    </main>
  );
}

async function CheckoutData({ searchParams }: { searchParams: SearchParams }) {
  // Customers and products are live API data: fetch per request, never at build time
  await connection();
  const [customers, products] = await Promise.all([
    fetch(`${API}/api/customers`, { cache: "no-cache" }).then((res) => res.json() as Promise<Customer[]>),
    fetch(`${API}/api/products`, { cache: "no-cache" }).then((res) => res.json() as Promise<Product[]>),
  ]);

  const { customerId, productId } = await searchParams;

  return (
    <CheckoutForm
      customers={customers}
      products={products}
      initialCustomerId={Number(customerId)}
      initialProductId={Number(productId)}
    />
  );
}
