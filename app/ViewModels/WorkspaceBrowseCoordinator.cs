using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceBrowseCoordinator : IDisposable
{
    private readonly TweaksShellStateViewModel _shellState;
    private readonly TweaksPresentationStateViewModel _presentationState;
    private readonly ConfigurationWorkspaceClassifier _workspaceClassifier = new();
    private CancellationTokenSource? _searchCts;

    public WorkspaceBrowseCoordinator(
        TweaksShellStateViewModel shellState,
        TweaksPresentationStateViewModel presentationState,
        bool showContributorEvidenceUi)
    {
        _shellState = shellState ?? throw new ArgumentNullException(nameof(shellState));
        _presentationState = presentationState ?? throw new ArgumentNullException(nameof(presentationState));
    }

    public ConfigurationWorkspaceKind GetWorkspaceKind(TweakItemViewModel tweak)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        return _workspaceClassifier.Classify(tweak.Id, tweak.Category);
    }

    public bool CurrentWorkspaceContainsCategory(IEnumerable<TweakItemViewModel> tweaks, string categoryName)
    {
        ArgumentNullException.ThrowIfNull(tweaks);

        return tweaks.Any(t =>
            t.ShowInApp &&
            GetWorkspaceKind(t) == _shellState.SelectedWorkspace &&
            string.Equals(t.Category, categoryName, StringComparison.OrdinalIgnoreCase));
    }

    public void TriggerSearchUpdate(Action refreshFilteredViews)
    {
        ArgumentNullException.ThrowIfNull(refreshFilteredViews);

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(refreshFilteredViews);
            }
        }, token);
    }

    public bool FilterTweak(TweakItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return MatchesFilters(item, includeCategoryFilter: true);
    }

    public bool FilterRepair(RepairsItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return MatchesFilters(item.Source, includeCategoryFilter: true);
    }

    public void RefreshPresentation(
        IEnumerable<TweakItemViewModel> tweaks,
        int totalCount,
        int visibleCount,
        bool rebuildCategoryGroups,
        Action clearSelectedCategory)
    {
        ArgumentNullException.ThrowIfNull(tweaks);
        ArgumentNullException.ThrowIfNull(clearSelectedCategory);

        var noun = _shellState.IsMaintenanceWorkspaceSelected ? "repairs" : "settings";
        var scopedText = _shellState.IsAllCategoriesSelected
            ? $"{visibleCount} of {totalCount} {noun}"
            : $"{visibleCount} of {totalCount} {noun} in {_shellState.SelectedCategoryLabel}";
        var filterSuffix = string.IsNullOrWhiteSpace(_shellState.SearchText) && !_shellState.ShowFavoritesOnly
            ? string.Empty
            : " filtered";

        _presentationState.SetFilterSummary($"{scopedText}{filterSuffix}", visibleCount > 0);

        if (rebuildCategoryGroups)
        {
            RebuildCategoryGroups(tweaks, clearSelectedCategory);
        }
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }

    private bool MatchesFilters(TweakItemViewModel item, bool includeCategoryFilter)
    {
        if (!item.ShowInApp)
        {
            return false;
        }

        if (GetWorkspaceKind(item) != _shellState.SelectedWorkspace)
        {
            return false;
        }

        if (includeCategoryFilter && !_shellState.IsAllCategoriesSelected)
        {
            if (!string.Equals(item.Category, _shellState.SelectedCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(_shellState.StatusFilter))
        {
            if (_shellState.StatusFilter == "applied" && !item.IsApplied)
            {
                return false;
            }

            if (_shellState.StatusFilter == "rolledback" && !item.WasRolledBack)
            {
                return false;
            }
        }

        if (_shellState.ShowFavoritesOnly && !item.IsFavorite)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Safe && !_shellState.ShowSafe)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Advanced && !_shellState.ShowAdvanced)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Risky && !_shellState.ShowRisky)
        {
            return false;
        }

        if (item.EvidenceClassId == "A" && !_shellState.ShowClassA)
        {
            return false;
        }

        if (item.EvidenceClassId == "B" && !_shellState.ShowClassB)
        {
            return false;
        }

        if (item.EvidenceClassId == "C" && !_shellState.ShowClassC)
        {
            return false;
        }

        if (item.EvidenceClassId == "D" && !_shellState.ShowClassD)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_shellState.SearchText))
        {
            item.IsHighlighted = false;
            return true;
        }

        var matches = item.Name.Contains(_shellState.SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_shellState.SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(_shellState.SearchText, StringComparison.OrdinalIgnoreCase)
            || item.RegistryPath.Contains(_shellState.SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Risk.ToString().Contains(_shellState.SearchText, StringComparison.OrdinalIgnoreCase);

        item.IsHighlighted = matches;
        return matches;
    }

    private void RebuildCategoryGroups(IEnumerable<TweakItemViewModel> tweaks, Action clearSelectedCategory)
    {
        if (!(System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true))
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => RebuildCategoryGroups(tweaks, clearSelectedCategory));
            return;
        }

        static string FormatGroupName(string segment, string fallback)
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

        var categoryOrder = _shellState.IsMaintenanceWorkspaceSelected
            ? new[] { "Cleanup", "Network", "System", "Security", "Privacy", "Peripheral", "Power" }
            : new[] { "System", "Security", "Privacy", "Network", "Visibility", "Audio", "Peripheral", "Power", "Performance", "Cleanup", "Explorer", "Notifications", "Devtools" };
        var rootGroups = new Dictionary<string, CategoryGroupViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in tweaks.Where(t => MatchesFilters(t, includeCategoryFilter: false)))
        {
            var tweakId = tweak.Id ?? string.Empty;
            var parts = tweakId.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                continue;
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
        _presentationState.ReplaceCategoryGroups(orderedGroups);

        if (!string.IsNullOrWhiteSpace(_shellState.SelectedCategoryName)
            && !_presentationState.CategoryGroups.Any(g => string.Equals(g.CategoryName, _shellState.SelectedCategoryName, StringComparison.OrdinalIgnoreCase)))
        {
            clearSelectedCategory();
        }
    }
}
