using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WindowsOptimizer.App.HardwareDb;

public static class HardwareIconResolverV2
{
    private sealed class IconMapping
    {
        public string Match { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    private sealed class IconMapDocument
    {
        public Dictionary<string, string> Fallbacks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<IconMapping>> Mappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private static readonly Lazy<IconMapDocument> IconMap = new(LoadIconMap);
    private static readonly Dictionary<string, ImageSource> CachedImages = new(StringComparer.OrdinalIgnoreCase);

    private static string P(string file) => $"pack://application:,,,/{file}";

    public static string GetFallbackKey(string hardwareType)
    {
        var type = (hardwareType ?? string.Empty).Trim().ToLowerInvariant();
        var mappedFallback = IconMap.Value.Fallbacks.TryGetValue(type, out var fallback)
            ? fallback
            : $"{type}_default";
        return HardwareIconSourceDb.ResolveFallbackKey(type, mappedFallback);
    }

    public static string ResolveIconKey(string hardwareType, string? modelName)
    {
        var type = (hardwareType ?? string.Empty).Trim().ToLowerInvariant();
        var fallback = GetFallbackKey(type);

        if (string.IsNullOrWhiteSpace(modelName))
        {
            return fallback;
        }

        if (!IconMap.Value.Mappings.TryGetValue(type, out var mappings) || mappings.Count == 0)
        {
            return fallback;
        }

        var normalized = HardwareNameNormalizer.Normalize(modelName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        var sortedMappings = mappings.OrderByDescending(m => m.Priority).ToList();
        foreach (var mapping in sortedMappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.Match) || string.IsNullOrWhiteSpace(mapping.Icon))
            {
                continue;
            }

            if (normalized.Contains(mapping.Match, StringComparison.OrdinalIgnoreCase))
            {
                return HardwareIconSourceDb.NormalizeKey(mapping.Icon, type, fallback);
            }
        }

        return fallback;
    }

    public static ImageSource ResolveIcon(string? iconKey, string fallbackKey)
    {
        var normalizedFallback = HardwareIconSourceDb.NormalizeKeyForFallback(fallbackKey, fallbackKey);
        var key = NormalizeIconKey(iconKey, normalizedFallback);
        var fileKey = ResolveExistingFileKey(key, normalizedFallback);

        if (CachedImages.TryGetValue(fileKey, out var cached))
        {
            return cached;
        }

        var image = CreateImage(fileKey);
        CachedImages[fileKey] = image;
        return image;
    }

    public static ImageSource ResolveCategoryIcon(string hardwareType, string? modelName)
    {
        var fallback = GetFallbackKey(hardwareType);
        var iconKey = ResolveIconKey(hardwareType, modelName);
        return ResolveIcon(iconKey, fallback);
    }

    public static void PreloadIcons()
    {
        var preloadKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fallback in IconMap.Value.Fallbacks.Values)
        {
            preloadKeys.Add(fallback);
        }

        foreach (var mappings in IconMap.Value.Mappings.Values)
        {
            foreach (var mapping in mappings)
            {
                if (!string.IsNullOrWhiteSpace(mapping.Icon))
                {
                    preloadKeys.Add(mapping.Icon);
                }
            }
        }

        foreach (var key in preloadKeys)
        {
            _ = ResolveIcon(key, key);
        }
    }

    public static void ClearCache()
    {
        CachedImages.Clear();
    }

    public static string FromIconKey(string? iconKey, string fallbackFile)
    {
        var fallbackKey = fallbackFile
            .Replace("Assets/Icons/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase);
        var normalizedFallback = HardwareIconSourceDb.NormalizeKeyForFallback(fallbackKey, fallbackKey);
        var fileKey = ResolveExistingFileKey(iconKey, normalizedFallback);
        return P($"Assets/Icons/{fileKey}.png");
    }

    private static string NormalizeIconKey(string? iconKey, string fallbackKey)
    {
        return HardwareIconSourceDb.NormalizeKeyForFallback(iconKey, fallbackKey);
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

        if (key.Contains('/'))
        {
            var directUris = BuildCandidateUris(key);
            if (directUris.Any(ResourceExists))
            {
                return key;
            }
        }

        var flatUris = BuildCandidateUris(key);
        if (flatUris.Any(ResourceExists))
        {
            return key;
        }

        var fallbackUris = BuildCandidateUris(fallbackKey);
        if (fallbackUris.Any(ResourceExists))
        {
            return fallbackKey;
        }

        var genericFallback = GetGenericFallback(key);
        if (!string.IsNullOrEmpty(genericFallback))
        {
            var genericUris = BuildCandidateUris(genericFallback);
            if (genericUris.Any(ResourceExists))
            {
                return genericFallback;
            }
        }

        return fallbackKey;
    }

    private static string? GetGenericFallback(string iconKey)
    {
        if (iconKey.Equals("os_windows11", StringComparison.OrdinalIgnoreCase)) return "os/windows11";
        if (iconKey.Equals("os_windows10", StringComparison.OrdinalIgnoreCase)) return "os/windows10";
        if (iconKey.Equals("amd_ryzen_cpu", StringComparison.OrdinalIgnoreCase)) return "amd_ryzen";
        if (iconKey.StartsWith("cpu_amd_ryzen", StringComparison.OrdinalIgnoreCase)) return "amd_ryzen";
        if (iconKey.StartsWith("cpu_ryzen", StringComparison.OrdinalIgnoreCase)) return "amd_ryzen";
        if (iconKey.StartsWith("cpu_i", StringComparison.OrdinalIgnoreCase)) return "cpu_intel";
        if (iconKey.StartsWith("cpu_xeon", StringComparison.OrdinalIgnoreCase)) return "cpu_intel";
        if (iconKey.StartsWith("cpu_amd", StringComparison.OrdinalIgnoreCase)) return "cpu_amd";
        if (iconKey.StartsWith("gpu_nvidia_rtx", StringComparison.OrdinalIgnoreCase)) return "gpu_rtx";
        if (iconKey.StartsWith("gpu_nvidia_gtx", StringComparison.OrdinalIgnoreCase)) return "gpu_gtx";
        if (iconKey.StartsWith("gpu_nvidia", StringComparison.OrdinalIgnoreCase)) return "gpu_nvidia";
        if (iconKey.StartsWith("gpu_amd_rx", StringComparison.OrdinalIgnoreCase)) return "gpu_radeon";
        if (iconKey.StartsWith("gpu_rtx", StringComparison.OrdinalIgnoreCase)) return "gpu_rtx";
        if (iconKey.StartsWith("gpu_gtx", StringComparison.OrdinalIgnoreCase)) return "gpu_gtx";
        if (iconKey.StartsWith("gpu_rx", StringComparison.OrdinalIgnoreCase)) return "gpu_radeon";
        if (iconKey.StartsWith("gpu_rdna", StringComparison.OrdinalIgnoreCase)) return "gpu_radeon";
        if (iconKey.StartsWith("mb_asus", StringComparison.OrdinalIgnoreCase)) return "mb_asus";
        if (iconKey.StartsWith("mb_msi", StringComparison.OrdinalIgnoreCase)) return "mb_msi";
        if (iconKey.StartsWith("mb_gigabyte", StringComparison.OrdinalIgnoreCase)) return "mb_gigabyte";
        if (iconKey.StartsWith("mb_asrock", StringComparison.OrdinalIgnoreCase)) return "mb_asrock";
        if (iconKey.StartsWith("chipset_", StringComparison.OrdinalIgnoreCase)) return "motherboard_default";
        if (iconKey.StartsWith("memory_corsair", StringComparison.OrdinalIgnoreCase)) return "memory_corsair";
        if (iconKey.StartsWith("memory_kingston", StringComparison.OrdinalIgnoreCase)) return "memory_kingston";
        if (iconKey.StartsWith("memory_gskill", StringComparison.OrdinalIgnoreCase)) return "memory_gskill";
        if (iconKey.StartsWith("memory_crucial", StringComparison.OrdinalIgnoreCase)) return "memory_crucial";
        if (iconKey.StartsWith("storage_samsung", StringComparison.OrdinalIgnoreCase)) return "storage_samsung";
        if (iconKey.StartsWith("storage_wd", StringComparison.OrdinalIgnoreCase)) return "storage_wd";
        if (iconKey.StartsWith("storage_seagate", StringComparison.OrdinalIgnoreCase)) return "storage_seagate";
        if (iconKey.StartsWith("storage_crucial", StringComparison.OrdinalIgnoreCase)) return "storage_crucial";
        if (iconKey.StartsWith("storage_kingston", StringComparison.OrdinalIgnoreCase)) return "storage_kingston";
        if (iconKey.StartsWith("storage_corsair", StringComparison.OrdinalIgnoreCase)) return "storage_corsair";
        if (iconKey.StartsWith("network_intel", StringComparison.OrdinalIgnoreCase)) return "network_intel";
        if (iconKey.StartsWith("network_realtek", StringComparison.OrdinalIgnoreCase)) return "network_realtek";
        if (iconKey.StartsWith("network_killer", StringComparison.OrdinalIgnoreCase)) return "network_killer";
        if (iconKey.StartsWith("usb_", StringComparison.OrdinalIgnoreCase)) return "usb_default";
        if (iconKey.StartsWith("display_", StringComparison.OrdinalIgnoreCase)) return "display_default";

        return null;
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

    private static IconMapDocument LoadIconMap()
    {
        var document = new IconMapDocument();

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "HardwareDb", "HardwareIconMap.json");
            if (!File.Exists(path))
            {
                SetDefaultFallbacks(document);
                return document;
            }

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("fallbacks", out var fallbacksEl))
            {
                foreach (var prop in fallbacksEl.EnumerateObject())
                {
                    document.Fallbacks[prop.Name] = prop.Value.GetString() ?? $"{prop.Name}_default";
                }
            }

            if (root.TryGetProperty("mappings", out var mappingsEl))
            {
                foreach (var categoryProp in mappingsEl.EnumerateObject())
                {
                    var category = categoryProp.Name;
                    var mappings = new List<IconMapping>();

                    foreach (var mappingEl in categoryProp.Value.EnumerateArray())
                    {
                        var mapping = new IconMapping();
                        if (mappingEl.TryGetProperty("match", out var matchEl))
                        {
                            mapping.Match = matchEl.GetString() ?? string.Empty;
                        }
                        if (mappingEl.TryGetProperty("icon", out var iconEl))
                        {
                            mapping.Icon = iconEl.GetString() ?? string.Empty;
                        }
                        if (mappingEl.TryGetProperty("priority", out var priorityEl))
                        {
                            mapping.Priority = priorityEl.GetInt32();
                        }
                        mappings.Add(mapping);
                    }

                    document.Mappings[category] = mappings;
                }
            }
        }
        catch
        {
            SetDefaultFallbacks(document);
        }

        return document;
    }

    private static void SetDefaultFallbacks(IconMapDocument document)
    {
        document.Fallbacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cpu"] = "cpu_default",
            ["gpu"] = "gpu_default",
            ["motherboard"] = "motherboard_default",
            ["memory"] = "memory_default",
            ["storage"] = "storage_default",
            ["chipset"] = "motherboard_default",
            ["network"] = "network_default",
            ["usb"] = "usb_default",
            ["display"] = "display_default",
            ["os"] = "os/windows10"
        };
    }

    public static string GetCpuIcon(string? cpuName)
    {
        var iconKey = ResolveIconKey("cpu", cpuName);
        return FromIconKey(iconKey, "Assets/Icons/cpu_default.png");
    }

    public static string GetGpuIcon(string? gpuName)
    {
        var iconKey = ResolveIconKey("gpu", gpuName);
        return FromIconKey(iconKey, "Assets/Icons/gpu_default.png");
    }

    public static string GetStorageIcon(string? storageName)
    {
        var iconKey = ResolveIconKey("storage", storageName);
        return FromIconKey(iconKey, "Assets/Icons/storage_default.png");
    }

    public static string GetMemoryIcon(string? memoryName)
    {
        var iconKey = ResolveIconKey("memory", memoryName);
        return FromIconKey(iconKey, "Assets/Icons/memory_default.png");
    }

    public static string GetMotherboardIcon(string? boardName)
    {
        var iconKey = ResolveIconKey("motherboard", boardName);
        return FromIconKey(iconKey, "Assets/Icons/motherboard_default.png");
    }

    public static string GetChipsetIcon(string? chipset)
    {
        var iconKey = ResolveIconKey("chipset", chipset);
        return FromIconKey(iconKey, "Assets/Icons/chipset_default.png");
    }

    public static string GetNetworkIcon(string? networkName)
    {
        var iconKey = ResolveIconKey("network", networkName);
        return FromIconKey(iconKey, "Assets/Icons/network_default.png");
    }

    public static string GetUsbIcon(string? usbName)
    {
        var iconKey = ResolveIconKey("usb", usbName);
        return FromIconKey(iconKey, "Assets/Icons/usb_default.png");
    }

    public static string GetDisplayIcon(string? displayName)
    {
        var iconKey = ResolveIconKey("display", displayName);
        return FromIconKey(iconKey, "Assets/Icons/display_default.png");
    }

    public static ImageSource ResolveOsIcon(string? normalizedOsName)
    {
        var iconKey = ResolveIconKey("os", normalizedOsName);
        return ResolveIcon(iconKey, "os/windows10");
    }

    public static string ResolveOsIconKey(string? normalizedOsName)
    {
        return ResolveIconKey("os", normalizedOsName);
    }
}
