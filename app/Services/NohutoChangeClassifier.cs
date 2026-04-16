namespace RegProbe.App.Services;

internal static class NohutoChangeClassifier
{
    public static string ResolveCategory(string repoId, string path)
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
            _ => ResolveKeywordCategory(fileName)
        };
    }

    public static NohutoChangeKind ResolveChangeKind(string path)
    {
        if (path.StartsWith("guide/", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Documentation;
        }

        if (path.StartsWith("records/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Data;
        }

        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var byExtension = extension switch
        {
            ".ps1" or ".cmd" or ".bat" or ".vbs" or ".reg" or ".py" or ".iss" => NohutoChangeKind.Script,
            ".c" or ".cc" or ".cpp" or ".h" or ".hpp" or ".rc" or ".vcxproj" or ".filters" or ".props" or ".sln" => NohutoChangeKind.Source,
            ".ico" or ".png" or ".jpg" or ".jpeg" or ".svg" or ".bmp" => NohutoChangeKind.Asset,
            ".txt" or ".json" or ".csv" => NohutoChangeKind.Data,
            _ => NohutoChangeKind.Data
        };

        if (byExtension != NohutoChangeKind.Data)
        {
            return byExtension;
        }

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Asset;
        }

        if (path.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("include/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/src/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/include/", StringComparison.OrdinalIgnoreCase))
        {
            return NohutoChangeKind.Source;
        }

        return NohutoChangeKind.Data;
    }

    public static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimStart('/');

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
            _ => ResolveKeywordCategory(fileName)
        };
    }

    private static string ResolveWinRegistryCategory(string path, string fileName)
    {
        if (path.StartsWith("records/", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveKeywordCategory(fileName);
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

        return ResolveKeywordCategory(fileName);
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
            _ => ResolveKeywordCategory(fileName)
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

        return ResolveKeywordCategory(fileName);
    }

    private static string ResolveKeywordCategory(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Misc";
        }

        var key = value.ToLowerInvariant();

        if (key.Contains("power") || key.Contains("hiber") || key.Contains("sleep") || key.Contains("acpi")) return "Power";
        if (key.Contains("tcpip") || key.Contains("dnscache") || key.Contains("dns") || key.Contains("net") || key.Contains("nic") || key.Contains("ndis") || key.Contains("nla")) return "Network";
        if (key.Contains("defender") || key.Contains("lsa") || key.Contains("security") || key.Contains("tpm") || key.Contains("bitlocker") || key.Contains("crypt")) return "Security";
        if (key.Contains("privacy") || key.Contains("error-report") || key.Contains("telemetry")) return "Privacy";
        if (key.Contains("audio") || key.Contains("sound")) return "Audio";
        if (key.Contains("explorer") || key.Contains("desktop") || key.Contains("mouse") || key.Contains("visibility") || key.Contains("monitor")) return "Display";
        if (key.Contains("dxg") || key.Contains("graphics") || key.Contains("dwm") || key.Contains("gpu") || key.Contains("nvidia")) return "Graphics";
        if (key.Contains("perf") || key.Contains("stornvme") || key.Contains("storport") || key.Contains("storage") || key.Contains("disk") || key.Contains("nvme")) return "Storage";
        if (key.Contains("usb") || key.Contains("xhci") || key.Contains("kbd") || key.Contains("mou") || key.Contains("input") || key.Contains("touch") || key.Contains("pen") || key.Contains("peripheral")) return "Peripheral";
        if (key.Contains("policy")) return "Policy";
        if (key.Contains("cleanup")) return "Maintenance";
        if (key.Contains("trace") || key.Contains("record") || key.Contains("registry")) return "Research";
        if (key.Contains("kernel") || key.Contains("system") || key.Contains("mmcss") || key.Contains("service") || key.Contains("session") || key.Contains("pnp") || key.Contains("wdf")) return "System";

        return "Misc";
    }
}

internal enum NohutoChangeKind
{
    Documentation,
    Script,
    Source,
    Asset,
    Data
}
