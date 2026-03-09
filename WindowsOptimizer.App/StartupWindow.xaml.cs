using System.Windows;
using System.Windows.Media.Animation;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
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
        StartGlowAnimation();
    }

    private void StartGlowAnimation()
    {
        if (!IsLoaded || TrackHost.ActualWidth <= 0)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            From = -88,
            To = TrackHost.ActualWidth,
            Duration = TimeSpan.FromSeconds(1.05),
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        GlowTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }
}
