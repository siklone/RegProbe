using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceCommandCoordinator : ViewModelBase, IDisposable
{
    private readonly WorkspaceActionCoordinator _actionCoordinator;
    private readonly WorkspaceCommandSet _commands;
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
        _commands = new WorkspaceCommandSet(
            RunBulkAsync,
            CanRunBulkInspectable,
            CanRunBulkMutating,
            () => IsBulkRunning,
            CancelBulk,
            SelectAllVisible,
            DeselectAll,
            _getAllFilteredTweaks,
            _getAllActionableFilteredTweaks,
            _getSelectedTweaks,
            _getSelectedActionableTweaks,
            () => _setDetailsExpanded(true),
            () => _setDetailsExpanded(false));
    }

    public ICommand PreviewAllCommand => _commands.PreviewAllCommand;

    public ICommand ApplyAllCommand => _commands.ApplyAllCommand;

    public ICommand VerifyAllCommand => _commands.VerifyAllCommand;

    public ICommand RollbackAllCommand => _commands.RollbackAllCommand;

    public ICommand CancelAllCommand => _commands.CancelAllCommand;

    public ICommand SelectAllCommand => _commands.SelectAllCommand;

    public ICommand DeselectAllCommand => _commands.DeselectAllCommand;

    public ICommand DetectSelectedCommand => _commands.DetectSelectedCommand;

    public ICommand ApplySelectedCommand => _commands.ApplySelectedCommand;

    public ICommand VerifySelectedCommand => _commands.VerifySelectedCommand;

    public ICommand RollbackSelectedCommand => _commands.RollbackSelectedCommand;

    public ICommand ExpandAllDetailsCommand => _commands.ExpandAllDetailsCommand;

    public ICommand CollapseAllDetailsCommand => _commands.CollapseAllDetailsCommand;

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
        ? "No items selected"
        : $"{SelectedCount} item{(SelectedCount == 1 ? string.Empty : "s")} selected";

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
        _commands.RaiseBulkCanExecuteChanged();
    }

    private void RaiseSelectedCommandCanExecuteChanged()
    {
        _commands.RaiseSelectedCanExecuteChanged();
    }
}
