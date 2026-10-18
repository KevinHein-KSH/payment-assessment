using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PaymentAssessment.Application.Payments;
using PaymentAssessment.Application.Transactions;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Api.Tests;

/// <summary>
/// Result-page Inquiry fallback: a hosted-page cancel sends no notification, so a Pending row is checked with Payment Inquiry.
/// Answer shape from a real sandbox Inquiry after Cancel (2026-10-18): 0003 "Customer Cancellation", 220.0 SGD.
/// </summary>
[Collection("FakeProviderApi")]
public class InquiryFallbackTests(FakeProviderApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client = factory.CreateClient();

    private Task<TransactionDetailsDto?> GetAsync(string invoiceNo)
        => _client.GetFromJsonAsync<TransactionDetailsDto>($"/api/transactions/{invoiceNo}", Json);

    [Fact]
    public async Task Cancelled_hosted_payment_becomes_failed_with_the_reason()
    {
        var tx = await factory.SeedPendingAsync(amount: 220.00m, redirectUrl: "https://pay.example/cancelled");
        factory.Provider.InquiryResults[tx.InvoiceNo] = new PaymentNotification(tx.InvoiceNo, PaymentStatus.Failed,
            220.0m, "SGD", "0003", "Customer Cancellation", "", null, "1561566", null);

        var details = await GetAsync(tx.InvoiceNo);

        Assert.Equal(TransactionStatus.Failed, details!.Status);
        Assert.Equal("Customer Cancellation", details.RespDesc);
    }

    [Fact]
    public async Task Payment_not_found_at_the_provider_stays_pending()
    {
        // The customer may still be on the hosted page; Failed here would block their later successful notification
        var tx = await factory.SeedPendingAsync(redirectUrl: "https://pay.example/not-paid-yet");

        var details = await GetAsync(tx.InvoiceNo);

        Assert.Equal(TransactionStatus.Pending, details!.Status);
    }
}
