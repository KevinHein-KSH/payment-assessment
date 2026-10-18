namespace PaymentAssessment.Application.Payments;

/// <summary>Payment provider boundary (docs/02). The only implementation is TwoC2PPaymentProvider in Infrastructure.</summary>
public interface IPaymentProvider
{
    /// <summary>Requests a hosted-page token for a new payment method. Phase 5/6 add channel, tokenize and saved-card parameters.</summary>
    Task<PaymentTokenResult> CreatePaymentTokenAsync(
        string invoiceNo, string description, decimal amount, string currency, CancellationToken ct);

    /// <summary>Verifies and decodes a backend notification payload. Returns null when the signature is invalid.</summary>
    Task<PaymentNotification?> VerifyNotificationAsync(string payload, CancellationToken ct);

    /// <summary>
    /// Payment Inquiry: the provider's current answer for an invoice, in the same shape as a verified notification
    /// (docs/05: treat the inquiry result like a notification). Not found comes back as PaymentStatus.NotFound with Amount 0.
    /// </summary>
    Task<PaymentNotification> InquireAsync(string invoiceNo, CancellationToken ct);
}
