using System;
using System.Collections.ObjectModel;
using System.IO;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearEventLogsTweak : CommandTweak
{
    private const string WevtutilExe = "wevtutil.exe";
    private readonly string _logName;

    public ClearEventLogsTweak(ICommandRunner commandRunner, string logName = "System")
        : base(
            id: $"cleanup-eventlog-{logName.ToLowerInvariant()}",
            name: $"Clear {logName} Event Log",
            description: $"Clears the Windows {logName} event log. WARNING: Logs cannot be recovered after clearing.",
            risk: TweakRiskLevel.Advanced,
            commandRunner: commandRunner)
    {
        _logName = logName ?? throw new ArgumentNullException(nameof(logName));
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, WevtutilExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "gli", _logName }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = Path.Combine(Environment.SystemDirectory, WevtutilExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "cl", _logName }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        // Event logs cannot be restored once cleared
        return null;
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput;

        // Extract log file size from output
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains("numberOfLogRecords:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    var recordCount = parts[1].Trim();
                    state = $"{recordCount} records";
                    return true;
                }
            }
        }

        state = "Event log analyzed";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        // Successful clear returns exit code 0
        return result.ExitCode == 0;
    }
}
