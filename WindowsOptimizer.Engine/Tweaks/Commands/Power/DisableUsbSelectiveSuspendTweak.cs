using System;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Power;

public sealed class DisableUsbSelectiveSuspendTweak : CommandTweak
{
    private const string System32PowerCfgExe = "powercfg.exe";

    public DisableUsbSelectiveSuspendTweak(ICommandRunner commandRunner)
        : base(
            id: "power-disable-usb-selective-suspend",
            name: "Disable USB Selective Suspend",
            description: "Disables USB Selective Suspend to prevent USB devices from powering down unexpectedly. This can resolve issues with USB devices disconnecting or becoming unresponsive.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = System.IO.Path.Combine(Environment.SystemDirectory, System32PowerCfgExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/query" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = System.IO.Path.Combine(Environment.SystemDirectory, System32PowerCfgExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "0" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        if (detectedState.Contains("USB Selective Suspend: Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var executable = System.IO.Path.Combine(Environment.SystemDirectory, System32PowerCfgExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "1" }));
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput;
        if (output.Contains("USB selective suspend", StringComparison.OrdinalIgnoreCase))
        {
            if (output.Contains("0x00000000", StringComparison.OrdinalIgnoreCase))
            {
                state = "USB Selective Suspend: Disabled";
                return true;
            }

            if (output.Contains("0x00000001", StringComparison.OrdinalIgnoreCase))
            {
                state = "USB Selective Suspend: Enabled";
                return true;
            }
        }

        state = "USB Selective Suspend: Unknown";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        var output = result.StandardOutput;
        return output.Contains("USB selective suspend", StringComparison.OrdinalIgnoreCase) &&
               output.Contains("0x00000000", StringComparison.OrdinalIgnoreCase);
    }
}
