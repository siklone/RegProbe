using System;
using System.Collections.Generic;
using System.IO;
using RegProbe.Core;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearMemoryDumpFilesTweak : FileCleanupTweak
{
    public ClearMemoryDumpFilesTweak()
        : base(
            id: "cleanup.memory-dumps",
            name: "Clear Memory Dump Files",
            description: "Deletes BSoD memory dump files (MEMORY.DMP). These can be several GB in size.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var winDir = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";

        // Main memory dump
        var memoryDmpPath = Path.Combine(winDir, "MEMORY.DMP");
        if (File.Exists(memoryDmpPath))
        {
            yield return memoryDmpPath;
        }

        // Minidumps folder
        yield return Path.Combine(winDir, "Minidump");
    }
}
