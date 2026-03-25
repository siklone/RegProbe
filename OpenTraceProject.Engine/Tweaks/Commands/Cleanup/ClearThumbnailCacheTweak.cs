using System;
using System.Collections.Generic;
using System.IO;
using OpenTraceProject.Core;

namespace OpenTraceProject.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearThumbnailCacheTweak : FileCleanupTweak
{
    public ClearThumbnailCacheTweak()
        : base(
            id: "cleanup.thumbnail-cache",
            name: "Clear Thumbnail Cache",
            description: "Clears Explorer thumbnail cache files. Thumbnails will be regenerated when needed.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: false)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var thumbnailPath = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");

        if (Directory.Exists(thumbnailPath))
        {
            // Return all thumbcache files
            foreach (var file in Directory.GetFiles(thumbnailPath, "thumbcache_*.db"))
            {
                yield return file;
            }
        }
    }
}
