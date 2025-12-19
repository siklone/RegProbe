using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
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
    private string _exportStatusMessage = "Logs are ready to export.";
    private bool _isExporting;

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

        _exportLogsCommand = new RelayCommand(_ => _ = ExportLogsAsync(), _ => !IsExporting);
    }

    public string Title => "Tweaks";

    public ObservableCollection<TweakItemViewModel> Tweaks { get; }

    public ICommand ExportLogsCommand => _exportLogsCommand;

    public string ExportStatusMessage
    {
        get => _exportStatusMessage;
        private set => SetProperty(ref _exportStatusMessage, value);
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
}
