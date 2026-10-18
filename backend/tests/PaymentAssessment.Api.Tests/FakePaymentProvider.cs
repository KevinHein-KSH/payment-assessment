using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaymentAssessment.Application.Payments;

namespace PaymentAssessment.Api.Tests;

/// <summary>Stands in for 2C2P (docs/05 allows mocking it): counts Payment Token calls and serves canned Inquiry answers.</summary>
public class FakePaymentProvider : IPaymentProvider
{
    private int _tokenCalls;
    private int _doPaymentCalls;
    private readonly ConcurrentDictionary<string, (decimal Amount, string Currency)> _amounts = new();

    public int TokenCalls => Volatile.Read(ref _tokenCalls);

    public int DoPaymentCalls => Volatile.Read(ref _doPaymentCalls);

    /// <summary>The customerToken each Payment Token request was sent with (null = new payment method), by invoiceNo.</summary>
    public ConcurrentDictionary<string, string?> CustomerTokenRequests { get; } = new();

    /// <summary>Do Payment reply per customerToken; anything not listed is 2000 "completed", like the sandbox.</summary>
    public ConcurrentDictionary<string, DoPaymentResult> DoPaymentResults { get; } = new();

    /// <summary>What Inquiry reports after a completed Do Payment, per customerToken; anything not listed is 0000 Success.</summary>
    public ConcurrentDictionary<string, (PaymentStatus Status, string RespCode, string RespDesc)> ChargeResults { get; } = new();

    /// <summary>Inquiry answer per invoiceNo; anything not listed is "2002 Transaction not found." like the sandbox.</summary>
    public ConcurrentDictionary<string, PaymentNotification> InquiryResults { get; } = new();

    /// <summary>The saveCard flag each Payment Token request was sent with, by invoiceNo.</summary>
    public ConcurrentDictionary<string, bool> SaveCardRequests { get; } = new();

    public async Task<PaymentTokenResult> CreatePaymentTokenAsync(
        string invoiceNo, string description, decimal amount, string currency, bool saveCard, string? customerToken,
        CancellationToken ct)
    {
        Interlocked.Increment(ref _tokenCalls);
        SaveCardRequests[invoiceNo] = saveCard;
        CustomerTokenRequests[invoiceNo] = customerToken;
        _amounts[invoiceNo] = (amount, currency);
        await Task.Delay(300, ct); // a slow 2C2P keeps parallel requests overlapping
        return new PaymentTokenResult(true, "0000", "Success", $"fake-token-{invoiceNo}", $"https://pay.example/{invoiceNo}");
    }

    public Task<DoPaymentResult> DoPaymentAsync(string paymentToken, string customerToken, string invoiceNo, CancellationToken ct)
    {
        Interlocked.Increment(ref _doPaymentCalls);
        var result = DoPaymentResults.GetValueOrDefault(customerToken, new DoPaymentResult(DoPaymentOutcome.Completed, "2000",
            "Transaction is completed, please do payment inquiry request for full payment information.", null));

        // A completed charge is what Inquiry then reports (shape from the sandbox Inquiry, 2026-10-18)
        if (result.Outcome == DoPaymentOutcome.Completed)
        {
            var (status, code, desc) = ChargeResults.GetValueOrDefault(customerToken, (PaymentStatus.Succeeded, "0000", "Success"));
            var (amount, currency) = _amounts[invoiceNo];
            InquiryResults.TryAdd(invoiceNo, new PaymentNotification(invoiceNo, status, amount, currency, code, desc,
                "VI", "411111XXXXXX1111", "15707875", customerToken));
        }
        return Task.FromResult(result);
    }

    public Task<PaymentNotification?> VerifyNotificationAsync(string payload, CancellationToken ct)
        => throw new NotSupportedException("Notification tests use the real 2C2P adapter (ApiFactory).");

    public Task<PaymentNotification> InquireAsync(string invoiceNo, CancellationToken ct)
        => Task.FromResult(InquiryResults.TryGetValue(invoiceNo, out var result)
            ? result
            : new PaymentNotification(invoiceNo, PaymentStatus.NotFound, 0m, "", "2002", "Transaction not found.", null, null, null, null));
}

/// <summary>The real API with IPaymentProvider swapped for <see cref="FakePaymentProvider"/>.</summary>
public class FakeProviderApiFactory : ApiFactory
{
    public FakePaymentProvider Provider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPaymentProvider>();
            services.AddSingleton<IPaymentProvider>(Provider);
        });
    }
}

[CollectionDefinition("FakeProviderApi")]
public class FakeProviderApiCollection : ICollectionFixture<FakeProviderApiFactory>;
