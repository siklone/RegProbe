using System;
using System.Windows;
using WindowsOptimizer.App.Diagnostics;

namespace WindowsOptimizer.App.Views;

public partial class HardwareDetailWindow : Window
{
    public HardwareDetailWindow()
    {
        InitializeComponent();
        // Log actual DataContext and Specs count when the window is shown (helps diagnose which VM is used).
        this.Loaded += (s, e) =>
        {
            var dc = this.DataContext;
            var specsCount = -1;
            try { specsCount = ((dynamic)dc)?.Specs?.Count ?? -1; } catch { }
            AppDiagnostics.Log($"[HardwareDetailWindow.Loaded] DataContext type: {dc?.GetType().Name}, Specs count: {specsCount}");
        };

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var ownerHeight = Owner?.ActualHeight ?? 0;
        MaxHeight = ownerHeight > 0
            ? ownerHeight * 0.85
            : SystemParameters.WorkArea.Height * 0.85;

        var scrollViewer = SpecsScrollViewer;
        if (scrollViewer != null && MaxHeight > 0)
        {
            scrollViewer.MaxHeight = MaxHeight - 180;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }
}
