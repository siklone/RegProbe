using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTraceProject.App.Services;

/// <summary>
/// Manages preloading of app resources during splash screen.
/// Supports critical (must succeed) and non-critical (can fail) tasks.
/// Critical tasks run sequentially, non-critical run in parallel.
/// </summary>
public sealed class PreloadManager
{
    private readonly ConcurrentDictionary<string, PreloadTask> _tasks = new();
    private readonly SemaphoreSlim _throttle;
    private readonly IProgress<PreloadProgress> _progress;
    private readonly int _maxConcurrency;

    /// <summary>
    /// Creates a new PreloadManager.
    /// </summary>
    /// <param name="progress">Progress reporter for UI updates.</param>
    /// <param name="maxConcurrency">Max parallel non-critical tasks.</param>
    public PreloadManager(IProgress<PreloadProgress> progress, int maxConcurrency = 4)
    {
        _progress = progress;
        _maxConcurrency = maxConcurrency;
        _throttle = new SemaphoreSlim(maxConcurrency);
    }

    /// <summary>
    /// Registers a preload task.
    /// </summary>
    /// <param name="name">Task name for display/logging.</param>
    /// <param name="task">Async function to execute.</param>
    /// <param name="isCritical">If true, failure stops startup.</param>
    /// <param name="priority">Higher = runs first.</param>
    public void RegisterTask(string name, Func<CancellationToken, Task<object?>> task,
        bool isCritical = false, int priority = 0)
    {
        _tasks[name] = new PreloadTask
        {
            Name = name,
            Task = task,
            IsCritical = isCritical,
            Priority = priority
        };
    }

    /// <summary>
    /// Registers a preload task that returns void.
    /// </summary>
    public void RegisterTask(string name, Func<CancellationToken, Task> task,
        bool isCritical = false, int priority = 0)
    {
        RegisterTask(name, async ct =>
        {
            await task(ct);
            return null;
        }, isCritical, priority);
    }

    /// <summary>
    /// Runs all registered tasks.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing all loaded values and any errors.</returns>
    public async Task<PreloadResult> RunAllAsync(CancellationToken ct)
    {
        var result = new PreloadResult();
        var orderedTasks = _tasks.Values
            .OrderByDescending(t => t.IsCritical)
            .ThenByDescending(t => t.Priority)
            .ToList();

        var total = orderedTasks.Count;
        var completed = 0;

        Debug.WriteLine($"[PreloadManager] Starting {total} tasks ({orderedTasks.Count(t => t.IsCritical)} critical)");

        // 1. Run critical tasks sequentially
        var critical = orderedTasks.Where(t => t.IsCritical).ToList();
        foreach (var task in critical)
        {
            try
            {
                _progress.Report(new PreloadProgress(
                    completed, total, task.Name, PreloadState.Running));

                var sw = Stopwatch.StartNew();
                var value = await task.Task(ct);
                sw.Stop();

                result.Results[task.Name] = value;
                Debug.WriteLine($"[PreloadManager] Critical '{task.Name}' completed in {sw.ElapsedMilliseconds}ms");

                var current = Interlocked.Increment(ref completed);

                _progress.Report(new PreloadProgress(
                    current, total, task.Name, PreloadState.Completed));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Errors[task.Name] = ex;
                Debug.WriteLine($"[PreloadManager] Critical '{task.Name}' FAILED: {ex.Message}");

                var current = Interlocked.Increment(ref completed);

                _progress.Report(new PreloadProgress(
                    current, total, task.Name, PreloadState.Failed, ex.Message));

                // Critical task failed - throw to stop startup
                throw new PreloadException($"Critical task '{task.Name}' failed", ex);
            }
        }

        // 2. Run non-critical tasks in parallel
        var nonCritical = orderedTasks.Where(t => !t.IsCritical).ToList();
        var parallelTasks = nonCritical.Select(async task =>
        {
            await _throttle.WaitAsync(ct);
            try
            {
                _progress.Report(new PreloadProgress(
                    Volatile.Read(ref completed), total, task.Name, PreloadState.Running));

                var sw = Stopwatch.StartNew();
                var value = await task.Task(ct);
                sw.Stop();

                result.Results[task.Name] = value;
                Debug.WriteLine($"[PreloadManager] Non-critical '{task.Name}' completed in {sw.ElapsedMilliseconds}ms");

                var current = Interlocked.Increment(ref completed);

                _progress.Report(new PreloadProgress(
                    current, total, task.Name, PreloadState.Completed));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Don't log cancelled tasks
            }
            catch (Exception ex)
            {
                result.Errors[task.Name] = ex;
                Debug.WriteLine($"[PreloadManager] Non-critical '{task.Name}' failed: {ex.Message}");

                var current = Interlocked.Increment(ref completed);
                _progress.Report(new PreloadProgress(
                    current, total, task.Name, PreloadState.Failed, ex.Message));
            }
            finally
            {
                _throttle.Release();
            }
        });

        await Task.WhenAll(parallelTasks);

        Debug.WriteLine($"[PreloadManager] Completed with {result.Errors.Count} errors");
        return result;
    }

    /// <summary>
    /// Gets the number of registered tasks.
    /// </summary>
    public int TaskCount => _tasks.Count;

    /// <summary>
    /// Gets registered task names.
    /// </summary>
    public IEnumerable<string> TaskNames => _tasks.Keys;
}

/// <summary>
/// Progress update for preload operations.
/// </summary>
/// <param name="Completed">Number of tasks completed.</param>
/// <param name="Total">Total number of tasks.</param>
/// <param name="CurrentTask">Name of current task.</param>
/// <param name="State">Current state.</param>
/// <param name="Message">Optional message (e.g., error details).</param>
public record PreloadProgress(
    int Completed,
    int Total,
    string CurrentTask,
    PreloadState State,
    string? Message = null)
{
    /// <summary>
    /// Gets progress as percentage (0-100).
    /// </summary>
    public double Percentage => Total > 0 ? (double)Completed / Total * 100 : 0;
}

/// <summary>
/// State of a preload task.
/// </summary>
public enum PreloadState
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Result of preload operations.
/// </summary>
public class PreloadResult
{
    /// <summary>
    /// Successfully loaded values, keyed by task name.
    /// </summary>
    public ConcurrentDictionary<string, object?> Results { get; } = new();

    /// <summary>
    /// Errors from failed tasks, keyed by task name.
    /// </summary>
    public ConcurrentDictionary<string, Exception> Errors { get; } = new();

    /// <summary>
    /// Whether any tasks failed.
    /// </summary>
    public bool HasErrors => !Errors.IsEmpty;

    /// <summary>
    /// Gets a result value by name.
    /// </summary>
    public T? GetResult<T>(string name) where T : class
    {
        if (Results.TryGetValue(name, out var value))
            return value as T;
        return null;
    }
}

/// <summary>
/// Exception thrown when a critical preload task fails.
/// </summary>
public class PreloadException : Exception
{
    public PreloadException(string message) : base(message) { }
    public PreloadException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Internal representation of a preload task.
/// </summary>
internal class PreloadTask
{
    public required string Name { get; init; }
    public required Func<CancellationToken, Task<object?>> Task { get; init; }
    public bool IsCritical { get; init; }
    public int Priority { get; init; }
}
