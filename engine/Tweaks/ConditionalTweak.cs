using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core;

namespace OpenTraceProject.Engine.Tweaks;

public sealed class ConditionalTweak : ITweak, IRollbackAwareTweak, ITweakStepTimeouts
{
    private readonly ITweak _inner;
    private readonly Func<CancellationToken, Task<TweakResult?>> _evaluateAsync;

    public ConditionalTweak(ITweak inner, Func<bool> isApplicable, string notApplicableMessage)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (isApplicable is null)
        {
            throw new ArgumentNullException(nameof(isApplicable));
        }

        var effectiveMessage = string.IsNullOrWhiteSpace(notApplicableMessage)
            ? "This tweak is not applicable on the current system."
            : notApplicableMessage;
        _evaluateAsync = _ => Task.FromResult(
            isApplicable()
                ? null
                : new TweakResult(TweakStatus.NotApplicable, effectiveMessage, DateTimeOffset.UtcNow));
    }

    public ConditionalTweak(ITweak inner, Func<CancellationToken, Task<TweakResult?>> evaluateAsync)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _evaluateAsync = evaluateAsync ?? throw new ArgumentNullException(nameof(evaluateAsync));
    }

    public string Id => _inner.Id;
    public string Name => _inner.Name;
    public string Description => _inner.Description;
    public TweakRiskLevel Risk => _inner.Risk;
    public bool RequiresElevation => _inner.RequiresElevation;

    public bool HasCapturedState => _inner is IRollbackAwareTweak rollbackAware && rollbackAware.HasCapturedState;

    public Task<TweakResult> DetectAsync(CancellationToken ct)
        => ExecuteAsync(ct, static (inner, token) => inner.DetectAsync(token), TweakAction.Detect);

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
        => ExecuteAsync(ct, static (inner, token) => inner.ApplyAsync(token), TweakAction.Apply);

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
        => ExecuteAsync(ct, static (inner, token) => inner.VerifyAsync(token), TweakAction.Verify);

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
        => ExecuteAsync(ct, static (inner, token) => inner.RollbackAsync(token), TweakAction.Rollback);

    public TweakRollbackSnapshot? GetRollbackSnapshot()
        => _inner is IRollbackAwareTweak rollbackAware ? rollbackAware.GetRollbackSnapshot() : null;

    public void RestoreFromSnapshot(TweakRollbackSnapshot snapshot)
    {
        if (_inner is IRollbackAwareTweak rollbackAware)
        {
            rollbackAware.RestoreFromSnapshot(snapshot);
        }
    }

    public TimeSpan? GetStepTimeout(TweakAction action)
        => _inner is ITweakStepTimeouts timeoutProvider ? timeoutProvider.GetStepTimeout(action) : null;

    private async Task<TweakResult> ExecuteAsync(
        CancellationToken ct,
        Func<ITweak, CancellationToken, Task<TweakResult>> action,
        TweakAction tweakAction)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var conditionResult = await _evaluateAsync(ct);
            if (conditionResult is not null)
            {
                return conditionResult;
            }

            return await action(_inner, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"{tweakAction} condition failed: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }
}
