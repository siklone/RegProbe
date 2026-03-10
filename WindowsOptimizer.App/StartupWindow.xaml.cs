using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

public partial class StartupWindow : Window
{
    private static readonly string[] PhaseLabels =
    {
        "Initializing system scan...",
        "Loading configuration...",
        "Checking hardware...",
        "Verifying drivers..."
    };

    private static readonly SolidColorBrush IdleLabelBrush = CreateBrush(Color.FromRgb(0x58, 0x64, 0x73));
    private static readonly SolidColorBrush ReadyLabelBrush = CreateBrush(Color.FromRgb(0x39, 0xFF, 0x8A));

    private readonly Stopwatch _timelineClock = new();
    private readonly DispatcherTimer _phaseDelayTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _phaseTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _minimumTimelineTimer = new() { Interval = TimeSpan.FromSeconds(3.8) };
    private readonly TaskCompletionSource<bool> _closeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Random _random = new(5070);

    private Storyboard? _sceneStoryboard;
    private Storyboard? _shimmerStoryboard;
    private bool _isRenderingActive;
    private bool _isReadyRequested;
    private bool _minimumTimelineElapsed;
    private bool _isExitStarted;
    private bool _phasesStarted;
    private bool _sceneStarted;
    private int _phaseIndex;
    private double _targetProgressRatio;
    private double _displayedProgressRatio;

    public StartupWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        ContentRendered += OnContentRendered;
        Unloaded += OnUnloaded;

        _phaseDelayTimer.Tick += OnPhaseDelayTick;
        _phaseTimer.Tick += OnPhaseTimerTick;
        _minimumTimelineTimer.Tick += OnMinimumTimelineTick;
    }

    public void UpdateScanProgress(StartupScanProgress progress)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => UpdateScanProgress(progress), DispatcherPriority.Background);
            return;
        }

        _targetProgressRatio = Math.Max(_targetProgressRatio, Math.Clamp(progress.Percent / 100d, 0d, 1d));
    }

    public void UpdatePreloadProgress(PreloadProgress progress)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => UpdatePreloadProgress(progress), DispatcherPriority.Background);
            return;
        }

        _targetProgressRatio = Math.Max(_targetProgressRatio, Math.Clamp(progress.Percentage / 100d, 0d, 1d));
    }

    public Task CompleteAndCloseAsync()
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.InvokeAsync(CompleteAndCloseAsync).Task.Unwrap();
        }

        _isReadyRequested = true;
        _targetProgressRatio = 1d;
        TryStartExit();
        return _closeTcs.Task;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyVersionText();
        GenerateParticles();
        SceneRoot.Opacity = 1;
        RenderOptions.SetBitmapScalingMode(SceneRoot, BitmapScalingMode.LowQuality);
        TextOptions.SetTextFormattingMode(SceneRoot, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(SceneRoot, TextRenderingMode.ClearType);
    }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        if (_sceneStarted)
        {
            return;
        }

        _sceneStarted = true;

        RenderOptions.ProcessRenderMode = RenderMode.Default;

        // Let the first frame settle before the heavy splash timeline starts.
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        await Task.Delay(16);

        _sceneStoryboard = (Storyboard)Resources["StartupSceneStoryboard"];
        _sceneStoryboard.SetValue(Timeline.DesiredFrameRateProperty, 30);
        _sceneStoryboard.Begin(this, true);

        _timelineClock.Start();
        StartRenderLoop();

        _phaseDelayTimer.Start();
        _minimumTimelineTimer.Start();
    }

    private void ApplyVersionText()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var fallbackVersion = assembly.GetName().Version?.ToString(3) ?? "0.1.0";
        var cleanVersion = string.IsNullOrWhiteSpace(productVersion)
            ? fallbackVersion
            : productVersion.Split('+')[0];

        VersionText.Text = $"v{cleanVersion} - Preview Build";
    }

    private void GenerateParticles()
    {
        ParticleCanvas.Children.Clear();

        var width = Math.Max(ActualWidth, Width);
        var height = Math.Max(ActualHeight, Height);
        var colors = new[]
        {
            Color.FromArgb(110, 0xFF, 0x3B, 0x3B),
            Color.FromArgb(88, 0x00, 0xD4, 0xFF),
            Color.FromArgb(76, 0xFF, 0x8C, 0x42)
        };

        for (var index = 0; index < 6; index++)
        {
            var size = 2d + (_random.NextDouble() * 3d);
            var particle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = CreateBrush(colors[index % colors.Length]),
                Opacity = 0.18 + (_random.NextDouble() * 0.18)
            };

            Canvas.SetLeft(particle, width * (0.10 + (_random.NextDouble() * 0.80)));
            Canvas.SetTop(particle, height * (0.10 + (_random.NextDouble() * 0.80)));
            ParticleCanvas.Children.Add(particle);
        }
    }

    private void StartRenderLoop()
    {
        if (_isRenderingActive)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _isRenderingActive = true;
    }

    private void StopRenderLoop()
    {
        if (!_isRenderingActive)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _isRenderingActive = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var elapsedSeconds = _timelineClock.Elapsed.TotalSeconds;
        var timelineFloor = elapsedSeconds switch
        {
            < 2.0 => 0d,
            < 3.2 => Math.Clamp((elapsedSeconds - 2.0) / 1.2, 0d, 1d) * 0.86d,
            _ => 0.86d
        };

        var target = _isReadyRequested
            ? 1d
            : Math.Max(timelineFloor, _targetProgressRatio * 0.92d);

        _displayedProgressRatio += (target - _displayedProgressRatio) * 0.12d;
        _displayedProgressRatio = Math.Clamp(_displayedProgressRatio, 0d, 1d);

        LoadFillScale.ScaleX = _displayedProgressRatio;
    }

    private void OnPhaseDelayTick(object? sender, EventArgs e)
    {
        _phaseDelayTimer.Stop();
        _phasesStarted = true;
        _phaseIndex = 0;

        TransitionLoadLabel(PhaseLabels[_phaseIndex]);
        StartShimmer();
        _phaseTimer.Start();
    }

    private void OnPhaseTimerTick(object? sender, EventArgs e)
    {
        if (!_phasesStarted)
        {
            return;
        }

        if (_phaseIndex < PhaseLabels.Length - 1)
        {
            _phaseIndex++;
            TransitionLoadLabel(PhaseLabels[_phaseIndex]);
            return;
        }

        if (_isReadyRequested)
        {
            SetReadyLabel();
            _phaseTimer.Stop();
            return;
        }

        TransitionLoadLabel("Finishing startup...");
    }

    private void OnMinimumTimelineTick(object? sender, EventArgs e)
    {
        _minimumTimelineTimer.Stop();
        _minimumTimelineElapsed = true;
        TryStartExit();
    }

    private void TryStartExit()
    {
        if (!_isReadyRequested || !_minimumTimelineElapsed || _isExitStarted)
        {
            return;
        }

        SetReadyLabel();
        _isExitStarted = true;
        StartExitAnimation();
    }

    private void StartShimmer()
    {
        if (_shimmerStoryboard != null)
        {
            return;
        }

        _shimmerStoryboard = (Storyboard)Resources["ShimmerStoryboard"];
        _shimmerStoryboard.SetValue(Timeline.DesiredFrameRateProperty, 30);
        _shimmerStoryboard.Begin(this, true);
    }

    private void SetReadyLabel()
    {
        LoadLabel.BeginAnimation(OpacityProperty, null);
        LoadLabel.Text = "Ready.";
        LoadLabel.Foreground = ReadyLabelBrush;
        LoadLabel.Opacity = 1;
    }

    private void TransitionLoadLabel(string text)
    {
        if (LoadLabel.Text == text)
        {
            return;
        }

        LoadLabel.Foreground = IdleLabelBrush;

        var fadeOut = new DoubleAnimation
        {
            To = 0.18,
            Duration = TimeSpan.FromMilliseconds(120),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        fadeOut.Completed += (_, _) =>
        {
            LoadLabel.Text = text;

            var fadeIn = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(160),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            LoadLabel.BeginAnimation(OpacityProperty, fadeIn);
        };

        LoadLabel.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void StartExitAnimation()
    {
        StopRenderLoop();
        _phaseDelayTimer.Stop();
        _phaseTimer.Stop();
        _minimumTimelineTimer.Stop();
        _shimmerStoryboard?.Stop(this);

        var opacity = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(360),
            EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn }
        };

        var scale = new DoubleAnimation
        {
            From = 1,
            To = 1.03,
            Duration = TimeSpan.FromMilliseconds(360),
            EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn }
        };

        scale.Completed += (_, _) =>
        {
            _closeTcs.TrySetResult(true);
            Close();
        };

        BeginAnimation(OpacityProperty, opacity);
        SceneScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        SceneScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRenderLoop();
        _phaseDelayTimer.Stop();
        _phaseTimer.Stop();
        _minimumTimelineTimer.Stop();
        _timelineClock.Stop();
        _sceneStoryboard?.Stop(this);
        _shimmerStoryboard?.Stop(this);
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
