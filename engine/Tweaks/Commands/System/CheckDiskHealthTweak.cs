using System;
using System.Collections.ObjectModel;
using RegProbe.Core;
using RegProbe.Core.Commands;

namespace RegProbe.Engine.Tweaks.Commands.System;

public sealed class CheckDiskHealthTweak : CommandTweak, ITweakStepTimeouts
{
    private const string ChkdskExe = "chkdsk.exe";
    private static readonly TimeSpan DiskHealthTimeout = TimeSpan.FromMinutes(5);

    public CheckDiskHealthTweak(ICommandRunner commandRunner)
        : base(
            id: "system-check-disk-health",
            name: "Check Disk Health (C:)",
            description: "Performs a read-only check of the C: drive for file system errors without making any changes. Provides information about disk health and potential issues.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, ChkdskExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "C:" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, ChkdskExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "C:" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        if (result.StandardOutput.Contains("Windows has scanned", StringComparison.OrdinalIgnoreCase))
        {
            state = "Disk check completed - No errors found";
            return true;
        }

        if (result.StandardOutput.Contains("errors", StringComparison.OrdinalIgnoreCase))
        {
            state = "Disk check found errors";
            return true;
        }

        state = "Disk check status available";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains("Windows has scanned", StringComparison.OrdinalIgnoreCase) ||
               result.StandardOutput.Contains("volume", StringComparison.OrdinalIgnoreCase);
    }

    public TimeSpan? GetStepTimeout(TweakAction action) => action switch
    {
        TweakAction.Detect or TweakAction.Apply or TweakAction.Verify => DiskHealthTimeout,
        _ => null
    };
}
