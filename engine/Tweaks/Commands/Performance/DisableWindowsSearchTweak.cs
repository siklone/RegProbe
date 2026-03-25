using System;
using System.Collections.ObjectModel;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Engine.Tweaks.Commands.Performance;

public sealed class DisableWindowsSearchTweak : CommandTweak
{
    private const string ScExe = "sc.exe";

    public DisableWindowsSearchTweak(ICommandRunner commandRunner)
        : base(
            id: "power.disable-windows-search",
            name: "Disable Windows Search",
            description: "Disables the Windows Search indexing service. This can improve system performance but will slow down file searches. Useful for systems with SSDs where search performance is already fast.",
            risk: TweakRiskLevel.Advanced,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, ScExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "query", "WSearch" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, ScExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "stop", "WSearch" }));
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
            new ReadOnlyCollection<string>(new[] { "start", "WSearch" }));
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        if (result.StandardOutput.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
        {
            state = "Windows Search is running";
            return true;
        }

        if (result.StandardOutput.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
        {
            state = "Windows Search is stopped";
            return true;
        }

        state = "Unknown Windows Search state";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains("STOPPED", StringComparison.OrdinalIgnoreCase);
    }
}
