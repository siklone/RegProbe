using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using OpenTraceProject.App.Services;
using OpenTraceProject.App.ViewModels;

namespace OpenTraceProject.App;

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
            ? "Scanning system..."
            : progress.CurrentName;

        SetProgress(progress.Percent, label);
    }

    public void UpdatePreloadProgress(PreloadProgress progress)
    {
        var label = !string.IsNullOrWhiteSpace(progress.Message)
            ? progress.Message
            : !string.IsNullOrWhiteSpace(progress.CurrentTask)
                ? progress.CurrentTask
                : "Loading services...";

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

        var remaining = TimeSpan.FromMilliseconds(500) - (DateTime.UtcNow - _shownAtUtc);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining);
        }

        Close();
        await _closeTcs.Task;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyVersionText();
        LoadProgress.Value = 0;
        LoadLabel.Text = "Initializing...";
    }

    private void ApplyVersionText()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var fallbackVersion = assembly.GetName().Version?.ToString(3) ?? "0.1.0";
        var cleanVersion = string.IsNullOrWhiteSpace(productVersion)
            ? fallbackVersion
            : productVersion.Split('+')[0];

        VersionText.Text = $"v{cleanVersion}";
    }

    private void SetProgress(double percent, string label)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => SetProgress(percent, label), DispatcherPriority.Background);
            return;
        }

        LoadProgress.Value = Math.Clamp(percent, 0d, 100d);

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
