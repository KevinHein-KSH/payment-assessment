using PaymentAssessment.Application.Caching;
using StackExchange.Redis;
using System.Text.Json;

namespace PaymentAssessment.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> load, TimeSpan ttl)
    {
        var db = redis.GetDatabase();
        var cachedValue = await db.StringGetAsync(key);
        if (cachedValue.HasValue)
            return JsonSerializer.Deserialize<T>(cachedValue.ToString())!;
        
        var value = await load();
        await db.StringSetAsync(key, JsonSerializer.Serialize(value), ttl);
        return value;
    }

    public Task RemoveAsync(string key)
        => redis.GetDatabase().KeyDeleteAsync(key);
}
