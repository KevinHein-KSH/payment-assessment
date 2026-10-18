using PaymentAssessment.Application.Transactions;

namespace PaymentAssessment.Application.Payments;

/// <summary>Tells the controller which status code to return.</summary>
public enum NotificationOutcome { Processed, InvalidSignature, NotFound, Mismatch }

/// <summary>backendReturnUrl handling (docs/05, Backend notification steps 1–7).</summary>
public class NotificationService(IPaymentProvider provider, ITransactionRepository transactions)
{
    public async Task<NotificationOutcome> HandleAsync(string payload, CancellationToken ct)
    {
        var n = await provider.VerifyNotificationAsync(payload, ct);
        if (n is null)
            return NotificationOutcome.InvalidSignature;

        var tx = await transactions.GetByInvoiceNoAsync(n.InvoiceNo, ct);
        if (tx is null)
            return NotificationOutcome.NotFound;

        if (n.Amount != tx.Amount || n.CurrencyCode != tx.Currency)
            return NotificationOutcome.Mismatch;

        if (n.RespCode == "0000")
        {
            await transactions.MarkSucceededAsync(n.InvoiceNo, n.ChannelCode, n.AccountNo, n.TranRef, DateTime.UtcNow, ct);
        }
        else
        {
            await transactions.MarkFailedAsync(n.InvoiceNo, n.RespCode, n.RespDesc, ct);
        }
        // Phase 5: save n.CustomerToken to customer_card + invalidate the cards cache
        return NotificationOutcome.Processed;
    }
}
