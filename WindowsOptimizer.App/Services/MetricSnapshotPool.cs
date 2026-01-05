using System.Collections.Concurrent;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Object pool for reusing metric snapshot objects to reduce GC pressure.
/// Target: 5-10 MB memory savings.
/// </summary>
public class MetricSnapshotPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly int _maxSize;
    private int _currentSize;

    public MetricSnapshotPool(int maxSize = 50)
    {
        _maxSize = maxSize;
    }

    /// <summary>
    /// Rent an object from the pool or create a new one.
    /// </summary>
    public T Rent()
    {
        if (_pool.TryTake(out var obj))
        {
            return obj;
        }

        Interlocked.Increment(ref _currentSize);
        return new T();
    }

    /// <summary>
    /// Return an object to the pool for reuse.
    /// </summary>
    public void Return(T obj)
    {
        if (_currentSize <= _maxSize)
        {
            _pool.Add(obj);
        }
        else
        {
            Interlocked.Decrement(ref _currentSize);
        }
    }

    public int PoolSize => _pool.Count;
    public int TotalCreated => _currentSize;
}
