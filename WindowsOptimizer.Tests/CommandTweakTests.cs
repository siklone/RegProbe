using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks.Commands.Power;
using WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;
using WindowsOptimizer.Infrastructure.Commands;
using Xunit;

namespace WindowsOptimizer.Tests;

public sealed class CommandTweakTests
{
    [Fact]
    public async Task DisableHibernationTweak_DetectAsync_WhenHibernationEnabled_ReturnsDetectedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "The following sleep states are available on this system:\n    Standby (S3)\n    Hibernate\n    Fast Startup\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new DisableHibernationTweak(mockRunner.Object);

        // Act
        var result = await tweak.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("Hibernation enabled", result.Message);
    }

    [Fact]
    public async Task DisableHibernationTweak_DetectAsync_WhenHibernationDisabled_ReturnsDetectedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "The following sleep states are available on this system:\n    Standby (S3)\n    Fast Startup\n    Hibernation has not been enabled.\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new DisableHibernationTweak(mockRunner.Object);

        // Act
        var result = await tweak.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("Hibernation disabled", result.Message);
    }

    [Fact]
    public async Task DisableHibernationTweak_ApplyAsync_WhenSuccessful_ReturnsAppliedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(200)));

        var tweak = new DisableHibernationTweak(mockRunner.Object);

        // Act
        var result = await tweak.ApplyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Applied, result.Status);
    }

    [Fact]
    public async Task DisableHibernationTweak_VerifyAsync_WhenHibernationDisabled_ReturnsVerifiedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "The following sleep states are available on this system:\n    Standby (S3)\n    Fast Startup\n    Hibernation has not been enabled.\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new DisableHibernationTweak(mockRunner.Object);

        // Act
        var result = await tweak.VerifyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Verified, result.Status);
    }

    [Fact]
    public async Task DisableHibernationTweak_RollbackAsync_WithoutPriorDetect_ReturnsSkippedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        var tweak = new DisableHibernationTweak(mockRunner.Object);

        // Act
        var result = await tweak.RollbackAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Skipped, result.Status);
        Assert.Contains("no prior detect state", result.Message);
    }

    [Fact]
    public async Task CleanupComponentStoreTweak_DetectAsync_WhenCleanupRecommended_ReturnsDetectedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Component Store (WinSxS) information:\n\nComponent Store Cleanup Recommended : Yes\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(500)));

        var tweak = new CleanupComponentStoreTweak(mockRunner.Object);

        // Act
        var result = await tweak.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("Cleanup recommended", result.Message);
    }

    [Fact]
    public async Task DisableReservedStorageTweak_DetectAsync_WhenEnabled_ReturnsDetectedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Reserved Storage State: Enabled\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(300)));

        var tweak = new DisableReservedStorageTweak(mockRunner.Object);

        // Act
        var result = await tweak.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("Reserved Storage: Enabled", result.Message);
    }

    [Fact]
    public async Task DisableUsbSelectiveSuspendTweak_DetectAsync_WhenDisabled_ReturnsDetectedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)\n  Subgroup GUID: 2a737441-1930-4402-8d77-b2bebba308a3  (USB settings)\n    GUID Alias: SUB_USB\n    USB selective suspend setting\n      GUID Alias: USBSELECTIVESUSPEND\n      Current AC Power Setting Index: 0x00000000\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(200)));

        var tweak = new DisableUsbSelectiveSuspendTweak(mockRunner.Object);

        // Act
        var result = await tweak.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("USB Selective Suspend: Disabled", result.Message);
    }

    [Fact]
    public async Task CommandTweak_ApplyAsync_WhenCommandTimesOut_ReturnsFailedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 1,
                StandardOutput: "",
                StandardError: "",
                TimedOut: true,
                Duration: TimeSpan.FromSeconds(120)));

        var tweak = new DisableHibernationTweak(mockRunner.Object);

        // Act
        var result = await tweak.ApplyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Failed, result.Status);
        Assert.Contains("timed out", result.Message);
    }

    [Fact]
    public async Task CommandTweak_ApplyAsync_WhenCommandFails_ReturnsFailedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 1,
                StandardOutput: "",
                StandardError: "Access denied",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new DisableHibernationTweak(mockRunner.Object);

        // Act
        var result = await tweak.ApplyAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Failed, result.Status);
        Assert.Contains("exit code 1", result.Message);
        Assert.Contains("Access denied", result.Message);
    }
}
