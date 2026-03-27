using System;

namespace RegProbe.App.Services;

internal sealed class CacheEntry
{
    public object Value { get; init; } = null!;
    public DateTime CachedAt { get; init; }
    public TimeSpan Ttl { get; init; }

    public bool IsExpired => CachedAt + Ttl < DateTime.UtcNow;
}
