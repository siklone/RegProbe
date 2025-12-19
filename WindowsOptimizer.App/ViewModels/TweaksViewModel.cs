using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
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
    private readonly RelayCommand _detectAllCommand;
    private readonly RelayCommand _previewAllCommand;
    private readonly RelayCommand _applyAllCommand;
    private readonly RelayCommand _verifyAllCommand;
    private readonly RelayCommand _rollbackAllCommand;
    private readonly RelayCommand _cancelAllCommand;
    private readonly RelayCommand _resetAllCommand;
    private readonly RelayCommand _resetFiltersCommand;
    private readonly RelayCommand _openLogFolderCommand;
    private readonly RelayCommand _openAppLogCommand;
    private readonly RelayCommand _copyAppLogPathCommand;
    private readonly RelayCommand _openCsvLogCommand;
    private readonly RelayCommand _copyCsvPathCommand;
    private readonly RelayCommand _expandAllDetailsCommand;
    private readonly RelayCommand _collapseAllDetailsCommand;
    private string _exportStatusMessage = "Logs are ready to export.";
    private string _bulkStatusMessage = "Bulk actions are idle.";
    private string _filterSummary = "Showing 0 of 0 tweaks.";
    private string _riskSummary = "Safe: 0 | Advanced: 0 | Risky: 0";
    private string _bulkTargetSummary = "Bulk actions target 0 tweaks.";
    private string _outcomeSummary = "Outcomes: 0 success | 0 failed | 0 cancelled | 0 running | 0 skipped";
    private bool _isExporting;
    private bool _isBulkRunning;
    private int _bulkProgressCurrent;
    private int _bulkProgressTotal;
    private bool _hasBulkProgress;
    private string _searchText = string.Empty;
    private bool _showSafe = true;
    private bool _showAdvanced = true;
    private bool _showRisky = true;
    private bool _showOnlyRunning;
    private bool _showOnlyFailed;
    private bool _showOnlySucceeded;
    private bool _sortFailedFirst;
    private bool _hasVisibleTweaks;
    private CancellationTokenSource? _bulkCts;
    private readonly string _logFolderPath;
    private readonly string _appLogFilePath;
    private readonly string _tweakLogFilePath;

    public TweaksViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        var logger = new FileAppLogger(paths);
        _logFolderPath = paths.LogDirectory;
        _appLogFilePath = paths.LogFilePath;
        _tweakLogFilePath = paths.TweakLogFilePath;
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
                pipeline),
            new(new SettingsToggleTweak(
                    "demo.gamma",
                    "Demo: Tune startup sequencing",
                    "Demo toggle stored in app settings. Marked advanced to exercise filtering.",
                    TweakRiskLevel.Advanced,
                    settingsStore,
                    settings => settings.DemoTweakGammaEnabled,
                    (settings, value) => settings.DemoTweakGammaEnabled = value),
                pipeline),
            new(new SettingsToggleTweak(
                    "demo.delta",
                    "Demo: Aggressive cleanup mode",
                    "Demo toggle stored in app settings. Marked risky to exercise confirmations.",
                    TweakRiskLevel.Risky,
                    settingsStore,
                    settings => settings.DemoTweakDeltaEnabled,
                    (settings, value) => settings.DemoTweakDeltaEnabled = value),
                pipeline)
        };

        foreach (var tweak in Tweaks)
        {
            tweak.PropertyChanged += OnTweakPropertyChanged;
        }

        TweaksView = CollectionViewSource.GetDefaultView(Tweaks);
        TweaksView.Filter = FilterTweaks;
        ApplySort();

        _exportLogsCommand = new RelayCommand(_ => _ = ExportLogsAsync(), _ => !IsExporting);
        _detectAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Detect", (item, token) => item.RunDetectAsync(token)), _ => CanRunBulk());
        _previewAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Preview", (item, token) => item.RunPreviewAsync(token)), _ => CanRunBulk());
        _applyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Apply", (item, token) => item.RunApplyAsync(token)), _ => CanRunBulk());
        _verifyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Verify", (item, token) => item.RunVerifyAsync(token)), _ => CanRunBulk());
        _rollbackAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Rollback", (item, token) => item.RunRollbackAsync(token)), _ => CanRunBulk());
        _cancelAllCommand = new RelayCommand(_ => CancelBulk(), _ => IsBulkRunning);
        _resetAllCommand = new RelayCommand(_ => ResetAllStatuses(), _ => CanResetAll());
        _resetFiltersCommand = new RelayCommand(_ => ResetFilters());
        _openLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
        _openAppLogCommand = new RelayCommand(_ => OpenAppLog());
        _copyAppLogPathCommand = new RelayCommand(_ => CopyAppLogPath());
        _openCsvLogCommand = new RelayCommand(_ => OpenCsvLog());
        _copyCsvPathCommand = new RelayCommand(_ => CopyCsvPath());
        _expandAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(true));
        _collapseAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(false));

        UpdateFilterSummary();
    }

    public string Title => "Tweaks";

    public ObservableCollection<TweakItemViewModel> Tweaks { get; }

    public ICollectionView TweaksView { get; }

    public ICommand ExportLogsCommand => _exportLogsCommand;

    public ICommand DetectAllCommand => _detectAllCommand;

    public ICommand PreviewAllCommand => _previewAllCommand;

    public ICommand ApplyAllCommand => _applyAllCommand;

    public ICommand VerifyAllCommand => _verifyAllCommand;

    public ICommand RollbackAllCommand => _rollbackAllCommand;

    public ICommand CancelAllCommand => _cancelAllCommand;

    public ICommand ResetAllCommand => _resetAllCommand;

    public ICommand ResetFiltersCommand => _resetFiltersCommand;

    public ICommand OpenLogFolderCommand => _openLogFolderCommand;

    public ICommand OpenAppLogCommand => _openAppLogCommand;

    public ICommand CopyAppLogPathCommand => _copyAppLogPathCommand;

    public ICommand OpenCsvLogCommand => _openCsvLogCommand;

    public ICommand CopyCsvPathCommand => _copyCsvPathCommand;

    public ICommand ExpandAllDetailsCommand => _expandAllDetailsCommand;

    public ICommand CollapseAllDetailsCommand => _collapseAllDetailsCommand;

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
                _detectAllCommand.RaiseCanExecuteChanged();
                _previewAllCommand.RaiseCanExecuteChanged();
                _applyAllCommand.RaiseCanExecuteChanged();
                _verifyAllCommand.RaiseCanExecuteChanged();
                _rollbackAllCommand.RaiseCanExecuteChanged();
                _cancelAllCommand.RaiseCanExecuteChanged();
                _resetAllCommand.RaiseCanExecuteChanged();
                SetBulkLock(value);
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
                HasBulkProgress = value > 0;
                OnPropertyChanged(nameof(BulkProgressText));
            }
        }
    }

    public bool HasBulkProgress
    {
        get => _hasBulkProgress;
        private set => SetProperty(ref _hasBulkProgress, value);
    }

    public string BulkProgressText => BulkProgressTotal == 0
        ? "Bulk progress: 0/0"
        : $"Bulk progress: {BulkProgressCurrent}/{BulkProgressTotal} ({BulkProgressCurrent * 100 / BulkProgressTotal}%)";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowSafe
    {
        get => _showSafe;
        set
        {
            if (SetProperty(ref _showSafe, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set
        {
            if (SetProperty(ref _showAdvanced, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowRisky
    {
        get => _showRisky;
        set
        {
            if (SetProperty(ref _showRisky, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowOnlyFailed
    {
        get => _showOnlyFailed;
        set
        {
            if (SetProperty(ref _showOnlyFailed, value))
            {
                if (value && ShowOnlyRunning)
                {
                    ShowOnlyRunning = false;
                }
                if (value && ShowOnlySucceeded)
                {
                    ShowOnlySucceeded = false;
                }

                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowOnlyRunning
    {
        get => _showOnlyRunning;
        set
        {
            if (SetProperty(ref _showOnlyRunning, value))
            {
                if (value && ShowOnlyFailed)
                {
                    ShowOnlyFailed = false;
                }
                if (value && ShowOnlySucceeded)
                {
                    ShowOnlySucceeded = false;
                }

                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowOnlySucceeded
    {
        get => _showOnlySucceeded;
        set
        {
            if (SetProperty(ref _showOnlySucceeded, value))
            {
                if (value && ShowOnlyFailed)
                {
                    ShowOnlyFailed = false;
                }
                if (value && ShowOnlyRunning)
                {
                    ShowOnlyRunning = false;
                }

                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool SortFailedFirst
    {
        get => _sortFailedFirst;
        set
        {
            if (SetProperty(ref _sortFailedFirst, value))
            {
                ApplySort();
            }
        }
    }

    public string FilterSummary
    {
        get => _filterSummary;
        private set => SetProperty(ref _filterSummary, value);
    }

    public string RiskSummary
    {
        get => _riskSummary;
        private set => SetProperty(ref _riskSummary, value);
    }

    public string BulkTargetSummary
    {
        get => _bulkTargetSummary;
        private set => SetProperty(ref _bulkTargetSummary, value);
    }

    public string OutcomeSummary
    {
        get => _outcomeSummary;
        private set => SetProperty(ref _outcomeSummary, value);
    }

    public bool HasVisibleTweaks
    {
        get => _hasVisibleTweaks;
        private set => SetProperty(ref _hasVisibleTweaks, value);
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

    private bool CanRunBulk()
    {
        if (IsBulkRunning || Tweaks.Any(item => item.IsRunning))
        {
            return false;
        }

        return TweaksView.Cast<object>().Any();
    }

    private bool CanResetAll()
    {
        return !IsBulkRunning && !Tweaks.Any(item => item.IsRunning);
    }

    private async Task RunBulkAsync(string label, Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        if (IsBulkRunning)
        {
            return;
        }

        var items = TweaksView.Cast<TweakItemViewModel>().ToList();
        if (items.Count == 0)
        {
            return;
        }

        var actionLabel = label.ToLowerInvariant();
        var requiresRiskConfirmation = IsRiskyBulkAction(label);
        if (requiresRiskConfirmation)
        {
            var riskyCount = items.Count(item => item.Risk == TweakRiskLevel.Risky);
            if (riskyCount > 0 && !ConfirmBulkRisky(actionLabel, riskyCount))
            {
                BulkStatusMessage = $"Bulk {actionLabel} cancelled.";
                return;
            }
        }

        StartBulkCancellation();
        IsBulkRunning = true;
        BulkStatusMessage = $"Bulk {actionLabel} started.";

        if (requiresRiskConfirmation)
        {
            SetRiskConfirmationSuppressed(items, true);
        }

        try
        {
            BulkProgressTotal = items.Count;
            BulkProgressCurrent = 0;
            OnPropertyChanged(nameof(BulkProgressText));
            foreach (var item in items)
            {
                _bulkCts?.Token.ThrowIfCancellationRequested();
                BulkStatusMessage = $"Running {actionLabel} on {item.Name}...";
                await runner(item, _bulkCts?.Token ?? CancellationToken.None);

                BulkProgressCurrent++;
                OnPropertyChanged(nameof(BulkProgressText));
            }

            var outcomeSummary = BuildOutcomeSummary(items);
            BulkStatusMessage = $"Bulk {actionLabel} completed: {outcomeSummary}.";
        }
        catch (OperationCanceledException)
        {
            BulkStatusMessage = "Bulk run cancelled.";
        }
        finally
        {
            IsBulkRunning = false;
            BulkProgressCurrent = 0;
            BulkProgressTotal = 0;
            OnPropertyChanged(nameof(BulkProgressText));
            ClearBulkCancellation();
            if (requiresRiskConfirmation)
            {
                SetRiskConfirmationSuppressed(items, false);
            }
        }
    }

    private static bool IsRiskyBulkAction(string label)
    {
        return label.Equals("Apply", StringComparison.OrdinalIgnoreCase)
            || label.Equals("Rollback", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetRiskConfirmationSuppressed(IEnumerable<TweakItemViewModel> items, bool isSuppressed)
    {
        foreach (var item in items)
        {
            if (item.Risk == TweakRiskLevel.Risky)
            {
                item.SuppressRiskConfirmation = isSuppressed;
            }
        }
    }

    private static bool ConfirmBulkRisky(string actionLabel, int riskyCount)
    {
        var result = MessageBox.Show(
            $"Bulk {actionLabel} includes {riskyCount} risky tweak(s). Proceed?",
            "Confirm risky bulk action",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
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

    private void ResetAllStatuses()
    {
        if (!CanResetAll())
        {
            return;
        }

        foreach (var item in Tweaks)
        {
            item.ResetStatus();
        }

        BulkStatusMessage = "All tweak statuses cleared.";
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

        if (item.Risk == TweakRiskLevel.Safe && !_showSafe)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Advanced && !_showAdvanced)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Risky && !_showRisky)
        {
            return false;
        }

        if (_showOnlyFailed && item.LastOutcome != TweakRunOutcome.Failed)
        {
            return false;
        }

        if (_showOnlySucceeded && item.LastOutcome != TweakRunOutcome.Success)
        {
            return false;
        }

        if (_showOnlyRunning && !item.IsRunning)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        return item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Risk.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplySort()
    {
        TweaksView.SortDescriptions.Clear();
        if (SortFailedFirst)
        {
            TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.OutcomeSortRank), ListSortDirection.Ascending));
        }

        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Risk), ListSortDirection.Ascending));
        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Name), ListSortDirection.Ascending));
        TweaksView.Refresh();
    }

    private void UpdateFilterSummary()
    {
        var total = Tweaks.Count;
        var visible = TweaksView.Cast<object>().Count();
        FilterSummary = $"Showing {visible} of {total} tweaks.";
        BulkTargetSummary = $"Bulk actions target {visible} tweak{(visible == 1 ? string.Empty : "s")}.";
        var safeCount = Tweaks.Count(item => item.Risk == TweakRiskLevel.Safe);
        var advancedCount = Tweaks.Count(item => item.Risk == TweakRiskLevel.Advanced);
        var riskyCount = Tweaks.Count(item => item.Risk == TweakRiskLevel.Risky);
        RiskSummary = $"Safe: {safeCount} | Advanced: {advancedCount} | Risky: {riskyCount}";
        UpdateOutcomeSummary();
        HasVisibleTweaks = visible > 0;
        _detectAllCommand.RaiseCanExecuteChanged();
        _previewAllCommand.RaiseCanExecuteChanged();
        _applyAllCommand.RaiseCanExecuteChanged();
        _verifyAllCommand.RaiseCanExecuteChanged();
        _rollbackAllCommand.RaiseCanExecuteChanged();
        _resetAllCommand.RaiseCanExecuteChanged();
    }

    private static string BuildOutcomeSummary(IReadOnlyCollection<TweakItemViewModel> items)
    {
        var succeeded = items.Count(item => item.LastOutcome == TweakRunOutcome.Success);
        var failed = items.Count(item => item.LastOutcome == TweakRunOutcome.Failed);
        var cancelled = items.Count(item => item.LastOutcome == TweakRunOutcome.Cancelled);
        var skipped = items.Count(item => item.LastOutcome == TweakRunOutcome.Skipped);

        var parts = new System.Collections.Generic.List<string>();
        if (succeeded > 0)
        {
            parts.Add($"{succeeded} success");
        }

        if (failed > 0)
        {
            parts.Add($"{failed} failed");
        }

        if (cancelled > 0)
        {
            parts.Add($"{cancelled} cancelled");
        }

        if (skipped > 0)
        {
            parts.Add($"{skipped} skipped");
        }

        return parts.Count == 0 ? "no results" : string.Join(", ", parts);
    }

    private void OnTweakPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var isRunningChange = e.PropertyName == nameof(TweakItemViewModel.IsRunning);
        var outcomeChange = e.PropertyName == nameof(TweakItemViewModel.LastOutcome);

        if (isRunningChange)
        {
            _detectAllCommand.RaiseCanExecuteChanged();
            _previewAllCommand.RaiseCanExecuteChanged();
            _applyAllCommand.RaiseCanExecuteChanged();
            _verifyAllCommand.RaiseCanExecuteChanged();
            _rollbackAllCommand.RaiseCanExecuteChanged();
            _resetAllCommand.RaiseCanExecuteChanged();
        }

        if ((isRunningChange && ShowOnlyRunning) || (outcomeChange && (ShowOnlyFailed || ShowOnlySucceeded || SortFailedFirst)))
        {
            TweaksView.Refresh();
            UpdateFilterSummary();
        }

        if (isRunningChange || outcomeChange)
        {
            UpdateOutcomeSummary();
        }
    }

    private void UpdateOutcomeSummary()
    {
        var successCount = Tweaks.Count(item => item.LastOutcome == TweakRunOutcome.Success);
        var failedCount = Tweaks.Count(item => item.LastOutcome == TweakRunOutcome.Failed);
        var cancelledCount = Tweaks.Count(item => item.LastOutcome == TweakRunOutcome.Cancelled);
        var runningCount = Tweaks.Count(item => item.LastOutcome == TweakRunOutcome.InProgress);
        var skippedCount = Tweaks.Count(item => item.LastOutcome == TweakRunOutcome.Skipped);
        OutcomeSummary = $"Outcomes: {successCount} success | {failedCount} failed | {cancelledCount} cancelled | {runningCount} running | {skippedCount} skipped";
    }

    private void ResetFilters()
    {
        SearchText = string.Empty;
        ShowSafe = true;
        ShowAdvanced = true;
        ShowRisky = true;
        ShowOnlyRunning = false;
        ShowOnlyFailed = false;
        ShowOnlySucceeded = false;
        SortFailedFirst = false;
    }

    private void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _logFolderPath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log folder: {_logFolderPath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log folder failed: {ex.Message}";
        }
    }

    private void OpenCsvLog()
    {
        try
        {
            if (!File.Exists(_tweakLogFilePath))
            {
                ExportStatusMessage = "No tweak log file yet. Run a tweak to generate one.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _tweakLogFilePath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log file: {_tweakLogFilePath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log failed: {ex.Message}";
        }
    }

    private void OpenAppLog()
    {
        try
        {
            if (!File.Exists(_appLogFilePath))
            {
                ExportStatusMessage = "No app log file yet. Run a tweak to generate one.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _appLogFilePath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened app log: {_appLogFilePath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open app log failed: {ex.Message}";
        }
    }

    private void CopyAppLogPath()
    {
        try
        {
            Clipboard.SetText(_appLogFilePath);
            ExportStatusMessage = $"Copied app log path: {_appLogFilePath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Copy app log path failed: {ex.Message}";
        }
    }

    private void CopyCsvPath()
    {
        try
        {
            Clipboard.SetText(_tweakLogFilePath);
            ExportStatusMessage = $"Copied log path: {_tweakLogFilePath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Copy log path failed: {ex.Message}";
        }
    }

    private void SetDetailsExpanded(bool isExpanded)
    {
        foreach (var item in Tweaks)
        {
            item.IsDetailsExpanded = isExpanded;
        }
    }

    private void SetBulkLock(bool isLocked)
    {
        foreach (var item in Tweaks)
        {
            item.IsBulkLocked = isLocked;
        }
    }
}
