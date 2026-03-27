using System;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;
using RegProbe.Engine.Tweaks;

namespace RegProbe.Tests;

public sealed class ConditionalTweakTests
{
    [Fact]
    public async Task AllSteps_WhenConditionReturnsNotApplicable_ShortCircuitWithoutInvokingInner()
    {
        var inner = new TrackingTweak();
        var tweak = new ConditionalTweak(
            inner,
            _ => Task.FromResult<TweakResult?>(new TweakResult(TweakStatus.NotApplicable, "Not supported here.", DateTimeOffset.UtcNow)));

        var detect = await tweak.DetectAsync(CancellationToken.None);
        var apply = await tweak.ApplyAsync(CancellationToken.None);
        var verify = await tweak.VerifyAsync(CancellationToken.None);
        var rollback = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.NotApplicable, detect.Status);
        Assert.Equal(TweakStatus.NotApplicable, apply.Status);
        Assert.Equal(TweakStatus.NotApplicable, verify.Status);
        Assert.Equal(TweakStatus.NotApplicable, rollback.Status);
        Assert.Equal(0, inner.DetectCalls);
        Assert.Equal(0, inner.ApplyCalls);
        Assert.Equal(0, inner.VerifyCalls);
        Assert.Equal(0, inner.RollbackCalls);
    }

    [Fact]
    public async Task ConditionPassing_PreservesInnerBehaviorAndMetadataDelegation()
    {
        var inner = new TrackingTweak();
        var tweak = new ConditionalTweak(inner, _ => Task.FromResult<TweakResult?>(null));

        var detect = await tweak.DetectAsync(CancellationToken.None);
        var apply = await tweak.ApplyAsync(CancellationToken.None);
        var verify = await tweak.VerifyAsync(CancellationToken.None);
        var rollback = await tweak.RollbackAsync(CancellationToken.None);
        var snapshot = tweak.GetRollbackSnapshot();

        Assert.Equal(TweakStatus.Detected, detect.Status);
        Assert.Equal(TweakStatus.Applied, apply.Status);
        Assert.Equal(TweakStatus.Verified, verify.Status);
        Assert.Equal(TweakStatus.RolledBack, rollback.Status);
        Assert.Equal(TimeSpan.FromSeconds(7), ((ITweakStepTimeouts)tweak).GetStepTimeout(TweakAction.Apply));
        Assert.NotNull(snapshot);

        tweak.RestoreFromSnapshot(snapshot!);

        Assert.True(inner.HasCapturedState);
        Assert.True(inner.RestoreCalled);
        Assert.Equal(1, inner.DetectCalls);
        Assert.Equal(1, inner.ApplyCalls);
        Assert.Equal(1, inner.VerifyCalls);
        Assert.Equal(1, inner.RollbackCalls);
    }

    [Fact]
    public async Task ConditionFailure_ReturnsFailedResult()
    {
        var tweak = new ConditionalTweak(
            new TrackingTweak(),
            _ => throw new InvalidOperationException("boom"));

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Failed, result.Status);
        Assert.Contains("condition failed", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<InvalidOperationException>(result.Error);
    }

    private sealed class TrackingTweak : ITweak, IRollbackAwareTweak, ITweakStepTimeouts
    {
        public string Id => "test.conditional";
        public string Name => "Conditional Test";
        public string Description => "Tracks delegated calls.";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;
        public bool HasCapturedState { get; private set; }
        public bool RestoreCalled { get; private set; }
        public int DetectCalls { get; private set; }
        public int ApplyCalls { get; private set; }
        public int VerifyCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public Task<TweakResult> DetectAsync(CancellationToken ct)
        {
            DetectCalls++;
            HasCapturedState = true;
            return Task.FromResult(new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
        {
            ApplyCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
        {
            VerifyCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
        {
            RollbackCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
        }

        public TweakRollbackSnapshot? GetRollbackSnapshot()
        {
            if (!HasCapturedState)
            {
                return null;
            }

            return new TweakRollbackSnapshot
            {
                TweakId = Id,
                TweakName = Name,
                SnapshotType = TweakSnapshotType.Other
            };
        }

        public void RestoreFromSnapshot(TweakRollbackSnapshot snapshot)
        {
            if (snapshot.TweakId == Id)
            {
                RestoreCalled = true;
                HasCapturedState = true;
            }
        }

        public TimeSpan? GetStepTimeout(TweakAction action)
            => action == TweakAction.Apply ? TimeSpan.FromSeconds(7) : null;
    }
}
