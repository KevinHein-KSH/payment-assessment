import FailedResult from "@/components/result/FailedResult";
import ProcessingResult from "@/components/result/ProcessingResult";
import SucceededResult from "@/components/result/SucceededResult";
import { MOCK_RESULT_STATE, mockTransactions } from "@/lib/mock-data";

export default function ResultPage() {
  const transaction = mockTransactions[MOCK_RESULT_STATE];

  return (
    <main className="mx-auto w-full max-w-xl px-4 py-8 sm:py-12">
      <div className="rounded-lg border border-gray-200 bg-white p-6 sm:p-8">
        {transaction.status === "Succeeded" && <SucceededResult transaction={transaction} />}
        {transaction.status === "Failed" && <FailedResult transaction={transaction} />}
        {transaction.status === "Pending" && <ProcessingResult />}
      </div>
    </main>
  );
}
