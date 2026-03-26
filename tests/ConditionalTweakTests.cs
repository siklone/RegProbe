using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core;
using OpenTraceProject.Engine.Tweaks;
using Xunit;

namespace OpenTraceProject.Tests;

public sealed class ConditionalTweakTests
{
    [Fact]
    public async Task Detect_WhenConditionIsFalse_ReturnsNotApplicable()
    {
        var inner = new TrackingTweak();
        var tweak = new ConditionalTweak(inner, () => false, "Blocked on this SKU.");

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.NotApplicable, result.Status);
        Assert.Equal("Blocked on this SKU.", result.Message);
        Assert.Equal(0, inner.DetectCalls);
    }

    [Fact]
    public async Task Apply_WhenConditionIsTrue_DelegatesToInnerTweak()
    {
        var inner = new TrackingTweak();
        var tweak = new ConditionalTweak(inner, () => true, "Blocked on this SKU.");

        var result = await tweak.ApplyAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Applied, result.Status);
        Assert.Equal(1, inner.ApplyCalls);
    }

    [Fact]
    public void RollbackSnapshot_WhenInnerIsRollbackAware_DelegatesState()
    {
        var inner = new RollbackAwareTrackingTweak();
        var tweak = new ConditionalTweak(inner, () => true, "Blocked on this SKU.");

        Assert.True(tweak.HasCapturedState);
        Assert.NotNull(tweak.GetRollbackSnapshot());
    }

    private class TrackingTweak : ITweak
    {
        public int DetectCalls { get; private set; }
        public int ApplyCalls { get; private set; }

        public string Id => "test.conditional";
        public string Name => "Conditional";
        public string Description => "Conditional test tweak";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
        {
            DetectCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
        {
            ApplyCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }

    private sealed class RollbackAwareTrackingTweak : TrackingTweak, IRollbackAwareTweak
    {
        public bool HasCapturedState => true;

        public TweakRollbackSnapshot? GetRollbackSnapshot()
            => new()
            {
                TweakId = "test.conditional.rollback",
                TweakName = "Conditional rollback",
                SnapshotType = TweakSnapshotType.Registry,
            };

        public void RestoreFromSnapshot(TweakRollbackSnapshot snapshot)
        {
        }
    }
}
