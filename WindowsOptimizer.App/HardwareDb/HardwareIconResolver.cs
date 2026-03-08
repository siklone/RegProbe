using System;
using System.Collections.Generic;
using System.Windows.Media;
using WindowsOptimizer.App.HardwareDb.Models;

namespace WindowsOptimizer.App.HardwareDb;

public static class HardwareIconResolver
{
    private static readonly Dictionary<string, string> LegacyFallbackByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["os"] = "os/windows10",
        ["cpu"] = "cpu_default",
        ["gpu"] = "gpu_default",
        ["chipset"] = "chipset_default",
        ["memory"] = "memory_default",
        ["storage"] = "storage_default",
        ["motherboard"] = "motherboard_default",
        ["network"] = "network_default",
        ["usb"] = "usb_default",
        ["display"] = "display_default",
        ["audio"] = "cpu_default"
    };

    private static readonly Dictionary<string, HardwareType> CategoryToType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["os"] = HardwareType.Os,
        ["cpu"] = HardwareType.Cpu,
        ["gpu"] = HardwareType.Gpu,
        ["motherboard"] = HardwareType.Motherboard,
        ["memory"] = HardwareType.Memory,
        ["storage"] = HardwareType.Storage,
        ["display"] = HardwareType.Display,
        ["network"] = HardwareType.Network,
        ["usb"] = HardwareType.Usb,
        ["audio"] = HardwareType.Audio
    };

    public static string GetFallbackKey(string category)
    {
        var normalizedCategory = HardwareIconSourceDb.NormalizeCategory(category);
        if (normalizedCategory.Equals("os", StringComparison.OrdinalIgnoreCase))
        {
            return GetRuntimeOsFallbackKey();
        }

        var candidate = LegacyFallbackByCategory.TryGetValue(normalizedCategory, out var fallback)
            ? fallback
            : $"{normalizedCategory}_default";
        return HardwareIconSourceDb.ResolveFallbackKey(normalizedCategory, candidate);
    }

    public static string ResolveIconKey(string category, string? modelName, string? fallbackKey = null)
    {
        var normalizedCategory = HardwareIconSourceDb.NormalizeCategory(category);
        var fallback = HardwareIconSourceDb.ResolveFallbackKey(normalizedCategory, fallbackKey ?? GetFallbackKey(normalizedCategory));

        if (CategoryToType.TryGetValue(normalizedCategory, out var hardwareType))
        {
            var resolved = HardwareIconService.ResolveIconKey(hardwareType, modelName);
            return HardwareIconSourceDb.NormalizeKey(resolved, normalizedCategory, fallback);
        }

        var keyFromV2 = HardwareIconResolverV2.ResolveIconKey(normalizedCategory, modelName);
        return HardwareIconSourceDb.NormalizeKey(keyFromV2, normalizedCategory, fallback);
    }

    public static ImageSource ResolveCategoryIcon(string category, string? modelName)
    {
        var fallback = GetFallbackKey(category);
        var iconKey = ResolveIconKey(category, modelName, fallback);
        return ResolveIcon(iconKey, fallback);
    }

    public static string NormalizeIconKey(string? iconKey, string fallbackKey)
    {
        return HardwareIconSourceDb.NormalizeKeyForFallback(iconKey, fallbackKey);
    }

    public static ImageSource ResolveIcon(HardwareModelBase? model, string fallbackKey)
    {
        return ResolveIcon(model?.IconKey, fallbackKey);
    }

    public static ImageSource ResolveIcon(string? iconKey, string fallbackKey)
    {
        var normalizedFallback = HardwareIconSourceDb.NormalizeKeyForFallback(fallbackKey, fallbackKey);
        var normalizedIconKey = HardwareIconSourceDb.NormalizeKeyForFallback(iconKey, normalizedFallback);
        return HardwareIconResolverV2.ResolveIcon(normalizedIconKey, normalizedFallback);
    }

    public static void PreloadIcons()
    {
        HardwareIconResolverV2.PreloadIcons();
    }

    public static string FromIconKey(string? iconKey, string fallbackFile)
    {
        return HardwareIconResolverV2.FromIconKey(iconKey, fallbackFile);
    }

    public static ImageSource ResolveOsIcon(string? normalizedOsName)
    {
        if (string.IsNullOrWhiteSpace(normalizedOsName))
        {
            var fallback = GetRuntimeOsFallbackKey();
            return ResolveIcon(fallback, fallback);
        }

        var iconKey = ResolveOsIconKey(normalizedOsName);
        return ResolveIcon(iconKey, GetRuntimeOsFallbackKey());
    }

    public static string ResolveOsIconKey(string? normalizedOsName)
    {
        if (string.IsNullOrWhiteSpace(normalizedOsName))
        {
            return GetRuntimeOsFallbackKey();
        }

        var normalized = HardwareNameNormalizer.Normalize(normalizedOsName);
        if (normalized.Contains("windows 11", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareIconSourceDb.ResolveFallbackKey("os", "os/windows11");
        }

        if (normalized.Contains("windows 10", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareIconSourceDb.ResolveFallbackKey("os", "os/windows10");
        }

        return HardwareIconResolverV2.ResolveOsIconKey(normalizedOsName);
    }

    private static string GetRuntimeOsFallbackKey()
    {
        var fallback = Environment.OSVersion.Version.Build >= 22000 ? "os/windows11" : "os/windows10";
        return HardwareIconSourceDb.ResolveFallbackKey("os", fallback);
    }

    public static string GetCpuIcon(string? cpuName)
    {
        return HardwareIconResolverV2.GetCpuIcon(cpuName);
    }

    public static string GetGpuIcon(string? gpuName)
    {
        return HardwareIconResolverV2.GetGpuIcon(gpuName);
    }

    public static string GetStorageIcon(string? storageName)
    {
        return HardwareIconResolverV2.GetStorageIcon(storageName);
    }

    public static string GetMemoryIcon(string? memoryName)
    {
        return HardwareIconResolverV2.GetMemoryIcon(memoryName);
    }

    public static string GetMotherboardIcon(string? boardName)
    {
        return HardwareIconResolverV2.GetMotherboardIcon(boardName);
    }
}
