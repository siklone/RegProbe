using System;

namespace RegProbe.App.ViewModels;

internal static class TweakInventoryPresentation
{
    public static string BuildInventoryFreshnessText(
        DateTimeOffset? lastDetectedAtUtc,
        bool isStateFromCache)
    {
        if (!lastDetectedAtUtc.HasValue)
        {
            return "Not scanned yet";
        }

        var source = isStateFromCache ? "Cached" : "Live";
        return $"{source} {BuildAgeText(lastDetectedAtUtc.Value)}";
    }

    public static string BuildConfigurationInventoryFreshnessText(
        DateTimeOffset? lastDetectedAtUtc,
        bool isStateFromCache)
    {
        if (!lastDetectedAtUtc.HasValue)
        {
            return "Not checked yet";
        }

        var ageText = BuildAgeText(lastDetectedAtUtc.Value);
        return isStateFromCache
            ? $"Checked from cache {ageText}"
            : $"Checked live {ageText}";
    }

    private static string BuildAgeText(DateTimeOffset lastDetectedAtUtc)
    {
        var elapsed = DateTimeOffset.UtcNow - lastDetectedAtUtc;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (elapsed.TotalMinutes < 1)
        {
            return $"{Math.Max(1, (int)elapsed.TotalSeconds)}s ago";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"{(int)elapsed.TotalMinutes}m ago";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"{(int)elapsed.TotalHours}h ago";
        }

        return $"{(int)elapsed.TotalDays}d ago";
    }
}
