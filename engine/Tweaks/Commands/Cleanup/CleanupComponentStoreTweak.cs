using System;
using System.IO;
using System.Collections.ObjectModel;
using RegProbe.Core;
using RegProbe.Core.Commands;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class CleanupComponentStoreTweak : CommandTweak, ITweakStepTimeouts
{
    private const string System32DismExe = "dism.exe";
    private static readonly TimeSpan AnalyzeTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ApplyTimeout = TimeSpan.FromMinutes(20);

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
        if (TryGetCleanupRecommendation(result.StandardOutput, out var cleanupRecommended))
        {
            state = cleanupRecommended ? "Cleanup recommended" : "Cleanup not needed";
            return true;
        }

        state = "Component store analyzed";
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return TryGetCleanupRecommendation(result.StandardOutput, out var cleanupRecommended)
            && !cleanupRecommended;
    }

    public TimeSpan? GetStepTimeout(TweakAction action)
    {
        return action switch
        {
            TweakAction.Detect => AnalyzeTimeout,
            TweakAction.Apply => ApplyTimeout,
            TweakAction.Verify => AnalyzeTimeout,
            _ => null
        };
    }

    private static bool TryGetCleanupRecommendation(string output, out bool cleanupRecommended)
    {
        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.Contains("Component Store Cleanup Recommended", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separatorIndex = line.LastIndexOf(':');
            var value = separatorIndex >= 0
                ? line[(separatorIndex + 1)..].Trim()
                : string.Empty;

            if (value.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                cleanupRecommended = true;
                return true;
            }

            if (value.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                cleanupRecommended = false;
                return true;
            }
        }

        cleanupRecommended = false;
        return false;
    }
}
