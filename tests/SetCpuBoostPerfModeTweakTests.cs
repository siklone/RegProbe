using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Commands;
using OpenTraceProject.Engine.Tweaks.Commands.Power;
using Xunit;

namespace OpenTraceProject.Tests;

public sealed class SetCpuBoostPerfModeTweakTests
{
    [Fact]
    public async Task SetCpuBoostPerfModeTweak_DetectAsync_WhenAggressive_ReturnsAppliedStatus()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Current AC Power Setting Index: 0x00000002\nCurrent DC Power Setting Index: 0x00000002\n",
                StandardError: string.Empty,
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new SetCpuBoostPerfModeTweak(mockRunner.Object);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Applied, result.Status);
        Assert.Contains("Current state", result.Message);
    }

    [Fact]
    public async Task SetCpuBoostPerfModeTweak_ApplyAndRollback_UsePowerCfgCommands()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Current AC Power Setting Index: 0x00000001\nCurrent DC Power Setting Index: 0x00000001\n",
                StandardError: string.Empty,
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: string.Empty,
                StandardError: string.Empty,
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: string.Empty,
                StandardError: string.Empty,
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new SetCpuBoostPerfModeTweak(mockRunner.Object);

        var detectResult = await tweak.DetectAsync(CancellationToken.None);
        var applyResult = await tweak.ApplyAsync(CancellationToken.None);
        var rollbackResult = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, detectResult.Status);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        Assert.Equal(TweakStatus.RolledBack, rollbackResult.Status);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/qh") && req.Arguments.Contains("PERFBOOSTMODE")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setacvalueindex") && req.Arguments.Contains("PERFBOOSTMODE") && req.Arguments.Contains("2")),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setdcvalueindex") && req.Arguments.Contains("PERFBOOSTMODE") && req.Arguments.Contains("2")),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setactive") && req.Arguments.Contains("SCHEME_CURRENT")),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
