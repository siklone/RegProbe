using System;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class ConfigurationWorkspaceCoordinator
{
    private readonly TweaksViewModel _workspace;

    public ConfigurationWorkspaceCoordinator(TweaksViewModel workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public void ShowConfigurationWorkspace()
    {
        _workspace.SelectedWorkspace = ConfigurationWorkspaceKind.Settings;
    }

    public void ShowAppliedOnly()
    {
        ShowConfigurationWorkspace();
        _workspace.StatusFilter = "applied";
    }

    public void ShowRolledBackOnly()
    {
        ShowConfigurationWorkspace();
        _workspace.StatusFilter = "rolledback";
    }

    public void ClearFilters()
    {
        ShowConfigurationWorkspace();
        _workspace.SearchText = string.Empty;
        _workspace.StatusFilter = string.Empty;
        _workspace.ShowFavoritesOnly = false;
    }

    public void OpenPolicyReferenceEntry(PolicyReferenceEntry entry)
    {
        if (entry is null)
        {
            return;
        }

        _workspace.SelectedMainTabIndex = 0;
        ShowConfigurationWorkspace();
        _workspace.SelectedCategoryName = string.Empty;
        _workspace.StatusFilter = string.Empty;
        _workspace.SearchText = entry.SearchFragment;
    }
}
