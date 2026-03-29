using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using RegProbe.Core.Commands;

namespace RegProbe.App.ViewModels;

public sealed class ConfigurationShellViewModel : ViewModelBase
{
    private readonly ConfigurationWorkspaceCoordinator _configurationCoordinator;

    public ConfigurationShellViewModel(TweaksViewModel workspace)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _configurationCoordinator = new ConfigurationWorkspaceCoordinator(Workspace);
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        ShowConfigurationWorkspaceCommand = new RelayCommand(_ => _configurationCoordinator.ShowConfigurationWorkspace());
        ShowAppliedOnlyCommand = new RelayCommand(_ => _configurationCoordinator.ShowAppliedOnly());
        ShowRolledBackOnlyCommand = new RelayCommand(_ => _configurationCoordinator.ShowRolledBackOnly());
    }

    public TweaksViewModel Workspace { get; }

    public ICommand ShowConfigurationWorkspaceCommand { get; }

    public ICommand ShowAppliedOnlyCommand { get; }

    public ICommand ShowRolledBackOnlyCommand { get; }

    public ObservableCollection<CategoryGroupViewModel> CategoryGroups => Workspace.CategoryGroups;

    public ICollectionView TweaksView => Workspace.TweaksView;

    public ICommand ClearCategorySelectionCommand => Workspace.ClearCategorySelectionCommand;

    public ICommand ResetFiltersCommand => Workspace.ResetFiltersCommand;

    public string WorkspaceCategoryHeader => Workspace.WorkspaceCategoryHeader;

    public string SelectedCategoryLabel => Workspace.SelectedCategoryLabel;

    public string SelectedCategoryContext => Workspace.SelectedCategoryContext;

    public bool IsAllCategoriesSelected => Workspace.IsAllCategoriesSelected;

    public string AllItemsLabel => Workspace.AllItemsLabel;

    public int CurrentWorkspaceItemCount => Workspace.CurrentWorkspaceItemCount;

    public string SelectedCategoryName
    {
        get => Workspace.SelectedCategoryName;
        set => Workspace.SelectedCategoryName = value;
    }

    public string CurrentWorkspaceLabel => Workspace.CurrentWorkspaceLabel;

    public string CurrentWorkspaceDescription => Workspace.CurrentWorkspaceDescription;

    public string CurrentWorkspaceModeNote => Workspace.CurrentWorkspaceModeNote;

    public string WorkspaceStatusHint => Workspace.WorkspaceStatusHint;

    public string ToolbarSectionLabel => Workspace.ToolbarSectionLabel;

    public string SearchText
    {
        get => Workspace.SearchText;
        set => Workspace.SearchText = value;
    }

    public string SearchPlaceholder => Workspace.SearchPlaceholder;

    public bool ShowSafe
    {
        get => Workspace.ShowSafe;
        set => Workspace.ShowSafe = value;
    }

    public bool ShowAdvanced
    {
        get => Workspace.ShowAdvanced;
        set => Workspace.ShowAdvanced = value;
    }

    public bool ShowRisky
    {
        get => Workspace.ShowRisky;
        set => Workspace.ShowRisky = value;
    }

    public bool ShowFavoritesOnly
    {
        get => Workspace.ShowFavoritesOnly;
        set => Workspace.ShowFavoritesOnly = value;
    }

    public bool ShowClassA
    {
        get => Workspace.ShowClassA;
        set => Workspace.ShowClassA = value;
    }

    public bool ShowClassB
    {
        get => Workspace.ShowClassB;
        set => Workspace.ShowClassB = value;
    }

    public int FavoritesCount => Workspace.FavoritesCount;

    public bool ShowContributorEvidenceUi => Workspace.ShowContributorEvidenceUi;

    public string ToolbarSectionHint => Workspace.ToolbarSectionHint;

    public string FilterSummaryLabel => Workspace.FilterSummaryLabel;

    public string FilterSummary => Workspace.FilterSummary;

    public string InventorySummaryLabel => Workspace.InventorySummaryLabel;

    public string InventoryStatusMessage => Workspace.InventoryStatusMessage;

    public bool HasVisibleTweaks => Workspace.HasVisibleTweaks;

    public string EmptyStateTitle => Workspace.EmptyStateTitle;

    public string EmptyStateDescription => Workspace.EmptyStateDescription;

    public string EmptyStateActionText => Workspace.EmptyStateActionText;

    public string ClearCategorySelectionText => Workspace.ClearCategorySelectionText;

    public bool CanClearCategorySelection => Workspace.CanClearCategorySelection;

    public void ShowConfigurationWorkspace()
    {
        _configurationCoordinator.ShowConfigurationWorkspace();
    }

    public void ClearFilters()
    {
        _configurationCoordinator.ClearFilters();
    }

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        OnPropertyChanged(e.PropertyName);
    }
}
