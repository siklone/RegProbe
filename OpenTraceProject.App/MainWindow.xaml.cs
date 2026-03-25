using System;
using System.ComponentModel;
using System.Windows;
using OpenTraceProject.App.Services;
using OpenTraceProject.App.ViewModels;

namespace OpenTraceProject.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Global notification service.
    /// </summary>
    public NotificationService Notifications { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += MainWindow_Loaded;

        // Wire up notification host
        NotificationHost.NotificationService = Notifications;

        // Show welcome notification
        Notifications.ShowInfo("Open Trace Project is ready", "Welcome");
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ClampToWorkArea();
    }

    private void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        var minLeft = workArea.Left;
        var minTop = workArea.Top;
        var maxLeft = workArea.Right - ActualWidth;
        var maxTop = workArea.Bottom - ActualHeight;

        if (double.IsNaN(ActualWidth) || double.IsNaN(ActualHeight))
        {
            return;
        }

        if (Left < minLeft)
        {
            Left = minLeft;
        }
        else if (Left > maxLeft)
        {
            Left = maxLeft;
        }

        if (Top < minTop)
        {
            Top = minTop;
        }
        else if (Top > maxTop)
        {
            Top = maxTop;
        }
    }

    #region Custom Title Bar Handlers
    
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeRestore();
        }
        else
        {
            DragMove();
        }
    }
    
    private void TitleBar_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Required for DragMove to work properly
    }
    
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        MaximizeRestore();
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void MaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }
    
    #endregion

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnClosing(e);
    }
}
