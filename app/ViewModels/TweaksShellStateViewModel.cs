using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class TweaksShellStateViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private string _statusFilter = string.Empty;
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
        set => SetProperty(ref _searchText, value);
    }

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                OnPropertyChanged(nameof(StatusFilterLabel));
                OnPropertyChanged(nameof(HasStatusFilter));
            }
        }
    }

    public string StatusFilterLabel => _statusFilter switch
    {
        "applied" => "Applied Settings",
        "rolledback" => "Rolled Back Settings",
        _ => string.Empty
    };

    public bool HasStatusFilter => !string.IsNullOrEmpty(_statusFilter);

    public bool ShowSafe
    {
        get => _showSafe;
        set => SetProperty(ref _showSafe, value);
    }

    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set => SetProperty(ref _showAdvanced, value);
    }

    public bool ShowRisky
    {
        get => _showRisky;
        set => SetProperty(ref _showRisky, value);
    }

    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set => SetProperty(ref _showFavoritesOnly, value);
    }

    public bool ShowClassA
    {
        get => _showClassA;
        set => SetProperty(ref _showClassA, value);
    }

    public bool ShowClassB
    {
        get => _showClassB;
        set => SetProperty(ref _showClassB, value);
    }

    public bool ShowClassC
    {
        get => _showClassC;
        set => SetProperty(ref _showClassC, value);
    }

    public bool ShowClassD
    {
        get => _showClassD;
        set => SetProperty(ref _showClassD, value);
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

    public string CurrentWorkspaceLabel => IsMaintenanceWorkspaceSelected ? "Repairs" : "Configuration";

    public string CurrentWorkspaceDescription => IsMaintenanceWorkspaceSelected
        ? "One-off cleanup, reset, and recovery actions."
        : "Registry-backed settings and feature switches that remain in place until you change them.";

    public string WorkspaceCategoryHeader => IsMaintenanceWorkspaceSelected ? "Categories" : "Configuration Areas";

    public string AllItemsLabel => "All";

    public string SearchPlaceholder => IsMaintenanceWorkspaceSelected
        ? "Search cleanup, reset, and recovery actions..."
        : "Search settings, features, and switches...";

    public string ToolbarSectionLabel => IsMaintenanceWorkspaceSelected ? "Repair filters" : "Configuration filters";

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
        ? "No repairs match"
        : "No settings match";

    public string EmptyStateDescription => IsMaintenanceWorkspaceSelected
        ? "Try a broader search or choose another category."
        : "Try a simpler search or pick a different area.";

    public string EmptyStateActionText => IsMaintenanceWorkspaceSelected
        ? "Show all repairs"
        : "Show all settings";

    public bool CanClearCategorySelection => !IsAllCategoriesSelected;

    public string ClearCategorySelectionText => IsMaintenanceWorkspaceSelected
        ? "Browse all repair categories"
        : "Browse all areas";

    public string FilterSummaryLabel => IsMaintenanceWorkspaceSelected ? "Repair scope" : "Configuration scope";

    public string InventorySummaryLabel => IsMaintenanceWorkspaceSelected ? "Repair status" : "Configuration status";

    public bool IsAllCategoriesSelected => string.IsNullOrWhiteSpace(_selectedCategoryName);

    public string SelectedCategoryLabel => IsAllCategoriesSelected ? AllItemsLabel : _selectedCategoryName;

    public string SelectedCategoryContext => IsAllCategoriesSelected
        ? (IsMaintenanceWorkspaceSelected ? "All repair categories" : "All configuration areas")
        : SelectedCategoryLabel;

    public void ResetFilters()
    {
        SearchText = string.Empty;
        StatusFilter = string.Empty;
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

    private void RaiseWorkspacePropertiesChanged()
    {
        OnPropertyChanged(nameof(IsSettingsWorkspaceSelected));
        OnPropertyChanged(nameof(IsMaintenanceWorkspaceSelected));
        OnPropertyChanged(nameof(CurrentWorkspaceLabel));
        OnPropertyChanged(nameof(CurrentWorkspaceDescription));
        OnPropertyChanged(nameof(WorkspaceCategoryHeader));
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
}
