using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Threading;

/// <summary>
/// Lock-free message bus for publishing metric updates from worker threads.
/// Uses bounded Channel for backpressure and debounced batching for efficiency.
/// UI-agnostic - dispatching to UI thread is handled by the consumer via the UiDispatcher callback.
/// </summary>
public sealed class MetricDataBus : IDisposable
{
    private readonly Channel<MetricUpdate> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _dispatcherTask;
    private readonly Dictionary<string, MetricUpdate> _latestValues;
    private readonly object _lock = new();
    private readonly Action<Action>? _uiDispatcher;
    private DateTime _lastDispatch = DateTime.MinValue;
    private bool _disposed;

    // 60 FPS = ~16.67ms per frame
    private const int TargetFps = 60;
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(1000.0 / TargetFps);
    private const int ChannelCapacity = 1000;

    /// <summary>
    /// Raised with batched metric updates. If a UI dispatcher was provided,
    /// this event is raised on the UI thread; otherwise on a background thread.
    /// </summary>
    public event EventHandler<MetricBatchEventArgs>? MetricsUpdated;

    /// <summary>
    /// Creates a new MetricDataBus.
    /// </summary>
    /// <param name="uiDispatcher">
    /// Optional callback to dispatch events to the UI thread.
    /// For WPF, pass: action => Application.Current.Dispatcher.InvokeAsync(action, DispatcherPriority.DataBind)
    /// If null, events are raised on a background thread.
    /// </param>
    public MetricDataBus(Action<Action>? uiDispatcher = null)
    {
        _uiDispatcher = uiDispatcher;
        _channel = Channel.CreateBounded<MetricUpdate>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        _latestValues = new Dictionary<string, MetricUpdate>();
        _cts = new CancellationTokenSource();
        _dispatcherTask = Task.Run(() => DispatchLoop(_cts.Token));
    }

    /// <summary>
    /// Publishes a metric update. Thread-safe, non-blocking.
    /// If the channel is full, oldest updates are dropped.
    /// </summary>
    /// <param name="update">The metric update to publish.</param>
    public void Publish(MetricUpdate update)
    {
        if (_disposed) return;

        // TryWrite is non-blocking
        if (!_channel.Writer.TryWrite(update))
        {
            Debug.WriteLine($"[MetricDataBus] Channel full, update dropped: {update.Key}");
        }
    }

    /// <summary>
    /// Publishes a metric update with the specified key and value.
    /// </summary>
    /// <param name="key">Unique identifier for the metric (e.g., "cpu.usage").</param>
    /// <param name="value">The metric value.</param>
    public void Publish(string key, object value)
    {
        Publish(new MetricUpdate(key, value, DateTime.UtcNow));
    }

    /// <summary>
    /// Publishes multiple metric updates atomically.
    /// </summary>
    /// <param name="updates">The updates to publish.</param>
    public void PublishBatch(IEnumerable<MetricUpdate> updates)
    {
        if (_disposed) return;

        foreach (var update in updates)
        {
            _channel.Writer.TryWrite(update);
        }
    }

    private async Task DispatchLoop(CancellationToken ct)
    {
        Debug.WriteLine("[MetricDataBus] Dispatch loop started");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Drain all available items from channel
                while (_channel.Reader.TryRead(out var update))
                {
                    lock (_lock)
                    {
                        // Only keep latest value per key
                        _latestValues[update.Key] = update;
                    }
                }

                // Throttle to target FPS
                var elapsed = DateTime.UtcNow - _lastDispatch;
                if (elapsed >= FrameInterval)
                {
                    DispatchBatch();
                    _lastDispatch = DateTime.UtcNow;
                }

                // Wait for next item or frame interval
                var waitTask = _channel.Reader.WaitToReadAsync(ct).AsTask();
                var delayTask = Task.Delay(FrameInterval, ct);
                await Task.WhenAny(waitTask, delayTask);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MetricDataBus] Dispatch error: {ex.Message}");
            }
        }

        Debug.WriteLine("[MetricDataBus] Dispatch loop ended");
    }

    private void DispatchBatch()
    {
        Dictionary<string, MetricUpdate> snapshot;
        lock (_lock)
        {
            if (_latestValues.Count == 0) return;
            snapshot = new Dictionary<string, MetricUpdate>(_latestValues);
            _latestValues.Clear();
        }

        void RaiseEvent()
        {
            try
            {
                MetricsUpdated?.Invoke(this, new MetricBatchEventArgs(snapshot));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MetricDataBus] Handler error: {ex.Message}");
            }
        }

        if (_uiDispatcher != null)
        {
            _uiDispatcher(RaiseEvent);
        }
        else
        {
            RaiseEvent();
        }
    }

    /// <summary>
    /// Gets the number of pending updates in the channel.
    /// </summary>
    public int PendingCount => _channel.Reader.Count;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _channel.Writer.Complete();

        try
        {
            _dispatcherTask.Wait(1000);
        }
        catch { /* Timeout or cancelled */ }

        _cts.Dispose();
        Debug.WriteLine("[MetricDataBus] Disposed");
    }
}

/// <summary>
/// Represents a single metric update.
/// </summary>
/// <param name="Key">Unique identifier for the metric (e.g., "cpu.usage", "ram.used").</param>
/// <param name="Value">The metric value (can be any type).</param>
/// <param name="Timestamp">When the measurement was taken.</param>
public record MetricUpdate(string Key, object Value, DateTime Timestamp);

/// <summary>
/// Event args containing a batch of metric updates.
/// </summary>
public class MetricBatchEventArgs : EventArgs
{
    /// <summary>
    /// Gets the updates in this batch, keyed by metric name.
    /// </summary>
    public IReadOnlyDictionary<string, MetricUpdate> Updates { get; }

    public MetricBatchEventArgs(Dictionary<string, MetricUpdate> updates)
    {
        Updates = updates;
    }

    /// <summary>
    /// Gets a typed value from the batch.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The metric key.</param>
    /// <returns>The value if found and of correct type, otherwise default.</returns>
    public T? GetValue<T>(string key)
    {
        if (Updates.TryGetValue(key, out var update) && update.Value is T typed)
            return typed;
        return default;
    }

    /// <summary>
    /// Tries to get a typed value from the batch.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The metric key.</param>
    /// <param name="value">The value if found.</param>
    /// <returns>True if the key exists and value is of correct type.</returns>
    public bool TryGetValue<T>(string key, out T? value)
    {
        value = default;
        if (Updates.TryGetValue(key, out var update) && update.Value is T typed)
        {
            value = typed;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a key exists in this batch.
    /// </summary>
    /// <param name="key">The metric key to check.</param>
    /// <returns>True if the key exists.</returns>
    public bool Contains(string key) => Updates.ContainsKey(key);

    /// <summary>
    /// Gets the number of updates in this batch.
    /// </summary>
    public int Count => Updates.Count;
}
