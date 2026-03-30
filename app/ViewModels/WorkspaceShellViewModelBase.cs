using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace RegProbe.App.ViewModels;

public abstract class WorkspaceShellViewModelBase : ViewModelBase, IDisposable
{
    protected WorkspaceShellViewModelBase(TweaksViewModel workspace)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
    }

    public TweaksViewModel Workspace { get; }

    public ObservableCollection<CategoryGroupViewModel> CategoryGroups => Workspace.CategoryGroups;

    public ICommand ClearCategorySelectionCommand => Workspace.ClearCategorySelectionCommand;

    public ICommand ResetFiltersCommand => Workspace.ResetFiltersCommand;

    public string WorkspaceCategoryHeader => Workspace.WorkspaceCategoryHeader;

    public string WorkspaceCategoryHint => Workspace.WorkspaceCategoryHint;

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

    public bool IsSettingsWorkspaceSelected => Workspace.IsSettingsWorkspaceSelected;

    public bool IsMaintenanceWorkspaceSelected => Workspace.IsMaintenanceWorkspaceSelected;

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

    public bool ShowClassC
    {
        get => Workspace.ShowClassC;
        set => Workspace.ShowClassC = value;
    }

    public bool ShowClassD
    {
        get => Workspace.ShowClassD;
        set => Workspace.ShowClassD = value;
    }

    public int FavoritesCount => Workspace.FavoritesCount;

    public bool ShowContributorEvidenceUi => Workspace.ShowContributorEvidenceUi;

    public string ToolbarSectionHint => Workspace.ToolbarSectionHint;

    public string FilterSummaryLabel => Workspace.FilterSummaryLabel;

    public string FilterSummary => Workspace.FilterSummary;

    public string InventorySummaryLabel => Workspace.InventorySummaryLabel;

    public string InventoryStatusMessage => Workspace.InventoryStatusMessage;

    public int TotalTweaksAvailable => Workspace.TotalTweaksAvailable;

    public bool HasVisibleTweaks => Workspace.HasVisibleTweaks;

    public string EmptyStateTitle => Workspace.EmptyStateTitle;

    public string EmptyStateDescription => Workspace.EmptyStateDescription;

    public string EmptyStateActionText => Workspace.EmptyStateActionText;

    public string ClearCategorySelectionText => Workspace.ClearCategorySelectionText;

    public bool CanClearCategorySelection => Workspace.CanClearCategorySelection;

    public void Dispose()
    {
        Workspace.PropertyChanged -= OnWorkspacePropertyChanged;
        OnDispose();
    }

    protected virtual void AfterWorkspacePropertyChanged(PropertyChangedEventArgs e)
    {
    }

    protected virtual void OnDispose()
    {
    }

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        OnPropertyChanged(e.PropertyName);
        AfterWorkspacePropertyChanged(e);
    }
}
