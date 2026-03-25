using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OpenTraceProject.App.HardwareDb;

public static class HardwareIconSourceDb
{
    private sealed record SourceDocument(HashSet<string> Keys, Dictionary<string, string> CategoryFallbacks);

    private static readonly Lazy<SourceDocument> Document = new(Load);

    private static readonly Dictionary<string, string> LegacyAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cpu_generic"] = "cpu_default",
        ["gpu_generic"] = "gpu_default",
        ["chipset_generic"] = "chipset_default",
        ["memory_generic"] = "memory_default",
        ["storage_generic"] = "storage_default",
        ["network_generic"] = "network_default",
        ["usb_generic"] = "usb_default",
        ["display_generic"] = "display_default",
        ["audio_generic"] = "audio_default",
        ["mb_default"] = "motherboard_default",
        ["os_windows10"] = "os/windows10",
        ["os_windows11"] = "os/windows11"
    };

    private static readonly Dictionary<string, string> BuiltInFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cpu"] = "cpu_default",
        ["gpu"] = "gpu_default",
        ["motherboard"] = "motherboard_default",
        ["chipset"] = "chipset_default",
        ["memory"] = "memory_default",
        ["storage"] = "storage_default",
        ["network"] = "network_default",
        ["usb"] = "usb_default",
        ["display"] = "display_default",
        ["os"] = "os/windows10",
        ["audio"] = "audio_default"
    };

    public static bool ContainsKey(string? iconKey)
    {
        var normalized = StripPathAndExtension(iconKey);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return Document.Value.Keys.Contains(normalized);
    }

    public static string ResolveFallbackKey(string category, string? requestedFallback = null)
    {
        var normalizedCategory = NormalizeCategory(category);

        var requested = StripPathAndExtension(requestedFallback);
        if (TryResolveKnownKey(requested, out var requestedResolved))
        {
            return requestedResolved;
        }

        if (Document.Value.CategoryFallbacks.TryGetValue(normalizedCategory, out var dbFallback) &&
            TryResolveKnownKey(dbFallback, out var dbFallbackResolved))
        {
            return dbFallbackResolved;
        }

        if (BuiltInFallbacks.TryGetValue(normalizedCategory, out var builtInFallback) &&
            TryResolveKnownKey(builtInFallback, out var builtInFallbackResolved))
        {
            return builtInFallbackResolved;
        }

        return TryResolveKnownKey("cpu_default", out var cpuFallback)
            ? cpuFallback
            : "cpu_default";
    }

    public static string NormalizeKey(string? iconKey, string category, string fallbackKey)
    {
        var normalizedCategory = NormalizeCategory(category);
        var fallback = ResolveFallbackKey(normalizedCategory, fallbackKey);
        var candidate = StripPathAndExtension(iconKey);
        if (TryResolveKnownKey(candidate, out var resolvedCandidate))
        {
            return resolvedCandidate;
        }

        return fallback;
    }

    public static string NormalizeKeyForFallback(string? iconKey, string fallbackKey)
    {
        var category = InferCategoryFromFallback(fallbackKey);
        return NormalizeKey(iconKey, category, fallbackKey);
    }

    public static string InferCategoryFromFallback(string? fallbackKey)
    {
        var key = StripPathAndExtension(fallbackKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            return "cpu";
        }

        var slashIndex = key.IndexOf('/');
        if (slashIndex > 0)
        {
            return key[..slashIndex].ToLowerInvariant();
        }

        if (key.StartsWith("mb_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("motherboard_", StringComparison.OrdinalIgnoreCase))
        {
            return "motherboard";
        }

        if (key.StartsWith("chipset_", StringComparison.OrdinalIgnoreCase))
        {
            return "chipset";
        }

        var underscoreIndex = key.IndexOf('_');
        if (underscoreIndex > 0)
        {
            return key[..underscoreIndex].ToLowerInvariant();
        }

        return "cpu";
    }

    public static string NormalizeCategory(string? category)
    {
        var value = (category ?? string.Empty).Trim().ToLowerInvariant();
        return value switch
        {
            "mainboard" => "motherboard",
            "board" => "motherboard",
            "ram" => "memory",
            "disk" => "storage",
            "nic" => "network",
            _ => value
        };
    }

    private static bool TryResolveKnownKey(string? candidate, out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var normalized = StripPathAndExtension(candidate);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (Document.Value.Keys.Contains(normalized))
        {
            resolved = normalized;
            return true;
        }

        if (LegacyAlias.TryGetValue(normalized, out var alias) && Document.Value.Keys.Contains(alias))
        {
            resolved = alias;
            return true;
        }

        if (normalized.StartsWith("os_", StringComparison.OrdinalIgnoreCase))
        {
            var slashVariant = $"os/{normalized[3..]}";
            if (Document.Value.Keys.Contains(slashVariant))
            {
                resolved = slashVariant;
                return true;
            }
        }

        return false;
    }

    private static string StripPathAndExtension(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var normalized = key.Trim()
            .Replace('\\', '/')
            .Replace("pack://application:,,,/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Assets/Icons/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Resources/Icons/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase);

        return normalized.Trim();
    }

    private static SourceDocument Load()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fallbacks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in BuiltInFallbacks)
        {
            fallbacks[kvp.Key] = kvp.Value;
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "HardwareDb", "HardwareIconSourceDb.json");
            if (!File.Exists(path))
            {
                return new SourceDocument(keys, fallbacks);
            }

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("categoryFallbacks", out var fallbackEl) &&
                fallbackEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in fallbackEl.EnumerateObject())
                {
                    var category = NormalizeCategory(property.Name);
                    var key = StripPathAndExtension(property.Value.GetString());
                    if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    fallbacks[category] = key;
                    keys.Add(key);
                }
            }

            if (!doc.RootElement.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
            {
                return new SourceDocument(keys, fallbacks);
            }

            foreach (var item in itemsEl.EnumerateArray())
            {
                if (!item.TryGetProperty("key", out var keyEl))
                {
                    continue;
                }

                var rawKey = keyEl.GetString();
                var key = StripPathAndExtension(rawKey);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                keys.Add(key);

                string category = string.Empty;
                if (item.TryGetProperty("category", out var categoryEl))
                {
                    category = NormalizeCategory(categoryEl.GetString());
                }
                if (string.IsNullOrWhiteSpace(category))
                {
                    category = InferCategoryFromFallback(key);
                }

                if (string.IsNullOrWhiteSpace(category))
                {
                    continue;
                }

                if (key.Equals($"{category}_default", StringComparison.OrdinalIgnoreCase) ||
                    (category.Equals("os", StringComparison.OrdinalIgnoreCase) && key.Equals("os/windows10", StringComparison.OrdinalIgnoreCase)))
                {
                    fallbacks[category] = key;
                }
            }
        }
        catch
        {
            // DB load is best-effort; callers still have built-in fallbacks.
        }

        return new SourceDocument(keys, fallbacks);
    }
}
