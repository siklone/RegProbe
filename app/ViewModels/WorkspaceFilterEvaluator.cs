using RegProbe.App.Services;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

internal sealed class WorkspaceFilterEvaluator
{
    private readonly TweaksShellStateViewModel _shellState;
    private readonly ConfigurationWorkspaceClassifier _workspaceClassifier = new();

    public WorkspaceFilterEvaluator(TweaksShellStateViewModel shellState)
    {
        _shellState = shellState ?? throw new ArgumentNullException(nameof(shellState));
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
            t.IsEndUserAppCardAllowed &&
            GetWorkspaceKind(t) == _shellState.SelectedWorkspace &&
            string.Equals(t.Category, categoryName, StringComparison.OrdinalIgnoreCase));
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

    public bool MatchesFilters(TweakItemViewModel item, bool includeCategoryFilter)
    {
        if (!item.ShowInApp)
        {
            return false;
        }

        if (!item.IsEndUserAppCardAllowed)
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

        if (!string.IsNullOrWhiteSpace(_shellState.ScopeFilter)
            && !string.Equals(item.ScopeFilterKey, _shellState.ScopeFilter, StringComparison.OrdinalIgnoreCase))
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
}
