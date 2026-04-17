using System;
using System.Linq;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceBrowseCoordinator : IDisposable
{
    private readonly TweaksShellStateViewModel _shellState;
    private readonly TweaksPresentationStateViewModel _presentationState;
    private readonly WorkspaceSearchDebouncer _searchDebouncer = new();
    private readonly WorkspaceFilterEvaluator _filterEvaluator;
    private readonly WorkspaceCategoryGroupBuilder _categoryGroupBuilder;

    public WorkspaceBrowseCoordinator(
        TweaksShellStateViewModel shellState,
        TweaksPresentationStateViewModel presentationState,
        bool showContributorEvidenceUi)
    {
        _shellState = shellState ?? throw new ArgumentNullException(nameof(shellState));
        _presentationState = presentationState ?? throw new ArgumentNullException(nameof(presentationState));
        _filterEvaluator = new WorkspaceFilterEvaluator(_shellState);
        _categoryGroupBuilder = new WorkspaceCategoryGroupBuilder(_shellState, _filterEvaluator);
    }

    public ConfigurationWorkspaceKind GetWorkspaceKind(TweakItemViewModel tweak)
        => _filterEvaluator.GetWorkspaceKind(tweak);

    public bool CurrentWorkspaceContainsCategory(IEnumerable<TweakItemViewModel> tweaks, string categoryName)
        => _filterEvaluator.CurrentWorkspaceContainsCategory(tweaks, categoryName);

    public void TriggerSearchUpdate(Action refreshFilteredViews)
        => _searchDebouncer.Trigger(refreshFilteredViews);

    public bool FilterTweak(TweakItemViewModel item)
        => _filterEvaluator.FilterTweak(item);

    public bool FilterRepair(RepairsItemViewModel item)
        => _filterEvaluator.FilterRepair(item);

    public void RefreshPresentation(
        IEnumerable<TweakItemViewModel> tweaks,
        int totalCount,
        int visibleCount,
        bool rebuildCategoryGroups,
        Action clearSelectedCategory)
    {
        ArgumentNullException.ThrowIfNull(tweaks);
        ArgumentNullException.ThrowIfNull(clearSelectedCategory);

        var noun = _shellState.IsMaintenanceWorkspaceSelected ? "recovery actions" : "tweaks";
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
        _searchDebouncer.Dispose();
    }

    private void RebuildCategoryGroups(IEnumerable<TweakItemViewModel> tweaks, Action clearSelectedCategory)
    {
        if (!(System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true))
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => RebuildCategoryGroups(tweaks, clearSelectedCategory));
            return;
        }

        var orderedGroups = _categoryGroupBuilder.Build(tweaks);
        _presentationState.ReplaceCategoryGroups(orderedGroups);

        if (!string.IsNullOrWhiteSpace(_shellState.SelectedCategoryName)
            && !_presentationState.CategoryGroups.Any(g => string.Equals(g.CategoryName, _shellState.SelectedCategoryName, StringComparison.OrdinalIgnoreCase)))
        {
            clearSelectedCategory();
        }
    }
}
