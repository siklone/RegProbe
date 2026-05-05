using System;
using System.IO;
using System.Collections.ObjectModel;
using RegProbe.Core;
using RegProbe.Core.Commands;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class DisableReservedStorageTweak : CommandTweak
{
    private const string System32DismExe = "dism.exe";

    public DisableReservedStorageTweak(
        ICommandRunner commandRunner,
        string? name = null,
        string? description = null)
        : base(
            id: "cleanup.disable-reserved-storage",
            name: name ?? "Disable Reserved Storage",
            description: description ?? "Disables Windows Reserved Storage, which reserves disk space for updates and temporary files.",
            risk: TweakRiskLevel.Advanced,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, System32DismExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/online", "/Get-ReservedStorageState" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, System32DismExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/online", "/Set-ReservedStorageState", "/State:Disabled", "/NoRestart" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        if (detectedState.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var executable = Path.Combine(Environment.SystemDirectory, System32DismExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/online", "/Set-ReservedStorageState", "/State:Enabled", "/NoRestart" }));
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput;
        if (output.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            state = "Reserved Storage: Disabled";
            return true;
        }

        if (output.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
        {
            state = "Reserved Storage: Enabled";
            return true;
        }

        state = "Reserved Storage: Unknown";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains("Disabled", StringComparison.OrdinalIgnoreCase);
    }
}
