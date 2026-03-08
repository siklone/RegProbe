using System;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Power;

public sealed class DisableHibernationTweak : CommandTweak
{
    private const string System32PowerCfgExe = "powercfg.exe";

    public DisableHibernationTweak(ICommandRunner commandRunner)
        : base(
            id: "power.disable-hibernation",
            name: "Disable Hibernation",
            description: "Disables hibernation and deletes hiberfil.sys to save disk space. This prevents the system from entering hibernation mode but does not affect sleep mode.",
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
        if (detectedState.Contains("Hibernation has not been enabled", StringComparison.OrdinalIgnoreCase))
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
        if (result.StandardOutput.Contains("Hibernation has not been enabled", StringComparison.OrdinalIgnoreCase))
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

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains("Hibernation has not been enabled", StringComparison.OrdinalIgnoreCase);
    }
}
