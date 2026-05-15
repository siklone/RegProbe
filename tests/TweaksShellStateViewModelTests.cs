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

    [Fact]
    public void FilterOptions_ExposeStableLabelsAndValues_ForDropdownBinding()
    {
        var viewModel = new TweaksShellStateViewModel();

        Assert.Collection(
            viewModel.StatusFilterOptions,
            option =>
            {
                Assert.Equal("STATUS", option.Label);
                Assert.Equal(string.Empty, option.Value);
            },
            option =>
            {
                Assert.Equal("APPLIED", option.Label);
                Assert.Equal("applied", option.Value);
            },
            option =>
            {
                Assert.Equal("ROLLED BACK", option.Label);
                Assert.Equal("rolledback", option.Value);
            });
        Assert.Contains(viewModel.ScopeFilterOptions, option => option.Label == "MACHINE" && option.Value == "machine");
        Assert.Contains(viewModel.ScopeFilterOptions, option => option.Label == "USER" && option.Value == "user");
        Assert.Contains(viewModel.ScopeFilterOptions, option => option.Label == "MIXED" && option.Value == "mixed");
    }

    [Fact]
    public void HasActiveFilters_IncludesStatusAndScope()
    {
        var viewModel = new TweaksShellStateViewModel();
        Assert.False(viewModel.HasActiveFilters);

        viewModel.StatusFilter = "applied";
        Assert.True(viewModel.HasActiveFilters);

        viewModel.StatusFilter = string.Empty;
        viewModel.ScopeFilter = "machine";
        Assert.True(viewModel.HasActiveFilters);
    }
}
