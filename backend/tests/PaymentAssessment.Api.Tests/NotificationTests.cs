using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Api.Tests;

/// <summary>Backend notification rules from docs/05: signature, amount/currency check, idempotent final status.</summary>
[Collection("Api")]
public class NotificationTests(ApiFactory factory)
{
    private const string Url = "/api/payments/2c2p/notification";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client = factory.CreateClient();

    // Same field names and string amount format as 2C2P's backend response sample
    private static object Payload(string invoiceNo, string amount = "25.00", string respCode = "0000", string respDesc = "Success",
        string customerToken = "") => new
    {
        customerToken,
        merchantID = "JT01",
        invoiceNo,
        accountNo = "411111XXXXXX1111",
        amount,
        currencyCode = "SGD",
        tranRef = "2868821",
        channelCode = "VI",
        respCode,
        respDesc
    };

    [Fact]
    public async Task Success_notification_marks_transaction_succeeded()
    {
        var tx = await factory.SeedPendingAsync();

        var res = await _client.PostAsJsonAsync(Url, ApiFactory.NotificationBody(Payload(tx.InvoiceNo)));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var details = await _client.GetFromJsonAsync<TransactionDetailsDto>($"/api/transactions/{tx.InvoiceNo}", Json);
        Assert.Equal(TransactionStatus.Succeeded, details!.Status);
        Assert.Equal("VI", details.PaymentChannel);
        Assert.Equal("411111XXXXXX1111", details.MaskedCardNo);
        var saved = await factory.ReloadAsync(tx.InvoiceNo);
        Assert.Equal("2868821", saved.ProviderRef);
        Assert.NotNull(saved.PaidAt);
    }

    // Shape captured from a real sandbox notification (2026-10-18): settled in THB, our SGD request in fx*
    private static object ConvertedPayload(string invoiceNo, decimal fxAmount) => new
    {
        merchantID = "JT01",
        invoiceNo,
        accountNo = "411111XXXXXX1111",
        amount = 605.88m,
        currencyCode = "THB",
        fxAmount,
        fxCurrencyCode = "SGD",
        fxRate = 24.2354868m,
        tranRef = "15707541",
        channelCode = "VI",
        customerToken = "",
        respCode = "0000",
        respDesc = "Success"
    };

    [Fact]
    public async Task Converted_notification_is_checked_against_the_requested_currency()
    {
        var tx = await factory.SeedPendingAsync(amount: 25.00m);

        var res = await _client.PostAsJsonAsync(Url, ApiFactory.NotificationBody(ConvertedPayload(tx.InvoiceNo, fxAmount: 25.0m)));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(TransactionStatus.Succeeded, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
    }

    [Fact]
    public async Task Converted_notification_with_wrong_requested_amount_is_rejected()
    {
        var tx = await factory.SeedPendingAsync(amount: 25.00m);

        var res = await _client.PostAsJsonAsync(Url, ApiFactory.NotificationBody(ConvertedPayload(tx.InvoiceNo, fxAmount: 1.0m)));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(TransactionStatus.Pending, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
    }

    [Fact]
    public async Task Failure_notification_marks_transaction_failed_with_reason()
    {
        var tx = await factory.SeedPendingAsync();

        var res = await _client.PostAsJsonAsync(Url,
            ApiFactory.NotificationBody(Payload(tx.InvoiceNo, respCode: "4005", respDesc: "Transaction declined")));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var saved = await factory.ReloadAsync(tx.InvoiceNo);
        Assert.Equal(TransactionStatus.Failed, saved.Status);
        Assert.Equal("4005", saved.RespCode);
        Assert.Equal("Transaction declined", saved.RespDesc);
    }

    [Fact]
    public async Task In_progress_notification_keeps_transaction_pending()
    {
        var tx = await factory.SeedPendingAsync();

        var res = await _client.PostAsJsonAsync(Url,
            ApiFactory.NotificationBody(Payload(tx.InvoiceNo, respCode: "2001", respDesc: "Transaction in progress")));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(TransactionStatus.Pending, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
    }

    [Fact]
    public async Task Notification_with_bad_signature_is_rejected()
    {
        var tx = await factory.SeedPendingAsync();

        var res = await _client.PostAsJsonAsync(Url,
            ApiFactory.NotificationBody(Payload(tx.InvoiceNo), secret: "wrong-secret-key-at-least-32-bytes-long!"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(TransactionStatus.Pending, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
    }

    [Fact]
    public async Task Notification_with_wrong_amount_is_rejected()
    {
        var tx = await factory.SeedPendingAsync(amount: 25.00m);

        var res = await _client.PostAsJsonAsync(Url, ApiFactory.NotificationBody(Payload(tx.InvoiceNo, amount: "1.00")));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(TransactionStatus.Pending, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
    }

    [Fact]
    public async Task Repeated_notifications_do_not_change_a_final_status()
    {
        var tx = await factory.SeedPendingAsync();
        var success = ApiFactory.NotificationBody(Payload(tx.InvoiceNo));

        var first = await _client.PostAsJsonAsync(Url, success);
        var duplicate = await _client.PostAsJsonAsync(Url, success);
        var lateFailure = await _client.PostAsJsonAsync(Url,
            ApiFactory.NotificationBody(Payload(tx.InvoiceNo, respCode: "4005", respDesc: "Transaction declined")));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, lateFailure.StatusCode);
        var saved = await factory.ReloadAsync(tx.InvoiceNo);
        Assert.Equal(TransactionStatus.Succeeded, saved.Status);
        Assert.NotEqual("4005", saved.RespCode);
    }

    [Fact]
    public async Task Tokenized_notification_saves_the_card_once_and_refreshes_the_card_list()
    {
        var tx = await factory.SeedPendingAsync();
        var token = "TEST" + Guid.NewGuid().ToString("N")[..16];
        var cardsUrl = $"/api/customers/{tx.CustomerId}/cards";
        try
        {
            await _client.GetAsync(cardsUrl); // fill the cache, so the list below proves the invalidation
            var body = ApiFactory.NotificationBody(Payload(tx.InvoiceNo, customerToken: token));

            var first = await _client.PostAsJsonAsync(Url, body);
            var duplicate = await _client.PostAsJsonAsync(Url, body);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
            Assert.Equal(TransactionStatus.Succeeded, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
            Assert.Equal(1, await factory.CountCardsAsync(tx.CustomerId, token));
            var cards = await _client.GetFromJsonAsync<List<CustomerCardDto>>(cardsUrl, Json);
            Assert.Contains(cards!, c => c.MaskedCardNo == "411111XXXXXX1111");
        }
        finally
        {
            await factory.DeleteCardAsync(tx.CustomerId, token);
        }
    }

    [Fact]
    public async Task Notification_with_empty_customer_token_saves_no_card()
    {
        var tx = await factory.SeedPendingAsync();
        var cardsBefore = await factory.CountCardsAsync(tx.CustomerId);

        var res = await _client.PostAsJsonAsync(Url, ApiFactory.NotificationBody(Payload(tx.InvoiceNo, customerToken: "")));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(TransactionStatus.Succeeded, (await factory.ReloadAsync(tx.InvoiceNo)).Status);
        Assert.Equal(cardsBefore, await factory.CountCardsAsync(tx.CustomerId));
    }
}
