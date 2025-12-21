using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Tasks;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed class ScheduledTaskBatchTweak : ITweak
{
    private readonly IReadOnlyList<string> _taskPaths;
    private readonly IScheduledTaskManager _taskManager;
    private readonly Dictionary<string, TaskSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasDetected;

    public ScheduledTaskBatchTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<string> taskPaths,
        IScheduledTaskManager taskManager,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));

        if (taskPaths is null)
        {
            throw new ArgumentNullException(nameof(taskPaths));
        }

        if (taskPaths.Count == 0)
        {
            throw new ArgumentException("At least one scheduled task is required.", nameof(taskPaths));
        }

        if (taskPaths.Any(path => string.IsNullOrWhiteSpace(path)))
        {
            throw new ArgumentException("Task paths must be provided.", nameof(taskPaths));
        }

        _taskPaths = taskPaths;
        RequiresElevation = requiresElevation ?? true;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            _snapshots.Clear();
            foreach (var taskPath in _taskPaths)
            {
                var info = await _taskManager.QueryAsync(taskPath, ct);
                _snapshots[taskPath] = new TaskSnapshot(info.Exists, info.Enabled);
            }

            _hasDetected = true;
            var detectedCount = _snapshots.Values.Count(snapshot => snapshot.Exists);
            var message = $"Detected {detectedCount} of {_taskPaths.Count} tasks.";
            return new TweakResult(TweakStatus.Detected, message, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Detect failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var updatedCount = 0;
            foreach (var taskPath in _taskPaths)
            {
                var snapshot = _snapshots.TryGetValue(taskPath, out var cached)
                    ? cached
                    : await GetSnapshotAsync(taskPath, ct);

                if (!snapshot.Exists)
                {
                    continue;
                }

                await _taskManager.SetEnabledAsync(taskPath, false, ct);
                updatedCount++;
            }

            return new TweakResult(
                TweakStatus.Applied,
                $"Disabled {updatedCount} tasks.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Apply failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            foreach (var taskPath in _taskPaths)
            {
                var info = await _taskManager.QueryAsync(taskPath, ct);
                if (!info.Exists)
                {
                    continue;
                }

                if (info.Enabled)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Verification failed. '{taskPath}' is still enabled.",
                        DateTimeOffset.UtcNow);
                }
            }

            return new TweakResult(TweakStatus.Verified, "Verified scheduled tasks are disabled.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Verify failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected)
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            var failures = 0;
            foreach (var (taskPath, snapshot) in _snapshots)
            {
                if (!snapshot.Exists)
                {
                    continue;
                }

                try
                {
                    await _taskManager.SetEnabledAsync(taskPath, snapshot.Enabled, ct);
                }
                catch
                {
                    failures++;
                }
            }

            if (failures > 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Rollback failed for {failures} tasks.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(TweakStatus.RolledBack, "Rolled back scheduled task state.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Rollback failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    private async Task<TaskSnapshot> GetSnapshotAsync(string taskPath, CancellationToken ct)
    {
        var info = await _taskManager.QueryAsync(taskPath, ct);
        return new TaskSnapshot(info.Exists, info.Enabled);
    }

    private sealed record TaskSnapshot(bool Exists, bool Enabled);
}
