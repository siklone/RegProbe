using System.Collections;
using System.Collections.ObjectModel;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Fixed-size circular buffer for history data.
/// Avoids memory allocations when updating history collections.
/// Target: Prevents ObservableCollection growth overhead.
/// </summary>
public class RingBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        
        _buffer = new T[capacity];
        _head = 0;
        _count = 0;
    }

    public int Capacity => _buffer.Length;
    public int Count => _count;

    /// <summary>
    /// Add an item to the buffer. If full, overwrites the oldest item.
    /// </summary>
    public void Push(T item)
    {
        _buffer[_head] = item;
        _head = (_head + 1) % _buffer.Length;
        if (_count < _buffer.Length)
            _count++;
    }

    /// <summary>
    /// Get item at logical index (0 = oldest, Count-1 = newest).
    /// </summary>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            // Calculate physical index from logical index
            var physicalIndex = (_head - _count + index + _buffer.Length) % _buffer.Length;
            return _buffer[physicalIndex];
        }
    }

    /// <summary>
    /// Get the most recent item.
    /// </summary>
    public T? Latest => _count > 0 ? this[_count - 1] : default;

    /// <summary>
    /// Get the oldest item.
    /// </summary>
    public T? Oldest => _count > 0 ? this[0] : default;

    /// <summary>
    /// Clear the buffer.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _head = 0;
        _count = 0;
    }

    /// <summary>
    /// Copy to an ObservableCollection efficiently.
    /// </summary>
    public void CopyTo(ObservableCollection<T> target)
    {
        while (target.Count > _count)
            target.RemoveAt(target.Count - 1);

        for (int i = 0; i < _count; i++)
        {
            if (i < target.Count)
                target[i] = this[i];
            else
                target.Add(this[i]);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
