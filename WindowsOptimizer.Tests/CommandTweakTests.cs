using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Moq;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks.Commands.Power;
using WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;
using WindowsOptimizer.Engine.Tweaks.Commands.Network;
using WindowsOptimizer.Engine.Tweaks.Commands.Privacy;
using WindowsOptimizer.Engine.Tweaks.Commands.RegistryOps;
using WindowsOptimizer.Engine.Tweaks.Commands.Security;
using WindowsOptimizer.Core.Commands;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Engine.Tweaks;
using Xunit;

namespace WindowsOptimizer.Tests;

public sealed class CommandTweakTests
{
    [Fact]
    public async Task DisableSmbLeasingTweak_DetectAsync_WhenLeasingEnabled_ReturnsDetectedStatus()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "True\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)));

        var tweak = new DisableSmbLeasingTweak(mockRunner.Object);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("Current state: True", result.Message);
    }

    [Fact]
    public async Task DisableSmbLeasingTweak_ApplyVerifyAndRollback_UseSmbServerConfigurationCommands()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "True\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "EnableLeasing=False\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "False\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "EnableLeasing=True\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)));

        var tweak = new DisableSmbLeasingTweak(mockRunner.Object);

        var detectResult = await tweak.DetectAsync(CancellationToken.None);
        var applyResult = await tweak.ApplyAsync(CancellationToken.None);
        var verifyResult = await tweak.VerifyAsync(CancellationToken.None);
        var rollbackResult = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, detectResult.Status);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        Assert.Equal(TweakStatus.Verified, verifyResult.Status);
        Assert.Equal(TweakStatus.RolledBack, rollbackResult.Status);

        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("Set-SmbServerConfiguration -EnableLeasing $false -Force | Out-Null; Write-Output 'EnableLeasing=False'")),
            It.IsAny<CancellationToken>()), Times.Once);

        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("Set-SmbServerConfiguration -EnableLeasing $true -Force | Out-Null; Write-Output 'EnableLeasing=True'")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

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
    public async Task DisableHibernationTweak_DetectAsync_WhenHibernationDisabled_ReturnsAppliedStatus()
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
        Assert.Equal(TweakStatus.Applied, result.Status);
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
    public async Task DisableUsbSelectiveSuspendTweak_DetectAsync_WhenDisabled_ReturnsAppliedStatus()
    {
        // Arrange
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)\n  Subgroup GUID: 2a737441-1930-4402-8d77-b2bebba308a3  (USB settings)\n    GUID Alias: SUB_USB\n    USB selective suspend setting\n      GUID Alias: USBSELECTIVESUSPEND\n    Current AC Power Setting Index: 0x00000000\n    Current DC Power Setting Index: 0x00000000\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(200)));

        var tweak = new DisableUsbSelectiveSuspendTweak(mockRunner.Object);

        // Act
        var result = await tweak.DetectAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TweakStatus.Applied, result.Status);
        Assert.Contains("USB Selective Suspend: Disabled", result.Message);
    }

    [Fact]
    public async Task DisableUsbSelectiveSuspendTweak_DetectAsync_WhenDcRemainsEnabled_ReturnsDetectedStatus()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Power Scheme GUID: fe18663b-823b-473d-bd6c-d663e6cc3b42  (Custom)\n  Subgroup GUID: 2a737441-1930-4402-8d77-b2bebba308a3  (USB settings)\n    Power Setting GUID: 48e6b7a6-50f5-4782-a5d4-53bb8f07e226  (USB selective suspend setting)\n      Possible Setting Index: 000\n      Possible Setting Friendly Name: Disabled\n      Possible Setting Index: 001\n      Possible Setting Friendly Name: Enabled\n    Current AC Power Setting Index: 0x00000000\n    Current DC Power Setting Index: 0x00000001\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(200)));

        var tweak = new DisableUsbSelectiveSuspendTweak(mockRunner.Object);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("AC/DC: 0/1", result.Message);
    }

    [Fact]
    public async Task DisableUsbSelectiveSuspendTweak_ApplyAndRollback_UseAcAndDcCommands()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Current AC Power Setting Index: 0x00000001\nCurrent DC Power Setting Index: 0x00000001\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(0, "", "", false, TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(0, "", "", false, TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(0, "", "", false, TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(0, "", "", false, TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(0, "", "", false, TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(0, "", "", false, TimeSpan.FromMilliseconds(100)));

        var tweak = new DisableUsbSelectiveSuspendTweak(mockRunner.Object);

        var detectResult = await tweak.DetectAsync(CancellationToken.None);
        var applyResult = await tweak.ApplyAsync(CancellationToken.None);
        var rollbackResult = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, detectResult.Status);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        Assert.Equal(TweakStatus.RolledBack, rollbackResult.Status);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setacvalueindex") && req.Arguments.Contains("0")),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setdcvalueindex") && req.Arguments.Contains("0")),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setacvalueindex") && req.Arguments.Contains("1")),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setdcvalueindex") && req.Arguments.Contains("1")),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setactive") && req.Arguments.Contains("SCHEME_CURRENT")),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DisableCpuCoreParkingTweak_DetectAsync_WhenDcValueIsNotDisabled_ReturnsDetectedStatus()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Current AC Power Setting Index: 0x00000064\nCurrent DC Power Setting Index: 0x0000000a\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Current AC Power Setting Index: 0x00000064\nCurrent DC Power Setting Index: 0x00000064\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new DisableCpuCoreParkingTweak(mockRunner.Object);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("Min AC/DC: 100/10", result.Message);
    }

    [Fact]
    public async Task DisableCpuCoreParkingTweak_ApplyAndRollback_UsePowerCfgCommands()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "Current AC Power Setting Index: 0x0000000a\nCurrent DC Power Setting Index: 0x0000000a\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(100)));

        var tweak = new DisableCpuCoreParkingTweak(mockRunner.Object);

        var detectResult = await tweak.DetectAsync(CancellationToken.None);
        var applyResult = await tweak.ApplyAsync(CancellationToken.None);
        var rollbackResult = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, detectResult.Status);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        Assert.Equal(TweakStatus.RolledBack, rollbackResult.Status);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/qh") && req.Arguments.Contains("CPMINCORES")),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setacvalueindex") && req.Arguments.Contains("CPMINCORES") && req.Arguments.Contains("100")),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Arguments.Contains("/setdcvalueindex") && req.Arguments.Contains("CPMAXCORES") && req.Arguments.Contains("10")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistryCommandBatchTweak_ApplyAsync_WhenValuesAlreadyMatch_SkipsWrites()
    {
        var reference = new RegistryValueReference(
            RegistryHive.LocalMachine,
            RegistryView.Default,
            @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
            "AllowGameDVR");

        var readAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        readAccessor
            .Setup(x => x.ReadValueAsync(reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegistryValueReadResult(
                true,
                new RegistryValueData(RegistryValueKind.DWord, NumericValue: 0)));

        var writeAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);

        var tweak = new RegistryCommandBatchTweak(
            "system.disable-game-recording-broadcasting",
            "Disable Game Recording & Broadcasting",
            "Disables Windows game recording and broadcasting for all users through the official policy setting.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
                    "AllowGameDVR",
                    RegistryValueKind.DWord,
                    0,
                    RegistryView.Default)
            },
            readAccessor.Object,
            writeAccessor.Object);

        var result = await tweak.ApplyAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Applied, result.Status);
        Assert.Contains("already match", result.Message, StringComparison.OrdinalIgnoreCase);
        writeAccessor.VerifyNoOtherCalls();
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

    [Fact]
    public async Task DisableUacFullTweak_DetectAsync_WhenEnableLuaIsOne_ReturnsDetectedStatus()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .Setup(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "\r\nHKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\r\n    EnableLUA    REG_DWORD    0x1\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)));

        var tweak = new DisableUacFullTweak(mockRunner.Object);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("Current state: 1", result.Message);
    }

    [Fact]
    public async Task DisableUacFullTweak_ApplyVerifyAndRollback_UseRegCommands()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "\r\nHKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\r\n    EnableLUA    REG_DWORD    0x1\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "The operation completed successfully.\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "\r\nHKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\r\n    EnableLUA    REG_DWORD    0x0\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "The operation completed successfully.\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)));

        var tweak = new DisableUacFullTweak(mockRunner.Object);

        var detectResult = await tweak.DetectAsync(CancellationToken.None);
        var applyResult = await tweak.ApplyAsync(CancellationToken.None);
        var verifyResult = await tweak.VerifyAsync(CancellationToken.None);
        var rollbackResult = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, detectResult.Status);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        Assert.Equal(TweakStatus.Verified, verifyResult.Status);
        Assert.Equal(TweakStatus.RolledBack, rollbackResult.Status);

        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Executable.EndsWith("reg.exe", StringComparison.OrdinalIgnoreCase)
                && req.Arguments.Count >= 8
                && req.Arguments[0] == "add"
                && req.Arguments.Contains("EnableLUA")
                && req.Arguments.Contains("0")),
            It.IsAny<CancellationToken>()), Times.Once);

        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Executable.EndsWith("reg.exe", StringComparison.OrdinalIgnoreCase)
                && req.Arguments.Count >= 8
                && req.Arguments[0] == "add"
                && req.Arguments.Contains("EnableLUA")
                && req.Arguments.Contains("1")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisableFindMyDeviceTweak_ApplyVerifyAndRollback_UseRegCommands()
    {
        var mockRunner = new Mock<ICommandRunner>();
        mockRunner
            .SetupSequence(r => r.RunAsync(It.IsAny<CommandRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "\r\nHKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice\r\n    AllowFindMyDevice    REG_DWORD    0x1\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "The operation completed successfully.\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "\r\nHKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice\r\n    AllowFindMyDevice    REG_DWORD    0x0\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)))
            .ReturnsAsync(new CommandResult(
                ExitCode: 0,
                StandardOutput: "The operation completed successfully.\r\n",
                StandardError: "",
                TimedOut: false,
                Duration: TimeSpan.FromMilliseconds(50)));

        var tweak = new DisableFindMyDeviceTweak(mockRunner.Object);

        var detectResult = await tweak.DetectAsync(CancellationToken.None);
        var applyResult = await tweak.ApplyAsync(CancellationToken.None);
        var verifyResult = await tweak.VerifyAsync(CancellationToken.None);
        var rollbackResult = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, detectResult.Status);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        Assert.Equal(TweakStatus.Verified, verifyResult.Status);
        Assert.Equal(TweakStatus.RolledBack, rollbackResult.Status);

        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Executable.EndsWith("reg.exe", StringComparison.OrdinalIgnoreCase)
                && req.Arguments.Count >= 8
                && req.Arguments[0] == "add"
                && req.Arguments.Contains("AllowFindMyDevice")
                && req.Arguments.Contains("0")),
            It.IsAny<CancellationToken>()), Times.Once);

        mockRunner.Verify(r => r.RunAsync(
            It.Is<CommandRequest>(req => req.Executable.EndsWith("reg.exe", StringComparison.OrdinalIgnoreCase)
                && req.Arguments.Count >= 8
                && req.Arguments[0] == "add"
                && req.Arguments.Contains("AllowFindMyDevice")
                && req.Arguments.Contains("1")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistryCommandBatchTweak_ApplyVerifyAndRollback_UsesElevatedRegistryWrites()
    {
        var readRegistryAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var writeRegistryAccessor = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "DisableSearchHistory");
        var targetData = RegistryValueData.FromObject(RegistryValueKind.DWord, 1);

        readRegistryAccessor
            .SetupSequence(x => x.ReadValueAsync(reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegistryValueReadResult(false, null))
            .ReturnsAsync(new RegistryValueReadResult(true, targetData));

        writeRegistryAccessor
            .Setup(x => x.SetValueAsync(reference, It.Is<RegistryValueData>(value => value.Kind == RegistryValueKind.DWord && value.NumericValue == 1), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        writeRegistryAccessor
            .Setup(x => x.DeleteValueAsync(reference, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tweak = new RegistryCommandBatchTweak(
            "privacy.disable-search-history",
            "Disable Search History",
            "Prevents search history from being stored for this user.",
            TweakRiskLevel.Safe,
            new[]
            {
                new RegistryValueBatchEntry(
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "DisableSearchHistory",
                    RegistryValueKind.DWord,
                    1)
            },
            readRegistryAccessor.Object,
            writeRegistryAccessor.Object);

        var detectResult = await tweak.DetectAsync(CancellationToken.None);
        var applyResult = await tweak.ApplyAsync(CancellationToken.None);
        var verifyResult = await tweak.VerifyAsync(CancellationToken.None);
        var rollbackResult = await tweak.RollbackAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, detectResult.Status);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        Assert.Equal(TweakStatus.Verified, verifyResult.Status);
        Assert.Equal(TweakStatus.RolledBack, rollbackResult.Status);

        writeRegistryAccessor.Verify(x => x.SetValueAsync(
            reference,
            It.Is<RegistryValueData>(value => value.Kind == RegistryValueKind.DWord && value.NumericValue == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        writeRegistryAccessor.Verify(x => x.DeleteValueAsync(
            reference,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
