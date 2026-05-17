using System;
using System.Linq;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

internal static class StartupNavigationCoordinator
{
    public static bool FocusTweakById(
        TweaksViewModel workspace,
        string tweakId,
        bool expandPlanDrawer)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (string.IsNullOrWhiteSpace(tweakId))
        {
            return false;
        }

        var target = workspace.Tweaks.FirstOrDefault(tweak =>
            tweak.IsEndUserAppCardAllowed
            && string.Equals(tweak.Id, tweakId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return false;
        }

        workspace.IsSecondaryPanelCollapsed = false;
        workspace.SelectedWorkspace = workspace.GetWorkspaceKindForRepairs(target);
        workspace.SelectedCategoryName = string.Empty;
        workspace.StatusFilter = string.Empty;
        workspace.ScopeFilter = string.Empty;
        workspace.SearchText = string.Empty;
        workspace.ShowFavoritesOnly = false;
        workspace.ShowSafe = true;
        workspace.ShowAdvanced = true;
        workspace.ShowRisky = true;
        workspace.ShowClassA = true;
        workspace.ShowClassB = true;
        workspace.ShowClassC = true;
        workspace.ShowClassD = true;

        target.IsDetailsExpanded = true;
        workspace.SelectedTweakPane.SelectedTweak = target;
        workspace.SelectedTweakPane.IsPlanDrawerExpanded = expandPlanDrawer;
        return true;
    }
}
