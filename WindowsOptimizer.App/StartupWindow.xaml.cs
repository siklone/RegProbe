using System;
using System.Windows;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
    }

    public void UpdateScanProgress(StartupScanProgress progress)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateScanProgress(progress));
            return;
        }

        var total = Math.Max(progress.Total, 0);
        var current = Math.Min(Math.Max(progress.Current, 0), total);

        ScanProgressBar.IsIndeterminate = total <= 0;
        if (total > 0)
        {
            ScanProgressBar.Maximum = total;
            ScanProgressBar.Value = current;
        }

        ScanStatusText.Text = total > 0
            ? $"Scanning tweaks {current}/{total}"
            : "Scanning tweaks...";

        if (!string.IsNullOrWhiteSpace(progress.CurrentName))
        {
            ScanDetailText.Text = $"Checking: {progress.CurrentName}";
        }
    }

    public void UpdatePreloadProgress(PreloadProgress progress)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdatePreloadProgress(progress));
            return;
        }

        var total = Math.Max(progress.Total, 0);
        var completed = Math.Min(Math.Max(progress.Completed, 0), total);

        ScanProgressBar.IsIndeterminate = total <= 0;
        if (total > 0)
        {
            ScanProgressBar.Maximum = total;
            ScanProgressBar.Value = completed;
        }

        ScanStatusText.Text = total > 0
            ? $"Loading {completed}/{total}"
            : "Loading...";

        var stateText = progress.State switch
        {
            PreloadState.Running => "Running",
            PreloadState.Completed => "Completed",
            PreloadState.Failed => "Failed",
            _ => "Waiting"
        };

        var detail = $"{stateText}: {progress.CurrentTask}";
        if (!string.IsNullOrWhiteSpace(progress.Message))
        {
            detail = $"{detail} - {progress.Message}";
        }

        ScanDetailText.Text = detail;
    }
}
