using System;
using System.Collections.Generic;
using System.IO;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearPrefetchFilesTweak : FileCleanupTweak
{
    public ClearPrefetchFilesTweak()
        : base(
            id: "cleanup-prefetch-files",
            name: "Clear Prefetch Files",
            description: "Clears Windows prefetch files used for application launch optimization. Files will be regenerated over time.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var winDir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
        yield return Path.Combine(winDir, "Prefetch");
    }
}
