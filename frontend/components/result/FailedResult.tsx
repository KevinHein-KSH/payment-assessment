import type { TransactionDetails } from "@/lib/types";

interface FailedResultProps {
  transaction: TransactionDetails;
}

export default function FailedResult({ transaction }: FailedResultProps) {
  return (
    <div>
      <h1 className="text-lg font-semibold text-gray-900">Payment failed</h1>
      <div className="mt-4 rounded-md border border-gray-200 bg-gray-50 px-4 py-3 text-sm">
        <p className="text-gray-500">Reason</p>
        <p className="mt-1 text-gray-900">{transaction.respDesc ?? "Unknown error"}</p>
      </div>
      <button
        type="button"
        className="mt-6 w-full rounded-md bg-blue-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-blue-700"
      >
        Go back
      </button>
    </div>
  );
}
