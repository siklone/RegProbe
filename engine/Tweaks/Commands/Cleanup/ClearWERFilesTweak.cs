using System;
using System.Collections.Generic;
using System.IO;
using RegProbe.Core;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearWERFilesTweak : FileCleanupTweak
{
    public ClearWERFilesTweak()
        : base(
            id: "cleanup.wer-files",
            name: "Clear Windows Error Reporting Files",
            description: "Deletes Windows Error Reporting (WER) crash dumps and report metadata from system and user folders.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // System WER
        yield return Path.Combine(programData, "Microsoft", "Windows", "WER");

        // User WER
        yield return Path.Combine(localAppData, "Microsoft", "Windows", "WER");

        // Temp WER folders
        var temp = Path.GetTempPath();
        yield return Path.Combine(temp, "WER");
    }
}
