"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import FailedResult from "@/components/result/FailedResult";
import ProcessingResult from "@/components/result/ProcessingResult";
import SucceededResult from "@/components/result/SucceededResult";
import type { TransactionDetails } from "@/lib/types";

const POLL_MS = 3000;

// Status always comes from our own DB, never from the redirect query string (docs/03).
export default function ResultPage() {
  const { invoiceNo } = useParams<{ invoiceNo: string }>();
  const [transaction, setTransaction] = useState<TransactionDetails | null>(null);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    let cancelled = false;
    let timer: ReturnType<typeof setTimeout> | undefined;

    async function load() {
      try {
        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_BASE_URL}/api/transactions/${encodeURIComponent(invoiceNo)}`,
          { cache: "no-store" },
        );
        if (cancelled) return;
        if (res.status === 404) {
          setNotFound(true);
          return;
        }
        if (res.ok) {
          const data: TransactionDetails = await res.json();
          if (cancelled) return;
          setTransaction(data);
          if (data.status !== "Pending") return; // final: stop polling
        }
      } catch {
        // network blip: try again on the next tick
      }
      if (!cancelled) timer = setTimeout(load, POLL_MS);
    }

    load();
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [invoiceNo]);

  return (
    <main className="mx-auto w-full max-w-xl px-4 py-8 sm:py-12">
      <div className="rounded-lg border border-gray-200 bg-white p-6 sm:p-8">
        {notFound ? (
          <p className="text-sm text-gray-700">Transaction not found.</p>
        ) : transaction?.status === "Succeeded" ? (
          <SucceededResult transaction={transaction} />
        ) : transaction?.status === "Failed" ? (
          <FailedResult transaction={transaction} />
        ) : (
          <ProcessingResult />
        )}
      </div>
    </main>
  );
}
