using System;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Network;

public sealed class ResetNetworkStackTweak : CommandTweak
{
    private const string NetshExe = "netsh.exe";

    public ResetNetworkStackTweak(ICommandRunner commandRunner)
        : base(
            id: "network-reset-winsock",
            name: "Reset Winsock Catalog",
            description: "Resets the Winsock catalog to default settings. Useful for fixing network connectivity issues. Requires system restart to take effect.",
            risk: TweakRiskLevel.Advanced,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = System.IO.Path.Combine(Environment.SystemDirectory, NetshExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "winsock", "show", "catalog" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = System.IO.Path.Combine(Environment.SystemDirectory, NetshExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "winsock", "reset" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        state = $"Winsock catalog has {lines.Length} entries";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains("reset", StringComparison.OrdinalIgnoreCase) ||
               result.StandardOutput.Contains("successfully", StringComparison.OrdinalIgnoreCase);
    }
}
