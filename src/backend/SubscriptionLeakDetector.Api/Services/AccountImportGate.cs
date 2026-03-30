using System.Collections.Concurrent;

namespace SubscriptionLeakDetector.Api.Services;

/// <summary>
/// Ensures only one import + detection + alerts pipeline runs at a time per account.
/// Concurrent POSTs would otherwise interleave recurring detection and corrupt subscription state.
/// </summary>
public interface IAccountImportGate
{
    Task<IDisposable> EnterAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public sealed class AccountImportGate : IAccountImportGate
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> EnterAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var sem = _locks.GetOrAdd(accountId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(cancellationToken);
        return new Releaser(sem);
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _sem;

        public Releaser(SemaphoreSlim sem) => _sem = sem;

        public void Dispose()
        {
            var s = Interlocked.Exchange(ref _sem, null);
            s?.Release();
        }
    }
}
