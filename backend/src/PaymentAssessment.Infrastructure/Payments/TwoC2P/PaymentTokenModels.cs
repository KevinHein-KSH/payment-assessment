using System.Text.Json.Serialization;

namespace PaymentAssessment.Infrastructure.Payments.TwoC2P;

/// <summary>Payment Token request payload that is signed as a JWT and sent to 2C2P.</summary>
public record PaymentTokenRequest(
    [property: JsonPropertyName("merchantID")] string MerchantId,
    [property: JsonPropertyName("invoiceNo")] string InvoiceNo,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("frontendReturnUrl")] string FrontendReturnUrl,
    [property: JsonPropertyName("backendReturnUrl")] string BackendReturnUrl,
    // Recurring only; null fields are left out of the JSON so OneTime requests don't change
    [property: JsonPropertyName("paymentChannel"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string[]? PaymentChannel = null,
    [property: JsonPropertyName("tokenize"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Tokenize = null,
    // Saved card only (docs/05 "Saved card" row)
    [property: JsonPropertyName("customerToken"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string[]? CustomerToken = null,
    [property: JsonPropertyName("customerTokenOnly"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? CustomerTokenOnly = null,
    [property: JsonPropertyName("request3DS"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Request3DS = null);

/// <summary>Do Payment request: plain JSON, not a JWT (docs/05). Sandbox-verified 2026-10-18 with a saved customerToken.</summary>
public record DoPaymentRequest(
    [property: JsonPropertyName("paymentToken")] string PaymentToken,
    [property: JsonPropertyName("locale")] string Locale,
    [property: JsonPropertyName("responseReturnUrl")] string ResponseReturnUrl, // where the browser lands after 3DS
    [property: JsonPropertyName("payment")] DoPaymentPayment Payment);

public record DoPaymentPayment(
    [property: JsonPropertyName("code")] DoPaymentCode Code,
    [property: JsonPropertyName("data")] DoPaymentData Data);

public record DoPaymentCode([property: JsonPropertyName("channelCode")] string ChannelCode);

public record DoPaymentData([property: JsonPropertyName("customerToken")] string CustomerToken);

/// <summary>
/// Do Payment reply (plain JSON). Sandbox 2026-10-18: {"invoiceNo":"…","channelCode":"CC","respCode":"2000",
/// "respDesc":"Transaction is completed, please do payment inquiry request for full payment information."}.
/// On 1001, Data holds the 3DS URL (2C2P docs).
/// </summary>
public record DoPaymentResponse(
    [property: JsonPropertyName("invoiceNo")] string? InvoiceNo,
    [property: JsonPropertyName("channelCode")] string? ChannelCode,
    [property: JsonPropertyName("respCode")] string RespCode,
    [property: JsonPropertyName("respDesc")] string RespDesc,
    [property: JsonPropertyName("data")] string? Data);

/// <summary>Decoded Payment Token response JWT payload from 2C2P.</summary>
public record PaymentTokenResponse(
    [property: JsonPropertyName("respCode")] string RespCode,
    [property: JsonPropertyName("respDesc")] string RespDesc,
    [property: JsonPropertyName("paymentToken")] string? PaymentToken,
    [property: JsonPropertyName("webPaymentUrl")] string? WebPaymentUrl);

/// <summary>Raw 2C2P HTTP reply before decoding: a signed payload, or a plain error code and description.</summary>
public record TwoC2PEnvelope(
    [property: JsonPropertyName("payload")] string? Payload,
    [property: JsonPropertyName("respCode")] string? RespCode,
    [property: JsonPropertyName("respDesc")] string? RespDesc);

/// <summary>Decoded notification or Payment Inquiry JWT payload (same shape, docs/05). Fields beyond these are ignored.</summary>
public record NotificationPayload(
    [property: JsonPropertyName("invoiceNo")] string? InvoiceNo, // null in an Inquiry "2002 not found" reply
    // 2C2P's sample sends amount as a string ("230.87"); accept both forms
    [property: JsonPropertyName("amount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Amount,
    [property: JsonPropertyName("currencyCode")] string? CurrencyCode,
    [property: JsonPropertyName("respCode")] string RespCode,
    [property: JsonPropertyName("respDesc")] string RespDesc,
    [property: JsonPropertyName("channelCode")] string? ChannelCode,
    [property: JsonPropertyName("accountNo")] string? AccountNo,
    [property: JsonPropertyName("tranRef")] string? TranRef,
    [property: JsonPropertyName("customerToken")] string? CustomerToken,
    // Set when 2C2P settled in another currency: our requested amount/currency, e.g. 25.0 SGD settled as 605.88 THB
    [property: JsonPropertyName("fxAmount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? FxAmount = null,
    [property: JsonPropertyName("fxCurrencyCode")] string? FxCurrencyCode = null);
