using System;
using System.Diagnostics;
using System.IO;

namespace RegProbe.App.Utilities;

public static class ContributorMode
{
    private static readonly Lazy<bool> CachedIsEnabled = new(EvaluateIsEnabled);

    public static bool IsEnabled => CachedIsEnabled.Value;

    private static bool EvaluateIsEnabled()
    {
        var overrideValue = Environment.GetEnvironmentVariable("REGPROBE_CONTRIBUTOR_MODE");
        if (!string.IsNullOrWhiteSpace(overrideValue))
        {
            if (overrideValue.Equals("1", StringComparison.OrdinalIgnoreCase)
                || overrideValue.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (overrideValue.Equals("0", StringComparison.OrdinalIgnoreCase)
                || overrideValue.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (Debugger.IsAttached)
        {
            return true;
        }

        try
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            for (var depth = 0; depth < 8 && current is not null; depth++)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git"))
                    || File.Exists(Path.Combine(current.FullName, "RegProbe.sln"))
                    || File.Exists(Path.Combine(current.FullName, "RegProbe.slnx")))
                {
                    return true;
                }

                current = current.Parent;
            }
        }
        catch
        {
        }

        return false;
    }
}
