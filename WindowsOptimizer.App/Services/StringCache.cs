using System.Collections.Concurrent;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Thread-safe string interning cache to reduce memory allocations.
/// Reuses string instances for repeated values like process names, status texts, etc.
/// Target: 2-5 MB memory savings in high-frequency update scenarios.
/// </summary>
public static class StringCache
{
    private static readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);
    private const int MaxCacheSize = 2000;
    private static int _cacheHits;
    private static int _cacheMisses;

    /// <summary>
    /// Intern a string, returning a cached instance if available.
    /// </summary>
    public static string Intern(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Check if already in cache
        if (_cache.TryGetValue(value, out var cached))
        {
            Interlocked.Increment(ref _cacheHits);
            return cached;
        }

        // Prevent unbounded growth
        if (_cache.Count >= MaxCacheSize)
        {
            Interlocked.Increment(ref _cacheMisses);
            return value;
        }

        // Intern and cache
        var interned = string.Intern(value);
        _cache[value] = interned;
        Interlocked.Increment(ref _cacheMisses);
        return interned;
    }

    /// <summary>
    /// Clear the cache (e.g., on view unload).
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
        _cacheHits = 0;
        _cacheMisses = 0;
    }

    public static int CacheHits => _cacheHits;
    public static int CacheMisses => _cacheMisses;
    public static int CacheSize => _cache.Count;
    public static double HitRatio => _cacheHits + _cacheMisses > 0 
        ? (double)_cacheHits / (_cacheHits + _cacheMisses) * 100 
        : 0;
}
