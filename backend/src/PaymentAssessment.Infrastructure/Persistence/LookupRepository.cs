using PaymentAssessment.Application.Lookups;
using Microsoft.EntityFrameworkCore;
using PaymentAssessment.Domain.Entities;


namespace PaymentAssessment.Infrastructure.Persistence;

public class LookupRepository(AppDbContext db) : ILookupRepository
{
    public async Task<IEnumerable<CustomerDto>> GetCustomersAsync(CancellationToken ct)
        =>  await db.Customers.AsNoTracking()
            .Select(c => new CustomerDto(c.Id, c.FirstName, c.LastName, c.Email))
            .ToListAsync(ct);

    public async Task<IEnumerable<ProductDto>> GetProductsAsync(CancellationToken ct)
        =>  await db.Products.AsNoTracking()
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Currency, p.PaymentType))
            .ToListAsync(ct);

    public async Task<IEnumerable<CustomerCardDto>> GetCustomerCardsAsync(int customerId, CancellationToken ct)
        =>  await db.CustomerCards.AsNoTracking()
            .Where(cc => cc.CustomerId == customerId)
            .Select(cc => new CustomerCardDto(cc.Id, cc.MaskedCardNo))
            .ToListAsync(ct);

    public Task<bool> CustomerExistsAsync(int customerId, CancellationToken ct)
        => db.Customers.AnyAsync(c => c.Id == customerId, ct);

    public async Task<bool> AddCardAsync(int customerId, string customerToken, string maskedCardNo, CancellationToken ct)
    {
        // Common duplicate (2C2P resending a notification) without an EF error log; the catch below covers the race
        if (await db.CustomerCards.AnyAsync(c => c.CustomerId == customerId && c.CustomerToken == customerToken, ct))
            return false;

        db.CustomerCards.Add(new CustomerCard
        {
            CustomerId = customerId,
            CustomerToken = customerToken,
            MaskedCardNo = maskedCardNo,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlConnector.MySqlException { ErrorCode: MySqlConnector.MySqlErrorCode.DuplicateKeyEntry })
        {
            return false;
        }
    }

    // Lookup and ownership check in one query. Not cached: the cards cache holds masked numbers only, never tokens.
    public Task<string?> GetCustomerTokenAsync(int customerId, int cardId, CancellationToken ct)
        => db.CustomerCards.AsNoTracking()
            .Where(c => c.Id == cardId && c.CustomerId == customerId)
            .Select(c => (string?)c.CustomerToken)
            .FirstOrDefaultAsync(ct);
}
