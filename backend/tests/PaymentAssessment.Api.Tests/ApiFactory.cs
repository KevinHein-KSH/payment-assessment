using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentAssessment.Application.Lookups;
using PaymentAssessment.Domain.Entities;
using PaymentAssessment.Domain.Enums;
using PaymentAssessment.Infrastructure.Payments.TwoC2P;
using PaymentAssessment.Infrastructure.Persistence;

namespace PaymentAssessment.Api.Tests;

/// <summary>
/// Runs the real API against the docker compose MySQL and Redis (docs/06: no Testcontainers).
/// Needs `docker compose up -d mysql redis` and the git-ignored appsettings.Development.json for the DB login.
/// Tests share the dev database, so every row uses a unique invoiceNo and idempotency key.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestSecret = "test-secret-key-at-least-32-bytes-long!!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Notifications in these tests are signed with TestSecret, never the real merchant secret
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["TwoC2P:SecretKey"] = TestSecret }));
    }

    public async Task<PaymentTransaction> SeedPendingAsync(decimal amount = 25.00m, string? redirectUrl = null)
    {
        var tx = new PaymentTransaction
        {
            CustomerId = 1,
            ProductId = 1,
            InvoiceNo = "TEST" + Guid.NewGuid().ToString("N"),
            IdempotencyKey = Guid.NewGuid().ToString(),
            Amount = amount,
            Currency = "SGD",
            Status = TransactionStatus.Pending,
            RedirectUrl = redirectUrl,
            CreatedAt = DateTime.UtcNow
        };
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.PaymentTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx;
    }

    public async Task<int> CountByIdempotencyKeyAsync(string idempotencyKey)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PaymentTransactions.CountAsync(t => t.IdempotencyKey == idempotencyKey);
    }

    public async Task<PaymentTransaction> ReloadAsync(string invoiceNo)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PaymentTransactions.AsNoTracking().SingleAsync(t => t.InvoiceNo == invoiceNo);
    }

    public async Task<int> CountCardsAsync(int customerId, string? customerToken = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.CustomerCards.CountAsync(c => c.CustomerId == customerId
                                                      && (customerToken == null || c.CustomerToken == customerToken));
    }

    /// <summary>Removes a test card and its cached list, so test cards never show up in the demo's saved-card list.</summary>
    public async Task DeleteCardAsync(int customerId, string customerToken)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.CustomerCards.Where(c => c.CustomerId == customerId && c.CustomerToken == customerToken).ExecuteDeleteAsync();
        await scope.ServiceProvider.GetRequiredService<LookupService>().RemoveCardsCacheAsync(customerId);
    }

    /// <summary>Builds the { "payload": "&lt;JWT&gt;" } body 2C2P POSTs to backendReturnUrl.</summary>
    public static object NotificationBody(object payload, string secret = TestSecret)
        => new { payload = TwoC2PJwt.Sign(JsonSerializer.Serialize(payload), secret) };
}

/// <summary>One shared API instance: migrations run once, and test classes don't start the app in parallel.</summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFactory>;
