using System;
using System.Windows;
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
}
