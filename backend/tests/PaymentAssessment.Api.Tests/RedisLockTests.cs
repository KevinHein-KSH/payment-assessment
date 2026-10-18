using Microsoft.Extensions.DependencyInjection;
using PaymentAssessment.Application.Locking;

namespace PaymentAssessment.Api.Tests;

/// <summary>The Redis lock from docs/05: SET NX PX, released only by its owner.</summary>
[Collection("Api")]
public class RedisLockTests(ApiFactory factory)
{
    private IDistributedLock Lock => factory.Services.GetRequiredService<IDistributedLock>();
    private static string NewKey() => $"lock:test:{Guid.NewGuid():N}";

    [Fact]
    public async Task Second_acquire_fails_while_held_and_succeeds_after_release()
    {
        var key = NewKey();
        var first = await Lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), default);

        Assert.NotNull(first);
        Assert.Null(await Lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), default));

        await first.DisposeAsync();
        var again = await Lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), default);
        Assert.NotNull(again);
        await again.DisposeAsync();
    }

    [Fact]
    public async Task Expired_owner_cannot_release_the_next_owners_lock()
    {
        var key = NewKey();
        var expired = await Lock.TryAcquireAsync(key, TimeSpan.FromMilliseconds(300), default);
        Assert.NotNull(expired);
        await Task.Delay(800); // TTL passes: Redis frees the key

        var current = await Lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), default);
        Assert.NotNull(current);

        await expired.DisposeAsync(); // must be a no-op: the token no longer matches
        Assert.Null(await Lock.TryAcquireAsync(key, TimeSpan.FromSeconds(30), default));

        await current.DisposeAsync();
    }
}
