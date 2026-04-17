using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RegProbe.Core.Commands;

namespace RegProbe.App.ViewModels;

internal sealed class WorkspaceCommandSet
{
    public WorkspaceCommandSet(
        Func<string, Func<List<TweakItemViewModel>>, Func<TweakItemViewModel, CancellationToken, Task>, Task> runBulkAsync,
        Func<Func<List<TweakItemViewModel>>, bool> canRunBulkInspectable,
        Func<Func<List<TweakItemViewModel>>, bool> canRunBulkMutating,
        Func<bool> isBulkRunning,
        Action cancelBulk,
        Action selectAllVisible,
        Action deselectAll,
        Func<List<TweakItemViewModel>> getAllFilteredTweaks,
        Func<List<TweakItemViewModel>> getAllActionableFilteredTweaks,
        Func<List<TweakItemViewModel>> getSelectedTweaks,
        Func<List<TweakItemViewModel>> getSelectedActionableTweaks,
        Action expandAllDetails,
        Action collapseAllDetails)
    {
        ArgumentNullException.ThrowIfNull(runBulkAsync);
        ArgumentNullException.ThrowIfNull(canRunBulkInspectable);
        ArgumentNullException.ThrowIfNull(canRunBulkMutating);
        ArgumentNullException.ThrowIfNull(isBulkRunning);
        ArgumentNullException.ThrowIfNull(cancelBulk);
        ArgumentNullException.ThrowIfNull(selectAllVisible);
        ArgumentNullException.ThrowIfNull(deselectAll);
        ArgumentNullException.ThrowIfNull(getAllFilteredTweaks);
        ArgumentNullException.ThrowIfNull(getAllActionableFilteredTweaks);
        ArgumentNullException.ThrowIfNull(getSelectedTweaks);
        ArgumentNullException.ThrowIfNull(getSelectedActionableTweaks);
        ArgumentNullException.ThrowIfNull(expandAllDetails);
        ArgumentNullException.ThrowIfNull(collapseAllDetails);

        PreviewAllCommand = new RelayCommand(
            _ => _ = runBulkAsync("Preview", getAllFilteredTweaks, (item, token) => item.RunPreviewAsync(token)),
            _ => canRunBulkInspectable(getAllFilteredTweaks));
        ApplyAllCommand = new RelayCommand(
            _ => _ = runBulkAsync("Apply", getAllActionableFilteredTweaks, (item, token) => item.RunApplyAsync(token)),
            _ => canRunBulkMutating(getAllActionableFilteredTweaks));
        VerifyAllCommand = new RelayCommand(
            _ => _ = runBulkAsync("Verify", getAllFilteredTweaks, (item, token) => item.RunVerifyAsync(token)),
            _ => canRunBulkInspectable(getAllFilteredTweaks));
        RollbackAllCommand = new RelayCommand(
            _ => _ = runBulkAsync("Rollback", getAllActionableFilteredTweaks, (item, token) => item.RunRollbackAsync(token)),
            _ => canRunBulkMutating(getAllActionableFilteredTweaks));
        CancelAllCommand = new RelayCommand(_ => cancelBulk(), _ => isBulkRunning());
        SelectAllCommand = new RelayCommand(_ => selectAllVisible());
        DeselectAllCommand = new RelayCommand(_ => deselectAll());
        DetectSelectedCommand = new RelayCommand(
            _ => _ = runBulkAsync("Detect Selected", getSelectedTweaks, (item, token) => item.RunDetectAsync(token)),
            _ => canRunBulkInspectable(getSelectedTweaks));
        ApplySelectedCommand = new RelayCommand(
            _ => _ = runBulkAsync("Apply Selected", getSelectedActionableTweaks, (item, token) => item.RunApplyAsync(token)),
            _ => canRunBulkMutating(getSelectedActionableTweaks));
        VerifySelectedCommand = new RelayCommand(
            _ => _ = runBulkAsync("Verify Selected", getSelectedTweaks, (item, token) => item.RunVerifyAsync(token)),
            _ => canRunBulkInspectable(getSelectedTweaks));
        RollbackSelectedCommand = new RelayCommand(
            _ => _ = runBulkAsync("Rollback Selected", getSelectedActionableTweaks, (item, token) => item.RunRollbackAsync(token)),
            _ => canRunBulkMutating(getSelectedActionableTweaks));
        ExpandAllDetailsCommand = new RelayCommand(_ => expandAllDetails());
        CollapseAllDetailsCommand = new RelayCommand(_ => collapseAllDetails());
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

    public void RaiseBulkCanExecuteChanged()
    {
        RaiseIfRelay(PreviewAllCommand);
        RaiseIfRelay(ApplyAllCommand);
        RaiseIfRelay(VerifyAllCommand);
        RaiseIfRelay(RollbackAllCommand);
        RaiseIfRelay(CancelAllCommand);
        RaiseSelectedCanExecuteChanged();
    }

    public void RaiseSelectedCanExecuteChanged()
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
