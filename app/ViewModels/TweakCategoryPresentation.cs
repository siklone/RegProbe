using System;
using RegProbe.App.Utilities;
using RegProbe.Core;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands;
using RegProbe.Engine.Tweaks.Commands.Cleanup;

namespace RegProbe.App.ViewModels;

internal static class TweakCategoryPresentation
{
    public static string ExtractCategory(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return StringPool.Intern("Other");
        }

        // Plugins follow: plugin.<pluginId>.<tweakId>...
        // Group plugin tweaks by pluginId in the UI (e.g. DevTools).
        if (id.StartsWith("plugin.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = id.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                return StringPool.GetCategory(parts[1]);
            }
        }

        var dotIndex = id.IndexOf('.');
        if (dotIndex <= 0)
        {
            var hyphenIndex = id.IndexOf('-');
            if (hyphenIndex > 0)
            {
                var candidate = id[..hyphenIndex];
                if (IsKnownCategoryPrefix(candidate))
                {
                    return StringPool.GetCategory(candidate);
                }
            }

            return StringPool.Intern("Other");
        }

        var cat = id.Substring(0, dotIndex);
        return StringPool.GetCategory(cat);
    }

    public static string GetCategoryIcon(string category) => category.ToLowerInvariant() switch
    {
        "system" => "SYS",
        "security" => "SEC",
        "privacy" => "PRV",
        "network" => "NET",
        "visibility" => "UI",
        "audio" => "AUD",
        "peripheral" => "DEV",
        "power" => "PWR",
        "performance" => "PERF",
        "cleanup" => "CLN",
        "explorer" => "EXP",
        "notifications" => "NTF",
        "devtools" => "DEV",
        _ => "CFG"
    };

    public static string DetermineImpactAreaLabel(ITweak tweak)
    {
        var area = tweak switch
        {
            RegistryValueTweak or RegistryValueBatchTweak or RegistryValueSetTweak or RegistryValuePresetBatchTweak => "Registry",
            IChoiceTweak => "Preset",
            ServiceStartModeBatchTweak => "Service",
            ScheduledTaskBatchTweak => "Task",
            SettingsToggleTweak => "Settings",
            FileCleanupTweak or FileRenameTweak => "File",
            CommandTweak => "Command",
            CompositeTweak => "Composite",
            _ => "Other"
        };
        return StringPool.GetImpactArea(area);
    }

    private static bool IsKnownCategoryPrefix(string candidate) => candidate.ToLowerInvariant() switch
    {
        "system" or
        "security" or
        "privacy" or
        "network" or
        "visibility" or
        "audio" or
        "peripheral" or
        "power" or
        "performance" or
        "cleanup" or
        "explorer" or
        "notifications" or
        "devtools" => true,
        _ => false
    };
}
