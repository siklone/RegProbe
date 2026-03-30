using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceDetectionCoordinator
{
    public async Task DetectAllTweaksAsync(
        IEnumerable<TweakItemViewModel> tweaks,
        IEnumerable<CategoryGroupViewModel> categoryGroups,
        WorkspaceInventoryCoordinator inventoryCoordinator,
        WorkspaceHealthCoordinator healthCoordinator,
        CancellationToken ct = default,
        bool forceRedetect = false,
        bool skipElevationPrompts = false,
        bool skipExpensiveOperations = false)
    {
        ArgumentNullException.ThrowIfNull(tweaks);
        ArgumentNullException.ThrowIfNull(categoryGroups);
        ArgumentNullException.ThrowIfNull(inventoryCoordinator);
        ArgumentNullException.ThrowIfNull(healthCoordinator);

        IEnumerable<TweakItemViewModel> candidates = tweaks;

        if (skipExpensiveOperations)
        {
            candidates = candidates.Where(t => t.IsScanFriendly);
        }

        if (skipElevationPrompts)
        {
            candidates = candidates.Where(t => !t.WillPromptForDetect);
        }

        var tweaksToScan = candidates.ToList();
        var perTweakTimeout = TimeSpan.FromSeconds(6);

        foreach (var tweak in tweaksToScan)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (forceRedetect || tweak.AppliedStatus == TweakAppliedStatus.Unknown)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(perTweakTimeout);
                    await tweak.DetectStatusAsync(timeoutCts.Token);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                // Timed out. Leave the previous state and continue.
            }
            catch
            {
                // Silently ignore detection failures for individual tweaks.
            }
        }

        var tweakList = tweaks.ToList();
        inventoryCoordinator.SaveSnapshot(tweakList);
        inventoryCoordinator.UpdateStatusMessage(tweakList);
        healthCoordinator.Refresh(tweakList);

        if (!skipElevationPrompts && !skipExpensiveOperations)
        {
            foreach (var category in categoryGroups)
            {
                category.MarkDetected();
            }
        }
    }

    public Task RefreshInventoryInBackgroundAsync(
        IEnumerable<TweakItemViewModel> tweaks,
        IEnumerable<CategoryGroupViewModel> categoryGroups,
        WorkspaceInventoryCoordinator inventoryCoordinator,
        WorkspaceHealthCoordinator healthCoordinator,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tweaks);
        ArgumentNullException.ThrowIfNull(categoryGroups);
        ArgumentNullException.ThrowIfNull(inventoryCoordinator);
        ArgumentNullException.ThrowIfNull(healthCoordinator);

        var tweakList = tweaks.ToList();
        var categoryList = categoryGroups.ToList();

        return inventoryCoordinator.RunBackgroundRefreshAsync(
            tweakList,
            async token => await DetectAllTweaksAsync(
                tweakList,
                categoryList,
                inventoryCoordinator,
                healthCoordinator,
                ct: token,
                forceRedetect: true,
                skipElevationPrompts: true,
                skipExpensiveOperations: false),
            ct);
    }
}
