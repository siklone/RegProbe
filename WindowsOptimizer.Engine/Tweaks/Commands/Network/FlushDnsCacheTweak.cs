using System;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Network;

public sealed class FlushDnsCacheTweak : CommandTweak
{
    private const string IpConfigExe = "ipconfig.exe";

    public FlushDnsCacheTweak(ICommandRunner commandRunner)
        : base(
            id: "network.flush-dns-cache",
            name: "Flush DNS Cache",
            description: "Clears the DNS resolver cache. Useful for resolving DNS issues or ensuring fresh DNS lookups. This is a one-time operation and cannot be rolled back.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, IpConfigExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/displaydns" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, IpConfigExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "/flushdns" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var entryCount = 0;

        foreach (var line in lines)
        {
            if (line.Contains("Record Name", StringComparison.OrdinalIgnoreCase))
            {
                entryCount++;
            }
        }

        state = $"DNS cache has {entryCount} entries";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.StandardOutput.Contains("Successfully flushed", StringComparison.OrdinalIgnoreCase);
    }
}
