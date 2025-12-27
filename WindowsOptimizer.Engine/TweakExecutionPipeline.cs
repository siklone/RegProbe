using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.Engine;

public sealed class TweakExecutionPipeline
{
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

    private static TweakExecutionReport BuildReport(
        ITweak tweak,
        TweakExecutionOptions options,
        IReadOnlyList<TweakExecutionStep> steps,
        DateTimeOffset startedAt)
    {
        var applied = HasStep(steps, TweakAction.Apply, TweakStatus.Applied);
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

            // Add timeout to prevent hanging (30 seconds per step)
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var operationTask = operation(cts.Token);
            var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);

            var completedTask = await Task.WhenAny(operationTask, timeoutTask);

            if (completedTask == operationTask)
            {
                result = await operationTask;
            }
            else
            {
                result = new TweakResult(
                    TweakStatus.Failed,
                    $"Operation timed out after 30 seconds during {action}.",
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
