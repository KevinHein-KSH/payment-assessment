using PaymentAssessment.Application.Caching;

namespace PaymentAssessment.Application.Lookups;

public class LookupService
{
    public LookupService(ILookupRepository lookupRepository, ICacheService cacheService)
    {
        LookupRepository = lookupRepository;
        CacheService = cacheService;
    }

    private ILookupRepository LookupRepository { get; }
    private ICacheService CacheService { get; }

    public Task<IEnumerable<CustomerDto>> GetCustomersAsync(CancellationToken ct)
        => CacheService.GetOrSetAsync("customers", () => LookupRepository.GetCustomersAsync(ct), TimeSpan.FromMinutes(10));

    public Task<IEnumerable<ProductDto>> GetProductsAsync(CancellationToken ct)
        => CacheService.GetOrSetAsync("products", () => LookupRepository.GetProductsAsync(ct), TimeSpan.FromMinutes(10));

    public async Task<IEnumerable<CustomerCardDto>?> GetCustomerCardsAsync(int customerId, CancellationToken ct)
    {
        if (!await LookupRepository.CustomerExistsAsync(customerId, ct))
            return null;
        return await CacheService.GetOrSetAsync($"customer:{customerId}:cards", () => LookupRepository.GetCustomerCardsAsync(customerId, ct), TimeSpan.FromMinutes(10));
    }
    public Task RemoveCardsCacheAsync(int customerId)
        => CacheService.RemoveAsync($"customer:{customerId}:cards");
}
