using System;
using System.Windows;
using System.Windows.Controls;

namespace RegProbe.App.Views;

public partial class HardwareDetailWindow : Window
{
    public HardwareDetailWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowBounds();
        UpdateScrollableRegion();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScrollableRegion();
    }

    private void ApplyWindowBounds()
    {
        var workArea = SystemParameters.WorkArea;
        var ownerWidth = Owner?.ActualWidth ?? 0;
        var ownerHeight = Owner?.ActualHeight ?? 0;

        MaxWidth = Math.Min(workArea.Width * 0.92, 1100);
        MaxHeight = Math.Min(workArea.Height * 0.92, 920);

        var targetWidth = ownerWidth > 0 ? ownerWidth * 0.62 : workArea.Width * 0.56;
        var targetHeight = ownerHeight > 0 ? ownerHeight * 0.84 : workArea.Height * 0.8;

        Width = Math.Clamp(targetWidth, MinWidth, MaxWidth);
        Height = Math.Clamp(targetHeight, MinHeight, MaxHeight);
    }

    private void UpdateScrollableRegion()
    {
        if (SpecsScrollViewer is not ScrollViewer scrollViewer)
        {
            return;
        }

        var headerHeight = HeaderCard?.ActualHeight ?? 0;
        var tabsHeight = HasDeviceTabsBorder?.ActualHeight ?? 0;
        var reservedHeight = headerHeight + tabsHeight + 96;
        var viewportTarget = ActualHeight > 0 ? ActualHeight : Height;
        scrollViewer.MaxHeight = Math.Max(320, viewportTarget - reservedHeight);
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
