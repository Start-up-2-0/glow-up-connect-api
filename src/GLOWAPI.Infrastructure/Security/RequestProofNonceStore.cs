using System.Collections.Concurrent;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Infrastructure.Security;

public class RequestProofNonceStore : IRequestProofNonceStore
{
    private readonly ConcurrentDictionary<string, DateTime> _consumed = new();
    private readonly object _cleanupLock = new();
    private DateTime _lastCleanup = DateTime.UtcNow;

    public bool TryConsume(string nonce, TimeSpan ttl)
    {
        CleanupIfNeeded();

        var expiresAt = DateTime.UtcNow.Add(ttl);
        return _consumed.TryAdd(nonce, expiresAt);
    }

    private void CleanupIfNeeded()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < TimeSpan.FromMinutes(1))
        {
            return;
        }

        lock (_cleanupLock)
        {
            if (now - _lastCleanup < TimeSpan.FromMinutes(1))
            {
                return;
            }

            foreach (var entry in _consumed)
            {
                if (entry.Value <= now)
                {
                    _consumed.TryRemove(entry.Key, out _);
                }
            }

            _lastCleanup = now;
        }
    }
}
