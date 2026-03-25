using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using OpenTraceProject.App.HardwareDb.Models;

namespace OpenTraceProject.App.HardwareDb;

public enum HardwareIconResolutionSource
{
    ExplicitKey = 0,
    DatabaseModel = 1,
    RuleMap = 2,
    Fallback = 3
}

public sealed record HardwareIconResolutionResult(
    HardwareType HardwareType,
    string Category,
    string RequestedName,
    string IconKey,
    string FallbackKey,
    HardwareIconResolutionSource Source,
    HardwareMatchKind MatchKind,
    HardwareModelBase? MatchedModel = null)
{
    public bool UsesFallback => string.Equals(IconKey, FallbackKey, StringComparison.OrdinalIgnoreCase);

    public string SourceLabel => Source switch
    {
        HardwareIconResolutionSource.ExplicitKey => "Explicit key",
        HardwareIconResolutionSource.DatabaseModel => "Hardware database",
        HardwareIconResolutionSource.RuleMap => "Rule-based mapping",
        _ => "Category fallback"
    };

    public string MatchLabel => MatchKind switch
    {
        HardwareMatchKind.ExactName => "Exact product name",
        HardwareMatchKind.ExactAlias => "Exact alias",
        HardwareMatchKind.PartialName => "Partial product match",
        HardwareMatchKind.PartialAlias => "Partial alias match",
        HardwareMatchKind.ProvidedModel => "Provided database record",
        _ => UsesFallback ? "Default fallback" : "Category rule"
    };

    public string DatabaseMatchLabel => MatchKind switch
    {
        HardwareMatchKind.ExactName => "Exact model",
        HardwareMatchKind.ExactAlias => "Exact alias",
        HardwareMatchKind.PartialName => "Partial model",
        HardwareMatchKind.PartialAlias => "Partial alias",
        HardwareMatchKind.ProvidedModel => "Provided record",
        _ => "Verified"
    };
}

public static class HardwareIconService
{
    private static readonly Regex PciVendorRegex = new(@"VEN[_-]?(?<vendor>[0-9A-F]{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MotherboardChipsetRegex = new(@"\b(?<chipset>(?:z|x|b|a|h|w)\d{3,4}[a-z]?)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly ConcurrentDictionary<string, ImageSource> IconCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, string> DisplayVendorAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ACI"] = "ASUS",
        ["AUS"] = "ASUS",
        ["ACR"] = "Acer",
        ["AOC"] = "AOC",
        ["BNQ"] = "BenQ",
        ["DEL"] = "Dell",
        ["DELL"] = "Dell",
        ["GBT"] = "Gigabyte",
        ["GSM"] = "LG Electronics",
        ["LGD"] = "LG Electronics",
        ["MSI"] = "MSI",
        ["SAM"] = "Samsung",
        ["SEC"] = "Samsung",
        ["VSC"] = "ViewSonic",
        ["WAM"] = "Excalibur"
    };
    private static readonly IReadOnlyDictionary<string, string> GpuVendorAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["10DE"] = "NVIDIA",
        ["1002"] = "AMD Radeon",
        ["1022"] = "AMD Radeon",
        ["8086"] = "Intel Arc"
    };
    private static readonly IReadOnlyDictionary<string, string> MotherboardVendorAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["asus"] = "ASUS",
        ["asustek"] = "ASUS",
        ["asustek computer"] = "ASUS",
        ["micro star"] = "MSI",
        ["microstar"] = "MSI",
        ["micro star international"] = "MSI",
        ["gigabyte technology"] = "Gigabyte",
        ["asrock"] = "ASRock",
        ["biostar group"] = "Biostar",
        ["super micro"] = "Supermicro",
        ["supermicro"] = "Supermicro",
        ["evga corp"] = "EVGA"
    };
    private static readonly string[] FirmwareVendorNoise =
    {
        "american megatrends",
        "ami",
        "insyde",
        "phoenix",
        "uefi",
        "bios"
    };
    
    public static ImageSource Resolve(HardwareType type, string? modelName, HardwareModelBase? matchedModel = null)
    {
        return Resolve(ResolveResult(type, modelName, matchedModel));
    }

    public static ImageSource Resolve(HardwareIconResolutionResult resolution)
    {
        return GetOrLoadIcon(resolution.IconKey, resolution.HardwareType, resolution.FallbackKey);
    }

    public static ImageSource ResolveByIconKey(HardwareType type, string? iconKey, string? modelName = null, HardwareModelBase? matchedModel = null)
    {
        return Resolve(ResolveByIconKeyResult(type, iconKey, modelName, matchedModel));
    }

    public static string ResolveIconKey(HardwareType type, string? modelName, HardwareModelBase? matchedModel = null)
    {
        return ResolveResult(type, modelName, matchedModel).IconKey;
    }

    public static string BuildLookupSeed(params string?[] parts)
    {
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new System.Collections.Generic.List<string>();

        foreach (var part in parts)
        {
            var value = (part ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value) || !unique.Add(value))
            {
                continue;
            }

            ordered.Add(value);
        }

        return string.Join(" ", ordered);
    }

    public static string BuildDisplayLookupSeed(params string?[] parts)
    {
        var expanded = new System.Collections.Generic.List<string>();

        foreach (var part in parts)
        {
            var value = (part ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            expanded.Add(value);
            if (DisplayVendorAliases.TryGetValue(value, out var vendorName))
            {
                expanded.Add(vendorName);
            }
        }

        return BuildLookupSeed(expanded.ToArray());
    }

    public static string BuildGpuLookupSeed(params string?[] parts)
    {
        var expanded = new System.Collections.Generic.List<string>();

        foreach (var part in parts)
        {
            var value = (part ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            expanded.Add(value);
            if (GpuVendorAliases.TryGetValue(value, out var vendorName))
            {
                expanded.Add(vendorName);
            }

            var vendorMatch = PciVendorRegex.Match(value);
            if (vendorMatch.Success &&
                GpuVendorAliases.TryGetValue(vendorMatch.Groups["vendor"].Value, out var vendorFromPci))
            {
                expanded.Add(vendorFromPci);
            }
        }

        return BuildLookupSeed(expanded.ToArray());
    }

    public static string BuildMotherboardLookupSeed(params string?[] parts)
    {
        var expanded = new System.Collections.Generic.List<string>();

        foreach (var part in parts)
        {
            var value = (part ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (IsFirmwareVendorNoise(value))
            {
                continue;
            }

            expanded.Add(value);

            var normalized = HardwareNameNormalizer.Normalize(value);
            foreach (var alias in MotherboardVendorAliases)
            {
                if (normalized.Contains(alias.Key, StringComparison.OrdinalIgnoreCase))
                {
                    expanded.Add(alias.Value);
                }
            }
        }

        return BuildLookupSeed(expanded.ToArray());
    }

    private static bool IsFirmwareVendorNoise(string value)
    {
        var normalized = HardwareNameNormalizer.Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        foreach (var token in FirmwareVendorNoise)
        {
            if (normalized.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static HardwareIconResolutionResult ResolveResult(HardwareType type, string? modelName, HardwareModelBase? matchedModel = null)
    {
        var category = type.ToString().ToLowerInvariant();
        var fallback = HardwareIconResolverV2.GetFallbackKey(category);
        string? resolved = null;
        var effectiveModel = matchedModel;
        var matchKind = matchedModel != null ? HardwareMatchKind.ProvidedModel : HardwareMatchKind.None;
        var source = HardwareIconResolutionSource.Fallback;
        var requestedName = (modelName ?? string.Empty).Trim();

        // Layer 1: Database Match has an explicit IconKey
        if (effectiveModel != null && !string.IsNullOrWhiteSpace(effectiveModel.IconKey))
        {
            resolved = effectiveModel.IconKey;
            source = HardwareIconResolutionSource.DatabaseModel;
        }

        // Layer 2: Knowledge DB Match (if not passed in)
        if (resolved == null && effectiveModel == null && !string.IsNullOrWhiteSpace(modelName))
        {
            var dbMatch = MatchFromDbDetailed(type, modelName);
            if (ShouldUseDatabaseMatch(type, requestedName, dbMatch.Model, dbMatch.MatchKind))
            {
                effectiveModel = dbMatch.Model;
                matchKind = dbMatch.MatchKind;
                if (effectiveModel != null && !string.IsNullOrWhiteSpace(effectiveModel.IconKey))
                {
                    resolved = effectiveModel.IconKey;
                    source = HardwareIconResolutionSource.DatabaseModel;
                }
            }
        }

        // Layer 3: V2 mapping can refine a generic database icon into a more specific
        // family/product icon when the matched DB entry only carries a broad brand key.
        var ruleSeed = BuildRuleSeed(modelName, effectiveModel);
        var keyFromV2 = HardwareIconResolverV2.ResolveIconKey(category, ruleSeed);
        if (IsBetterCandidate(keyFromV2, resolved, category, fallback))
        {
            resolved = keyFromV2;
            source = HardwareIconResolutionSource.RuleMap;
        }

        var normalized = HardwareIconSourceDb.NormalizeKey(resolved, category, fallback);
        if (string.IsNullOrWhiteSpace(resolved) ||
            (string.Equals(normalized, fallback, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(resolved, fallback, StringComparison.OrdinalIgnoreCase)))
        {
            source = HardwareIconResolutionSource.Fallback;
        }

        return new HardwareIconResolutionResult(
            type,
            category,
            requestedName,
            normalized,
            fallback,
            source,
            matchKind,
            effectiveModel);
    }

    public static HardwareIconResolutionResult ResolveByIconKeyResult(HardwareType type, string? iconKey, string? modelName = null, HardwareModelBase? matchedModel = null)
    {
        var category = type.ToString().ToLowerInvariant();
        var fallback = HardwareIconResolverV2.GetFallbackKey(category);
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            return ResolveResult(type, modelName, matchedModel);
        }

        var normalized = HardwareIconSourceDb.NormalizeKey(iconKey, category, fallback);
        var source = matchedModel != null
            ? HardwareIconResolutionSource.DatabaseModel
            : HardwareIconResolutionSource.ExplicitKey;
        if (string.Equals(normalized, fallback, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(iconKey, fallback, StringComparison.OrdinalIgnoreCase))
        {
            source = HardwareIconResolutionSource.Fallback;
        }

        return new HardwareIconResolutionResult(
            type,
            category,
            (modelName ?? string.Empty).Trim(),
            normalized,
            fallback,
            source,
            matchedModel != null ? HardwareMatchKind.ProvidedModel : HardwareMatchKind.None,
            matchedModel);
    }

    private static string BuildRuleSeed(string? requestedName, HardwareModelBase? matchedModel)
    {
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName;
        }

        if (matchedModel == null)
        {
            return string.Empty;
        }

        return string.Join(" ", new[]
        {
            matchedModel.Brand,
            matchedModel.Series,
            matchedModel.ModelName,
            matchedModel.Generation,
            matchedModel.Codename
        }.Where(static part => !string.IsNullOrWhiteSpace(part)));
    }

    private static bool IsBetterCandidate(string? candidate, string? current, string category, string fallback)
    {
        var normalizedCandidate = HardwareIconSourceDb.NormalizeKey(candidate, category, fallback);
        if (string.Equals(normalizedCandidate, fallback, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedCurrent = HardwareIconSourceDb.NormalizeKey(current, category, fallback);
        return GetSpecificityScore(normalizedCandidate, fallback) > GetSpecificityScore(normalizedCurrent, fallback);
    }

    private static int GetSpecificityScore(string iconKey, string fallback)
    {
        if (string.Equals(iconKey, fallback, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var score = 100;
        var tokens = iconKey.Split(new[] { '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
        score += tokens.Length * 10;
        if (tokens.Any(static token => token.Any(char.IsDigit)))
        {
            score += 15;
        }

        if (iconKey.Contains("pro", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("ultra", StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        if (iconKey.Contains("gaming", StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (iconKey.Contains("rog", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("aorus", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("meg", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("mpg", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("mag", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("tuf", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("taichi", StringComparison.OrdinalIgnoreCase) ||
            iconKey.Contains("odyssey", StringComparison.OrdinalIgnoreCase))
        {
            score += 8;
        }

        return score;
    }

    private static bool ShouldUseDatabaseMatch(HardwareType type, string requestedName, HardwareModelBase? model, HardwareMatchKind matchKind)
    {
        if (model == null)
        {
            return false;
        }

        if (type != HardwareType.Motherboard)
        {
            return true;
        }

        return ShouldTrustMotherboardMatch(requestedName, model, matchKind);
    }

    private static bool ShouldTrustMotherboardMatch(string requestedName, HardwareModelBase model, HardwareMatchKind matchKind)
    {
        if (matchKind is HardwareMatchKind.ExactName or HardwareMatchKind.ExactAlias or HardwareMatchKind.ProvidedModel)
        {
            return true;
        }

        var requested = HardwareNameNormalizer.Normalize(requestedName);
        if (string.IsNullOrWhiteSpace(requested))
        {
            return false;
        }

        var requestedChipset = ExtractMotherboardChipset(requested);
        var modelChipset = ExtractMotherboardChipset(string.Join(" ",
            model.ModelName,
            model.Generation,
            model.Codename,
            model.NormalizedName));

        if (!string.IsNullOrWhiteSpace(requestedChipset))
        {
            if (string.IsNullOrWhiteSpace(modelChipset) ||
                !string.Equals(requestedChipset, modelChipset, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        var requestedCompact = requested.Replace(" ", string.Empty, StringComparison.Ordinal);
        var modelCompact = HardwareNameNormalizer.Normalize(model.ModelName).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(modelCompact) &&
            modelCompact.Contains("mpro4", StringComparison.OrdinalIgnoreCase) &&
            requestedCompact.Contains("pro4", StringComparison.OrdinalIgnoreCase) &&
            !requestedCompact.Contains("mpro4", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(requestedChipset) || requested.Length >= 12;
    }

    private static string ExtractMotherboardChipset(string value)
    {
        var normalized = HardwareNameNormalizer.Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var match = MotherboardChipsetRegex.Match(normalized);
        return match.Success ? match.Groups["chipset"].Value : string.Empty;
    }

    private static (HardwareModelBase? Model, HardwareMatchKind MatchKind) MatchFromDbDetailed(HardwareType type, string modelName)
    {
        return type switch
        {
            HardwareType.Cpu => Convert(HardwareKnowledgeDbService.Instance.MatchCpuDetailed(modelName)),
            HardwareType.Gpu => Convert(HardwareKnowledgeDbService.Instance.MatchGpuDetailed(modelName)),
            HardwareType.Motherboard => Convert(HardwareKnowledgeDbService.Instance.MatchMotherboardDetailed(modelName)),
            HardwareType.Display => Convert(HardwareKnowledgeDbService.Instance.MatchDisplayDetailed(modelName)),
            HardwareType.Memory => Convert(HardwareKnowledgeDbService.Instance.MatchMemoryDetailed(modelName)),
            HardwareType.Storage => Convert(HardwareKnowledgeDbService.Instance.MatchStorageDetailed(modelName)),
            HardwareType.Network => Convert(HardwareKnowledgeDbService.Instance.MatchNetworkAdapterDetailed(modelName)),
            HardwareType.Usb => Convert(HardwareKnowledgeDbService.Instance.MatchUsbDetailed(modelName)),
            _ => (null, HardwareMatchKind.None)
        };
    }

    private static (HardwareModelBase? Model, HardwareMatchKind MatchKind) Convert<TModel>(HardwareMatchResult<TModel> result)
        where TModel : HardwareModelBase
    {
        return (result.Model, result.MatchKind);
    }

    private static ImageSource GetOrLoadIcon(string iconKey, HardwareType fallbackType, string? explicitFallbackKey = null)
    {
        var category = fallbackType.ToString().ToLowerInvariant();
        var fallbackKey = explicitFallbackKey ?? HardwareIconResolverV2.GetFallbackKey(category);
        if (string.IsNullOrWhiteSpace(fallbackKey) || string.Equals(fallbackKey, "audio_default", StringComparison.OrdinalIgnoreCase))
        {
            fallbackKey = IconResolver.GetDefaultKey(fallbackType);
        }

        fallbackKey = HardwareIconSourceDb.NormalizeKey(fallbackKey, category, fallbackKey);
        var normalizedIconKey = HardwareIconSourceDb.NormalizeKey(iconKey, category, fallbackKey);
        var cacheKey = $"{normalizedIconKey}|{fallbackKey}";
        if (IconCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var image = HardwareIconResolverV2.ResolveIcon(normalizedIconKey, fallbackKey);
        if (image.CanFreeze && !image.IsFrozen)
        {
            image.Freeze();
        }

        IconCache[cacheKey] = image;
        return image;
    }
}
