using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PaymentAssessment.Infrastructure.Payments.TwoC2P;

public class TwoC2PClient(HttpClient http, IOptions<TwoC2POptions> options)
{
    private readonly TwoC2POptions _options = options.Value;

    public async Task<PaymentTokenResponse> CreatePaymentTokenAsync(
        string invoiceNo, string description, decimal amount, CancellationToken ct = default)
    {
        var request = new PaymentTokenRequest(
            MerchantId: _options.MerchantId,
            InvoiceNo: invoiceNo,
            Description: description,
            Amount: amount,
            CurrencyCode: "SGD",
            FrontendReturnUrl: _options.FrontendReturnUrl,
            BackendReturnUrl: _options.BackendReturnUrl);

        var jwt = TwoC2PJwt.Sign(JsonSerializer.Serialize(request), _options.SecretKey);

        var httpResponse = await http.PostAsJsonAsync(_options.PaymentTokenPath, new { payload = jwt }, ct);
        httpResponse.EnsureSuccessStatusCode();

        var envelope = await httpResponse.Content.ReadFromJsonAsync<TwoC2PEnvelope>(cancellationToken: ct) 
            ?? throw new InvalidOperationException("2C2P returned an empty response");

        if (envelope.Payload is null)
        {
            return new PaymentTokenResponse(
                RespCode: envelope.RespCode ?? "",
                RespDesc: envelope.RespDesc ?? "",
                PaymentToken: null,
                WebPaymentUrl: null
            );
        }

        var decodedJson = await TwoC2PJwt.VerifyAndDecodeAsync(envelope.Payload, _options.SecretKey);
        return JsonSerializer.Deserialize<PaymentTokenResponse>(decodedJson)
            ?? throw new InvalidOperationException("2C2P payload could not be read");
    }
}
