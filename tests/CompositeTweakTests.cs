using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;
using RegProbe.Engine.Tweaks;
using Xunit;

namespace RegProbe.Tests;

/// <summary>
/// Unit tests for CompositeTweak - a tweak that aggregates multiple sub-tweaks.
/// Source: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices
/// </summary>
public sealed class CompositeTweakTests
{
    [Fact]
    public void Constructor_WithValidTweaks_CreatesInstance()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new SuccessfulTweak("sub.2")
        };

        // Act
        var composite = new CompositeTweak(
            "composite.test",
            "Test Composite",
            "A composite tweak for testing",
            TweakRiskLevel.Safe,
            subTweaks);

        // Assert
        Assert.Equal("composite.test", composite.Id);
        Assert.Equal("Test Composite", composite.Name);
        Assert.Equal(TweakRiskLevel.Safe, composite.Risk);
        Assert.False(composite.RequiresElevation);
    }

    [Fact]
    public void Constructor_WithElevatedSubTweak_RequiresElevation()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new ElevatedTweak("sub.2")
        };

        // Act
        var composite = new CompositeTweak(
            "composite.elevated",
            "Elevated Composite",
            "Requires elevation",
            TweakRiskLevel.Advanced,
            subTweaks);

        // Assert
        Assert.True(composite.RequiresElevation);
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        var subTweaks = new List<ITweak> { new SuccessfulTweak("sub.1") };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeTweak(null!, "Name", "Desc", TweakRiskLevel.Safe, subTweaks));
    }

    [Fact]
    public void Constructor_WithEmptyTweaks_ThrowsArgumentException()
    {
        // Arrange
        var emptyTweaks = new List<ITweak>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CompositeTweak("id", "Name", "Desc", TweakRiskLevel.Safe, emptyTweaks));
    }

    [Fact]
    public void Constructor_WithNullTweakInList_ThrowsArgumentException()
    {
        // Arrange
        var tweaksWithNull = new List<ITweak> { new SuccessfulTweak("sub.1"), null! };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CompositeTweak("id", "Name", "Desc", TweakRiskLevel.Safe, tweaksWithNull));
    }

    [Fact]
    public async Task DetectAsync_WhenAllSucceed_ReturnsDetected()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new SuccessfulTweak("sub.2"),
            new SuccessfulTweak("sub.3")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("3 applicable sub-tweaks", result.Message);
    }

    [Fact]
    public async Task DetectAsync_WhenAllApplicableTweaksAreAlreadyApplied_ReturnsApplied()
    {
        var subTweaks = new List<ITweak>
        {
            new AppliedDetectTweak("sub.1"),
            new AppliedDetectTweak("sub.2"),
            new NotApplicableTweak("sub.3")
        };
        var composite = CreateComposite(subTweaks);

        var result = await composite.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Applied, result.Status);
        Assert.Contains("already match", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenOneFails_ReturnsFailed()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new FailingTweak("sub.2"),
            new SuccessfulTweak("sub.3")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenAllSucceed_ReturnsApplied()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new SuccessfulTweak("sub.2")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.ApplyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Applied, result.Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenOneFails_ReturnsFailed()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new FailingTweak("sub.2")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.ApplyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Failed, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_WhenAllSucceed_ReturnsVerified()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new SuccessfulTweak("sub.2")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.VerifyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Verified, result.Status);
    }

    [Fact]
    public async Task RollbackAsync_WhenAllSucceed_ReturnsRolledBack()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new SuccessfulTweak("sub.2")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.RollbackAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.RolledBack, result.Status);
    }

    [Fact]
    public async Task RollbackAsync_WhenOneFails_ReportsFailureCount()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new SuccessfulTweak("sub.1"),
            new FailingTweak("sub.2"),
            new FailingTweak("sub.3")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.RollbackAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Failed, result.Status);
        Assert.Contains("2 sub-tweaks", result.Message);
    }

    [Fact]
    public async Task DetectAsync_WhenCanceled_ThrowsOperationCanceled()
    {
        // Arrange
        var subTweaks = new List<ITweak> { new SuccessfulTweak("sub.1") };
        var composite = CreateComposite(subTweaks);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => composite.DetectAsync(cts.Token));
    }

    [Fact]
    public async Task DetectAsync_WhenAllNotApplicable_ReturnsNotApplicable()
    {
        // Arrange
        var subTweaks = new List<ITweak>
        {
            new NotApplicableTweak("sub.1"),
            new NotApplicableTweak("sub.2")
        };
        var composite = CreateComposite(subTweaks);

        // Act
        var result = await composite.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.NotApplicable, result.Status);
    }

    private static CompositeTweak CreateComposite(IReadOnlyList<ITweak> subTweaks)
    {
        return new CompositeTweak(
            "test.composite",
            "Test Composite",
            "Composite for testing",
            TweakRiskLevel.Safe,
            subTweaks);
    }

    #region Test Doubles

    private sealed class SuccessfulTweak : ITweak
    {
        public string Id { get; }
        public string Name => "Successful Tweak";
        public string Description => "Always succeeds";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public SuccessfulTweak(string id) => Id = id;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
        }
    }

    private sealed class FailingTweak : ITweak
    {
        public string Id { get; }
        public string Name => "Failing Tweak";
        public string Description => "Always fails";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public FailingTweak(string id) => Id = id;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Failed, "Detection failed", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Failed, "Apply failed", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Failed, "Verify failed", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Failed, "Rollback failed", DateTimeOffset.UtcNow));
    }

    private sealed class ElevatedTweak : ITweak
    {
        public string Id { get; }
        public string Name => "Elevated Tweak";
        public string Description => "Requires elevation";
        public TweakRiskLevel Risk => TweakRiskLevel.Advanced;
        public bool RequiresElevation => true;

        public ElevatedTweak(string id) => Id = id;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }

    private sealed class AppliedDetectTweak : ITweak
    {
        public string Id { get; }
        public string Name => "Applied Detect Tweak";
        public string Description => "Reports applied during detect.";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public AppliedDetectTweak(string id) => Id = id;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Already applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }

    private sealed class NotApplicableTweak : ITweak
    {
        public string Id { get; }
        public string Name => "Not Applicable Tweak";
        public string Description => "Not applicable";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public NotApplicableTweak(string id) => Id = id;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.NotApplicable, "Not applicable", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.NotApplicable, "Not applicable", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.NotApplicable, "Not applicable", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.NotApplicable, "Not applicable", DateTimeOffset.UtcNow));
    }

    #endregion
}
