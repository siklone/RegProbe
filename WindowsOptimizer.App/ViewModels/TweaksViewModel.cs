using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class TweaksViewModel : ViewModelBase
{
    private readonly ITweakLogStore _logStore;
    private readonly RelayCommand _exportLogsCommand;
    private readonly RelayCommand _previewAllCommand;
    private readonly RelayCommand _applyAllCommand;
    private readonly RelayCommand _cancelAllCommand;
    private string _exportStatusMessage = "Logs are ready to export.";
    private string _bulkStatusMessage = "Bulk actions are idle.";
    private bool _isExporting;
    private bool _isBulkRunning;
    private string _searchText = string.Empty;
    private CancellationTokenSource? _bulkCts;

    public TweaksViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        var logger = new FileAppLogger(paths);
        _logStore = new FileTweakLogStore(paths);
        var pipeline = new TweakExecutionPipeline(logger, _logStore);
        var settingsStore = new SettingsStore(paths);

        Tweaks = new ObservableCollection<TweakItemViewModel>
        {
            new(new SettingsToggleTweak(
                    "demo.alpha",
                    "Demo: Enable performance profile",
                    "Demo toggle stored in app settings. Safe preview/apply/rollback for pipeline testing.",
                    TweakRiskLevel.Safe,
                    settingsStore,
                    settings => settings.DemoTweakAlphaEnabled,
                    (settings, value) => settings.DemoTweakAlphaEnabled = value),
                pipeline),
            new(new SettingsToggleTweak(
                    "demo.beta",
                    "Demo: Reduce background noise",
                    "Demo toggle stored in app settings. No system changes are applied.",
                    TweakRiskLevel.Safe,
                    settingsStore,
                    settings => settings.DemoTweakBetaEnabled,
                    (settings, value) => settings.DemoTweakBetaEnabled = value),
                pipeline)
        };

        TweaksView = CollectionViewSource.GetDefaultView(Tweaks);
        TweaksView.Filter = FilterTweaks;

        _exportLogsCommand = new RelayCommand(_ => _ = ExportLogsAsync(), _ => !IsExporting);
        _previewAllCommand = new RelayCommand(_ => _ = RunBulkAsync(true), _ => !IsBulkRunning);
        _applyAllCommand = new RelayCommand(_ => _ = RunBulkAsync(false), _ => !IsBulkRunning);
        _cancelAllCommand = new RelayCommand(_ => CancelBulk(), _ => IsBulkRunning);
    }

    public string Title => "Tweaks";

    public ObservableCollection<TweakItemViewModel> Tweaks { get; }

    public ICollectionView TweaksView { get; }

    public ICommand ExportLogsCommand => _exportLogsCommand;

    public ICommand PreviewAllCommand => _previewAllCommand;

    public ICommand ApplyAllCommand => _applyAllCommand;

    public ICommand CancelAllCommand => _cancelAllCommand;

    public string ExportStatusMessage
    {
        get => _exportStatusMessage;
        private set => SetProperty(ref _exportStatusMessage, value);
    }

    public string BulkStatusMessage
    {
        get => _bulkStatusMessage;
        private set => SetProperty(ref _bulkStatusMessage, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set
        {
            if (SetProperty(ref _isExporting, value))
            {
                _exportLogsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBulkRunning
    {
        get => _isBulkRunning;
        private set
        {
            if (SetProperty(ref _isBulkRunning, value))
            {
                _previewAllCommand.RaiseCanExecuteChanged();
                _applyAllCommand.RaiseCanExecuteChanged();
                _cancelAllCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                TweaksView.Refresh();
            }
        }
    }

    private async Task ExportLogsAsync()
    {
        if (IsExporting)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "tweak-log.csv",
            Title = "Export tweak logs"
        };

        if (dialog.ShowDialog() != true)
        {
            ExportStatusMessage = "Export cancelled.";
            return;
        }

        IsExporting = true;
        try
        {
            await _logStore.ExportCsvAsync(dialog.FileName, CancellationToken.None);
            ExportStatusMessage = $"Exported to {dialog.FileName}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task RunBulkAsync(bool dryRun)
    {
        if (IsBulkRunning)
        {
            return;
        }

        StartBulkCancellation();
        IsBulkRunning = true;
        BulkStatusMessage = dryRun ? "Bulk preview started." : "Bulk apply started.";

        try
        {
            var items = TweaksView.Cast<TweakItemViewModel>().ToList();
            foreach (var item in items)
            {
                _bulkCts?.Token.ThrowIfCancellationRequested();
                BulkStatusMessage = $"Running {item.Name}...";

                if (dryRun)
                {
                    await item.RunPreviewAsync(_bulkCts?.Token ?? CancellationToken.None);
                }
                else
                {
                    await item.RunApplyAsync(_bulkCts?.Token ?? CancellationToken.None);
                }
            }

            BulkStatusMessage = "Bulk run completed.";
        }
        catch (OperationCanceledException)
        {
            BulkStatusMessage = "Bulk run cancelled.";
        }
        finally
        {
            IsBulkRunning = false;
            ClearBulkCancellation();
        }
    }

    private void CancelBulk()
    {
        if (!IsBulkRunning || _bulkCts is null)
        {
            return;
        }

        _bulkCts.Cancel();
        BulkStatusMessage = "Bulk cancellation requested.";
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

    private bool FilterTweaks(object obj)
    {
        if (obj is not TweakItemViewModel item)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        return item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Risk.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }
}
