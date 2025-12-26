using System;
using System.IO;
using WindowsOptimizer.Infrastructure.Elevation;

namespace WindowsOptimizer.App.Utilities;

public static class ElevatedHostLocator
{
    public static string GetExecutablePath()
    {
        var baseDirectory = AppContext.BaseDirectory;

        // Preferred layout: keep the elevated host in a subfolder with its dependencies.
        var subfolderPath = Path.Combine(baseDirectory, "ElevatedHost", ElevatedHostDefaults.ExecutableName);
        if (File.Exists(subfolderPath))
        {
            return subfolderPath;
        }

        // Some build flows (or older copy targets) may place the host under a "publish" folder.
        var publishSubfolderPath = Path.Combine(baseDirectory, "publish", "ElevatedHost", ElevatedHostDefaults.ExecutableName);
        if (File.Exists(publishSubfolderPath))
        {
            return publishSubfolderPath;
        }

        // Backward compatibility: older builds placed the executable next to the main app.
        var legacyPath = Path.Combine(baseDirectory, ElevatedHostDefaults.ExecutableName);
        if (File.Exists(legacyPath))
        {
            return legacyPath;
        }

        return subfolderPath;
    }
}
