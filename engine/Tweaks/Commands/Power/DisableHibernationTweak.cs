using System;
using System.Collections.ObjectModel;
using RegProbe.Core;
using RegProbe.Core.Commands;

namespace RegProbe.Engine.Tweaks.Commands.Power;

public sealed class DisableHibernationTweak : CommandTweak
{
    private const string System32PowerCfgExe = "powercfg.exe";
    private const string HibernationDisabledText = "Hibernation has not been enabled";
    private const string FirmwareUnsupportedText = "system firmware does not support hibernation";

    public DisableHibernationTweak(ICommandRunner commandRunner)
        : base(
            id: "power.disable-hibernation",
            name: "Disable Hibernation",
            description: "Runs powercfg /hibernate off. This disables hibernation and removes hiberfil.sys.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, System32PowerCfgExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/availablesleepstates" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, System32PowerCfgExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/hibernate", "off" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        if (detectedState.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
            || detectedState.Contains(FirmwareUnsupportedText, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (detectedState.Contains(HibernationDisabledText, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, System32PowerCfgExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/hibernate", "on" }));
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        if (result.StandardOutput.Contains(FirmwareUnsupportedText, StringComparison.OrdinalIgnoreCase))
        {
            state = "Hibernation unavailable: system firmware does not support hibernation";
            return true;
        }

        if (result.StandardOutput.Contains(HibernationDisabledText, StringComparison.OrdinalIgnoreCase))
        {
            state = "Hibernation disabled";
            return true;
        }

        if (result.StandardOutput.Contains("Hibernate", StringComparison.OrdinalIgnoreCase))
        {
            state = "Hibernation enabled";
            return true;
        }

        state = "Unknown hibernation state";
        return true;
    }

    protected override TweakStatus GetDetectedStatus(CommandResult result, string detectedState)
    {
        if (result.StandardOutput.Contains(FirmwareUnsupportedText, StringComparison.OrdinalIgnoreCase))
        {
            return TweakStatus.NotApplicable;
        }

        return base.GetDetectedStatus(result, detectedState);
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains(HibernationDisabledText, StringComparison.OrdinalIgnoreCase);
    }
}
