using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;

namespace RegProbe.App;

public partial class StartupWindow : Window
{
    private readonly TaskCompletionSource<bool> _closeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DateTime _shownAtUtc = DateTime.UtcNow;
    private bool _isClosing;

    public StartupWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void UpdateScanProgress(StartupScanProgress progress)
    {
        var label = string.IsNullOrWhiteSpace(progress.CurrentName)
            ? "Scanning"
            : progress.CurrentName;

        SetProgress(progress.Percent, label);
    }

    public void UpdatePreloadProgress(PreloadProgress progress)
    {
        var label = !string.IsNullOrWhiteSpace(progress.Message)
            ? progress.Message
            : !string.IsNullOrWhiteSpace(progress.CurrentTask)
                ? progress.CurrentTask
                : "Loading";

        SetProgress(progress.Percentage, label);
    }

    public async Task CompleteAndCloseAsync()
    {
        if (!Dispatcher.CheckAccess())
        {
            await Dispatcher.InvokeAsync(CompleteAndCloseAsync).Task.Unwrap();
            return;
        }

        if (_isClosing)
        {
            await _closeTcs.Task;
            return;
        }

        _isClosing = true;
        SetProgress(100, "Ready");

        var remaining = TimeSpan.FromMilliseconds(320) - (DateTime.UtcNow - _shownAtUtc);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining);
        }

        Close();
        await _closeTcs.Task;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadLabel.Text = "Starting...";
    }

    private void SetProgress(double percent, string label)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => SetProgress(percent, label), DispatcherPriority.Background);
            return;
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            LoadLabel.Text = label;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _closeTcs.TrySetResult(true);
    }
}
