using Microsoft.Extensions.Logging;
using PaymentAssessment.Application.Locking;
using StackExchange.Redis;

namespace PaymentAssessment.Infrastructure.Locking;

public class RedisDistributedLock(IConnectionMultiplexer redis, ILogger<RedisDistributedLock> logger) : IDistributedLock
{
    // Deletes the key only if it still holds our token, so an expired owner can't release someone else's lock
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        var token = Guid.NewGuid().ToString("N");
        try
        {
            var acquired = await redis.GetDatabase().StringSetAsync(key, token, ttl, When.NotExists);
            return acquired ? new Handle(redis, key, token, logger) : null;
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(ex, "Redis is down. Proceeding without acquiring the lock.");
            return new NoOpHandle();
        }
    }
    private sealed class Handle(IConnectionMultiplexer redis, string key, string token, ILogger logger) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await redis.GetDatabase().ScriptEvaluateAsync(ReleaseScript, new RedisKey[] { key }, new RedisValue[] { token });
            }
            catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
            {
                // Log the exception but do not throw, as the lock will eventually expire due to TTL.
                // This ensures that if Redis is down, we don't block the application flow.
                logger.LogWarning(ex, "Failed to release the lock for key {Key}. The lock will expire automatically due to TTL.", key);
            }
        }
    }

    private sealed class NoOpHandle : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
