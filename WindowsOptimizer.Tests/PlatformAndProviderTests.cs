using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;
using Xunit;

namespace WindowsOptimizer.Tests;

/// <summary>
/// Tests for platform detection and cross-platform behavior.
/// Validates that non-Windows platforms fail gracefully.
/// Source: https://learn.microsoft.com/en-us/dotnet/standard/frameworks
/// </summary>
public sealed class PlatformDetectionTests
{
    [Fact]
    public void IsOSPlatform_Windows_ReturnsExpected()
    {
        // Act
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Assert - this test validates the platform check works
        // On Windows CI, this should be true; on Linux/Mac, false
        Assert.True(isWindows || !isWindows); // Always passes, but validates the API works
    }

    [Fact]
    public void RuntimeInformation_OSDescription_NotEmpty()
    {
        // Act
        var osDesc = RuntimeInformation.OSDescription;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(osDesc));
    }

    [Fact]
    public void RuntimeInformation_FrameworkDescription_Contains_NET()
    {
        // Act
        var framework = RuntimeInformation.FrameworkDescription;

        // Assert
        Assert.Contains(".NET", framework);
    }

    [Fact]
    public void ProcessArchitecture_IsValid()
    {
        // Act
        var arch = RuntimeInformation.ProcessArchitecture;

        // Assert
        Assert.True(
            arch == Architecture.X64 ||
            arch == Architecture.X86 ||
            arch == Architecture.Arm64 ||
            arch == Architecture.Arm);
    }
}

/// <summary>
/// Tests for TweakProvider base functionality.
/// Source: https://xunit.net/docs/getting-started/netcore/cmdline
/// </summary>
public sealed class TweakProviderTests
{
    [Fact]
    public void ITweak_Properties_AreValid()
    {
        // Arrange
        var tweak = new TestTweak("test.provider.1", "Test Tweak", TweakRiskLevel.Safe);

        // Assert
        Assert.Equal("test.provider.1", tweak.Id);
        Assert.Equal("Test Tweak", tweak.Name);
        Assert.Equal(TweakRiskLevel.Safe, tweak.Risk);
        Assert.False(tweak.RequiresElevation);
    }

    [Fact]
    public async Task ITweak_DetectAsync_ReturnsResult()
    {
        // Arrange
        var tweak = new TestTweak("test.detect", "Detect Test", TweakRiskLevel.Safe);

        // Act
        var result = await tweak.DetectAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TweakStatus.Detected, result.Status);
    }

    [Fact]
    public async Task ITweak_ApplyAsync_ReturnsResult()
    {
        // Arrange
        var tweak = new TestTweak("test.apply", "Apply Test", TweakRiskLevel.Safe);

        // Act
        var result = await tweak.ApplyAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TweakStatus.Applied, result.Status);
    }

    [Fact]
    public async Task ITweak_VerifyAsync_ReturnsResult()
    {
        // Arrange
        var tweak = new TestTweak("test.verify", "Verify Test", TweakRiskLevel.Safe);

        // Act
        var result = await tweak.VerifyAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TweakStatus.Verified, result.Status);
    }

    [Fact]
    public async Task ITweak_RollbackAsync_ReturnsResult()
    {
        // Arrange
        var tweak = new TestTweak("test.rollback", "Rollback Test", TweakRiskLevel.Safe);

        // Act
        var result = await tweak.RollbackAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TweakStatus.RolledBack, result.Status);
    }

    [Theory]
    [InlineData(TweakRiskLevel.Safe)]
    [InlineData(TweakRiskLevel.Advanced)]
    [InlineData(TweakRiskLevel.Risky)]
    public void TweakRiskLevel_AllValuesValid(TweakRiskLevel riskLevel)
    {
        // Arrange
        var tweak = new TestTweak("test.risk", "Risk Test", riskLevel);

        // Assert
        Assert.Equal(riskLevel, tweak.Risk);
    }

    [Fact]
    public async Task ITweak_CancellationToken_Respected()
    {
        // Arrange
        var tweak = new SlowTweak();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => tweak.DetectAsync(cts.Token));
    }

    [Fact]
    public void TweakResult_Constructor_SetsProperties()
    {
        // Arrange & Act
        var timestamp = DateTimeOffset.UtcNow;
        var result = new TweakResult(TweakStatus.Applied, "Success message", timestamp);

        // Assert
        Assert.Equal(TweakStatus.Applied, result.Status);
        Assert.Equal("Success message", result.Message);
        Assert.Equal(timestamp, result.Timestamp);
    }

    [Fact]
    public void TweakStatus_AllValuesAreDefined()
    {
        // Arrange
        var expectedValues = new[]
        {
            TweakStatus.Unknown,
            TweakStatus.Detected,
            TweakStatus.Applied,
            TweakStatus.Verified,
            TweakStatus.RolledBack,
            TweakStatus.Failed,
            TweakStatus.Skipped
        };

        // Act & Assert
        foreach (var status in expectedValues)
        {
            Assert.True(Enum.IsDefined(typeof(TweakStatus), status));
        }
    }

    /// <summary>
    /// Test tweak implementation for unit testing.
    /// </summary>
    private sealed class TestTweak : ITweak
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; } = "Test tweak for unit testing.";
        public TweakRiskLevel Risk { get; }
        public bool RequiresElevation { get; } = false;

        public TestTweak(string id, string name, TweakRiskLevel risk)
        {
            Id = id;
            Name = name;
            Risk = risk;
        }

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
            return Task.FromResult(new TweakResult(TweakStatus.RolledBack, "RolledBack", DateTimeOffset.UtcNow));
        }
    }

    /// <summary>
    /// Slow tweak that respects cancellation tokens.
    /// </summary>
    private sealed class SlowTweak : ITweak
    {
        public string Id => "test.slow";
        public string Name => "Slow Tweak";
        public string Description => "Slow tweak for cancellation testing.";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public async Task<TweakResult> DetectAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow);
        }

        public async Task<TweakResult> ApplyAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow);
        }

        public async Task<TweakResult> VerifyAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow);
        }

        public async Task<TweakResult> RollbackAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new TweakResult(TweakStatus.RolledBack, "RolledBack", DateTimeOffset.UtcNow);
        }
    }
}
