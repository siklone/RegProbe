using System;
using System.Windows;
using System.Windows.Input;
using RegProbe.App.Views.Controls;

namespace RegProbe.App.Services;

public sealed class MainWindowHostController
{
    public NotificationService Notifications { get; } = new();

    public void Initialize(Window window, NotificationHost notificationHost)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(notificationHost);

        notificationHost.NotificationService = Notifications;
        Notifications.ShowInfo("RegProbe is ready", "Welcome");
    }

    public void ClampToWorkArea(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var workArea = SystemParameters.WorkArea;
        var minLeft = workArea.Left;
        var minTop = workArea.Top;
        var maxLeft = workArea.Right - window.ActualWidth;
        var maxTop = workArea.Bottom - window.ActualHeight;

        if (double.IsNaN(window.ActualWidth) || double.IsNaN(window.ActualHeight))
        {
            return;
        }

        if (window.Left < minLeft)
        {
            window.Left = minLeft;
        }
        else if (window.Left > maxLeft)
        {
            window.Left = maxLeft;
        }

        if (window.Top < minTop)
        {
            window.Top = minTop;
        }
        else if (window.Top > maxTop)
        {
            window.Top = maxTop;
        }
    }

    public void HandleTitleBarMouseLeftButtonDown(Window window, MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(e);

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore(window);
            return;
        }

        window.DragMove();
    }

    public void ToggleMaximizeRestore(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    public void Minimize(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.WindowState = WindowState.Minimized;
    }

    public void Close(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Close();
    }

    public void DisposeDataContext(object? dataContext)
    {
        if (dataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
