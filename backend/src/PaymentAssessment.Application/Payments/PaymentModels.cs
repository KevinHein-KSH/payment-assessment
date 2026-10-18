namespace PaymentAssessment.Application.Payments;

// Provider-neutral shapes: Application code never sees 2C2P field names or JWTs.

/// <summary>Result of a Payment Token request. RedirectUrl is the hosted page (2C2P webPaymentUrl).</summary>
public record PaymentTokenResult(string RespCode, string RespDesc, string? PaymentToken, string? RedirectUrl);

/// <summary>A verified backend notification. AccountNo is already masked by the provider.</summary>
public record PaymentNotification(
    string InvoiceNo,
    decimal Amount,
    string CurrencyCode,
    string RespCode,
    string RespDesc,
    string? ChannelCode,
    string? AccountNo,
    string? TranRef,
    string? CustomerToken);

/// <summary>Body 2C2P POSTs to backendReturnUrl: { "payload": "&lt;JWT&gt;" }.</summary>
public record PaymentNotificationRequest(string Payload);
