using System;
using System.ComponentModel;
using System.Windows;
using RegProbe.App.Views.Controls;

namespace RegProbe.App.Services;

public sealed class MainWindowLifecycleCoordinator
{
    private readonly Window _window;
    private readonly NotificationService _notifications = new();

    public MainWindowLifecycleCoordinator(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public void Initialize(NotificationHost notificationHost)
    {
        ArgumentNullException.ThrowIfNull(notificationHost);

        _window.Loaded += OnWindowLoaded;
        _window.Closing += OnWindowClosing;
        notificationHost.NotificationService = _notifications;
        _notifications.ShowInfo("RegProbe is ready", "Welcome");
    }

    public void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        var minLeft = workArea.Left;
        var minTop = workArea.Top;
        var maxLeft = workArea.Right - _window.ActualWidth;
        var maxTop = workArea.Bottom - _window.ActualHeight;

        if (double.IsNaN(_window.ActualWidth) || double.IsNaN(_window.ActualHeight))
        {
            return;
        }

        if (_window.Left < minLeft)
        {
            _window.Left = minLeft;
        }
        else if (_window.Left > maxLeft)
        {
            _window.Left = maxLeft;
        }

        if (_window.Top < minTop)
        {
            _window.Top = minTop;
        }
        else if (_window.Top > maxTop)
        {
            _window.Top = maxTop;
        }
    }

    public void DisposeDataContext()
    {
        if (_window.DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        ClampToWorkArea();
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        DisposeDataContext();
        _window.Loaded -= OnWindowLoaded;
        _window.Closing -= OnWindowClosing;
    }
}
