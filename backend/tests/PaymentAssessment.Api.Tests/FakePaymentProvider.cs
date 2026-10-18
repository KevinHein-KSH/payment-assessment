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

    public int TokenCalls => Volatile.Read(ref _tokenCalls);

    /// <summary>Inquiry answer per invoiceNo; anything not listed is "2002 Transaction not found." like the sandbox.</summary>
    public ConcurrentDictionary<string, PaymentNotification> InquiryResults { get; } = new();

    public async Task<PaymentTokenResult> CreatePaymentTokenAsync(
        string invoiceNo, string description, decimal amount, string currency, CancellationToken ct)
    {
        Interlocked.Increment(ref _tokenCalls);
        await Task.Delay(300, ct); // a slow 2C2P keeps parallel requests overlapping
        return new PaymentTokenResult(true, "0000", "Success", $"fake-token-{invoiceNo}", $"https://pay.example/{invoiceNo}");
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
