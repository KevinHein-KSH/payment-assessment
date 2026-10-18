namespace PaymentAssessment.Application.Payments;

// Provider-neutral shapes: Application code never sees 2C2P field names, response codes or JWTs.
// RespCode/RespDesc are kept only to store and show the provider's own reason.

/// <summary>A payment's state as the provider reports it. Each adapter maps its own response codes to this.</summary>
public enum PaymentStatus { Succeeded, Failed, Pending, NotFound }

/// <summary>Result of a Payment Token request. RedirectUrl is the hosted page (2C2P webPaymentUrl).</summary>
public record PaymentTokenResult(bool Succeeded, string RespCode, string RespDesc, string? PaymentToken, string? RedirectUrl);

/// <summary>What Do Payment asks for next. Never proof of success: Completed still needs Payment Inquiry (docs/05).</summary>
public enum DoPaymentOutcome { Redirect3ds, Completed, Failed }

/// <summary>Result of Do Payment for a saved card. RedirectUrl is the 3DS page when Outcome is Redirect3ds.</summary>
public record DoPaymentResult(DoPaymentOutcome Outcome, string RespCode, string RespDesc, string? RedirectUrl);

/// <summary>A verified backend notification or Payment Inquiry answer. AccountNo is already masked by the provider.</summary>
public record PaymentNotification(
    string InvoiceNo,
    PaymentStatus Status,
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
