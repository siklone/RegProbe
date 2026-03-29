using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using RegProbe.Core.Commands;

namespace RegProbe.App.ViewModels;

public sealed class RepairsShellViewModel : ViewModelBase
{
    private readonly RepairsWorkspaceCoordinator _repairsCoordinator;

    public RepairsShellViewModel(TweaksViewModel workspace)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _repairsCoordinator = new RepairsWorkspaceCoordinator(Workspace);
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        ShowCleanupWorkspaceCommand = new RelayCommand(_ => _repairsCoordinator.ShowCleanupWorkspace());
        RunFastCleanCommand = new RelayCommand(async _ => await _repairsCoordinator.RunFastCleanAsync(), _ => !Workspace.IsBulkRunning);
    }

    public TweaksViewModel Workspace { get; }

    public ICommand ShowCleanupWorkspaceCommand { get; }

    public ICommand RunFastCleanCommand { get; }

    public ObservableCollection<CategoryGroupViewModel> CategoryGroups => Workspace.CategoryGroups;

    public ICollectionView RepairsRowsView => Workspace.RepairsRowsView;

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

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(TweaksViewModel.IsBulkRunning) && RunFastCleanCommand is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }
}
