using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands;

public abstract class CommandTweak : ITweak
{
    private readonly ICommandRunner _commandRunner;
    private string? _detectedState;
    private bool _hasDetected;

    protected CommandTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        ICommandRunner commandRunner)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        RequiresElevation = true;
        _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    protected abstract CommandRequest GetDetectCommand();
    protected abstract CommandRequest GetApplyCommand();
    protected abstract CommandRequest? GetRollbackCommand(string detectedState);
    protected abstract bool ParseDetectedState(CommandResult result, out string state);
    protected abstract bool VerifyApplied(CommandResult result);

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var detectCmd = GetDetectCommand();
            var result = await _commandRunner.RunAsync(detectCmd, ct);

            if (result.TimedOut)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Detect command timed out.",
                    DateTimeOffset.UtcNow);
            }

            if (result.ExitCode != 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Detect command failed with exit code {result.ExitCode}: {result.StandardError}",
                    DateTimeOffset.UtcNow);
            }

            if (!ParseDetectedState(result, out _detectedState))
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Failed to parse detected state.",
                    DateTimeOffset.UtcNow);
            }

            _hasDetected = true;

            // Check if the tweak is already in the desired (applied) state
            var isApplied = VerifyApplied(result);
            return new TweakResult(
                isApplied ? TweakStatus.Applied : TweakStatus.Detected,
                $"Current state: {_detectedState}",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Detect error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var applyCmd = GetApplyCommand();
            var result = await _commandRunner.RunAsync(applyCmd, ct);

            if (result.TimedOut)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Apply command timed out.",
                    DateTimeOffset.UtcNow);
            }

            if (result.ExitCode != 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Apply command failed with exit code {result.ExitCode}: {result.StandardError}",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.Applied,
                "Successfully applied changes.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Apply error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var detectCmd = GetDetectCommand();
            var result = await _commandRunner.RunAsync(detectCmd, ct);

            if (result.TimedOut)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Verify command timed out.",
                    DateTimeOffset.UtcNow);
            }

            if (result.ExitCode != 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Verify command failed with exit code {result.ExitCode}.",
                    DateTimeOffset.UtcNow);
            }

            if (VerifyApplied(result))
            {
                return new TweakResult(
                    TweakStatus.Verified,
                    "Changes verified successfully.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.Failed,
                "Verification failed - changes not applied.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Verify error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
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

        if (string.IsNullOrEmpty(_detectedState))
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "No snapshot available for rollback.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            var rollbackCmd = GetRollbackCommand(_detectedState);
            if (rollbackCmd is null)
            {
                return new TweakResult(
                    TweakStatus.NotApplicable,
                    "Rollback not supported for this tweak.",
                    DateTimeOffset.UtcNow);
            }

            var result = await _commandRunner.RunAsync(rollbackCmd, ct);

            if (result.TimedOut)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Rollback command timed out.",
                    DateTimeOffset.UtcNow);
            }

            if (result.ExitCode != 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Rollback command failed with exit code {result.ExitCode}: {result.StandardError}",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.RolledBack,
                "Successfully restored original state.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Rollback error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }
}
