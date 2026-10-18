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
        string invoiceNo, string description, decimal amount, string currency, bool saveCard, CancellationToken ct = default)
    {
        var request = new PaymentTokenRequest(
            MerchantId: _options.MerchantId,
            InvoiceNo: invoiceNo,
            Description: description,
            Amount: amount,
            CurrencyCode: currency,
            FrontendReturnUrl: $"{_options.FrontendReturnUrl}/{invoiceNo}",
            BackendReturnUrl: _options.BackendReturnUrl
        );

        if (saveCard)
        {
            request = request with { PaymentChannel = new[] { "CC" }, Tokenize = true };
        }

        var jwt = TwoC2PJwt.Sign(JsonSerializer.Serialize(request), _options.SecretKey);

        var httpResponse = await http.PostAsJsonAsync(_options.PaymentTokenPath, new { payload = jwt }, ct);
        httpResponse.EnsureSuccessStatusCode();

        var envelope = await httpResponse.Content.ReadFromJsonAsync<TwoC2PEnvelope>(cancellationToken: ct)
            ?? throw new InvalidOperationException("2C2P returned an empty response");

        if (envelope.Payload is null)
        {
            return new PaymentTokenResult(
                Succeeded: false,
                RespCode: envelope.RespCode ?? "",
                RespDesc: envelope.RespDesc ?? "",
                PaymentToken: null,
                RedirectUrl: null
            );
        }

        var decodedJson = await TwoC2PJwt.VerifyAndDecodeAsync(envelope.Payload, _options.SecretKey);
        var r = JsonSerializer.Deserialize<PaymentTokenResponse>(decodedJson)
                ?? throw new InvalidOperationException("2C2P payload could not be read");
        return new PaymentTokenResult(r.RespCode == "0000", r.RespCode, r.RespDesc, r.PaymentToken, r.WebPaymentUrl);
    }

    public async Task<PaymentNotification?> VerifyNotificationAsync(string payload, CancellationToken ct)
    {
        try
        {
            var json = await TwoC2PJwt.VerifyAndDecodeAsync(payload, _options.SecretKey);
            var p = JsonSerializer.Deserialize<NotificationPayload>(json)
                ?? throw new InvalidOperationException("2C2P payload could not be read");
            return ToPaymentNotification(p, p.InvoiceNo ?? "");
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    public async Task<PaymentNotification> InquireAsync(string invoiceNo, CancellationToken ct)
    {
        var request = new { merchantID = _options.MerchantId, invoiceNo, locale = "en" };
        var jwt = TwoC2PJwt.Sign(JsonSerializer.Serialize(request), _options.SecretKey);

        var httpResponse = await http.PostAsJsonAsync(_options.PaymentInquiryPath, new { payload = jwt }, ct);
        httpResponse.EnsureSuccessStatusCode();

        var envelope = await httpResponse.Content.ReadFromJsonAsync<TwoC2PEnvelope>(cancellationToken: ct)
            ?? throw new InvalidOperationException("2C2P returned an empty response");

        if (envelope.Payload is null)
            return new PaymentNotification(invoiceNo, ToStatus(envelope.RespCode ?? ""), 0m, "",
                envelope.RespCode ?? "", envelope.RespDesc ?? "", null, null, null, null);

        // Our own outbound call: a bad signature here is an error, so let VerifyAndDecodeAsync throw
        var decodedJson = await TwoC2PJwt.VerifyAndDecodeAsync(envelope.Payload, _options.SecretKey);
        var p = JsonSerializer.Deserialize<NotificationPayload>(decodedJson)
            ?? throw new InvalidOperationException("2C2P payload could not be read");
        // Not found ("2002") comes back with invoiceNo null and amount 0, so keep the invoice we asked about
        return ToPaymentNotification(p, invoiceNo);
    }

    // Inquiry replies have the notification's shape (docs/05, sandbox-verified), so both map the same way
    private static PaymentNotification ToPaymentNotification(NotificationPayload p, string invoiceNo)
    {
        // Report what we requested, not what 2C2P settled in: the sandbox converts SGD to THB and keeps our SGD in fx*
        var converted = !string.IsNullOrEmpty(p.FxCurrencyCode) && p.FxAmount is not null;
        return new PaymentNotification(
            InvoiceNo: p.InvoiceNo ?? invoiceNo,
            Status: ToStatus(p.RespCode),
            Amount: converted ? p.FxAmount!.Value : p.Amount,
            CurrencyCode: converted ? p.FxCurrencyCode! : p.CurrencyCode ?? "",
            RespCode: p.RespCode,
            RespDesc: p.RespDesc,
            ChannelCode: p.ChannelCode,
            AccountNo: p.AccountNo,
            TranRef: p.TranRef,
            CustomerToken: p.CustomerToken);
    }

    // The only place that knows 2C2P response codes (docs/05 "Response codes" and the interrupted-request table)
    private static PaymentStatus ToStatus(string respCode) => respCode switch
    {
        "0000" => PaymentStatus.Succeeded,
        "0001" or "2001" => PaymentStatus.Pending,
        "2002" or "4071" or "4140" => PaymentStatus.NotFound,
        _ => PaymentStatus.Failed
    };
}
