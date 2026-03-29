using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class RepairsWorkspaceCoordinator
{
    private static readonly string[] FastCleanTweakIds =
    {
        "cleanup.temp-files",
        "cleanup.directx-shader-cache",
        "cleanup.thumbnail-cache",
        "cleanup.wer-files"
    };

    private readonly TweaksViewModel _workspace;

    public RepairsWorkspaceCoordinator(TweaksViewModel workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    public void ShowCleanupWorkspace()
    {
        _workspace.FocusMaintenanceWorkspace(ResolveCleanupCategoryName());
    }

    public async Task RunFastCleanAsync(CancellationToken ct = default)
    {
        ShowCleanupWorkspace();

        if (_workspace.IsBulkRunning)
        {
            return;
        }

        var fastCleanItems = _workspace.AllTweaks
            .Where(t => FastCleanTweakIds.Contains(t.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (fastCleanItems.Count == 0)
        {
            _workspace.SetBulkStatusFromRepairs("Fast Clean is not available right now.");
            return;
        }

        _workspace.ClearSelectionFromRepairs();
        foreach (var item in fastCleanItems)
        {
            item.IsSelected = true;
        }

        await _workspace.RunRepairsBatchAsync(
            "Fast Clean",
            () => fastCleanItems,
            async (item, token) =>
            {
                ct.ThrowIfCancellationRequested();
                await item.RunApplyAsync(token);
            });
    }

    private string ResolveCleanupCategoryName()
    {
        return _workspace.AllTweaks
            .Where(t => _workspace.GetWorkspaceKindForRepairs(t) == ConfigurationWorkspaceKind.Maintenance)
            .Select(t => t.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(category =>
                category.Contains("cleanup", StringComparison.OrdinalIgnoreCase)
                || category.Contains("clean", StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
    }
}
