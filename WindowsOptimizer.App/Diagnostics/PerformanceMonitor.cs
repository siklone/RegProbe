using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowsOptimizer.App.Diagnostics;

public sealed class PerformanceMonitor : INotifyPropertyChanged
{
    private readonly Queue<TimeSpan> _frameTimes = new(60);
    private DateTime _lastFrame = DateTime.UtcNow;
    private double _currentFps;
    private double _averageFrameTime;
    private bool _isMonitoring;

    public double CurrentFps
    {
        get => _currentFps;
        private set
        {
            if (Math.Abs(_currentFps - value) > 0.01)
            {
                _currentFps = value;
                OnPropertyChanged();
            }
        }
    }

    public double AverageFrameTime
    {
        get => _averageFrameTime;
        private set
        {
            if (Math.Abs(_averageFrameTime - value) > 0.01)
            {
                _averageFrameTime = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (_isMonitoring != value)
            {
                _isMonitoring = value;
                OnPropertyChanged();
            }
        }
    }

    public void Start()
    {
        if (IsMonitoring)
            return;

        IsMonitoring = true;
        _lastFrame = DateTime.UtcNow;
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!IsMonitoring)
            return;

        IsMonitoring = false;
        CompositionTarget.Rendering -= OnRendering;
        _frameTimes.Clear();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var frameTime = now - _lastFrame;
        _lastFrame = now;

        // Ignore extremely long frame times (e.g., when debugging or app was suspended)
        if (frameTime.TotalMilliseconds > 100)
            return;

        _frameTimes.Enqueue(frameTime);
        if (_frameTimes.Count > 60)
            _frameTimes.Dequeue();

        if (_frameTimes.Count > 0)
        {
            AverageFrameTime = _frameTimes.Average(ft => ft.TotalMilliseconds);
            CurrentFps = AverageFrameTime > 0 ? 1000.0 / AverageFrameTime : 0;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
