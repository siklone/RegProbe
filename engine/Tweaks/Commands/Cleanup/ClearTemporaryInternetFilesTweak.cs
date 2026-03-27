using System;
using System.Collections.Generic;
using System.IO;
using RegProbe.Core;

namespace RegProbe.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearTemporaryInternetFilesTweak : FileCleanupTweak
{
    public ClearTemporaryInternetFilesTweak()
        : base(
            id: "cleanup.internet-temp-files",
            name: "Clear Temporary Internet Files",
            description: "Clears legacy WinINet cache (INetCache, INetCookies, WebCache, History). Used by Explorer, old Control Panel, and some installers.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: false)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var microsoftPath = Path.Combine(localAppData, "Microsoft", "Windows");

        yield return Path.Combine(microsoftPath, "INetCache");
        yield return Path.Combine(microsoftPath, "INetCookies");
        yield return Path.Combine(microsoftPath, "WebCache");
        yield return Path.Combine(microsoftPath, "History");
    }
}
