using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Tasks;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed class ScheduledTaskBatchTweak : ITweak
{
    private readonly IReadOnlyList<string> _taskPaths;
    private readonly IScheduledTaskManager _taskManager;
    private readonly Dictionary<string, TaskSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _detectErrors = new(StringComparer.OrdinalIgnoreCase);
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
            _detectErrors.Clear();
            var errors = 0;
            var missing = 0;
            string? firstError = null;
            foreach (var taskPath in _taskPaths)
            {
                try
                {
                    var info = await _taskManager.QueryAsync(taskPath, ct);
                    _snapshots[taskPath] = new TaskSnapshot(info.Exists, info.Enabled);
                    if (!info.Exists)
                    {
                        missing++;
                    }
                }
                catch (Exception ex) when (IsNotFound(ex))
                {
                    _snapshots[taskPath] = new TaskSnapshot(false, false);
                    missing++;
                }
                catch (Exception ex)
                {
                    _snapshots[taskPath] = new TaskSnapshot(false, false);
                    errors++;
                    firstError ??= $"{taskPath}: {ex.Message}";
                    _detectErrors[taskPath] = ex.Message;
                }
            }

            _hasDetected = true;
            var detectedCount = _snapshots.Values.Count(snapshot => snapshot.Exists);
            var enabledCount = _snapshots.Values.Count(snapshot => snapshot.Exists && snapshot.Enabled);

            if (errors > 0 && detectedCount == 0 && missing == 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Detect failed: {firstError ?? "Unknown error."}",
                    DateTimeOffset.UtcNow);
            }

            var summary = errors > 0
                ? $"Detected {detectedCount} of {_taskPaths.Count} tasks ({missing} missing, {errors} errors)."
                : missing > 0
                    ? $"Detected {detectedCount} of {_taskPaths.Count} tasks ({missing} missing)."
                    : $"Detected {detectedCount} of {_taskPaths.Count} tasks.";

            var status = detectedCount == 0 && errors == 0
                ? TweakStatus.NotApplicable
                : errors > 0
                    ? TweakStatus.Detected
                    : enabledCount == 0
                        ? TweakStatus.Applied
                        : TweakStatus.Detected;

            var currentState = detectedCount == 0
                ? errors > 0
                    ? "Unknown"
                    : "Not present"
                : errors > 0
                    ? "Unknown"
                : enabledCount == 0
                    ? "Disabled"
                    : enabledCount == detectedCount
                        ? "Enabled"
                        : $"{enabledCount} enabled";

            var details = BuildTaskDetails(_taskPaths, _snapshots, _detectErrors);
            var message = string.IsNullOrWhiteSpace(details)
                ? $"{summary} Current state: {currentState}."
                : $"{summary} Current state: {currentState}.\nTasks:\n{details}";
            return new TweakResult(status, message, DateTimeOffset.UtcNow);
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
                ScheduledTaskInfo info;
                try
                {
                    info = await _taskManager.QueryAsync(taskPath, ct);
                }
                catch (Exception ex) when (IsNotFound(ex))
                {
                    continue;
                }

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
        try
        {
            var info = await _taskManager.QueryAsync(taskPath, ct);
            return new TaskSnapshot(info.Exists, info.Enabled);
        }
        catch (Exception ex) when (IsNotFound(ex))
        {
            return new TaskSnapshot(false, false);
        }
    }

    private sealed record TaskSnapshot(bool Exists, bool Enabled);

    private static string BuildTaskDetails(
        IReadOnlyList<string> taskPaths,
        IReadOnlyDictionary<string, TaskSnapshot> snapshots,
        IReadOnlyDictionary<string, string> errors)
    {
        var lines = new List<string>();

        foreach (var taskPath in taskPaths.OrderBy(path => path))
        {
            if (errors.TryGetValue(taskPath, out var error))
            {
                lines.Add($"- {taskPath}: error ({error})");
                continue;
            }

            if (!snapshots.TryGetValue(taskPath, out var snapshot))
            {
                lines.Add($"- {taskPath}: unknown");
                continue;
            }

            if (!snapshot.Exists)
            {
                lines.Add($"- {taskPath}: missing");
                continue;
            }

            lines.Add($"- {taskPath}: {(snapshot.Enabled ? "Enabled" : "Disabled")}");
        }

        return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
    }

    private static bool IsNotFound(Exception ex)
    {
        const uint fileNotFound = 0x80070002;
        const uint pathNotFound = 0x80070003;

        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is COMException com)
            {
                var code = unchecked((uint)com.ErrorCode);
                if (code == fileNotFound || code == pathNotFound)
                {
                    return true;
                }
            }

            var hresult = unchecked((uint)current.HResult);
            if (hresult == fileNotFound || hresult == pathNotFound)
            {
                return true;
            }
        }

        return false;
    }
}
