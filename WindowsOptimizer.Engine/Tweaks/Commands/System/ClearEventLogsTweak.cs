using System;
using System.Collections.ObjectModel;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.System;

public sealed class ClearEventLogsTweak : CommandTweak
{
    private const string WevtutilExe = "wevtutil.exe";

    public ClearEventLogsTweak(ICommandRunner commandRunner)
        : base(
            id: "system-clear-event-logs",
            name: "Clear Windows Event Logs",
            description: "Clears all Windows Event Logs (Application, System, Security). This operation cannot be undone, so ensure you have backed up important logs if needed.",
            risk: TweakRiskLevel.Advanced,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = System.IO.Path.Combine(Environment.SystemDirectory, WevtutilExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "gli", "Application" }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = System.IO.Path.Combine(Environment.SystemDirectory, WevtutilExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "cl", "Application" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        if (result.StandardOutput.Contains("numberOfLogRecords:", StringComparison.OrdinalIgnoreCase))
        {
            var lines = result.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("numberOfLogRecords:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        state = $"Application log has {parts[1].Trim()} records";
                        return true;
                    }
                }
            }
        }

        state = "Event log status available";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return result.ExitCode == 0;
    }
}
