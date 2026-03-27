using System;
using System.IO;
using System.Collections.ObjectModel;
using RegProbe.Core;
using RegProbe.Core.Commands;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearShadowCopiesTweak : CommandTweak
{
    private const string VssAdminExe = "vssadmin.exe";

    public ClearShadowCopiesTweak(ICommandRunner commandRunner)
        : base(
            id: "cleanup.shadow-copies",
            name: "Clear Shadow Copies",
            description: "Removes all shadow copies (volume backups) to free up disk space. WARNING: This permanently removes System Restore points and volume snapshots.",
            risk: TweakRiskLevel.Risky,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, VssAdminExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "list", "shadows" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, VssAdminExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "delete", "shadows", "/all", "/quiet" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        // Shadow copies cannot be restored once deleted
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput;

        if (output.Contains("No items found that satisfy the query", StringComparison.OrdinalIgnoreCase))
        {
            state = "No shadow copies exist";
            return true;
        }

        // Count shadow copies
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var count = 0;
        foreach (var line in lines)
        {
            if (line.Contains("Shadow Copy ID:", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        state = count > 0 ? $"{count} shadow copies found" : "No shadow copies found";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        var output = result.StandardOutput;
        // Successful deletion returns empty or "successfully deleted" message
        return result.ExitCode == 0;
    }
}
