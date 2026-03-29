using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceActionCoordinator : IDisposable
{
    private readonly IBusyService _busyService;
    private CancellationTokenSource? _bulkCts;

    public WorkspaceActionCoordinator(IBusyService busyService)
    {
        _busyService = busyService ?? throw new ArgumentNullException(nameof(busyService));
    }

    public bool CanRunInspectable(
        bool isBulkRunning,
        IEnumerable<TweakItemViewModel> allTweaks,
        Func<List<TweakItemViewModel>> getTweaks)
    {
        if (isBulkRunning || allTweaks.Any(item => item.IsRunning))
        {
            return false;
        }

        return getTweaks().Count > 0;
    }

    public bool CanRunMutating(
        bool isBulkRunning,
        IEnumerable<TweakItemViewModel> allTweaks,
        Func<List<TweakItemViewModel>> getTweaks)
    {
        if (isBulkRunning || allTweaks.Any(item => item.IsRunning))
        {
            return false;
        }

        return getTweaks().Any(item => item.IsEvidenceClassActionable);
    }

    public async Task RunBulkAsync(
        string label,
        Func<List<TweakItemViewModel>> getTweaks,
        Func<TweakItemViewModel, CancellationToken, Task> runner,
        Func<bool> getIsBulkRunning,
        Action<bool> setIsBulkRunning,
        Action<int> setBulkProgressCurrent,
        Action<int> setBulkProgressTotal,
        Action<string> setBulkStatusMessage,
        Action notifyBulkProgressChanged,
        Action refreshSummaryStats)
    {
        if (getIsBulkRunning())
        {
            return;
        }

        StartBulkCancellation();
        setIsBulkRunning(true);
        var actionLabel = label.ToLowerInvariant();

        try
        {
            var items = getTweaks();
            if (items.Count == 0)
            {
                setBulkStatusMessage($"No tweaks to {actionLabel}.");
                return;
            }

            setBulkProgressTotal(items.Count);
            setBulkProgressCurrent(0);
            setBulkStatusMessage($"{label} in progress ({items.Count} items)...");
            using var busy = _busyService.Busy($"{label} in progress ({items.Count} items)...");

            notifyBulkProgressChanged();

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                _bulkCts?.Token.ThrowIfCancellationRequested();
                setBulkStatusMessage($"Running {actionLabel} on {item.Name}...");
                await runner(item, _bulkCts?.Token ?? CancellationToken.None);

                setBulkProgressCurrent(index + 1);
                notifyBulkProgressChanged();
            }

            setBulkStatusMessage($"Bulk {actionLabel} completed ({items.Count} tweaks).");
        }
        catch (OperationCanceledException)
        {
            setBulkStatusMessage($"Bulk {actionLabel} cancelled.");
        }
        catch (Exception ex)
        {
            setBulkStatusMessage($"Bulk {actionLabel} failed: {ex.Message}");
        }
        finally
        {
            setIsBulkRunning(false);
            setBulkProgressCurrent(0);
            setBulkProgressTotal(0);
            notifyBulkProgressChanged();
            ClearBulkCancellation();
            refreshSummaryStats();
        }
    }

    public void SelectAll(IEnumerable<TweakItemViewModel> visibleTweaks)
    {
        foreach (var tweak in visibleTweaks)
        {
            tweak.IsSelected = true;
        }
    }

    public void DeselectAll(IEnumerable<TweakItemViewModel> allTweaks)
    {
        foreach (var tweak in allTweaks)
        {
            tweak.IsSelected = false;
        }
    }

    public int CountSelected(IEnumerable<TweakItemViewModel> allTweaks)
    {
        return allTweaks.Count(t => t.IsSelected);
    }

    public void CancelBulk(bool isBulkRunning, Action<string> setBulkStatusMessage)
    {
        if (!isBulkRunning || _bulkCts is null)
        {
            return;
        }

        _bulkCts.Cancel();
        setBulkStatusMessage("Bulk cancellation requested.");
    }

    public void Dispose()
    {
        ClearBulkCancellation();
    }

    private void StartBulkCancellation()
    {
        ClearBulkCancellation();
        _bulkCts = new CancellationTokenSource();
    }

    private void ClearBulkCancellation()
    {
        _bulkCts?.Dispose();
        _bulkCts = null;
    }
}
