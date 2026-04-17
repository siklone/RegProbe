namespace RegProbe.App.Services;

internal static class NohutoRepositoryCategoryResolver
{
    public static string Resolve(string repoId, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var topLevel = segments.Length > 0 ? segments[0].ToLowerInvariant() : string.Empty;
        var fileName = segments.Length > 0 ? segments[^1].ToLowerInvariant() : path.ToLowerInvariant();

        return repoId.ToLowerInvariant() switch
        {
            "win-config" => ResolveWinConfigCategory(topLevel, fileName),
            "win-registry" => ResolveWinRegistryCategory(path, fileName),
            "decompiled-pseudocode" => ResolveDecompiledCategory(topLevel, fileName),
            "regkit" => ResolveRegKitCategory(path, topLevel, fileName),
            _ => NohutoKeywordCategoryResolver.Resolve(fileName)
        };
    }

    private static string ResolveWinConfigCategory(string topLevel, string fileName)
    {
        return topLevel switch
        {
            "affinities" => "Performance",
            "cleanup" => "Maintenance",
            "misc" => "Misc",
            "network" => "Network",
            "nvidia" => "Graphics",
            "peripheral" => "Peripheral",
            "policies" => "Policy",
            "power" => "Power",
            "privacy" => "Privacy",
            "security" => "Security",
            "system" => "System",
            "visibility" => "Display",
            _ => NohutoKeywordCategoryResolver.Resolve(fileName)
        };
    }

    private static string ResolveWinRegistryCategory(string path, string fileName)
    {
        if (path.StartsWith("records/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoKeywordCategoryResolver.Resolve(fileName);
        }

        if (path.StartsWith("guide/", StringComparison.OrdinalIgnoreCase))
        {
            return "Documentation";
        }

        if (path.Contains("assets/dxg", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("assets/dwm", StringComparison.OrdinalIgnoreCase))
        {
            return "Graphics";
        }

        if (path.Contains("assets/intel-nic", StringComparison.OrdinalIgnoreCase))
        {
            return "Network";
        }

        if (path.Contains("assets/stornvme", StringComparison.OrdinalIgnoreCase))
        {
            return "Storage";
        }

        if (path.Contains("assets/mmcss", StringComparison.OrdinalIgnoreCase))
        {
            return "System";
        }

        return NohutoKeywordCategoryResolver.Resolve(fileName);
    }

    private static string ResolveDecompiledCategory(string topLevel, string fileName)
    {
        return topLevel switch
        {
            "dxgkrnl" => "Graphics",
            "dxgmms2" => "Graphics",
            "dwm" => "Display",
            "dwmcore" => "Display",
            "win32kbase" => "Display",
            "win32kfull" => "Display",
            "usbhub3" => "Peripheral",
            "usbxhci" => "Peripheral",
            "usbhub" => "Peripheral",
            "stornvme" => "Storage",
            "mmcss" => "System",
            "wdf01000" => "System",
            "acpi" => "Power",
            "ntoskrnl" => "Kernel",
            _ => NohutoKeywordCategoryResolver.Resolve(fileName)
        };
    }

    private static string ResolveRegKitCategory(string path, string topLevel, string fileName)
    {
        if (topLevel.Equals("installer", StringComparison.OrdinalIgnoreCase))
        {
            return "Installer";
        }

        if (path.Contains("trace", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("default", StringComparison.OrdinalIgnoreCase))
        {
            return "Research";
        }

        if (path.Contains("theme", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("icon", StringComparison.OrdinalIgnoreCase) ||
            topLevel.Equals("resources", StringComparison.OrdinalIgnoreCase))
        {
            return "UI";
        }

        if (path.Contains("ti", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("system", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("elevat", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("rights", StringComparison.OrdinalIgnoreCase))
        {
            return "Security";
        }

        if (topLevel.Equals("src", StringComparison.OrdinalIgnoreCase) ||
            topLevel.Equals("include", StringComparison.OrdinalIgnoreCase))
        {
            return "Registry";
        }

        return NohutoKeywordCategoryResolver.Resolve(fileName);
    }
}
