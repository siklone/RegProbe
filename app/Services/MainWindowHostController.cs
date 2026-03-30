using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using RegProbe.App.ViewModels;
using RegProbe.App.Views.Controls;

namespace RegProbe.App.Services;

public sealed class MainWindowHostController
{
    private readonly Window _window;

    public MainWindowHostController(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        MinimizeCommand = new RelayCommand(_ => Minimize(_window));
        ToggleMaximizeRestoreCommand = new RelayCommand(_ => ToggleMaximizeRestore(_window));
        CloseCommand = new RelayCommand(_ => Close(_window));
    }

    public NotificationService Notifications { get; } = new();

    public ICommand MinimizeCommand { get; }

    public ICommand ToggleMaximizeRestoreCommand { get; }

    public ICommand CloseCommand { get; }

    public void Initialize(NotificationHost notificationHost)
    {
        ArgumentNullException.ThrowIfNull(notificationHost);

        _window.Loaded += OnWindowLoaded;
        _window.Closing += OnWindowClosing;
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

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        ClampToWorkArea(_window);
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        DisposeDataContext(_window.DataContext);
        _window.Loaded -= OnWindowLoaded;
        _window.Closing -= OnWindowClosing;
    }
}
