using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;
using OpenTraceProject.App.Services;
using OpenTraceProject.App.ViewModels;

namespace OpenTraceProject.App;

public partial class StartupWindow : Window
{
    private readonly TaskCompletionSource<bool> _closeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _isClosing;

    public StartupWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void UpdateScanProgress(StartupScanProgress progress)
    {
        var label = string.IsNullOrWhiteSpace(progress.CurrentName)
            ? "Running startup scan"
            : progress.CurrentName;

        SetProgress(progress.Percent, label);
    }

    public void UpdatePreloadProgress(PreloadProgress progress)
    {
        var label = !string.IsNullOrWhiteSpace(progress.Message)
            ? progress.Message
            : !string.IsNullOrWhiteSpace(progress.CurrentTask)
                ? progress.CurrentTask
                : "Loading research data and services";

        SetProgress(progress.Percentage, label);
    }

    public Task CompleteAndCloseAsync()
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.InvokeAsync(CompleteAndCloseAsync).Task.Unwrap();
        }

        if (_isClosing)
        {
            return _closeTcs.Task;
        }

        _isClosing = true;
        SetProgress(100, "Ready.");

        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(120)
        };

        fadeOut.Completed += (_, _) =>
        {
            _closeTcs.TrySetResult(true);
            Close();
        };

        BeginAnimation(OpacityProperty, fadeOut);
        return _closeTcs.Task;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyVersionText();
        Opacity = 0;
        LoadProgress.Value = 0;
        LoadLabel.Text = "Loading research data and services";

        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(120)
        };

        BeginAnimation(OpacityProperty, fadeIn);
    }

    private void ApplyVersionText()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var fallbackVersion = assembly.GetName().Version?.ToString(3) ?? "0.1.0";
        var cleanVersion = string.IsNullOrWhiteSpace(productVersion)
            ? fallbackVersion
            : productVersion.Split('+')[0];

        VersionText.Text = cleanVersion;
    }

    private void SetProgress(double percent, string label)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => SetProgress(percent, label));
            return;
        }

        LoadProgress.Value = Math.Clamp(percent, 0d, 100d);

        if (!string.IsNullOrWhiteSpace(label))
        {
            LoadLabel.Text = label;
        }
    }
}
