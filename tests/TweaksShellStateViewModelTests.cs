using RegProbe.App.Services;
using RegProbe.App.ViewModels;

namespace RegProbe.Tests;

public sealed class TweaksShellStateViewModelTests
{
    [Fact]
    public void SettingsWorkspace_UsesTweaksLanguage()
    {
        var viewModel = new TweaksShellStateViewModel
        {
            SelectedWorkspace = ConfigurationWorkspaceKind.Settings
        };

        Assert.Equal("Tweaks", viewModel.CurrentWorkspaceLabel);
        Assert.Equal("Tweak filters", viewModel.ToolbarSectionLabel);
        Assert.Equal("Tweak scope", viewModel.FilterSummaryLabel);
        Assert.Equal("Tweak status", viewModel.InventorySummaryLabel);
        Assert.Equal("No tweaks match", viewModel.EmptyStateTitle);
        Assert.Equal("Show all tweaks", viewModel.EmptyStateActionText);
        Assert.Equal("Browse all tweak areas", viewModel.ClearCategorySelectionText);
        Assert.Equal("All tweak areas", viewModel.SelectedCategoryContext);
    }

    [Fact]
    public void MaintenanceWorkspace_UsesRecoveryLanguage()
    {
        var viewModel = new TweaksShellStateViewModel
        {
            SelectedWorkspace = ConfigurationWorkspaceKind.Maintenance
        };

        Assert.Equal("Recovery", viewModel.CurrentWorkspaceLabel);
        Assert.Equal("Recovery filters", viewModel.ToolbarSectionLabel);
        Assert.Equal("Recovery scope", viewModel.FilterSummaryLabel);
        Assert.Equal("Recovery status", viewModel.InventorySummaryLabel);
        Assert.Equal("No recovery actions match", viewModel.EmptyStateTitle);
        Assert.Equal("Show all recovery actions", viewModel.EmptyStateActionText);
        Assert.Equal("Browse all recovery categories", viewModel.ClearCategorySelectionText);
        Assert.Equal("All recovery categories", viewModel.SelectedCategoryContext);
    }

    [Fact]
    public void AboutWorkspace_UsesDiagnosticsTitle()
    {
        var coordinator = new AboutWorkspaceCoordinator();

        Assert.Equal("About & Diagnostics", coordinator.Title);
    }

    [Fact]
    public void ResetFilters_ClearsScopeFilter()
    {
        var viewModel = new TweaksShellStateViewModel
        {
            ScopeFilter = "machine"
        };

        viewModel.ResetFilters();

        Assert.Equal(string.Empty, viewModel.ScopeFilter);
    }
}
