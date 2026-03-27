using System.Collections.Concurrent;

namespace RegProbe.App.Services;

/// <summary>
/// Caches WMI query results to reduce expensive WMI overhead.
/// Target: 30-50% reduction in WMI query cost.
/// </summary>
public class WmiCache
{
    private record CacheEntry<T>(T Value, DateTime Expiry);
    private readonly ConcurrentDictionary<string, object> _cache = new();

    /// <summary>
    /// Get cached result or execute query if expired/missing.
    /// </summary>
    public T GetOrQuery<T>(string queryKey, Func<T> queryFunc, TimeSpan cacheDuration)
    {
        if (_cache.TryGetValue(queryKey, out var cached))
        {
            var entry = (CacheEntry<T>)cached;
            if (entry.Expiry > DateTime.UtcNow)
            {
                return entry.Value;
            }
        }

        var result = queryFunc();
        _cache[queryKey] = new CacheEntry<T>(result, DateTime.UtcNow + cacheDuration);
        return result;
    }

    /// <summary>
    /// Async version for async queries.
    /// </summary>
    public async Task<T> GetOrQueryAsync<T>(string queryKey, Func<Task<T>> queryFunc, TimeSpan cacheDuration)
    {
        if (_cache.TryGetValue(queryKey, out var cached))
        {
            var entry = (CacheEntry<T>)cached;
            if (entry.Expiry > DateTime.UtcNow)
            {
                return entry.Value;
            }
        }

        var result = await queryFunc();
        _cache[queryKey] = new CacheEntry<T>(result, DateTime.UtcNow + cacheDuration);
        return result;
    }

    /// <summary>
    /// Clear specific cache entry.
    /// </summary>
    public void Invalidate(string queryKey)
    {
        _cache.TryRemove(queryKey, out _);
    }

    /// <summary>
    /// Clear all cached entries.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    public int CachedItemCount => _cache.Count;
}
