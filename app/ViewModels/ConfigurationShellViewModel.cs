using System;
using System.ComponentModel;
using System.Windows.Input;
using RegProbe.Core.Commands;

namespace RegProbe.App.ViewModels;

public sealed class ConfigurationShellViewModel : WorkspaceShellViewModelBase
{
    private readonly ConfigurationWorkspaceCoordinator _configurationCoordinator;

    public ConfigurationShellViewModel(TweaksViewModel workspace)
        : base(workspace)
    {
        _configurationCoordinator = new ConfigurationWorkspaceCoordinator(Workspace);
        ShowConfigurationWorkspaceCommand = new RelayCommand(_ => _configurationCoordinator.ShowConfigurationWorkspace());
        ShowAppliedOnlyCommand = new RelayCommand(_ => _configurationCoordinator.ShowAppliedOnly());
        ShowRolledBackOnlyCommand = new RelayCommand(_ => _configurationCoordinator.ShowRolledBackOnly());
    }

    public ICommand ShowConfigurationWorkspaceCommand { get; }

    public ICommand ShowAppliedOnlyCommand { get; }

    public ICommand ShowRolledBackOnlyCommand { get; }

    public ICollectionView TweaksView => Workspace.TweaksView;

    public void ShowConfigurationWorkspace()
    {
        _configurationCoordinator.ShowConfigurationWorkspace();
    }

    public void ClearFilters()
    {
        _configurationCoordinator.ClearFilters();
    }
}
