using System;
using System.ComponentModel;
using System.Windows.Input;
using RegProbe.Core.Commands;

namespace RegProbe.App.ViewModels;

public sealed class RepairsShellViewModel : WorkspaceShellViewModelBase
{
    private readonly RepairsWorkspaceCoordinator _repairsCoordinator;

    public RepairsShellViewModel(TweaksViewModel workspace)
        : base(workspace)
    {
        _repairsCoordinator = new RepairsWorkspaceCoordinator(Workspace);
        ShowCleanupWorkspaceCommand = new RelayCommand(_ => _repairsCoordinator.ShowCleanupWorkspace());
        RunFastCleanCommand = new RelayCommand(async _ => await _repairsCoordinator.RunFastCleanAsync(), _ => !Workspace.IsBulkRunning);
    }

    public ICommand ShowCleanupWorkspaceCommand { get; }

    public ICommand RunFastCleanCommand { get; }

    public ICollectionView RepairsRowsView => Workspace.RepairsRowsView;

    protected override void AfterWorkspacePropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TweaksViewModel.IsBulkRunning) && RunFastCleanCommand is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }
}
