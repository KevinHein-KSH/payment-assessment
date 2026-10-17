export default function ProcessingResult() {
  return (
    <div className="flex flex-col items-center py-6 text-center">
      <div
        className="h-8 w-8 animate-spin rounded-full border-2 border-gray-200 border-t-blue-600"
        role="status"
        aria-label="Loading"
      />
      <h1 className="mt-4 text-lg font-semibold text-gray-900">Processing payment</h1>
      <p className="mt-1 text-sm text-gray-500">
        Please wait while we confirm your payment.
      </p>
    </div>
  );
}
