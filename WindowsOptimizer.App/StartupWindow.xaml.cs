using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

public partial class StartupWindow : Window
{
    private readonly Stopwatch _animationClock = new();
    private bool _isSweepAnimationActive;

    public StartupWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
    }

    public void UpdateScanProgress(StartupScanProgress progress)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateScanProgress(progress));
        }
    }

    public void UpdatePreloadProgress(PreloadProgress progress)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdatePreloadProgress(progress));
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartGlowAnimation();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGlowPositions();
    }

    private void StartGlowAnimation()
    {
        if (_isSweepAnimationActive)
        {
            return;
        }

        _animationClock.Restart();
        CompositionTarget.Rendering += OnCompositionTargetRendering;
        _isSweepAnimationActive = true;
        UpdateGlowPositions();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopGlowAnimation();
    }

    private void StopGlowAnimation()
    {
        if (!_isSweepAnimationActive)
        {
            return;
        }

        CompositionTarget.Rendering -= OnCompositionTargetRendering;
        _animationClock.Stop();
        _isSweepAnimationActive = false;
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        UpdateGlowPositions();
    }

    private void UpdateGlowPositions()
    {
        if (!IsLoaded || TrackHost.ActualWidth <= 0)
        {
            return;
        }

        var travelWidth = TrackHost.ActualWidth + GlowSegment.Width + 36;
        var loopProgress = (_animationClock.Elapsed.TotalMilliseconds % 980.0) / 980.0;
        var easedProgress = loopProgress < 0.5
            ? 2 * loopProgress * loopProgress
            : 1 - Math.Pow(-2 * loopProgress + 2, 2) / 2;

        var headLeft = -GlowSegment.Width + (travelWidth * easedProgress);
        var trailLeft = headLeft - 34;

        SweepCanvas.Width = TrackHost.ActualWidth;
        System.Windows.Controls.Canvas.SetLeft(GlowSegment, headLeft);
        System.Windows.Controls.Canvas.SetLeft(TrailSegment, trailLeft);
    }
}
