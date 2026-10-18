using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Domain.Entities;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Application.Payments;

/// <summary>Tells the controller which status code to return.</summary>
public enum NotificationOutcome { Processed, InvalidSignature, NotFound, Mismatch }

/// <summary>backendReturnUrl handling (docs/05, Backend notification steps 1–7).</summary>
public class NotificationService(IPaymentProvider provider, ITransactionRepository transactions, LookupService lookups)
{
    public async Task<NotificationOutcome> HandleAsync(string payload, CancellationToken ct)
    {
        var n = await provider.VerifyNotificationAsync(payload, ct);
        if (n is null)
            return NotificationOutcome.InvalidSignature;

        var tx = await transactions.GetByInvoiceNoAsync(n.InvoiceNo, ct);
        if (tx is null)
            return NotificationOutcome.NotFound;

        return await ApplyAsync(tx, n, ct) ? NotificationOutcome.Processed : NotificationOutcome.Mismatch;
    }

    /// <summary>
    /// Result-page fallback (docs/05 Inquiry fallback): a hosted-page cancel sends no notification (sandbox, 2026-10-18),
    /// so while a row is Pending and the customer was sent to the provider, ask Payment Inquiry and apply the answer.
    /// </summary>
    // shortcut: one Inquiry per Result-page poll while Pending; throttle per invoice if poll traffic grows
    public async Task RefreshPendingAsync(string invoiceNo, CancellationToken ct)
    {
        var tx = await transactions.GetByInvoiceNoAsync(invoiceNo, ct);
        // No redirect_url: an interrupted checkout, which the checkout retry recovers (CheckoutService)
        if (tx is null || tx.Status != TransactionStatus.Pending || tx.RedirectUrl is null)
            return;

        PaymentNotification answer;
        try
        {
            answer = await provider.InquireAsync(invoiceNo, ct);
        }
        catch (HttpRequestException)
        {
            return; // provider unreachable: stay Pending, the next poll asks again
        }

        // Not found = the customer hasn't paid yet. Marking it Failed would block a later successful notification.
        if (answer.Status != PaymentStatus.NotFound)
            await ApplyAsync(tx, answer, ct);
    }

    // A verified notification or Inquiry answer (docs/05: treat the inquiry result like a notification). False on amount mismatch.
    private async Task<bool> ApplyAsync(PaymentTransaction tx, PaymentNotification n, CancellationToken ct)
    {
        if (n.Amount != tx.Amount || n.CurrencyCode != tx.Currency)
            return false;

        switch (n.Status)
        {
            case PaymentStatus.Succeeded:
                await transactions.MarkSucceededAsync(n.InvoiceNo, n.ChannelCode, n.AccountNo, n.TranRef, DateTime.UtcNow, ct);
                if (!string.IsNullOrEmpty(n.CustomerToken))
                    await lookups.SaveCardAsync(tx.CustomerId, n.CustomerToken, n.AccountNo ?? "", ct);
                break;
            case PaymentStatus.Pending:
                // Not final yet (e.g. a payment still being processed): keep Pending and wait for the final notification
                break;
            case PaymentStatus.Failed:
            case PaymentStatus.NotFound:
                await transactions.MarkFailedAsync(n.InvoiceNo, n.RespCode, n.RespDesc, ct);
                break;
        }
        return true;
    }
}
