using System;
using System.Collections.Generic;
using System.IO;
using OpenTraceProject.Core;

namespace OpenTraceProject.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearWindowsOldTweak : FileCleanupTweak
{
    public ClearWindowsOldTweak()
        : base(
            id: "cleanup.windows-old",
            name: "Delete Windows.old Folder",
            description: "Removes previous Windows installation files from Windows.old. WARNING: You will not be able to roll back to the previous Windows version after deletion.",
            risk: TweakRiskLevel.Risky,
            requiresElevation: true)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var drives = DriveInfo.GetDrives();
        foreach (var drive in drives)
        {
            if (drive.DriveType == DriveType.Fixed && drive.IsReady)
            {
                var windowsOldPath = Path.Combine(drive.RootDirectory.FullName, "Windows.old");
                if (Directory.Exists(windowsOldPath))
                {
                    yield return windowsOldPath;
                }
            }
        }
    }
}
