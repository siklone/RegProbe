using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Engine.Tweaks;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceInventoryCoordinator : ViewModelBase, IDisposable
{
    private readonly ITweakInventoryStateStore _inventoryStateStore;
    private CancellationTokenSource? _inventorySaveCts;
    private bool _isApplyingCachedInventory;
    private bool _isBackgroundRefreshRunning;
    private string _inventoryStatusMessage = "Status check not available yet.";

    public WorkspaceInventoryCoordinator(ITweakInventoryStateStore inventoryStateStore)
    {
        _inventoryStateStore = inventoryStateStore ?? throw new ArgumentNullException(nameof(inventoryStateStore));
    }

    public string InventoryStatusMessage
    {
        get => _inventoryStatusMessage;
        private set => SetProperty(ref _inventoryStatusMessage, value);
    }

    public void LoadCachedInventoryState(IEnumerable<TweakItemViewModel> tweaks)
    {
        var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
        var cachedStates = _inventoryStateStore.Load();
        if (cachedStates.Count == 0)
        {
            UpdateStatusMessage(tweakList);
            return;
        }

        _isApplyingCachedInventory = true;
        try
        {
            var appliedCount = 0;
            DateTimeOffset? latestTimestamp = null;

            foreach (var tweak in tweakList)
            {
                if (!cachedStates.TryGetValue(tweak.Id, out var cachedState))
                {
                    continue;
                }

                tweak.ApplyCachedInventoryState(cachedState);
                appliedCount++;

                if (cachedState.LastDetectedAtUtc.HasValue &&
                    (!latestTimestamp.HasValue || cachedState.LastDetectedAtUtc > latestTimestamp))
                {
                    latestTimestamp = cachedState.LastDetectedAtUtc;
                }
            }

            if (appliedCount > 0)
            {
                var latestText = latestTimestamp.HasValue
                    ? latestTimestamp.Value.ToLocalTime().ToString("HH:mm:ss")
                    : "unknown time";
                InventoryStatusMessage = $"Loaded last checked status for {appliedCount} settings (last: {latestText}).";
            }
        }
        finally
        {
            _isApplyingCachedInventory = false;
        }
    }

    public void UpdateStatusMessage(IEnumerable<TweakItemViewModel> tweaks)
    {
        var inventoryTweaks = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
        var total = inventoryTweaks.Count;
        var detected = inventoryTweaks.Count(t => t.AppliedStatus != TweakAppliedStatus.Unknown);
        var requiresPrompt = inventoryTweaks.Count(t => t.WillPromptForDetect);
        var suffix = _isBackgroundRefreshRunning ? " Refreshing in background..." : string.Empty;

        InventoryStatusMessage = requiresPrompt > 0
            ? $"Live status: {detected}/{total} checked. {requiresPrompt} need admin confirmation.{suffix}"
            : $"Live status: {detected}/{total} checked.{suffix}";
    }

    public async void ScheduleSnapshotSave(IEnumerable<TweakItemViewModel> tweaks)
    {
        if (_isApplyingCachedInventory)
        {
            return;
        }

        _inventorySaveCts?.Cancel();
        _inventorySaveCts?.Dispose();
        _inventorySaveCts = new CancellationTokenSource();
        var token = _inventorySaveCts.Token;

        try
        {
            await Task.Delay(400, token);
            token.ThrowIfCancellationRequested();
            SaveSnapshot(tweaks);
        }
        catch (OperationCanceledException)
        {
            // Debounced by a newer save request.
        }
        catch
        {
            // Cache persistence is best-effort.
        }
    }

    public void SaveSnapshot(IEnumerable<TweakItemViewModel> tweaks)
    {
        var snapshot = (tweaks ?? Enumerable.Empty<TweakItemViewModel>())
            .Select(t => t.ExportInventoryState())
            .ToList();

        _inventoryStateStore.Save(snapshot);
    }

    public async Task RunBackgroundRefreshAsync(
        IEnumerable<TweakItemViewModel> tweaks,
        Func<CancellationToken, Task> runner,
        CancellationToken ct = default)
    {
        if (_isBackgroundRefreshRunning)
        {
            return;
        }

        _isBackgroundRefreshRunning = true;
        UpdateStatusMessage(tweaks);
        try
        {
            await runner(ct);
        }
        finally
        {
            _isBackgroundRefreshRunning = false;
            UpdateStatusMessage(tweaks);
        }
    }

    public void Dispose()
    {
        _inventorySaveCts?.Cancel();
        _inventorySaveCts?.Dispose();
    }
}
