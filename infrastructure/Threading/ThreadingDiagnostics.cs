using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegProbe.Infrastructure.Threading;

/// <summary>
/// Collects and reports threading performance metrics.
/// Thread-safe for concurrent recording from multiple workers.
/// </summary>
public class ThreadingDiagnostics
{
    private readonly ConcurrentDictionary<string, WorkerStats> _stats = new();
    private DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Records the completion of a work item.
    /// </summary>
    /// <param name="workerName">Name of the work item.</param>
    /// <param name="duration">How long the work took.</param>
    /// <param name="success">Whether the work completed successfully.</param>
    public void RecordWork(string workerName, TimeSpan duration, bool success)
    {
        _stats.AddOrUpdate(
            workerName,
            _ => new WorkerStats
            {
                TotalTasks = 1,
                TotalTime = duration,
                Failures = success ? 0 : 1,
                MinTime = duration,
                MaxTime = duration,
                LastExecuted = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.TotalTasks++;
                existing.TotalTime += duration;
                if (!success) existing.Failures++;
                if (duration < existing.MinTime) existing.MinTime = duration;
                if (duration > existing.MaxTime) existing.MaxTime = duration;
                existing.LastExecuted = DateTime.UtcNow;
                return existing;
            });
    }

    /// <summary>
    /// Generates a formatted diagnostics report.
    /// </summary>
    /// <returns>A multi-line string containing the report.</returns>
    public string GenerateReport()
    {
        var uptime = DateTime.UtcNow - _startTime;
        var sb = new StringBuilder();

        sb.AppendLine("â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—");
        sb.AppendLine("â•‘                       Threading Diagnostics Report                        â•‘");
        sb.AppendLine($"â•‘  Uptime: {uptime.TotalMinutes:F1} minutes                                                        â•‘");
        sb.AppendLine("â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£");
        sb.AppendLine("â•‘ Worker Name               â”‚ Tasks  â”‚ Avg ms  â”‚ Min  â”‚ Max   â”‚ Fail %    â•‘");
        sb.AppendLine("â• â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•£");

        foreach (var (name, stats) in _stats)
        {
            var avgMs = stats.TotalTasks > 0
                ? stats.TotalTime.TotalMilliseconds / stats.TotalTasks
                : 0;
            var failRate = stats.TotalTasks > 0
                ? (double)stats.Failures / stats.TotalTasks * 100
                : 0;

            var displayName = name.Length > 24 ? name[..24] : name;
            sb.AppendLine(
                $"â•‘ {displayName,-25} â”‚ {stats.TotalTasks,6} â”‚ {avgMs,7:F1} â”‚ " +
                $"{stats.MinTime.TotalMilliseconds,4:F0} â”‚ {stats.MaxTime.TotalMilliseconds,5:F0} â”‚ {failRate,6:F1}%   â•‘");
        }

        if (_stats.IsEmpty)
        {
            sb.AppendLine("â•‘                          No data recorded yet                            â•‘");
        }

        sb.AppendLine("â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");
        return sb.ToString();
    }

    /// <summary>
    /// Gets statistics for a specific worker.
    /// </summary>
    /// <param name="workerName">The worker name.</param>
    /// <returns>Statistics if found, null otherwise.</returns>
    public WorkerStatsSnapshot? GetStats(string workerName)
    {
        if (_stats.TryGetValue(workerName, out var stats))
        {
            return new WorkerStatsSnapshot(
                stats.TotalTasks,
                stats.TotalTime,
                stats.Failures,
                stats.MinTime,
                stats.MaxTime,
                stats.LastExecuted);
        }
        return null;
    }

    /// <summary>
    /// Gets all worker names that have been recorded.
    /// </summary>
    public IEnumerable<string> GetWorkerNames() => _stats.Keys.ToArray();

    /// <summary>
    /// Gets the total number of tasks executed across all workers.
    /// </summary>
    public long TotalTasksExecuted
    {
        get
        {
            long total = 0;
            foreach (var stats in _stats.Values)
            {
                total += stats.TotalTasks;
            }
            return total;
        }
    }

    /// <summary>
    /// Gets the total number of failures across all workers.
    /// </summary>
    public long TotalFailures
    {
        get
        {
            long total = 0;
            foreach (var stats in _stats.Values)
            {
                total += stats.Failures;
            }
            return total;
        }
    }

    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public void Reset()
    {
        _stats.Clear();
        _startTime = DateTime.UtcNow;
    }

    private class WorkerStats
    {
        public long TotalTasks;
        public TimeSpan TotalTime;
        public long Failures;
        public TimeSpan MinTime = TimeSpan.MaxValue;
        public TimeSpan MaxTime = TimeSpan.Zero;
        public DateTime LastExecuted;
    }
}

/// <summary>
/// Immutable snapshot of worker statistics.
/// </summary>
/// <param name="TotalTasks">Total number of tasks executed.</param>
/// <param name="TotalTime">Total time spent executing tasks.</param>
/// <param name="Failures">Number of failed tasks.</param>
/// <param name="MinTime">Minimum execution time.</param>
/// <param name="MaxTime">Maximum execution time.</param>
/// <param name="LastExecuted">When the last task was executed.</param>
public record WorkerStatsSnapshot(
    long TotalTasks,
    TimeSpan TotalTime,
    long Failures,
    TimeSpan MinTime,
    TimeSpan MaxTime,
    DateTime LastExecuted)
{
    /// <summary>
    /// Gets the average execution time.
    /// </summary>
    public TimeSpan AverageTime => TotalTasks > 0
        ? TimeSpan.FromMilliseconds(TotalTime.TotalMilliseconds / TotalTasks)
        : TimeSpan.Zero;

    /// <summary>
    /// Gets the failure rate as a percentage.
    /// </summary>
    public double FailureRate => TotalTasks > 0
        ? (double)Failures / TotalTasks * 100
        : 0;
}
