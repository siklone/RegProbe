namespace RegProbe.App.ViewModels;

internal sealed class WinConfigCategoryCoverageMapper
{
    public IDictionary<string, int> Build(IEnumerable<TweakItemViewModel> tweaks)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in tweaks ?? Enumerable.Empty<TweakItemViewModel>())
        {
            var categoryId = MapLocalCategoryToWinConfigId(tweak.Category);
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                continue;
            }

            counts.TryGetValue(categoryId, out var current);
            counts[categoryId] = current + 1;
        }

        return counts;
    }

    private static string? MapLocalCategoryToWinConfigId(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        var normalized = category.Trim().ToLowerInvariant();

        if (normalized.Contains("network"))
            return "network";
        if (normalized.Contains("power"))
            return "power";
        if (normalized.Contains("privacy"))
            return "privacy";
        if (normalized.Contains("security"))
            return "security";
        if (normalized.Contains("system"))
            return "system";
        if (normalized.Contains("visibility") || normalized.Contains("display") || normalized.Contains("explorer"))
            return "visibility";
        if (normalized.Contains("peripheral") || normalized.Contains("input") || normalized.Contains("usb") || normalized.Contains("audio"))
            return "peripheral";
        if (normalized.Contains("nvidia") || normalized.Contains("graphics") || normalized.Contains("gpu"))
            return "nvidia";
        if (normalized.Contains("cleanup"))
            return "cleanup";
        if (normalized.Contains("policy"))
            return "policies";
        if (normalized.Contains("performance") || normalized.Contains("affinity"))
            return "affinities";
        if (normalized.Contains("misc"))
            return "misc";

        return null;
    }
}
