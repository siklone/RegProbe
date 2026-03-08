using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.App.Services;

public sealed class PolicyReferenceSourceItem
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string EffectSummary { get; init; } = string.Empty;
    public string RegistryPath { get; init; } = string.Empty;
    public TweakRiskLevel Risk { get; init; }
    public bool HasDetectedState { get; init; }
    public bool IsApplied { get; init; }
}

public sealed class PolicyReferenceEntry
{
    public string ComponentName { get; init; } = string.Empty;
    public string PrimaryCategory { get; init; } = string.Empty;
    public string ScopeLabel { get; init; } = string.Empty;
    public string SettingCountLabel { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string RiskLabel { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ExampleSummary { get; init; } = string.Empty;
    public string SearchFragment { get; init; } = string.Empty;
    public string ReadPathLabel { get; init; } = string.Empty;
    public string ScopeDetail { get; init; } = string.Empty;
    public string ExpectedBehavior { get; init; } = string.Empty;
    public string RelatedSettingsLabel { get; init; } = string.Empty;
}

public sealed class PolicyReferenceCatalog
{
    public IReadOnlyList<PolicyReferenceEntry> Entries { get; init; } = Array.Empty<PolicyReferenceEntry>();
    public int PolicyBackedSettingCount { get; init; }
    public int ComponentCount { get; init; }
    public int MachineScopedSettingCount { get; init; }
    public int UserScopedSettingCount { get; init; }
}

public sealed class PolicyReferenceCatalogBuilder
{
    private const string PoliciesMarker = "\\Software\\Policies\\";

    public PolicyReferenceCatalog Build(IEnumerable<PolicyReferenceSourceItem> sourceItems)
    {
        var policyItems = (sourceItems ?? Enumerable.Empty<PolicyReferenceSourceItem>())
            .Where(static item => !string.IsNullOrWhiteSpace(item.RegistryPath))
            .Where(item => IsPolicyBacked(item.RegistryPath))
            .ToList();

        var entries = policyItems
            .GroupBy(item => BuildComponentKey(item.RegistryPath), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(BuildEntry)
            .OrderByDescending(static entry => ExtractLeadingInteger(entry.SettingCountLabel))
            .ThenBy(static entry => entry.ComponentName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PolicyReferenceCatalog
        {
            Entries = entries,
            PolicyBackedSettingCount = policyItems.Count,
            ComponentCount = entries.Length,
            MachineScopedSettingCount = policyItems.Count(static item => item.RegistryPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase)),
            UserScopedSettingCount = policyItems.Count(static item => item.RegistryPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
        };
    }

    public static bool IsPolicyBacked(string? registryPath)
        => !string.IsNullOrWhiteSpace(registryPath) &&
           registryPath.Contains(PoliciesMarker, StringComparison.OrdinalIgnoreCase);

    private static PolicyReferenceEntry BuildEntry(IGrouping<string, PolicyReferenceSourceItem> group)
    {
        var items = group.ToList();
        var searchFragment = BuildSearchFragment(items[0].RegistryPath);
        var componentName = BuildDisplayName(group.Key);
        var primaryCategory = items
            .GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.Key)
            .FirstOrDefault() ?? "System";
        var detectedCount = items.Count(static item => item.HasDetectedState);
        var appliedCount = items.Count(static item => item.HasDetectedState && item.IsApplied);
        var safeCount = items.Count(static item => item.Risk == TweakRiskLevel.Safe);
        var advancedCount = items.Count(static item => item.Risk == TweakRiskLevel.Advanced);
        var riskyCount = items.Count(static item => item.Risk == TweakRiskLevel.Risky);

        return new PolicyReferenceEntry
        {
            ComponentName = componentName,
            PrimaryCategory = primaryCategory,
            ScopeLabel = BuildScopeLabel(items),
            SettingCountLabel = $"{items.Count} settings",
            StatusLabel = detectedCount == 0
                ? "Run Detect to read current status"
                : $"{appliedCount} / {detectedCount} currently matched",
            RiskLabel = BuildRiskLabel(safeCount, advancedCount, riskyCount),
            Description = $"Policy-backed {primaryCategory.ToLowerInvariant()} settings for {componentName}.",
            ExampleSummary = BuildExampleSummary(items),
            SearchFragment = searchFragment,
            ReadPathLabel = BuildReadPathLabel(items, searchFragment),
            ScopeDetail = BuildScopeDetail(items),
            ExpectedBehavior = BuildExpectedBehavior(items),
            RelatedSettingsLabel = BuildRelatedSettingsLabel(items)
        };
    }

    private static string BuildScopeLabel(IReadOnlyCollection<PolicyReferenceSourceItem> items)
    {
        var hasMachine = items.Any(static item => item.RegistryPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase));
        var hasUser = items.Any(static item => item.RegistryPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase));

        if (hasMachine && hasUser)
        {
            return "Machine + user";
        }

        if (hasMachine)
        {
            return "Machine";
        }

        if (hasUser)
        {
            return "User";
        }

        return "Policy path";
    }

    private static string BuildRiskLabel(int safeCount, int advancedCount, int riskyCount)
    {
        var parts = new List<string>();
        if (safeCount > 0)
        {
            parts.Add($"{safeCount} safe");
        }

        if (advancedCount > 0)
        {
            parts.Add($"{advancedCount} advanced");
        }

        if (riskyCount > 0)
        {
            parts.Add($"{riskyCount} risky");
        }

        return parts.Count == 0 ? "No risk data" : string.Join(" · ", parts);
    }

    private static string BuildExampleSummary(IReadOnlyCollection<PolicyReferenceSourceItem> items)
    {
        var examples = items
            .Select(static item => item.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        if (examples.Length == 0)
        {
            return "Examples will appear after the next refresh.";
        }

        return $"Examples: {string.Join(" · ", examples)}";
    }

    private static string BuildReadPathLabel(IReadOnlyCollection<PolicyReferenceSourceItem> items, string searchFragment)
    {
        var hasMachine = items.Any(static item => item.RegistryPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase));
        var hasUser = items.Any(static item => item.RegistryPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase));
        var hiveLabel = hasMachine && hasUser
            ? "HKLM/HKCU"
            : hasMachine
                ? "HKLM"
                : "HKCU";

        return string.IsNullOrWhiteSpace(searchFragment)
            ? $"{hiveLabel}\\Software\\Policies"
            : $"{hiveLabel}\\Software\\Policies\\{searchFragment}";
    }

    private static string BuildScopeDetail(IReadOnlyCollection<PolicyReferenceSourceItem> items)
    {
        var hasMachine = items.Any(static item => item.RegistryPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase));
        var hasUser = items.Any(static item => item.RegistryPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase));

        if (hasMachine && hasUser)
        {
            return "Supports both machine-wide and per-user policy paths.";
        }

        if (hasMachine)
        {
            return "Machine policy stored under HKLM and applied across the PC.";
        }

        return "User policy stored under HKCU and applied to the current account.";
    }

    private static string BuildExpectedBehavior(IReadOnlyCollection<PolicyReferenceSourceItem> items)
    {
        var effects = items
            .Select(static item => item.EffectSummary)
            .Where(static effect => !string.IsNullOrWhiteSpace(effect))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();

        if (effects.Length == 0)
        {
            return "These settings usually lock or preconfigure a Windows behavior through policy-backed registry paths.";
        }

        return Truncate(string.Join(" ", effects), 180);
    }

    private static string BuildRelatedSettingsLabel(IReadOnlyCollection<PolicyReferenceSourceItem> items)
    {
        var names = items
            .Select(static item => item.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
        {
            return "Related settings will appear after the next refresh.";
        }

        var preview = names.Take(3).ToArray();
        return names.Length > preview.Length
            ? $"{string.Join(" · ", preview)} +{names.Length - preview.Length} more"
            : string.Join(" · ", preview);
    }

    private static string BuildComponentKey(string registryPath)
    {
        var keyPath = ExtractPolicyKeyPath(registryPath);
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return string.Empty;
        }

        var segments = keyPath.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return string.Empty;
        }

        if (segments.Length >= 3 &&
            string.Equals(segments[0], "Microsoft", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(segments[1], "Windows", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join("\\", segments.Take(3));
        }

        if (segments.Length >= 2)
        {
            return string.Join("\\", segments.Take(2));
        }

        return segments[0];
    }

    private static string BuildSearchFragment(string registryPath)
    {
        var keyPath = ExtractPolicyKeyPath(registryPath);
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return string.Empty;
        }

        var segments = keyPath.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return string.Empty;
        }

        if (segments.Length >= 3 &&
            string.Equals(segments[0], "Microsoft", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(segments[1], "Windows", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join("\\", segments.Take(3));
        }

        if (segments.Length >= 2)
        {
            return string.Join("\\", segments.Take(2));
        }

        return segments[0];
    }

    private static string ExtractPolicyKeyPath(string registryPath)
    {
        var markerIndex = registryPath.IndexOf(PoliciesMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return string.Empty;
        }

        var afterMarker = registryPath[(markerIndex + PoliciesMarker.Length)..];
        var lastSeparator = afterMarker.LastIndexOf('\\');
        return lastSeparator <= 0
            ? afterMarker
            : afterMarker[..lastSeparator];
    }

    private static string BuildDisplayName(string componentKey)
    {
        var segments = componentKey.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return "Windows Policy";
        }

        if (segments.Length >= 3 &&
            string.Equals(segments[0], "Microsoft", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(segments[1], "Windows", StringComparison.OrdinalIgnoreCase))
        {
            return $"Windows {HumanizeToken(segments[2])}";
        }

        if (segments.Length >= 2 &&
            string.Equals(segments[0], "Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return $"Microsoft {HumanizeToken(segments[1])}";
        }

        return string.Join(" ", segments.Select(HumanizeToken));
    }

    private static string HumanizeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var spaced = Regex.Replace(token.Replace("-", " ").Replace("_", " "), "([a-z0-9])([A-Z])", "$1 $2");
        var words = spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" ", words.Select(TitleWord));
    }

    private static string TitleWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        if (word.All(static ch => !char.IsLetter(ch) || char.IsUpper(ch)) || word.Any(char.IsDigit))
        {
            return word;
        }

        return word.Length == 1
            ? word.ToUpperInvariant()
            : char.ToUpperInvariant(word[0]) + word[1..];
    }

    private static int ExtractLeadingInteger(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var digits = new string(text.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : 0;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }
}
