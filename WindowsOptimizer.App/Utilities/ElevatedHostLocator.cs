using System;
using System.IO;
using WindowsOptimizer.Infrastructure.Elevation;

namespace WindowsOptimizer.App.Utilities;

public static class ElevatedHostLocator
{
    public static string GetExecutablePath()
    {
        return Path.Combine(AppContext.BaseDirectory, ElevatedHostDefaults.ExecutableName);
    }
}
