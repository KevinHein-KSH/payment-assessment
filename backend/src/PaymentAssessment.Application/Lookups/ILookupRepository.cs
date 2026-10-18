using System;

namespace PaymentAssessment.Application.Lookups;

public interface ILookupRepository
{
    Task<IEnumerable<CustomerDto>> GetCustomersAsync(CancellationToken ct);

    Task<IEnumerable<ProductDto>> GetProductsAsync(CancellationToken ct);

    Task<IEnumerable<CustomerCardDto>> GetCustomerCardsAsync(int customerId, CancellationToken ct);
    Task<bool> CustomerExistsAsync(int customerId, CancellationToken ct);

    /// <summary>Inserts a saved card. Returns false when (customer_id, customer_token) already exists.</summary>
    Task<bool> AddCardAsync(int customerId, string customerToken, string maskedCardNo, CancellationToken ct);
}
