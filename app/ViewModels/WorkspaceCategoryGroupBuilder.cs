namespace RegProbe.App.ViewModels;

internal sealed class WorkspaceCategoryGroupBuilder
{
    private readonly TweaksShellStateViewModel _shellState;
    private readonly WorkspaceFilterEvaluator _filterEvaluator;

    public WorkspaceCategoryGroupBuilder(
        TweaksShellStateViewModel shellState,
        WorkspaceFilterEvaluator filterEvaluator)
    {
        _shellState = shellState ?? throw new ArgumentNullException(nameof(shellState));
        _filterEvaluator = filterEvaluator ?? throw new ArgumentNullException(nameof(filterEvaluator));
    }

    public IReadOnlyList<CategoryGroupViewModel> Build(IEnumerable<TweakItemViewModel> tweaks)
    {
        var categoryOrder = _shellState.IsMaintenanceWorkspaceSelected
            ? new[] { "Cleanup", "Network", "System", "Security", "Privacy", "Peripheral", "Power" }
            : new[] { "System", "Security", "Privacy", "Network", "Visibility", "Audio", "Peripheral", "Power", "Performance", "Cleanup", "Explorer", "Notifications", "Devtools" };
        var rootGroups = new Dictionary<string, CategoryGroupViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in tweaks.Where(t => _filterEvaluator.MatchesFilters(t, includeCategoryFilter: false)))
        {
            AddTweak(rootGroups, tweak);
        }

        return OrderGroups(rootGroups, categoryOrder);
    }

    private static void AddTweak(Dictionary<string, CategoryGroupViewModel> rootGroups, TweakItemViewModel tweak)
    {
        var tweakId = tweak.Id ?? string.Empty;
        var parts = tweakId.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var rootCatName = !string.IsNullOrWhiteSpace(tweak.Category) &&
                          !string.Equals(tweak.Category, "Other", StringComparison.OrdinalIgnoreCase)
            ? tweak.Category
            : FormatGroupName(parts[0], "Other");

        if (!rootGroups.TryGetValue(rootCatName, out var currentGroup))
        {
            currentGroup = new CategoryGroupViewModel(rootCatName, tweak.CategoryIcon)
            {
                IsDense = rootCatName.Equals("Visibility", StringComparison.OrdinalIgnoreCase)
            };
            rootGroups[rootCatName] = currentGroup;
        }

        var parent = currentGroup;
        var subgroupStartIndex = tweakId.StartsWith("plugin.", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        for (var i = subgroupStartIndex; i < parts.Length - 1; i++)
        {
            var subName = FormatGroupName(parts[i], "Other");
            var subGroup = parent.SubGroups.FirstOrDefault(g => g.CategoryName == subName);
            if (subGroup == null)
            {
                subGroup = new CategoryGroupViewModel(subName, "--")
                {
                    IsNested = true,
                    IsExpanded = true,
                    Parent = parent
                };
                parent.SubGroups.Add(subGroup);
            }

            parent = subGroup;
        }

        parent.AddTweak(tweak);
    }

    private static IReadOnlyList<CategoryGroupViewModel> OrderGroups(
        Dictionary<string, CategoryGroupViewModel> rootGroups,
        IEnumerable<string> categoryOrder)
    {
        var orderedGroups = new List<CategoryGroupViewModel>();
        foreach (var categoryName in categoryOrder)
        {
            if (rootGroups.TryGetValue(categoryName, out var group))
            {
                orderedGroups.Add(group);
                rootGroups.Remove(categoryName);
            }
        }

        orderedGroups.AddRange(rootGroups.Values.OrderBy(x => x.CategoryName));
        return orderedGroups;
    }

    private static string FormatGroupName(string segment, string fallback)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return fallback;
        }

        segment = segment.Trim();
        return segment.Length == 1
            ? segment.ToUpperInvariant()
            : char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant();
    }
}
