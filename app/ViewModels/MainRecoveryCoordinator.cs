using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class MainRecoveryCoordinator : ViewModelBase
{
    private readonly IRollbackStateStore _rollbackStore;
    private readonly TweaksViewModel _workspaceViewModel;
    private readonly Action<string> _log;
    private bool _hasPendingRollbacks;
    private int _pendingRollbackCount;
    private string _pendingRollbackMessage = string.Empty;
    private bool _isRecovering;

    public MainRecoveryCoordinator(
        IRollbackStateStore rollbackStore,
        TweaksViewModel workspaceViewModel,
        Action<string> log)
    {
        _rollbackStore = rollbackStore ?? throw new ArgumentNullException(nameof(rollbackStore));
        _workspaceViewModel = workspaceViewModel ?? throw new ArgumentNullException(nameof(workspaceViewModel));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        RecoverPendingRollbacksCommand = new RelayCommand(_ => _ = RecoverPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);
        DismissPendingRollbacksCommand = new RelayCommand(_ => _ = DismissPendingRollbacksAsync(), _ => HasPendingRollbacks && !IsRecovering);
    }

    public ICommand RecoverPendingRollbacksCommand { get; }

    public ICommand DismissPendingRollbacksCommand { get; }

    public bool HasPendingRollbacks
    {
        get => _hasPendingRollbacks;
        private set
        {
            if (SetProperty(ref _hasPendingRollbacks, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int PendingRollbackCount
    {
        get => _pendingRollbackCount;
        private set => SetProperty(ref _pendingRollbackCount, value);
    }

    public string PendingRollbackMessage
    {
        get => _pendingRollbackMessage;
        private set => SetProperty(ref _pendingRollbackMessage, value);
    }

    public bool IsRecovering
    {
        get => _isRecovering;
        private set
        {
            if (SetProperty(ref _isRecovering, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var pending = await _rollbackStore.GetPendingRollbacksAsync(CancellationToken.None);
            if (pending.Count > 0)
            {
                PendingRollbackCount = pending.Count;
                PendingRollbackMessage = pending.Count == 1
                    ? $"1 tweak was not properly rolled back after a crash: {pending[0].TweakId}"
                    : $"{pending.Count} tweaks were not properly rolled back after a crash.";
                HasPendingRollbacks = true;
                _log($"Crash recovery: Found {pending.Count} pending rollbacks");
            }
        }
        catch (Exception ex)
        {
            _log($"Crash recovery check failed: {ex.Message}");
        }
    }

    private async Task RecoverPendingRollbacksAsync()
    {
        IsRecovering = true;
        _log("Crash recovery: Starting rollback recovery...");

        try
        {
            var pending = await _rollbackStore.GetPendingRollbacksAsync(CancellationToken.None);
            var successCount = 0;

            foreach (var entry in pending)
            {
                try
                {
                    var tweakVm = _workspaceViewModel.Tweaks.FirstOrDefault(t => t.Id == entry.TweakId);
                    if (tweakVm != null && tweakVm.IsApplied)
                    {
                        await tweakVm.RunRollbackAsync(CancellationToken.None);
                        successCount++;
                        _log($"Crash recovery: Rolled back {entry.TweakId}");
                    }
                    else
                    {
                        await _rollbackStore.MarkRolledBackAsync(entry.TweakId, CancellationToken.None);
                        successCount++;
                        _log($"Crash recovery: Marked {entry.TweakId} as recovered (not found or already rolled back)");
                    }
                }
                catch (Exception ex)
                {
                    _log($"Crash recovery: Failed to rollback {entry.TweakId}: {ex.Message}");
                }
            }

            if (successCount == pending.Count)
            {
                ClearPendingRollbacks();
                _log("Crash recovery: All rollbacks completed successfully");
            }
            else
            {
                PendingRollbackMessage = $"Recovery completed with {pending.Count - successCount} failures. Check logs for details.";
            }
        }
        catch (Exception ex)
        {
            _log($"Crash recovery failed: {ex.Message}");
            PendingRollbackMessage = $"Recovery failed: {ex.Message}";
        }
        finally
        {
            IsRecovering = false;
        }
    }

    private async Task DismissPendingRollbacksAsync()
    {
        _log("Crash recovery: User dismissed pending rollbacks");

        try
        {
            await _rollbackStore.ClearAllAsync(CancellationToken.None);
            ClearPendingRollbacks();
        }
        catch (Exception ex)
        {
            _log($"Failed to clear pending rollbacks: {ex.Message}");
        }
    }

    private void ClearPendingRollbacks()
    {
        HasPendingRollbacks = false;
        PendingRollbackMessage = string.Empty;
        PendingRollbackCount = 0;
    }

    private void RaiseCommandStates()
    {
        if (RecoverPendingRollbacksCommand is RelayCommand recoverCommand)
        {
            recoverCommand.RaiseCanExecuteChanged();
        }

        if (DismissPendingRollbacksCommand is RelayCommand dismissCommand)
        {
            dismissCommand.RaiseCanExecuteChanged();
        }
    }
}
