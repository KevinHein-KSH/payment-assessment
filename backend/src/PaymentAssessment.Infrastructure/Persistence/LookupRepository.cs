using PaymentAssessment.Application.Lookups;
using Microsoft.EntityFrameworkCore;


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
}
