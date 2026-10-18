using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PaymentAssessment.Application.Payments;

namespace PaymentAssessment.Infrastructure.Payments.TwoC2P;

public class TwoC2PPaymentProvider(HttpClient http, IOptions<TwoC2POptions> options) : IPaymentProvider
{
    private readonly TwoC2POptions _options = options.Value;

    public async Task<PaymentTokenResult> CreatePaymentTokenAsync(
        string invoiceNo, string description, decimal amount, string currency, CancellationToken ct = default)
    {
        var request = new PaymentTokenRequest(
            MerchantId: _options.MerchantId,
            InvoiceNo: invoiceNo,
            Description: description,
            Amount: amount,
            CurrencyCode: currency,
            FrontendReturnUrl: $"{_options.FrontendReturnUrl}/{invoiceNo}",
            BackendReturnUrl: _options.BackendReturnUrl);

        var jwt = TwoC2PJwt.Sign(JsonSerializer.Serialize(request), _options.SecretKey);

        var httpResponse = await http.PostAsJsonAsync(_options.PaymentTokenPath, new { payload = jwt }, ct);
        httpResponse.EnsureSuccessStatusCode();

        var envelope = await httpResponse.Content.ReadFromJsonAsync<TwoC2PEnvelope>(cancellationToken: ct)
            ?? throw new InvalidOperationException("2C2P returned an empty response");

        if (envelope.Payload is null)
        {
            return new PaymentTokenResult(
                RespCode: envelope.RespCode ?? "",
                RespDesc: envelope.RespDesc ?? "",
                PaymentToken: null,
                RedirectUrl: null
            );
        }

        var decodedJson = await TwoC2PJwt.VerifyAndDecodeAsync(envelope.Payload, _options.SecretKey);
        var r = JsonSerializer.Deserialize<PaymentTokenResponse>(decodedJson)
                ?? throw new InvalidOperationException("2C2P payload could not be read");
        return new PaymentTokenResult(r.RespCode, r.RespDesc, r.PaymentToken, r.WebPaymentUrl);

    }

    public async Task<PaymentNotification?> VerifyNotificationAsync(string payload, CancellationToken ct)
    {
        try
        {
            var json = await TwoC2PJwt.VerifyAndDecodeAsync(payload, _options.SecretKey);
            var p = JsonSerializer.Deserialize<NotificationPayload>(json)
                ?? throw new InvalidOperationException("2C2P payload could not be read");
            return new PaymentNotification(
                InvoiceNo: p.InvoiceNo,
                Amount: p.Amount,
                CurrencyCode: p.CurrencyCode,
                RespCode: p.RespCode,
                RespDesc: p.RespDesc,
                ChannelCode: p.ChannelCode,
                AccountNo: p.AccountNo,
                TranRef: p.TranRef,
                CustomerToken: p.CustomerToken
            );
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
