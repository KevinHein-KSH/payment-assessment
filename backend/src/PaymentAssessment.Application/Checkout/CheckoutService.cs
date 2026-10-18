using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Application.Payments;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Application.Locking;
using PaymentAssessment.Domain.Entities;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Application.Checkout;
// Not in Phase 4: saved card / customerCardId (Phase 6).
public class CheckoutService
{
    private ILookupRepository LookupRepository { get; }
    private ITransactionRepository TransactionRepository { get; }
    private IPaymentProvider PaymentProvider { get; }
    private IDistributedLock Lock { get; }
    public CheckoutService(
        ILookupRepository lookups,
        ITransactionRepository transactions,
        IPaymentProvider provider,
        IDistributedLock distributedlock
    )
    {
        LookupRepository = lookups;
        TransactionRepository = transactions;
        PaymentProvider = provider;
        Lock = distributedlock;
    }
    private static string GenerateRandomAlphanumeric(int length)
            => Guid.NewGuid().ToString("N").Substring(0, length).ToUpperInvariant();
    public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, string idempotencyKey, CancellationToken ct)
    {
        // Gate 1: validate first; nothing is written yet, so no lock needed
        if (!await LookupRepository.CustomerExistsAsync(request.CustomerId, ct))
            return new CheckoutResult(CheckoutOutcome.NotFound);

        var product = (await LookupRepository.GetProductsAsync(ct)).FirstOrDefault(p => p.Id == request.ProductId);
        if (product is null)
            return new CheckoutResult(CheckoutOutcome.NotFound);

        // Gate 2: one request per key at a time, across all API instances. Released when this method ends.
        await using var lockHandle = await Lock.TryAcquireAsync($"lock:checkout:{idempotencyKey}", TimeSpan.FromSeconds(60), ct);
        if (lockHandle is null)
            return new CheckoutResult(CheckoutOutcome.InProgress);

        // Gate 3: key seen before → answer from the DB, never call 2C2P again
        var existing = await TransactionRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing is not null)
            return await ReplayAsync(existing, ct);

        // Gate 4: new purchase
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

        // Unique index said "key exists": another request got in without the lock (lock expired or Redis down).
        // It's still running, so answer 409 rather than touching its row; the client's retry gets the replay.
        if (!await TransactionRepository.AddAsync(transaction, ct))
            return new CheckoutResult(CheckoutOutcome.InProgress);

        var result = await PaymentProvider.CreatePaymentTokenAsync(invoiceNo, product.Name, product.Price, product.Currency, ct);

        if (result.RespCode == "0000")
        {
            await TransactionRepository.SetPaymentTokenAsync(transaction.Id, result.PaymentToken!, result.RedirectUrl!, ct);
            return new CheckoutResult(CheckoutOutcome.Ok, new CheckoutResponse(invoiceNo, TransactionStatus.Pending, result.RedirectUrl));
        }

        await TransactionRepository.MarkFailedAsync(invoiceNo, result.RespCode, result.RespDesc, ct);
        return new CheckoutResult(CheckoutOutcome.Ok, new CheckoutResponse(invoiceNo, TransactionStatus.Failed, null));
    }

    private async Task<CheckoutResult> ReplayAsync(PaymentTransaction existing, CancellationToken ct)
    {
        // Pending without redirect_url: the earlier request died before saving 2C2P's answer
        if (existing.Status == TransactionStatus.Pending && existing.RedirectUrl is null)
            return await RecoverInterruptedAsync(existing, ct);

        // Same invoice and redirect URL as the first response, with the status as it is now (docs/05)
        return new CheckoutResult(CheckoutOutcome.Ok,
            new CheckoutResponse(existing.InvoiceNo, existing.Status, existing.RedirectUrl));
    }

    // docs/05 "Interrupted request": we don't know whether 2C2P got the first request, so ask Payment Inquiry
    // instead of sending the same invoiceNo again. Updates use WHERE status = 'Pending', like a notification.
    private async Task<CheckoutResult> RecoverInterruptedAsync(PaymentTransaction transaction, CancellationToken ct)
    {
        var answer = await PaymentProvider.InquireAsync(transaction.InvoiceNo, ct);

        switch (answer.RespCode)
        {
            case "0000" when answer.Amount == transaction.Amount && answer.CurrencyCode == transaction.Currency:
                await TransactionRepository.MarkSucceededAsync(transaction.InvoiceNo, answer.ChannelCode,
                    answer.AccountNo, answer.TranRef, DateTime.UtcNow, ct);
                break;
            case "0000":
                // Paid, but not the amount we stored: same rule as a notification, don't mark it; leave Pending for review
                break;
            case "0001" or "2001":
                // Still in progress at 2C2P: stay Pending, the Result page keeps polling
                break;
            default:
                // Declined, cancelled, expired, or "2002" not found: the customer never got a payment link, nothing was charged
                await TransactionRepository.MarkFailedAsync(transaction.InvoiceNo, answer.RespCode, answer.RespDesc, ct);
                break;
        }

        var current = await TransactionRepository.GetByInvoiceNoAsync(transaction.InvoiceNo, ct);
        return new CheckoutResult(CheckoutOutcome.Ok, new CheckoutResponse(current!.InvoiceNo, current.Status, null));
    }
}
