using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentAssessment.Application.Checkout;
using PaymentAssessment.Application.Payments;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Api.Tests;

/// <summary>
/// docs/05 Saved-card flow: ownership check, Do Payment, mandatory Payment Inquiry, 3DS, no second charge on retry.
/// The fake mirrors the sandbox run of 2026-10-18: Do Payment → 2000, then Inquiry → 0000 with the card and token.
/// </summary>
[Collection("FakeProviderApi")]
public class SavedCardCheckoutTests(FakeProviderApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client = factory.CreateClient();
    private FakePaymentProvider Provider => factory.Provider;

    private Task<HttpResponseMessage> CheckoutAsync(int customerId, int productId, int cardId, string? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { customerId, productId, customerCardId = cardId })
        };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return _client.SendAsync(request);
    }

    private static async Task<CheckoutResponse> ReadAsync(HttpResponseMessage res)
    {
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<CheckoutResponse>(Json))!;
    }

    // Each test seeds its own card (unique token) on customer 1 and removes it, so the demo's card list stays clean
    private async Task WithCardAsync(Func<int, string, Task> test, int customerId = 1)
    {
        var token = "TEST" + Guid.NewGuid().ToString("N")[..16];
        var cardId = await factory.SeedCardAsync(customerId, token);
        try { await test(cardId, token); }
        finally { await factory.DeleteCardAsync(customerId, token); }
    }

    [Fact]
    public Task Saved_card_payment_is_confirmed_by_inquiry_and_succeeds() => WithCardAsync(async (cardId, token) =>
    {
        // Product 3 is Recurring: paying with a saved card must not ask to tokenize it again
        var body = await ReadAsync(await CheckoutAsync(1, 3, cardId));

        Assert.Equal(TransactionStatus.Succeeded, body.Status);
        Assert.Null(body.RedirectUrl); // no hosted page: the browser goes straight to the Result page
        Assert.Equal(token, Provider.CustomerTokenRequests[body.InvoiceNo]);
        Assert.False(Provider.SaveCardRequests[body.InvoiceNo]);
        var row = await factory.ReloadAsync(body.InvoiceNo);
        Assert.Equal("411111XXXXXX1111", row.MaskedCardNo);
        Assert.Null(row.RedirectUrl); // the saved-card token's webPaymentUrl is never stored
    });

    [Fact]
    public Task Do_payment_completed_is_not_success_when_inquiry_reports_a_decline() => WithCardAsync(async (cardId, token) =>
    {
        Provider.ChargeResults[token] = (PaymentStatus.Failed, "4001", "Refer to cared issuer");

        var body = await ReadAsync(await CheckoutAsync(1, 1, cardId));

        Assert.Equal(TransactionStatus.Failed, body.Status);
        Assert.Equal("Refer to cared issuer", (await factory.ReloadAsync(body.InvoiceNo)).RespDesc);
    });

    [Fact]
    public Task Card_of_another_customer_is_rejected_before_anything_is_written() => WithCardAsync(async (cardId, _) =>
    {
        var key = Guid.NewGuid().ToString();
        var tokenCallsBefore = Provider.TokenCalls;

        var res = await CheckoutAsync(customerId: 1, productId: 1, cardId, key); // card belongs to customer 2

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Equal(0, await factory.CountByIdempotencyKeyAsync(key));
        Assert.Equal(tokenCallsBefore, Provider.TokenCalls);
    }, customerId: 2);

    [Fact]
    public Task Three_ds_returns_the_3ds_url_and_inquiry_confirms_on_return() => WithCardAsync(async (cardId, token) =>
    {
        Provider.DoPaymentResults[token] = new DoPaymentResult(DoPaymentOutcome.Redirect3ds, "1001", "Redirect to 3DS",
            $"https://3ds.example/{token}");

        var body = await ReadAsync(await CheckoutAsync(1, 1, cardId));

        Assert.Equal(TransactionStatus.Pending, body.Status);
        Assert.Equal($"https://3ds.example/{token}", body.RedirectUrl);

        // Customer finishes 3DS and lands on /result/{invoiceNo}: the Result-page read asks Inquiry
        Provider.InquiryResults[body.InvoiceNo] = new PaymentNotification(body.InvoiceNo, PaymentStatus.Succeeded,
            25.00m, "SGD", "0000", "Success", "VI", "411111XXXXXX1111", "15707875", token);
        var details = await _client.GetFromJsonAsync<TransactionDetailsDto>($"/api/transactions/{body.InvoiceNo}", Json);

        Assert.Equal(TransactionStatus.Succeeded, details!.Status);
    });

    [Fact]
    public Task Retry_with_the_same_key_never_charges_the_saved_card_twice() => WithCardAsync(async (cardId, _) =>
    {
        var key = Guid.NewGuid().ToString();
        var doPaymentsBefore = Provider.DoPaymentCalls;

        var first = await ReadAsync(await CheckoutAsync(1, 1, cardId, key));
        var retry = await ReadAsync(await CheckoutAsync(1, 1, cardId, key));

        Assert.Equal(first.InvoiceNo, retry.InvoiceNo);
        Assert.Equal(TransactionStatus.Succeeded, retry.Status);
        Assert.Equal(1, Provider.DoPaymentCalls - doPaymentsBefore);
    });
}
