using System;
using System.IO;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearRecycleBinTweak : CommandTweak
{
    private const string PowerShellExe = "powershell.exe";

    public ClearRecycleBinTweak(ICommandRunner commandRunner)
        : base(
            id: "cleanup-recycle-bin",
            name: "Empty Recycle Bin",
            description: "Empties the Recycle Bin for all drives. Files cannot be recovered after deletion.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            PowerShellExe);

        // Check if recycle bin has items
        var script = "(New-Object -ComObject Shell.Application).NameSpace(0x0A).Items().Count";
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "-NoProfile", "-NonInteractive", "-Command", script }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            PowerShellExe);

        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                "Clear-RecycleBin",
                "-Force",
                "-ErrorAction",
                "SilentlyContinue"
            }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        // Recycle bin contents cannot be restored once deleted
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput.Trim();

        if (int.TryParse(output, out var itemCount))
        {
            state = itemCount > 0 ? $"{itemCount} items in Recycle Bin" : "Recycle Bin is empty";
            return true;
        }

        // If we can't parse, assume it's not empty
        state = "Recycle Bin status unknown";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        // PowerShell Clear-RecycleBin returns 0 on success
        return result.ExitCode == 0;
    }
}
