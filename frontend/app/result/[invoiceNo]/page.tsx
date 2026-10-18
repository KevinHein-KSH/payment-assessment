import { Suspense } from "react";
import ProcessingResult from "@/components/result/ProcessingResult";
import ResultStatus from "@/components/result/ResultStatus";

export default function ResultPage() {
  return (
    <main className="mx-auto w-full max-w-xl px-4 py-8 sm:py-12">
      <div className="rounded-lg border border-gray-200 bg-white p-6 sm:p-8">
        {/* Cache Components: the invoiceNo param is request-time data, so it must be read inside Suspense */}
        <Suspense fallback={<ProcessingResult />}>
          <ResultStatus />
        </Suspense>
      </div>
    </main>
  );
}
