namespace PaymentAssessment.Application.Locking;

/// <summary>Cross-instance lock (docs/05, Idempotency and locking). Implemented with Redis in Infrastructure.</summary>
public interface IDistributedLock
{
    /// <summary>
    /// Takes the lock for <paramref name="ttl"/>. Returns null when another owner holds it.
    /// Disposing the handle releases the lock only if this owner still holds it.
    /// If the lock store is unreachable, returns a no-op handle: the MySQL unique index on
    /// idempotency_key is then the guard against duplicates.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken ct);
}
