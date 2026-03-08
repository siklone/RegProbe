using System;
using System.Collections.Generic;
using System.IO;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearTemporaryFilesTweak : FileCleanupTweak
{
    public ClearTemporaryFilesTweak()
        : base(
            id: "cleanup.temp-files",
            name: "Clear Temporary Files",
            description: "Deletes temporary files from user and system temp folders. Files in use will be skipped.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        // User temp
        yield return Path.GetTempPath();

        // System temp
        yield return Path.Combine(Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows", "Temp");
    }
}
