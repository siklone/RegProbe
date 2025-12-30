using System;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Performance;

public sealed class DisableSuperfetchTweak : CommandTweak
{
    private const string ScExe = "sc.exe";

    public DisableSuperfetchTweak(ICommandRunner commandRunner)
        : base(
            id: "power.disable-superfetch",
            name: "Disable Superfetch (SysMain)",
            description: "Disables the Superfetch service (SysMain) which preloads frequently used applications. Can improve performance on SSDs where prefetching is not beneficial.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, ScExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "query", "SysMain" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, ScExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "stop", "SysMain" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        if (detectedState.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, ScExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "start", "SysMain" }));
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        if (result.StandardOutput.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
        {
            state = "Superfetch is running";
            return true;
        }

        if (result.StandardOutput.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            state = "Superfetch is stopped";
            return true;
        }

        state = "Unknown Superfetch state";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains("STOPPED", StringComparison.OrdinalIgnoreCase);
    }
}
