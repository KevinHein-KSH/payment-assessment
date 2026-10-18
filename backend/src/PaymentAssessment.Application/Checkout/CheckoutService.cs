using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Application.Payments;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Domain.Entities;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Application.Checkout;
// Not in Phase 3: Redis lock + replay of an existing key (Phase 4), saved card / customerCardId (Phase 6).
// Until Phase 4, a reused Idempotency-Key hits the unique index and returns 500.
public class CheckoutService
{
    private ILookupRepository LookupRepository { get; }
    private ITransactionRepository TransactionRepository { get; }
    private IPaymentProvider PaymentProvider { get; }
    public CheckoutService(
        ILookupRepository lookups,
        ITransactionRepository transactions,
        IPaymentProvider provider)
    {
        LookupRepository = lookups;
        TransactionRepository = transactions;
        PaymentProvider = provider;
    }
    private static string GenerateRandomAlphanumeric(int length)
            => Guid.NewGuid().ToString("N").Substring(0, length).ToUpperInvariant();
    public async Task<CheckoutResponse?> CheckoutAsync(CheckoutRequest request, string idempotencyKey, CancellationToken ct)
    {
        if (!await LookupRepository.CustomerExistsAsync(request.CustomerId, ct))
            return null;

        var product = (await LookupRepository.GetProductsAsync(ct)).FirstOrDefault(p => p.Id == request.ProductId);
        if (product is null)
            return null;

        var invoiceNo = "INV" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + GenerateRandomAlphanumeric(6);

        var transaction = new PaymentTransaction
        {
            InvoiceNo = invoiceNo,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Amount = product.Price,
            Currency = product.Currency,
            IdempotencyKey = idempotencyKey,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await TransactionRepository.AddAsync(transaction, ct);

        var result = await PaymentProvider.CreatePaymentTokenAsync(invoiceNo, product.Name, product.Price, product.Currency, ct);

        if (result.RespCode == "0000")
        {
            await TransactionRepository.SetPaymentTokenAsync(transaction.Id, result.PaymentToken!, result.RedirectUrl!, ct);
            return new CheckoutResponse(invoiceNo, TransactionStatus.Pending, result.RedirectUrl);
        }
        else
        {
            await TransactionRepository.MarkFailedAsync(invoiceNo, result.RespCode, result.RespDesc, ct);
            return new CheckoutResponse(invoiceNo, TransactionStatus.Failed, null);
        }
    }
}
