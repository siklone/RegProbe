using System.Collections.Generic;
namespace RegProbe.App.ViewModels;

public sealed partial class TweaksViewModel
{
    public IReadOnlyList<FilterOptionViewModel> StatusFilterOptions => _shellState.StatusFilterOptions;

    public IReadOnlyList<FilterOptionViewModel> ScopeFilterOptions => _shellState.ScopeFilterOptions;

    public string StatusFilter
    {
        get => _shellState.StatusFilter;
        set => _shellState.StatusFilter = value;
    }

    public string StatusFilterLabel => _shellState.StatusFilterLabel;

    public string StatusFilterDisplayText => _shellState.StatusFilterDisplayText;

    public bool HasStatusFilter => _shellState.HasStatusFilter;

    public string ScopeFilter
    {
        get => _shellState.ScopeFilter;
        set => _shellState.ScopeFilter = value;
    }

    public string ScopeFilterDisplayText => _shellState.ScopeFilterDisplayText;
}
