using System;
using System.Collections.Generic;
using System.IO;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearWindowsUpdateCacheTweak : FileCleanupTweak
{
    public ClearWindowsUpdateCacheTweak()
        : base(
            id: "cleanup-windows-update-cache",
            name: "Clear Windows Update Cache",
            description: "Resets Windows Update cache (SoftwareDistribution and catroot2). Use this to fix update loops. Update catalog metadata will be redownloaded.",
            risk: TweakRiskLevel.Advanced,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var winDir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";

        yield return Path.Combine(winDir, "SoftwareDistribution");
        yield return Path.Combine(winDir, "System32", "catroot2");
    }
}
