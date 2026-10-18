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
    [property: JsonPropertyName("backendReturnUrl")] string BackendReturnUrl);

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

/// <summary>Decoded backend notification JWT payload (docs/05, Backend notification). Fields beyond these are ignored.</summary>
public record NotificationPayload(
    [property: JsonPropertyName("invoiceNo")] string InvoiceNo,
    // 2C2P's sample sends amount as a string ("230.87"); accept both forms
    [property: JsonPropertyName("amount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Amount,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("respCode")] string RespCode,
    [property: JsonPropertyName("respDesc")] string RespDesc,
    [property: JsonPropertyName("channelCode")] string? ChannelCode,
    [property: JsonPropertyName("accountNo")] string? AccountNo,
    [property: JsonPropertyName("tranRef")] string? TranRef,
    [property: JsonPropertyName("customerToken")] string? CustomerToken,
    // Set when 2C2P settled in another currency: our requested amount/currency, e.g. 25.0 SGD settled as 605.88 THB
    [property: JsonPropertyName("fxAmount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? FxAmount = null,
    [property: JsonPropertyName("fxCurrencyCode")] string? FxCurrencyCode = null);
