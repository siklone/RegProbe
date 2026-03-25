using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core;
using OpenTraceProject.Infrastructure;

namespace OpenTraceProject.Engine;

public sealed class TweakExecutionPipeline
{
    private static readonly TimeSpan DefaultStepTimeout = TimeSpan.FromSeconds(30);
    private readonly IAppLogger _logger;
    private readonly ITweakLogStore? _logStore;
    private readonly IRollbackStateStore? _rollbackStore;

    public TweakExecutionPipeline(IAppLogger logger, ITweakLogStore? logStore = null, IRollbackStateStore? rollbackStore = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logStore = logStore;
        _rollbackStore = rollbackStore;
    }

    public async Task<TweakExecutionReport> ExecuteAsync(
        ITweak tweak,
        TweakExecutionOptions? options = null,
        IProgress<TweakExecutionUpdate>? progress = null,
        CancellationToken ct = default)
    {
        if (tweak is null)
        {
            throw new ArgumentNullException(nameof(tweak));
        }

        options ??= new TweakExecutionOptions();

        var steps = new List<TweakExecutionStep>();
        var startedAt = DateTimeOffset.UtcNow;

        ct.ThrowIfCancellationRequested();

        var detectStep = await RunStepAsync(tweak, TweakAction.Detect, tweak.DetectAsync, steps, progress, ct);
        if (detectStep.Result.Status == TweakStatus.NotApplicable)
        {
            await AppendSkippedStepsAsync(tweak, steps, progress, "Detect returned NotApplicable.", ct);
            return BuildReport(tweak, options, steps, startedAt);
        }

        if (detectStep.Result.Status == TweakStatus.Failed)
        {
            await AppendSkippedStepsAsync(tweak, steps, progress, "Detect failed.", ct);
            return BuildReport(tweak, options, steps, startedAt);
        }

        if (options.DryRun)
        {
            await AppendSkippedStepsAsync(tweak, steps, progress, "DryRun enabled.", ct);
            return BuildReport(tweak, options, steps, startedAt);
        }

        if (detectStep.Result.Status == TweakStatus.Applied)
        {
            await AppendSkippedStepAsync(tweak, TweakAction.Apply, steps, progress, "Already in the desired state.", ct);

            if (options.VerifyAfterApply)
            {
                var verifyStep = await RunStepAsync(tweak, TweakAction.Verify, tweak.VerifyAsync, steps, progress, ct);
                if (verifyStep.Result.Status == TweakStatus.Verified)
                {
                    await MarkAppliedAsync(tweak, ct);
                }
            }
            else
            {
                await MarkAppliedAsync(tweak, ct);
                await AppendSkippedStepAsync(tweak, TweakAction.Verify, steps, progress, "Already in the desired state.", ct);
            }

            await AppendSkippedStepAsync(tweak, TweakAction.Rollback, steps, progress, "No changes were made.", ct);
            return BuildReport(tweak, options, steps, startedAt);
        }

        // Save rollback state before Apply for crash recovery
        await SaveRollbackStateAsync(tweak, ct);

        var applyStep = await RunStepAsync(tweak, TweakAction.Apply, tweak.ApplyAsync, steps, progress, ct);
        if (applyStep.Result.Status == TweakStatus.Failed)
        {
            if (options.RollbackOnFailure)
            {
                var rollbackStep = await RunStepAsync(tweak, TweakAction.Rollback, tweak.RollbackAsync, steps, progress, ct);
                if (rollbackStep.Result.Status == TweakStatus.RolledBack)
                {
                    await MarkRolledBackAsync(tweak, ct);
                }
            }

            return BuildReport(tweak, options, steps, startedAt);
        }

        if (options.VerifyAfterApply)
        {
            var verifyStep = await RunStepAsync(tweak, TweakAction.Verify, tweak.VerifyAsync, steps, progress, ct);
            if (verifyStep.Result.Status == TweakStatus.Failed && options.RollbackOnFailure)
            {
                var rollbackStep = await RunStepAsync(tweak, TweakAction.Rollback, tweak.RollbackAsync, steps, progress, ct);
                if (rollbackStep.Result.Status == TweakStatus.RolledBack)
                {
                    await MarkRolledBackAsync(tweak, ct);
                }
            }
            else if (verifyStep.Result.Status == TweakStatus.Verified)
            {
                // Mark as successfully applied after verification
                await MarkAppliedAsync(tweak, ct);
            }
        }
        else
        {
            // No verification, but Apply succeeded, mark as applied
            await MarkAppliedAsync(tweak, ct);
            await AppendSkippedStepAsync(tweak, TweakAction.Verify, steps, progress, "Verify disabled by options.", ct);
        }

        return BuildReport(tweak, options, steps, startedAt);
    }

    public Task<TweakExecutionStep> ExecuteStepAsync(
        ITweak tweak,
        TweakAction action,
        IProgress<TweakExecutionUpdate>? progress = null,
        CancellationToken ct = default)
    {
        if (tweak is null)
        {
            throw new ArgumentNullException(nameof(tweak));
        }

        if (action == TweakAction.Rollback)
        {
            return ExecuteRollbackStepAsync(tweak, progress, ct);
        }

        Func<CancellationToken, Task<TweakResult>> operation = action switch
        {
            TweakAction.Detect => tweak.DetectAsync,
            TweakAction.Apply => tweak.ApplyAsync,
            TweakAction.Verify => tweak.VerifyAsync,
            TweakAction.Rollback => tweak.RollbackAsync,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported action.")
        };

        var steps = new List<TweakExecutionStep>();
        return RunStepAsync(tweak, action, operation, steps, progress, ct);
    }

    private async Task<TweakExecutionStep> ExecuteRollbackStepAsync(
        ITweak tweak,
        IProgress<TweakExecutionUpdate>? progress,
        CancellationToken ct)
    {
        await TryRestoreRollbackStateAsync(tweak, ct);
        var steps = new List<TweakExecutionStep>();
        return await RunStepAsync(tweak, TweakAction.Rollback, tweak.RollbackAsync, steps, progress, ct);
    }

    private static TweakExecutionReport BuildReport(
        ITweak tweak,
        TweakExecutionOptions options,
        IReadOnlyList<TweakExecutionStep> steps,
        DateTimeOffset startedAt)
    {
        var applied = HasStep(steps, TweakAction.Apply, TweakStatus.Applied)
            || HasStep(steps, TweakAction.Detect, TweakStatus.Applied)
            || HasStep(steps, TweakAction.Verify, TweakStatus.Verified);
        var verified = HasStep(steps, TweakAction.Verify, TweakStatus.Verified);
        var rolledBack = HasStep(steps, TweakAction.Rollback, TweakStatus.RolledBack);
        var completedAt = DateTimeOffset.UtcNow;

        return new TweakExecutionReport(
            tweak.Id,
            tweak.Name,
            options.DryRun,
            applied,
            verified,
            rolledBack,
            steps,
            startedAt,
            completedAt);
    }

    private static bool HasStep(
        IReadOnlyList<TweakExecutionStep> steps,
        TweakAction action,
        TweakStatus status)
    {
        foreach (var step in steps)
        {
            if (step.Action == action && step.Result.Status == status)
            {
                return true;
            }
        }

        return false;
    }

    private async Task AppendSkippedStepsAsync(
        ITweak tweak,
        List<TweakExecutionStep> steps,
        IProgress<TweakExecutionUpdate>? progress,
        string reason,
        CancellationToken ct)
    {
        await AppendSkippedStepAsync(tweak, TweakAction.Apply, steps, progress, reason, ct);
        await AppendSkippedStepAsync(tweak, TweakAction.Verify, steps, progress, reason, ct);
        await AppendSkippedStepAsync(tweak, TweakAction.Rollback, steps, progress, reason, ct);
    }

    private async Task AppendSkippedStepAsync(
        ITweak tweak,
        TweakAction action,
        List<TweakExecutionStep> steps,
        IProgress<TweakExecutionUpdate>? progress,
        string reason,
        CancellationToken ct)
    {
        var result = new TweakResult(
            TweakStatus.Skipped,
            reason,
            DateTimeOffset.UtcNow);

        var step = new TweakExecutionStep(action, result);
        steps.Add(step);
        await ReportStep(tweak, step, progress, ct);
    }

    private async Task<TweakExecutionStep> RunStepAsync(
        ITweak tweak,
        TweakAction action,
        Func<CancellationToken, Task<TweakResult>> operation,
        List<TweakExecutionStep> steps,
        IProgress<TweakExecutionUpdate>? progress,
        CancellationToken ct)
    {
        TweakResult result;
        try
        {
            ct.ThrowIfCancellationRequested();

            var stepTimeout = ResolveStepTimeout(tweak, action);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var operationTask = operation(cts.Token);
            _ = operationTask.ContinueWith(
                static faultedTask => _ = faultedTask.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
            var timeoutTask = Task.Delay(stepTimeout, ct);

            var completedTask = await Task.WhenAny(operationTask, timeoutTask);

            if (completedTask == operationTask)
            {
                result = await operationTask;
            }
            else
            {
                ct.ThrowIfCancellationRequested();
                cts.Cancel();
                result = new TweakResult(
                    TweakStatus.Failed,
                    $"Operation timed out after {FormatTimeout(stepTimeout)} during {action}.",
                    DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = new TweakResult(
                TweakStatus.Failed,
                $"Unhandled exception during {action}.",
                DateTimeOffset.UtcNow,
                ex);
        }

        var step = new TweakExecutionStep(action, result);
        steps.Add(step);
        await ReportStep(tweak, step, progress, ct);
        return step;
    }

    private static TimeSpan ResolveStepTimeout(ITweak tweak, TweakAction action)
    {
        if (tweak is ITweakStepTimeouts timeoutProvider)
        {
            var customTimeout = timeoutProvider.GetStepTimeout(action);
            if (customTimeout.HasValue && customTimeout.Value > TimeSpan.Zero)
            {
                return customTimeout.Value;
            }
        }

        return DefaultStepTimeout;
    }

    private static string FormatTimeout(TimeSpan timeout)
    {
        if (timeout.TotalMinutes >= 1 && Math.Abs(timeout.TotalMinutes - Math.Round(timeout.TotalMinutes)) < 0.001)
        {
            var minutes = (int)Math.Round(timeout.TotalMinutes);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        if (timeout.TotalSeconds >= 1 && Math.Abs(timeout.TotalSeconds - Math.Round(timeout.TotalSeconds)) < 0.001)
        {
            var seconds = (int)Math.Round(timeout.TotalSeconds);
            return seconds == 1 ? "1 second" : $"{seconds} seconds";
        }

        return $"{timeout.TotalMilliseconds:F0} ms";
    }

    private async Task ReportStep(
        ITweak tweak,
        TweakExecutionStep step,
        IProgress<TweakExecutionUpdate>? progress,
        CancellationToken? ct)
    {
        var level = step.Result.Status == TweakStatus.Failed ? LogLevel.Error : LogLevel.Info;
        var message = $"Tweak {tweak.Id} ({tweak.Name}) {step.Action} -> {step.Result.Status}: {step.Result.Message}";
        _logger.Log(level, message, step.Result.Error);

        if (_logStore is not null)
        {
            var error = step.Result.Error is null
                ? null
                : $"{step.Result.Error.GetType().Name}: {step.Result.Error.Message}";

            var entry = new TweakLogEntry(
                step.Result.Timestamp == default ? DateTimeOffset.UtcNow : step.Result.Timestamp,
                tweak.Id,
                tweak.Name,
                step.Action,
                step.Result.Status,
                step.Result.Message,
                error);

            if (ct.HasValue)
            {
                await _logStore.AppendAsync(entry, ct.Value);
            }
            else
            {
                await _logStore.AppendAsync(entry, CancellationToken.None);
            }
        }

        progress?.Report(new TweakExecutionUpdate(
            tweak.Id,
            tweak.Name,
            step.Action,
            step.Result.Status,
            step.Result.Message,
            step.Result.Timestamp == default ? DateTimeOffset.UtcNow : step.Result.Timestamp));
    }

    private async Task SaveRollbackStateAsync(ITweak tweak, CancellationToken ct)
    {
        if (_rollbackStore is null)
        {
            return;
        }

        try
        {
            if (tweak is IRollbackAwareTweak rollbackAware && rollbackAware.HasCapturedState)
            {
                var snapshot = rollbackAware.GetRollbackSnapshot();
                if (snapshot is not null)
                {
                    await _rollbackStore.SaveSnapshotAsync(snapshot, ct);
                    _logger.Log(LogLevel.Debug, $"Saved rollback state for {tweak.Id}", null);
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the operation
            _logger.Log(LogLevel.Warning, $"Failed to save rollback state for {tweak.Id}: {ex.Message}", ex);
        }
    }

    private async Task TryRestoreRollbackStateAsync(ITweak tweak, CancellationToken ct)
    {
        if (_rollbackStore is null || tweak is not IRollbackAwareTweak rollbackAware || rollbackAware.HasCapturedState)
        {
            return;
        }

        try
        {
            var snapshot = await _rollbackStore.GetSnapshotAsync(tweak.Id, ct);
            if (snapshot is null)
            {
                return;
            }

            rollbackAware.RestoreFromSnapshot(snapshot);
            _logger.Log(LogLevel.Debug, $"Restored rollback snapshot for {tweak.Id}", null);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, $"Failed to restore rollback state for {tweak.Id}: {ex.Message}", ex);
        }
    }

    private async Task MarkAppliedAsync(ITweak tweak, CancellationToken ct)
    {
        if (_rollbackStore is null)
        {
            return;
        }

        try
        {
            await _rollbackStore.MarkAppliedAsync(tweak.Id, ct);
            _logger.Log(LogLevel.Debug, $"Marked {tweak.Id} as applied", null);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, $"Failed to mark {tweak.Id} as applied: {ex.Message}", ex);
        }
    }

    private async Task MarkRolledBackAsync(ITweak tweak, CancellationToken ct)
    {
        if (_rollbackStore is null)
        {
            return;
        }

        try
        {
            await _rollbackStore.MarkRolledBackAsync(tweak.Id, ct);
            _logger.Log(LogLevel.Debug, $"Marked {tweak.Id} as rolled back", null);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, $"Failed to mark {tweak.Id} as rolled back: {ex.Message}", ex);
        }
    }
}
