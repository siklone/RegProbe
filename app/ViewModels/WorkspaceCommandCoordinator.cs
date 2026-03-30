using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RegProbe.App.Services;
using RegProbe.Core.Commands;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceCommandCoordinator : ViewModelBase, IDisposable
{
    private readonly WorkspaceActionCoordinator _actionCoordinator;
    private readonly Func<IEnumerable<TweakItemViewModel>> _getAllTweaks;
    private readonly Func<IEnumerable<TweakItemViewModel>> _getVisibleTweaks;
    private readonly Func<List<TweakItemViewModel>> _getAllFilteredTweaks;
    private readonly Func<List<TweakItemViewModel>> _getAllActionableFilteredTweaks;
    private readonly Func<List<TweakItemViewModel>> _getSelectedTweaks;
    private readonly Func<List<TweakItemViewModel>> _getSelectedActionableTweaks;
    private readonly Action _refreshSummaryStats;
    private readonly Action<bool> _setBulkLock;
    private readonly Action<bool> _setDetailsExpanded;
    private string _bulkStatusMessage = "Bulk actions are idle.";
    private bool _isBulkRunning;
    private int _bulkProgressCurrent;
    private int _bulkProgressTotal;
    private int _selectedCount;

    public WorkspaceCommandCoordinator(
        IBusyService busyService,
        Func<IEnumerable<TweakItemViewModel>> getAllTweaks,
        Func<IEnumerable<TweakItemViewModel>> getVisibleTweaks,
        Func<List<TweakItemViewModel>> getAllFilteredTweaks,
        Func<List<TweakItemViewModel>> getAllActionableFilteredTweaks,
        Func<List<TweakItemViewModel>> getSelectedTweaks,
        Func<List<TweakItemViewModel>> getSelectedActionableTweaks,
        Action refreshSummaryStats,
        Action<bool> setBulkLock,
        Action<bool> setDetailsExpanded)
    {
        ArgumentNullException.ThrowIfNull(busyService);
        _getAllTweaks = getAllTweaks ?? throw new ArgumentNullException(nameof(getAllTweaks));
        _getVisibleTweaks = getVisibleTweaks ?? throw new ArgumentNullException(nameof(getVisibleTweaks));
        _getAllFilteredTweaks = getAllFilteredTweaks ?? throw new ArgumentNullException(nameof(getAllFilteredTweaks));
        _getAllActionableFilteredTweaks = getAllActionableFilteredTweaks ?? throw new ArgumentNullException(nameof(getAllActionableFilteredTweaks));
        _getSelectedTweaks = getSelectedTweaks ?? throw new ArgumentNullException(nameof(getSelectedTweaks));
        _getSelectedActionableTweaks = getSelectedActionableTweaks ?? throw new ArgumentNullException(nameof(getSelectedActionableTweaks));
        _refreshSummaryStats = refreshSummaryStats ?? throw new ArgumentNullException(nameof(refreshSummaryStats));
        _setBulkLock = setBulkLock ?? throw new ArgumentNullException(nameof(setBulkLock));
        _setDetailsExpanded = setDetailsExpanded ?? throw new ArgumentNullException(nameof(setDetailsExpanded));

        _actionCoordinator = new WorkspaceActionCoordinator(busyService);

        PreviewAllCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Preview", _getAllFilteredTweaks, (item, token) => item.RunPreviewAsync(token)),
            _ => CanRunBulkInspectable(_getAllFilteredTweaks));
        ApplyAllCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Apply", _getAllActionableFilteredTweaks, (item, token) => item.RunApplyAsync(token)),
            _ => CanRunBulkMutating(_getAllActionableFilteredTweaks));
        VerifyAllCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Verify", _getAllFilteredTweaks, (item, token) => item.RunVerifyAsync(token)),
            _ => CanRunBulkInspectable(_getAllFilteredTweaks));
        RollbackAllCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Rollback", _getAllActionableFilteredTweaks, (item, token) => item.RunRollbackAsync(token)),
            _ => CanRunBulkMutating(_getAllActionableFilteredTweaks));
        CancelAllCommand = new RelayCommand(_ => CancelBulk(), _ => IsBulkRunning);
        SelectAllCommand = new RelayCommand(_ => SelectAllVisible());
        DeselectAllCommand = new RelayCommand(_ => DeselectAll());
        DetectSelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Detect Selected", _getSelectedTweaks, (item, token) => item.RunDetectAsync(token)),
            _ => CanRunBulkInspectable(_getSelectedTweaks));
        ApplySelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Apply Selected", _getSelectedActionableTweaks, (item, token) => item.RunApplyAsync(token)),
            _ => CanRunBulkMutating(_getSelectedActionableTweaks));
        VerifySelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Verify Selected", _getSelectedTweaks, (item, token) => item.RunVerifyAsync(token)),
            _ => CanRunBulkInspectable(_getSelectedTweaks));
        RollbackSelectedCommand = new RelayCommand(
            _ => _ = RunBulkAsync("Rollback Selected", _getSelectedActionableTweaks, (item, token) => item.RunRollbackAsync(token)),
            _ => CanRunBulkMutating(_getSelectedActionableTweaks));
        ExpandAllDetailsCommand = new RelayCommand(_ => _setDetailsExpanded(true));
        CollapseAllDetailsCommand = new RelayCommand(_ => _setDetailsExpanded(false));
    }

    public ICommand PreviewAllCommand { get; }

    public ICommand ApplyAllCommand { get; }

    public ICommand VerifyAllCommand { get; }

    public ICommand RollbackAllCommand { get; }

    public ICommand CancelAllCommand { get; }

    public ICommand SelectAllCommand { get; }

    public ICommand DeselectAllCommand { get; }

    public ICommand DetectSelectedCommand { get; }

    public ICommand ApplySelectedCommand { get; }

    public ICommand VerifySelectedCommand { get; }

    public ICommand RollbackSelectedCommand { get; }

    public ICommand ExpandAllDetailsCommand { get; }

    public ICommand CollapseAllDetailsCommand { get; }

    public string BulkStatusMessage
    {
        get => _bulkStatusMessage;
        private set => SetProperty(ref _bulkStatusMessage, value);
    }

    public bool IsBulkRunning
    {
        get => _isBulkRunning;
        private set
        {
            if (SetProperty(ref _isBulkRunning, value))
            {
                RaiseBulkCommandCanExecuteChanged();
                _setBulkLock(value);
            }
        }
    }

    public int BulkProgressCurrent
    {
        get => _bulkProgressCurrent;
        private set
        {
            if (SetProperty(ref _bulkProgressCurrent, value))
            {
                OnPropertyChanged(nameof(BulkProgressText));
            }
        }
    }

    public int BulkProgressTotal
    {
        get => _bulkProgressTotal;
        private set
        {
            if (SetProperty(ref _bulkProgressTotal, value))
            {
                OnPropertyChanged(nameof(BulkProgressText));
            }
        }
    }

    public string BulkProgressText => BulkProgressTotal == 0
        ? "Bulk progress: 0/0"
        : $"Bulk progress: {BulkProgressCurrent}/{BulkProgressTotal}";

    public int SelectedCount
    {
        get => _selectedCount;
        private set
        {
            if (SetProperty(ref _selectedCount, value))
            {
                OnPropertyChanged(nameof(SelectionSummary));
                OnPropertyChanged(nameof(HasSelection));
                RaiseSelectedCommandCanExecuteChanged();
            }
        }
    }

    public string SelectionSummary => SelectedCount == 0
        ? "No settings selected"
        : $"{SelectedCount} setting{(SelectedCount == 1 ? string.Empty : "s")} selected";

    public bool HasSelection => SelectedCount > 0;

    public void SyncSelectionState()
    {
        SelectedCount = _actionCoordinator.CountSelected(_getAllTweaks());
    }

    public void NotifyTweakRunningChanged()
    {
        RaiseBulkCommandCanExecuteChanged();
    }

    public void NotifyFilterStateChanged()
    {
        RaiseBulkCommandCanExecuteChanged();
    }

    public void SetBulkStatusMessage(string message)
    {
        BulkStatusMessage = message;
    }

    public void DeselectAll()
    {
        _actionCoordinator.DeselectAll(_getAllTweaks());
    }

    public Task RunRepairsBatchAsync(
        string label,
        Func<List<TweakItemViewModel>> getTweaks,
        Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        return RunBulkAsync(label, getTweaks, runner);
    }

    public void Dispose()
    {
        _actionCoordinator.Dispose();
    }

    private bool CanRunBulkInspectable(Func<List<TweakItemViewModel>> getTweaks)
    {
        return _actionCoordinator.CanRunInspectable(IsBulkRunning, _getAllTweaks(), getTweaks);
    }

    private bool CanRunBulkMutating(Func<List<TweakItemViewModel>> getTweaks)
    {
        return _actionCoordinator.CanRunMutating(IsBulkRunning, _getAllTweaks(), getTweaks);
    }

    private async Task RunBulkAsync(
        string label,
        Func<List<TweakItemViewModel>> getTweaks,
        Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        await _actionCoordinator.RunBulkAsync(
            label,
            getTweaks,
            runner,
            () => IsBulkRunning,
            value => IsBulkRunning = value,
            value => BulkProgressCurrent = value,
            value => BulkProgressTotal = value,
            value => BulkStatusMessage = value,
            () => OnPropertyChanged(nameof(BulkProgressText)),
            _refreshSummaryStats);
    }

    private void CancelBulk()
    {
        _actionCoordinator.CancelBulk(IsBulkRunning, value => BulkStatusMessage = value);
    }

    private void SelectAllVisible()
    {
        _actionCoordinator.SelectAll(_getVisibleTweaks());
    }

    private void RaiseBulkCommandCanExecuteChanged()
    {
        RaiseIfRelay(PreviewAllCommand);
        RaiseIfRelay(ApplyAllCommand);
        RaiseIfRelay(VerifyAllCommand);
        RaiseIfRelay(RollbackAllCommand);
        RaiseIfRelay(CancelAllCommand);
        RaiseSelectedCommandCanExecuteChanged();
    }

    private void RaiseSelectedCommandCanExecuteChanged()
    {
        RaiseIfRelay(DetectSelectedCommand);
        RaiseIfRelay(ApplySelectedCommand);
        RaiseIfRelay(VerifySelectedCommand);
        RaiseIfRelay(RollbackSelectedCommand);
    }

    private static void RaiseIfRelay(ICommand command)
    {
        if (command is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }
}
