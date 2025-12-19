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

    public TweakExecutionPipeline(IAppLogger logger, ITweakLogStore? logStore = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logStore = logStore;
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
            AppendSkippedSteps(tweak, steps, progress, "Detect returned NotApplicable.");
            return BuildReport(tweak, options, steps, startedAt);
        }

        if (detectStep.Result.Status == TweakStatus.Failed)
        {
            AppendSkippedSteps(tweak, steps, progress, "Detect failed.");
            return BuildReport(tweak, options, steps, startedAt);
        }

        if (options.DryRun)
        {
            AppendSkippedSteps(tweak, steps, progress, "DryRun enabled.");
            return BuildReport(tweak, options, steps, startedAt);
        }

        var applyStep = await RunStepAsync(tweak, TweakAction.Apply, tweak.ApplyAsync, steps, progress, ct);
        if (applyStep.Result.Status == TweakStatus.Failed)
        {
            if (options.RollbackOnFailure)
            {
                await RunStepAsync(tweak, TweakAction.Rollback, tweak.RollbackAsync, steps, progress, ct);
            }

            return BuildReport(tweak, options, steps, startedAt);
        }

        if (options.VerifyAfterApply)
        {
            var verifyStep = await RunStepAsync(tweak, TweakAction.Verify, tweak.VerifyAsync, steps, progress, ct);
            if (verifyStep.Result.Status == TweakStatus.Failed && options.RollbackOnFailure)
            {
                await RunStepAsync(tweak, TweakAction.Rollback, tweak.RollbackAsync, steps, progress, ct);
            }
        }
        else
        {
            AppendSkippedStep(tweak, TweakAction.Verify, steps, progress, "Verify disabled by options.");
        }

        return BuildReport(tweak, options, steps, startedAt);
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

    private void AppendSkippedSteps(
        ITweak tweak,
        List<TweakExecutionStep> steps,
        IProgress<TweakExecutionUpdate>? progress,
        string reason)
    {
        AppendSkippedStep(tweak, TweakAction.Apply, steps, progress, reason);
        AppendSkippedStep(tweak, TweakAction.Verify, steps, progress, reason);
        AppendSkippedStep(tweak, TweakAction.Rollback, steps, progress, reason);
    }

    private void AppendSkippedStep(
        ITweak tweak,
        TweakAction action,
        List<TweakExecutionStep> steps,
        IProgress<TweakExecutionUpdate>? progress,
        string reason)
    {
        var result = new TweakResult(
            TweakStatus.Skipped,
            reason,
            DateTimeOffset.UtcNow);

        var step = new TweakExecutionStep(action, result);
        steps.Add(step);
        ReportStep(tweak, step, progress, null);
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
            result = await operation(ct);
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
}
