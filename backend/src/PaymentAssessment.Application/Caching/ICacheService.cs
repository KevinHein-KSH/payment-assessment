using System;

namespace PaymentAssessment.Application.Caching;

public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> load, TimeSpan ttl);
    Task RemoveAsync(string key);
}
