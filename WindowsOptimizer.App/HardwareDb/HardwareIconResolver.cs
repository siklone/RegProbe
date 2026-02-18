using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowsOptimizer.App.HardwareDb.Models;

namespace WindowsOptimizer.App.HardwareDb;

public static class HardwareIconResolver
{
    private sealed class IconRule
    {
        public string Match { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<IconRule>>> RuleCatalog = new(LoadRuleCatalog);

    private static readonly Dictionary<string, string> FallbackByCategory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["os"] = "os/windows10",
        ["cpu"] = "cpu_generic",
        ["gpu"] = "gpu_generic",
        ["chipset"] = "chipset_generic",
        ["memory"] = "memory_generic",
        ["storage"] = "storage_generic",
        ["motherboard"] = "chipset_generic",
        ["network"] = "network_generic",
        ["usb"] = "usb_generic",
        ["display"] = "display_generic"
    };

    private static readonly Dictionary<string, string> GenericAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cpu_generic"] = "cpu_default",
        ["gpu_generic"] = "gpu_default",
        ["chipset_generic"] = "motherboard_default",
        ["memory_generic"] = "memory_default",
        ["storage_generic"] = "storage_default",
        ["network_generic"] = "network_default",
        ["usb_generic"] = "usb_default",
        ["display_generic"] = "display_default"
    };

    private static readonly Dictionary<string, ImageSource> CachedImages = new(StringComparer.OrdinalIgnoreCase);

    private static string P(string file) => $"pack://application:,,,/{file}";

    public static string GetFallbackKey(string category)
    {
        if (FallbackByCategory.TryGetValue(category, out var key))
        {
            return key;
        }

        return "cpu_generic";
    }

    public static string ResolveIconKey(string category, string? modelName, string? fallbackKey = null)
    {
        var key = (category ?? string.Empty).Trim().ToLowerInvariant();
        var fallback = string.IsNullOrWhiteSpace(fallbackKey) ? GetFallbackKey(key) : fallbackKey!;
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return fallback;
        }

        if (!RuleCatalog.Value.TryGetValue(key, out var rules) || rules.Count == 0)
        {
            return fallback;
        }

        var normalized = HardwareNameNormalizer.Normalize(modelName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Match) || string.IsNullOrWhiteSpace(rule.Icon))
            {
                continue;
            }

            var match = rule.Match.Trim().ToLowerInvariant();
            if (normalized.Contains(match, StringComparison.OrdinalIgnoreCase))
            {
                return rule.Icon.Trim();
            }
        }

        return fallback;
    }

    public static ImageSource ResolveCategoryIcon(string category, string? modelName)
    {
        var fallback = GetFallbackKey(category);
        var iconKey = ResolveIconKey(category, modelName, fallback);
        return ResolveIcon(iconKey, fallback);
    }

    public static string NormalizeIconKey(string? iconKey, string fallbackKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            return fallbackKey;
        }

        var normalized = iconKey.Trim();
        return normalized;
    }

    public static ImageSource ResolveIcon(HardwareModelBase? model, string fallbackKey)
    {
        return ResolveIcon(model?.IconKey, fallbackKey);
    }

    public static ImageSource ResolveIcon(string? iconKey, string fallbackKey)
    {
        var key = NormalizeIconKey(iconKey, fallbackKey);
        var fileKey = ResolveExistingFileKey(key, fallbackKey);

        if (CachedImages.TryGetValue(fileKey, out var cached))
        {
            return cached;
        }

        var image = CreateImage(fileKey);
        CachedImages[fileKey] = image;
        return image;
    }

    public static void PreloadIcons()
    {
        var preloadKeys = new[]
        {
            "windows10", "windows11", "os/windows10", "os/windows11",
            "cpu_default", "cpu_intel", "cpu_amd", "cpu_i5", "cpu_i7", "cpu_i9", "cpu_ryzen5", "cpu_ryzen7", "cpu_ryzen9",
            "gpu_default", "gpu_nvidia", "gpu_radeon", "gpu_gtx", "gpu_rtx", "gpu_rx9000",
            "memory_default", "memory_corsair", "memory_kingston",
            "storage_default", "storage_hdd", "storage_ssd", "storage_nvme",
            "motherboard_default", "mb_asus", "mb_asrock", "mb_msi", "mb_gigabyte",
            "display_default", "network_default", "usb_default", "asus", "asrock", "msi", "gigabyte"
        };

        foreach (var key in preloadKeys)
        {
            _ = ResolveIcon(key, key);
        }

        foreach (var fallback in FallbackByCategory.Values)
        {
            _ = ResolveIcon(fallback, fallback);
        }
    }

    public static string FromIconKey(string? iconKey, string fallbackFile)
    {
        var fallbackKey = fallbackFile.Replace("Assets/Icons/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase);
        var fileKey = ResolveExistingFileKey(iconKey, fallbackKey);
        return P($"Assets/Icons/{fileKey}.png");
    }

    public static ImageSource ResolveOsIcon(string? normalizedOsName)
    {
        var iconKey = ResolveIconKey("os", normalizedOsName, "os/windows10");
        return ResolveIcon(iconKey, "os/windows10");
    }

    public static string ResolveOsIconKey(string? normalizedOsName)
    {
        return ResolveIconKey("os", normalizedOsName, "os/windows10");
    }

    private static ImageSource CreateImage(string fileKey)
    {
        var uriText = BuildCandidateUris(fileKey).FirstOrDefault(ResourceExists)
            ?? P($"Assets/Icons/{ResolveExistingFileKey(fileKey, "cpu_default")}.png");
        var uri = new Uri(uriText, UriKind.Absolute);
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = uri;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static string ResolveExistingFileKey(string? iconKey, string fallbackKey)
    {
        var key = NormalizeIconKey(iconKey, fallbackKey);
        var directUris = BuildCandidateUris(key);
        if (directUris.Any(ResourceExists))
        {
            return key;
        }

        if (GenericAlias.TryGetValue(key, out var aliased))
        {
            var aliasedUris = BuildCandidateUris(aliased);
            if (aliasedUris.Any(ResourceExists))
            {
                return aliased;
            }
        }

        if (GenericAlias.TryGetValue(fallbackKey, out var fallbackAlias))
        {
            return fallbackAlias;
        }

        return fallbackKey;
    }

    private static bool ResourceExists(string uri)
    {
        try
        {
            return Application.GetResourceStream(new Uri(uri, UriKind.Absolute)) != null;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> BuildCandidateUris(string key)
    {
        if (key.Contains('/'))
        {
            yield return P($"Resources/Icons/{key}.png");
        }

        yield return P($"Assets/Icons/{key}.png");
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<IconRule>> LoadRuleCatalog()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "HardwareDb", "hardware_icons.json");
            if (!File.Exists(path))
            {
                return new Dictionary<string, IReadOnlyList<IconRule>>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, List<IconRule>>>(json)
                ?? new Dictionary<string, List<IconRule>>(StringComparer.OrdinalIgnoreCase);

            return parsed.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyList<IconRule>)(kv.Value ?? new List<IconRule>()),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, IReadOnlyList<IconRule>>(StringComparer.OrdinalIgnoreCase);
        }
    }

        public static string GetCpuIcon(string? cpuName)
        {
            var value = HardwareNameNormalizer.Normalize(cpuName);
            if (string.IsNullOrWhiteSpace(value)) return P("Assets/Icons/cpu_default.png");

            // Prefer generic Ryzen asset for any Ryzen model/series
            if (value.Contains("ryzen", StringComparison.Ordinal)) return P("Assets/Icons/amd_ryzen_cpu.png");

            if (value.Contains("ryzen 9", StringComparison.Ordinal)) return P("Assets/Icons/cpu_ryzen9.png");
            if (value.Contains("ryzen 7", StringComparison.Ordinal)) return P("Assets/Icons/cpu_ryzen7.png");
            if (value.Contains("ryzen 5", StringComparison.Ordinal)) return P("Assets/Icons/cpu_ryzen5.png");
            if (value.Contains("core i9", StringComparison.Ordinal)) return P("Assets/Icons/cpu_i9.png");
            if (value.Contains("core i7", StringComparison.Ordinal)) return P("Assets/Icons/cpu_i7.png");
            if (value.Contains("core i5", StringComparison.Ordinal)) return P("Assets/Icons/cpu_i5.png");
            if (value.Contains("intel", StringComparison.Ordinal)) return P("Assets/Icons/cpu_intel.png");
            if (value.Contains("amd", StringComparison.Ordinal)) return P("Assets/Icons/cpu_amd.png");
            return P("Assets/Icons/cpu_default.png");
        }

    public static string GetGpuIcon(string? gpuName)
    {
        var value = HardwareNameNormalizer.Normalize(gpuName);
        if (string.IsNullOrWhiteSpace(value)) return P("Assets/Icons/gpu_default.png");

        if (value.Contains("rtx", StringComparison.Ordinal)) return P("Assets/Icons/gpu_rtx.png");
        if (value.Contains("gtx", StringComparison.Ordinal)) return P("Assets/Icons/gpu_gtx.png");
        if (value.Contains("radeon", StringComparison.Ordinal)) return P("Assets/Icons/gpu_radeon.png");
        if (value.Contains("rx 9", StringComparison.Ordinal)) return P("Assets/Icons/gpu_rx9000.png");
        if (value.Contains("nvidia", StringComparison.Ordinal)) return P("Assets/Icons/gpu_nvidia.png");
        return P("Assets/Icons/gpu_default.png");
    }

    public static string GetStorageIcon(string? storageName)
    {
        var value = HardwareNameNormalizer.Normalize(storageName);
        if (value.Contains("nvme", StringComparison.Ordinal) || value.Contains("pcie", StringComparison.Ordinal)) return P("Assets/Icons/storage_nvme.png");
        if (value.Contains("ssd", StringComparison.Ordinal)) return P("Assets/Icons/storage_ssd.png");
        if (value.Contains("hdd", StringComparison.Ordinal) || value.Contains("sata", StringComparison.Ordinal)) return P("Assets/Icons/storage_hdd.png");
        return P("Assets/Icons/storage_default.png");
    }

    public static string GetMemoryIcon(string? memoryName)
    {
        var value = HardwareNameNormalizer.Normalize(memoryName);
        if (value.Contains("corsair", StringComparison.Ordinal)) return P("Assets/Icons/memory_corsair.png");
        if (value.Contains("kingston", StringComparison.Ordinal)) return P("Assets/Icons/memory_kingston.png");
        return P("Assets/Icons/memory_default.png");
    }

    public static string GetMotherboardIcon(string? boardName)
    {
        var value = HardwareNameNormalizer.Normalize(boardName);
        if (value.Contains("asus", StringComparison.Ordinal)) return P("Assets/Icons/mb_asus.png");
        if (value.Contains("asrock", StringComparison.Ordinal)) return P("Assets/Icons/mb_asrock.png");
        if (value.Contains("msi", StringComparison.Ordinal)) return P("Assets/Icons/mb_msi.png");
        if (value.Contains("gigabyte", StringComparison.Ordinal)) return P("Assets/Icons/mb_gigabyte.png");
        return P("Assets/Icons/motherboard_default.png");
    }
}
