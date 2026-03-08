namespace WindowsOptimizer.App.Services.Monitoring;

/// <summary>
/// Base class for dedicated monitoring threads.
/// Each metric type (CPU, GPU, Memory, etc.) runs on its own background thread
/// to prevent UI blocking and ensure smooth 60 FPS rendering.
/// </summary>
public abstract class MonitorThread : IDisposable
{
    private Thread? _thread;
    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    /// <summary>
    /// Update interval for this monitor (500ms or 1000ms).
    /// </summary>
    protected abstract TimeSpan UpdateInterval { get; }

    /// <summary>
    /// Collect metrics on background thread.
    /// </summary>
    protected abstract Task<object> CollectMetricsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called when new metrics are available (on background thread).
    /// </summary>
    protected abstract void OnMetricsCollected(object metrics);

    /// <summary>
    /// Start the monitoring thread.
    /// </summary>
    public void Start()
    {
        if (_thread != null)
            throw new InvalidOperationException("Monitor thread already started");

        _cts = new CancellationTokenSource();
        _thread = new Thread(async () => await MonitorLoopAsync())
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal, // Don't compete with user apps
            Name = GetType().Name
        };
        _thread.Start();
    }

    /// <summary>
    /// Stop the monitoring thread.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _thread?.Join(TimeSpan.FromSeconds(2)); // Wait max 2 seconds
    }

    private async Task MonitorLoopAsync()
    {
        while (!_cts!.Token.IsCancellationRequested)
        {
            try
            {
                var metrics = await CollectMetricsAsync(_cts.Token);
                
                if (!_cts.Token.IsCancellationRequested)
                {
                    OnMetricsCollected(metrics);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception)
            {
                // Log but don't crash monitor thread
                // Production: Use ILogger
            }

            try
            {
                await Task.Delay(UpdateInterval, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        Stop();
        _cts?.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
