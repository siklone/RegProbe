using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class TweaksShellStateViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private string _statusFilter = string.Empty;
    private string _scopeFilter = string.Empty;
    private bool _showSafe = true;
    private bool _showAdvanced = true;
    private bool _showRisky = true;
    private bool _showFavoritesOnly;
    private bool _showClassA = true;
    private bool _showClassB = true;
    private bool _showClassC = true;
    private bool _showClassD = true;
    private string _selectedCategoryName = string.Empty;
    private ConfigurationWorkspaceKind _selectedWorkspace = ConfigurationWorkspaceKind.Settings;
    private bool _isFlatView;

    public IReadOnlyList<FilterOptionViewModel> StatusFilterOptions { get; } = new[]
    {
        new FilterOptionViewModel("STATUS", string.Empty),
        new FilterOptionViewModel("PROMOTED", "promoted"),
        new FilterOptionViewModel("HOLD/REVIEW", "hold"),
        new FilterOptionViewModel("APPLIED", "applied"),
        new FilterOptionViewModel("ROLLED BACK", "rolledback")
    };

    public IReadOnlyList<FilterOptionViewModel> ScopeFilterOptions { get; } = new[]
    {
        new FilterOptionViewModel("SCOPE", string.Empty),
        new FilterOptionViewModel("MACHINE", "machine"),
        new FilterOptionViewModel("USER", "user"),
        new FilterOptionViewModel("MIXED", "mixed")
    };

    public ConfigurationWorkspaceKind SelectedWorkspace
    {
        get => _selectedWorkspace;
        set
        {
            if (SetProperty(ref _selectedWorkspace, value))
            {
                RaiseWorkspacePropertiesChanged();
            }
        }
    }

    public bool IsFlatView
    {
        get => _isFlatView;
        set => SetProperty(ref _isFlatView, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                OnPropertyChanged(nameof(StatusFilterLabel));
                OnPropertyChanged(nameof(StatusFilterDisplayText));
                OnPropertyChanged(nameof(HasStatusFilter));
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public string ScopeFilter
    {
        get => _scopeFilter;
        set
        {
            if (SetProperty(ref _scopeFilter, value))
            {
                OnPropertyChanged(nameof(ScopeFilterDisplayText));
                OnPropertyChanged(nameof(HasScopeFilter));
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public string StatusFilterLabel => _statusFilter switch
    {
        "promoted" => "Promoted Cards",
        "hold" => "Review/Hold Cards",
        "applied" => "Applied Settings",
        "rolledback" => "Rolled Back Settings",
        _ => string.Empty
    };

    public string StatusFilterDisplayText => FindOptionLabel(StatusFilterOptions, _statusFilter, "STATUS");

    public string ScopeFilterDisplayText => FindOptionLabel(ScopeFilterOptions, _scopeFilter, "SCOPE");

    public bool HasStatusFilter => !string.IsNullOrEmpty(_statusFilter);

    public bool HasScopeFilter => !string.IsNullOrEmpty(_scopeFilter);

    public bool HasActiveFilters =>
        HasStatusFilter
        || HasScopeFilter
        || ShowFavoritesOnly
        || !ShowSafe
        || !ShowAdvanced
        || !ShowRisky
        || !ShowClassA
        || !ShowClassB
        || !ShowClassC
        || !ShowClassD
        || !string.IsNullOrWhiteSpace(SearchText);

    public bool ShowSafe
    {
        get => _showSafe;
        set
        {
            if (SetProperty(ref _showSafe, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set
        {
            if (SetProperty(ref _showAdvanced, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public bool ShowRisky
    {
        get => _showRisky;
        set
        {
            if (SetProperty(ref _showRisky, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            if (SetProperty(ref _showFavoritesOnly, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public bool ShowClassA
    {
        get => _showClassA;
        set
        {
            if (SetProperty(ref _showClassA, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public bool ShowClassB
    {
        get => _showClassB;
        set
        {
            if (SetProperty(ref _showClassB, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public bool ShowClassC
    {
        get => _showClassC;
        set
        {
            if (SetProperty(ref _showClassC, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public bool ShowClassD
    {
        get => _showClassD;
        set
        {
            if (SetProperty(ref _showClassD, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
            }
        }
    }

    public string SelectedCategoryName
    {
        get => _selectedCategoryName;
        set
        {
            if (SetProperty(ref _selectedCategoryName, value))
            {
                RaiseCategoryPropertiesChanged();
            }
        }
    }

    public bool IsSettingsWorkspaceSelected => SelectedWorkspace == ConfigurationWorkspaceKind.Settings;

    public bool IsMaintenanceWorkspaceSelected => SelectedWorkspace == ConfigurationWorkspaceKind.Maintenance;

    public string CurrentWorkspaceLabel => IsMaintenanceWorkspaceSelected ? "Recovery" : "Tweaks";

    public string CurrentWorkspaceDescription => IsMaintenanceWorkspaceSelected
        ? "One-off cleanup, reset, and recovery actions."
        : "Registry-backed settings and feature switches that remain in place until you change them.";

    public string WorkspaceCategoryHeader => "Categories";

    public string WorkspaceCategoryHint => string.Empty;

    public string AllItemsLabel => "All";

    public string SearchPlaceholder => IsMaintenanceWorkspaceSelected
        ? "Search recovery actions..."
        : "Search settings and features...";

    public string SecondaryPanelSearchPlaceholder => IsMaintenanceWorkspaceSelected
        ? "Search actions..."
        : "Search cards...";

    public string ToolbarSectionLabel => IsMaintenanceWorkspaceSelected ? "Recovery filters" : "Tweak filters";

    public string ToolbarSectionHint => IsMaintenanceWorkspaceSelected
        ? "Surface one-off cleanup and recovery actions fast."
        : "Narrow the list to the settings you actively manage.";

    public string CurrentWorkspaceModeNote => IsMaintenanceWorkspaceSelected
        ? "Run these when Windows needs intervention, then get out of the way."
        : "These settings stay in place until you choose a different default.";

    public string WorkspaceStatusHint => IsMaintenanceWorkspaceSelected
        ? "Use filters for a targeted repair path, a reset, or a one-off maintenance action."
        : "Search by behavior, narrow to one area, or keep favorites close for the settings you revisit most.";

    public string EmptyStateTitle => IsMaintenanceWorkspaceSelected
        ? "No recovery actions match"
        : "No tweaks match";

    public string EmptyStateDescription => IsMaintenanceWorkspaceSelected
        ? "Try a broader search or choose another category."
        : "Try a simpler search or pick a different area.";

    public string EmptyStateActionText => IsMaintenanceWorkspaceSelected
        ? "Show all recovery actions"
        : "Show all tweaks";

    public bool CanClearCategorySelection => !IsAllCategoriesSelected;

    public string ClearCategorySelectionText => IsMaintenanceWorkspaceSelected
        ? "Browse all recovery categories"
        : "Browse all tweak areas";

    public string FilterSummaryLabel => IsMaintenanceWorkspaceSelected ? "Recovery scope" : "Tweak scope";

    public string InventorySummaryLabel => IsMaintenanceWorkspaceSelected ? "Recovery status" : "Tweak status";

    public bool IsAllCategoriesSelected => string.IsNullOrWhiteSpace(_selectedCategoryName);

    public string SelectedCategoryLabel => IsAllCategoriesSelected ? AllItemsLabel : _selectedCategoryName;

    public string SelectedCategoryContext => IsAllCategoriesSelected
        ? (IsMaintenanceWorkspaceSelected ? "All recovery categories" : "All tweak areas")
        : SelectedCategoryLabel;

    public void ResetFilters()
    {
        SearchText = string.Empty;
        StatusFilter = string.Empty;
        ScopeFilter = string.Empty;
        ShowSafe = true;
        ShowAdvanced = true;
        ShowRisky = true;
        ShowFavoritesOnly = false;
        ShowClassA = true;
        ShowClassB = true;
        ShowClassC = true;
        ShowClassD = true;
        SelectedCategoryName = string.Empty;
    }

    public void CycleStatusFilter() => StatusFilter = NextOptionValue(StatusFilterOptions, StatusFilter);

    public void CycleScopeFilter() => ScopeFilter = NextOptionValue(ScopeFilterOptions, ScopeFilter);

    private void RaiseWorkspacePropertiesChanged()
    {
        OnPropertyChanged(nameof(IsSettingsWorkspaceSelected));
        OnPropertyChanged(nameof(IsMaintenanceWorkspaceSelected));
        OnPropertyChanged(nameof(CurrentWorkspaceLabel));
        OnPropertyChanged(nameof(CurrentWorkspaceDescription));
        OnPropertyChanged(nameof(WorkspaceCategoryHeader));
        OnPropertyChanged(nameof(WorkspaceCategoryHint));
        OnPropertyChanged(nameof(AllItemsLabel));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnPropertyChanged(nameof(ToolbarSectionLabel));
        OnPropertyChanged(nameof(ToolbarSectionHint));
        OnPropertyChanged(nameof(CurrentWorkspaceModeNote));
        OnPropertyChanged(nameof(WorkspaceStatusHint));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
        OnPropertyChanged(nameof(EmptyStateActionText));
        OnPropertyChanged(nameof(ClearCategorySelectionText));
        OnPropertyChanged(nameof(FilterSummaryLabel));
        OnPropertyChanged(nameof(InventorySummaryLabel));
        OnPropertyChanged(nameof(SelectedCategoryLabel));
        OnPropertyChanged(nameof(SelectedCategoryContext));
    }

    private void RaiseCategoryPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsAllCategoriesSelected));
        OnPropertyChanged(nameof(SelectedCategoryLabel));
        OnPropertyChanged(nameof(CanClearCategorySelection));
        OnPropertyChanged(nameof(SelectedCategoryContext));
    }

    private static string FindOptionLabel(
        IReadOnlyList<FilterOptionViewModel> options,
        string value,
        string fallback)
    {
        return options.FirstOrDefault(option =>
            string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))?.Label ?? fallback;
    }

    private static string NextOptionValue(
        IReadOnlyList<FilterOptionViewModel> options,
        string currentValue)
    {
        if (options.Count == 0)
        {
            return string.Empty;
        }

        var currentIndex = -1;
        for (var index = 0; index < options.Count; index++)
        {
            if (string.Equals(options[index].Value, currentValue, StringComparison.OrdinalIgnoreCase))
            {
                currentIndex = index;
                break;
            }
        }

        return options[(currentIndex + 1) % options.Count].Value;
    }
}
