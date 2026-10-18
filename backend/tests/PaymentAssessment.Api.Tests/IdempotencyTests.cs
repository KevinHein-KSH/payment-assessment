using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentAssessment.Application.Checkout;
using PaymentAssessment.Application.Payments;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Api.Tests;

/// <summary>Idempotency rules from docs/05, against real MySQL and Redis with 2C2P faked.</summary>
[Collection("FakeProviderApi")]
public class IdempotencyTests(FakeProviderApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client = factory.CreateClient();
    private FakePaymentProvider Provider => factory.Provider;

    private Task<HttpResponseMessage> CheckoutAsync(string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
        {
            Content = JsonContent.Create(new { customerId = 1, productId = 1 })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return _client.SendAsync(request);
    }

    private static async Task<CheckoutResponse> ReadAsync(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<CheckoutResponse>(Json))!;

    [Fact]
    public async Task Ten_parallel_checkouts_with_one_key_create_one_transaction_and_one_2c2p_call()
    {
        var key = Guid.NewGuid().ToString();
        var callsBefore = Provider.TokenCalls;

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => CheckoutAsync(key)));

        Assert.Equal(1, await factory.CountByIdempotencyKeyAsync(key));
        Assert.Equal(1, Provider.TokenCalls - callsBefore);
        // docs/05: the others wait and get the same result, or get 409 while the first is still running
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        var invoices = await Task.WhenAll(responses.Where(r => r.StatusCode == HttpStatusCode.OK).Select(ReadAsync));
        Assert.NotEmpty(invoices);
        Assert.Single(invoices.Select(i => i.InvoiceNo).Distinct());
    }

    [Fact]
    public async Task Retry_with_the_same_key_replays_the_original_response_without_calling_2c2p()
    {
        var key = Guid.NewGuid().ToString();
        var first = await ReadAsync(await CheckoutAsync(key));
        var callsAfterFirst = Provider.TokenCalls;

        var retry = await CheckoutAsync(key);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var replay = await ReadAsync(retry);
        Assert.Equal(first.InvoiceNo, replay.InvoiceNo);
        Assert.Equal(first.RedirectUrl, replay.RedirectUrl);
        Assert.Equal(TransactionStatus.Pending, replay.Status);
        Assert.Equal(callsAfterFirst, Provider.TokenCalls);
    }

    // docs/05 "Interrupted request": a row without redirect_url is resolved with Payment Inquiry, never a second Payment Token
    [Theory]
    [InlineData(PaymentStatus.Succeeded, "0000", "Success", TransactionStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed, "4001", "Refer to cared issuer", TransactionStatus.Failed)]
    [InlineData(PaymentStatus.NotFound, "2002", "Transaction not found.", TransactionStatus.Failed)]
    [InlineData(PaymentStatus.Pending, "2001", "Transaction in progress", TransactionStatus.Pending)]
    public async Task Interrupted_checkout_is_resolved_with_payment_inquiry(
        PaymentStatus providerStatus, string respCode, string respDesc, TransactionStatus expected)
    {
        var tx = await factory.SeedPendingAsync(amount: 25.00m); // Pending, no redirect_url: 2C2P's answer was never saved
        if (providerStatus != PaymentStatus.NotFound)
            Provider.InquiryResults[tx.InvoiceNo] = new PaymentNotification(
                tx.InvoiceNo, providerStatus, 25.00m, "SGD", respCode, respDesc, "VI", "411111XXXXXX1111", "15707541", "");
        var callsBefore = Provider.TokenCalls;

        var res = await CheckoutAsync(tx.IdempotencyKey);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var replay = await ReadAsync(res);
        Assert.Equal(tx.InvoiceNo, replay.InvoiceNo);
        Assert.Equal(expected, replay.Status);
        Assert.Equal(expected, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
        Assert.Equal(callsBefore, Provider.TokenCalls);
    }
}
