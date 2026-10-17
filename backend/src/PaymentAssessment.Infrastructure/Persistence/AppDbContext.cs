using Microsoft.EntityFrameworkCore;
using PaymentAssessment.Domain.Entities;
using PaymentAssessment.Domain.Enums;

namespace PaymentAssessment.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<CustomerCard> CustomerCards => Set<CustomerCard>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<PaymentType>().HaveConversion<string>();
        c.Properties<TransactionStatus>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<PaymentTransaction>(e =>
        {
            e.HasIndex(t => t.InvoiceNo).IsUnique();
            e.HasIndex(t => t.IdempotencyKey).IsUnique();
            e.HasOne(t => t.Customer).WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Product).WithMany().HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<CustomerCard>(e =>
        {
            e.HasIndex(c => new { c.CustomerId, c.CustomerToken }).IsUnique();
            e.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        // Seeds go into the migration, so they are applied once and are repeat-safe; same data as frontend/lib/mock-data.ts
        b.Entity<Customer>().HasData(
            new Customer { Id = 1, FirstName = "Alice", LastName = "Tan", Email = "alice.tan@example.com" },
            new Customer { Id = 2, FirstName = "Ben", LastName = "Lim", Email = "ben.lim@example.com" },
            new Customer { Id = 3, FirstName = "Chloe", LastName = "Ng", Email = "chloe.ng@example.com" });

        b.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Single Class Pass", Description = "One drop-in class", Price = 25.00m, Currency = "SGD", PaymentType = PaymentType.OneTime },
            new Product { Id = 2, Name = "10-Class Pack", Description = "Ten classes, valid for 3 months", Price = 220.00m, Currency = "SGD", PaymentType = PaymentType.OneTime },
            new Product { Id = 3, Name = "Monthly Membership", Description = "Unlimited classes, billed monthly", Price = 149.00m, Currency = "SGD", PaymentType = PaymentType.Recurring },
            new Product { Id = 4, Name = "Annual Membership", Description = "Unlimited classes, billed yearly", Price = 1490.00m, Currency = "SGD", PaymentType = PaymentType.Recurring });
    }
    // Column types and lengths live on the entities (annotations); this class adds only
    // EF-specific settings: enum-as-string, unique keys, delete rules and seed data.
}