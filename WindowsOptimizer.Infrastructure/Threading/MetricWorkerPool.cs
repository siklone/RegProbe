using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Threading;

/// <summary>
/// Dedicated worker thread pool for metric collection.
/// Uses long-running tasks for CPU affinity and priority control.
/// </summary>
public sealed class MetricWorkerPool : IDisposable
{
    private readonly MetricDataBus _bus;
    private readonly CancellationTokenSource _cts;
    private readonly List<Task> _workers;
    private readonly BlockingCollection<WorkItem> _workQueue;
    private readonly ThreadingDiagnostics _diagnostics;
    private readonly int _workerCount;
    private bool _disposed;

    /// <summary>
    /// Creates a new worker pool with the specified number of workers.
    /// </summary>
    /// <param name="bus">The metric data bus to publish results to.</param>
    /// <param name="workerCount">Number of worker threads (default: 4).</param>
    /// <param name="diagnostics">Optional diagnostics instance for performance tracking.</param>
    public MetricWorkerPool(MetricDataBus bus, int workerCount = 4, ThreadingDiagnostics? diagnostics = null)
    {
        _bus = bus;
        _workerCount = workerCount;
        _diagnostics = diagnostics ?? new ThreadingDiagnostics();
        _cts = new CancellationTokenSource();
        _workQueue = new BlockingCollection<WorkItem>(
            new ConcurrentQueue<WorkItem>());

        _workers = Enumerable.Range(0, workerCount)
            .Select(i => Task.Factory.StartNew(
                async () => await WorkerLoop(i, _cts.Token),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap())
            .ToList();

        Debug.WriteLine($"[MetricWorkerPool] Started with {workerCount} workers");
    }

    /// <summary>
    /// Gets the metric data bus used by this pool.
    /// </summary>
    public MetricDataBus Bus => _bus;

    /// <summary>
    /// Gets the diagnostics instance for this pool.
    /// </summary>
    public ThreadingDiagnostics Diagnostics => _diagnostics;

    /// <summary>
    /// Queues work for execution by a worker thread.
    /// </summary>
    /// <param name="name">Name for diagnostics.</param>
    /// <param name="work">The work to execute.</param>
    /// <param name="priority">Higher priority = executed first (not yet implemented).</param>
    public void QueueWork(string name, Func<CancellationToken, Task> work, int priority = 0)
    {
        if (_disposed) return;

        var item = new WorkItem(name, work, priority, DateTime.UtcNow);
        _workQueue.Add(item);
    }

    /// <summary>
    /// Queues synchronous work for execution by a worker thread.
    /// </summary>
    /// <param name="name">Name for diagnostics.</param>
    /// <param name="work">The work to execute.</param>
    public void QueueWork(string name, Action<CancellationToken> work)
    {
        QueueWork(name, ct =>
        {
            work(ct);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Schedules recurring work at a specified interval.
    /// </summary>
    /// <param name="name">Name for diagnostics.</param>
    /// <param name="work">The work to execute on each interval.</param>
    /// <param name="interval">Time between executions.</param>
    /// <param name="priority">Priority for queued work items.</param>
    /// <returns>A CancellationTokenSource to stop the recurring work.</returns>
    public CancellationTokenSource ScheduleRecurring(string name, Func<CancellationToken, Task> work,
        TimeSpan interval, int priority = 0)
    {
        var recurringCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

        _ = Task.Run(async () =>
        {
            while (!recurringCts.Token.IsCancellationRequested)
            {
                QueueWork(name, work, priority);
                try
                {
                    await Task.Delay(interval, recurringCts.Token);
                }
                catch (OperationCanceledException) { break; }
            }
        }, recurringCts.Token);

        return recurringCts;
    }

    /// <summary>
    /// Schedules recurring synchronous work at a specified interval.
    /// </summary>
    public CancellationTokenSource ScheduleRecurring(string name, Action<CancellationToken> work,
        TimeSpan interval, int priority = 0)
    {
        return ScheduleRecurring(name, ct =>
        {
            work(ct);
            return Task.CompletedTask;
        }, interval, priority);
    }

    private async Task WorkerLoop(int workerId, CancellationToken ct)
    {
        Debug.WriteLine($"[MetricWorkerPool] Worker {workerId} started");

        foreach (var item in _workQueue.GetConsumingEnumerable(ct))
        {
            if (ct.IsCancellationRequested) break;

            var sw = Stopwatch.StartNew();
            var success = true;

            try
            {
                await item.Work(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                success = false;
                Debug.WriteLine($"[MetricWorkerPool] Worker {workerId} error in '{item.Name}': {ex.Message}");
            }
            finally
            {
                sw.Stop();
                _diagnostics.RecordWork(item.Name, sw.Elapsed, success);
            }
        }

        Debug.WriteLine($"[MetricWorkerPool] Worker {workerId} stopped");
    }

    /// <summary>
    /// Gets the current queue depth.
    /// </summary>
    public int QueueDepth => _workQueue.Count;

    /// <summary>
    /// Gets the number of worker threads.
    /// </summary>
    public int WorkerCount => _workerCount;

    /// <summary>
    /// Gets diagnostics report.
    /// </summary>
    public string GetDiagnosticsReport() => _diagnostics.GenerateReport();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _workQueue.CompleteAdding();
        _cts.Cancel();

        try
        {
            Task.WaitAll(_workers.ToArray(), 2000);
        }
        catch { /* Timeout or cancelled */ }

        _cts.Dispose();
        _workQueue.Dispose();

        Debug.WriteLine("[MetricWorkerPool] Disposed");
    }

    private record WorkItem(string Name, Func<CancellationToken, Task> Work, int Priority, DateTime QueuedAt);
}
