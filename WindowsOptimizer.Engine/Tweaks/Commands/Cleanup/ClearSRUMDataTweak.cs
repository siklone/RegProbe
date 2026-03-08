using System;
using System.Collections.Generic;
using System.IO;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearSRUMDataTweak : FileCleanupTweak
{
    public ClearSRUMDataTweak()
        : base(
            id: "cleanup.srum-data",
            name: "Clear SRUM Database",
            description: "Deletes the System Resource Usage Monitor (SRUM) database which tracks app, service, and network usage.",
            risk: TweakRiskLevel.Advanced,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var winDir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
        yield return Path.Combine(winDir, "System32", "sru");
    }
}
