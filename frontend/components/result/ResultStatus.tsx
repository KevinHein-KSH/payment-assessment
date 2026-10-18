"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import FailedResult from "./FailedResult";
import ProcessingResult from "./ProcessingResult";
import SucceededResult from "./SucceededResult";
import { clearIdempotencyKey } from "@/lib/idempotency";
import type { TransactionDetails } from "@/lib/types";

const POLL_MS = 3000;

// Status always comes from our own DB, never from the redirect query string (docs/03).
export default function ResultStatus() {
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
          if (data.status !== "Pending") {
            clearIdempotencyKey(); // purchase finished: Go back or a new checkout starts with a new key
            return; // final: stop polling
          }
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

  if (notFound) return <p className="text-sm text-gray-700">Transaction not found.</p>;
  if (transaction?.status === "Succeeded") return <SucceededResult transaction={transaction} />;
  if (transaction?.status === "Failed") return <FailedResult transaction={transaction} />;
  return <ProcessingResult />;
}
