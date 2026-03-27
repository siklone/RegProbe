using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RegProbe.App.Services;

public interface IMetricCacheService
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value, TimeSpan? ttl = null) where T : class;
    bool TryGet<T>(string key, out T? value) where T : class;
    bool IsExpired(string key);
    void Invalidate(string key);
    void InvalidateAll();
}

public sealed class MetricCacheService : IMetricCacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static MetricCacheService Instance { get; } = new();

    public T? Get<T>(string key) where T : class
    {
        if (!_cache.TryGetValue(key, out var entry))
        {
            return null;
        }

        if (entry.IsExpired)
        {
            _cache.TryRemove(key, out _);
            Debug.WriteLine($"[MetricCacheService] Key '{key}' expired, returning null");
            return null;
        }

        // Type-safe cast: ensure stored value matches requested generic type
        if (entry.Value is T typed)
        {
            return typed;
        }

        Debug.WriteLine($"[MetricCacheService] Type mismatch for key '{key}': stored={entry.Value?.GetType().FullName}, requested={typeof(T).FullName}");
        return null;
    }

    public void Set<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        var effectiveTtl = ttl ?? DefaultTtl;
        var entry = new CacheEntry
        {
            Value = value!,
            CachedAt = DateTime.UtcNow,
            Ttl = effectiveTtl
        };

        _cache[key] = entry;
        Debug.WriteLine($"[MetricCacheService] Set '{key}', TTL={effectiveTtl.TotalMinutes}min");
    }

    public bool TryGet<T>(string key, out T? value) where T : class
    {
        value = Get<T>(key);
        return value != null;
    }

    public bool IsExpired(string key)
    {
        if (!_cache.TryGetValue(key, out var entry))
        {
            return true;
        }

        return entry.IsExpired;
    }

    public void Invalidate(string key)
    {
        if (_cache.TryRemove(key, out _))
        {
            Debug.WriteLine($"[MetricCacheService] Invalidated '{key}'");
        }
    }

    public void InvalidateAll()
    {
        var count = _cache.Count;
        _cache.Clear();
        Debug.WriteLine($"[MetricCacheService] Invalidated all {count} entries");
    }

    public void SetRaw(string key, object value, TimeSpan? ttl = null)
    {
        var effectiveTtl = ttl ?? DefaultTtl;
        var entry = new CacheEntry
        {
            Value = value,
            CachedAt = DateTime.UtcNow,
            Ttl = effectiveTtl
        };

        _cache[key] = entry;
        Debug.WriteLine($"[MetricCacheService] SetRaw '{key}', TTL={effectiveTtl.TotalMinutes}min");
    }

    public object? GetRaw(string key)
    {
        if (!_cache.TryGetValue(key, out var entry))
        {
            return null;
        }

        if (entry.IsExpired)
        {
            _cache.TryRemove(key, out _);
            return null;
        }

        return entry.Value;
    }
}
