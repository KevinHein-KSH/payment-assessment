using PaymentAssessment.Domain.Entities;

namespace PaymentAssessment.Application.Transactions;

public interface ITransactionRepository
{
    /// <summary>False when the idempotency key already exists (unique index): the final guard if the Redis lock expired or Redis is down.</summary>
    Task<bool> AddAsync(PaymentTransaction transaction, CancellationToken ct);

    Task<PaymentTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);

    Task<PaymentTransaction?> GetByInvoiceNoAsync(string invoiceNo, CancellationToken ct);

    /// <summary>Fresh read for the Result page; transaction status is never cached (docs/05).</summary>
    Task<TransactionDetailsDto?> GetDetailsAsync(string invoiceNo, CancellationToken ct);

    /// <summary>redirectUrl: hosted page or 3DS URL; null for a saved-card payment that needed no redirect.</summary>
    Task SetPaymentTokenAsync(long id, string paymentToken, string? redirectUrl, CancellationToken ct);

    /// <summary>Updates only WHERE status = 'Pending'. False when the row was already final.</summary>
    Task<bool> MarkSucceededAsync(string invoiceNo, string? paymentChannel, string? maskedCardNo,
        string? providerRef, DateTime paidAtUtc, CancellationToken ct);

    /// <summary>Updates only WHERE status = 'Pending'. False when the row was already final.</summary>
    Task<bool> MarkFailedAsync(string invoiceNo, string respCode, string respDesc, CancellationToken ct);
}
