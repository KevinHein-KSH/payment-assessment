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
