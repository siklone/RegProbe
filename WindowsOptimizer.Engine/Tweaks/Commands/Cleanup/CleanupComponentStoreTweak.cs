using System;
using System.IO;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class CleanupComponentStoreTweak : CommandTweak
{
    private const string System32DismExe = "dism.exe";

    public CleanupComponentStoreTweak(ICommandRunner commandRunner)
        : base(
            id: "cleanup.component-store",
            name: "Cleanup Component Store",
            description: "Cleans up the Windows component store (WinSxS folder) to free up disk space. This is a safe operation that removes superseded components and reduces the size of the component store.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, System32DismExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/online", "/Cleanup-Image", "/AnalyzeComponentStore" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, System32DismExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/online", "/Cleanup-Image", "/StartComponentCleanup" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput;
        if (output.Contains("Component Store Cleanup Recommended", StringComparison.OrdinalIgnoreCase) &&
            output.Contains("Yes", StringComparison.OrdinalIgnoreCase))
        {
            state = "Cleanup recommended";
            return true;
        }

        if (output.Contains("Component Store Cleanup Recommended", StringComparison.OrdinalIgnoreCase) &&
            output.Contains("No", StringComparison.OrdinalIgnoreCase))
        {
            state = "Cleanup not needed";
            return true;
        }

        state = "Component store analyzed";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        var output = result.StandardOutput;
        return output.Contains("Component Store Cleanup Recommended", StringComparison.OrdinalIgnoreCase) &&
               output.Contains("No", StringComparison.OrdinalIgnoreCase);
    }
}
