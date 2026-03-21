using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _forceClose;

    /// <summary>
    /// Singleton instance of MainWindow (for notification access).
    /// </summary>
    public static MainWindow? Instance { get; private set; }

    /// <summary>
    /// Global notification service.
    /// </summary>
    public NotificationService Notifications { get; } = new();

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += MainWindow_Loaded;

        // Wire up notification host
        NotificationHost.NotificationService = Notifications;

        // Create tray icon programmatically
        TrayIcon.Icon = CreateTrayIcon();

        // Set tray tooltip data context to MainViewModel
        if (TrayIcon.TrayToolTip is FrameworkElement tooltip)
        {
            tooltip.DataContext = DataContext;
        }

        // Show welcome notification
        Notifications.ShowInfo("Windows Optimizer is ready", "Welcome");
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
    
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
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

    private static System.Drawing.Icon CreateTrayIcon()
    {
        // Create a simple 32x32 icon with a circle
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Draw filled circle with Nord theme colors
        using var fillBrush = new SolidBrush(Color.FromArgb(94, 129, 172)); // #5E81AC
        g.FillEllipse(fillBrush, 2, 2, 28, 28);

        // Draw border
        using var borderPen = new Pen(Color.FromArgb(136, 192, 208), 2); // #88C0D0
        g.DrawEllipse(borderPen, 2, 2, 28, 28);

        // Draw lightning bolt symbol in center
        using var symbolBrush = new SolidBrush(Color.FromArgb(236, 239, 244)); // #ECEFF4
        var lightning = new System.Drawing.Point[]
        {
            new(18, 6),
            new(12, 15),
            new(16, 15),
            new(14, 26),
            new(20, 14),
            new(16, 14),
        };
        g.FillPolygon(symbolBrush, lightning);

        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_forceClose)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
            TrayIcon.ShowBalloonTip(
                "Windows Optimizer",
                "App minimized to tray. Double-click to restore.",
                Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
        else
        {
            // Actually close - dispose tray icon
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
            TrayIcon.Dispose();
        }

        base.OnClosing(e);
    }

    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void MenuItem_Show_Click(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
    {
        _forceClose = true;
        Close();
    }

    private void MenuItem_Dashboard_Click(object sender, RoutedEventArgs e)
    {
        NavigateToTab("dashboard");
    }

    private void MenuItem_Tweaks_Click(object sender, RoutedEventArgs e)
    {
        NavigateToTab("tweaks");
    }

    private void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    private void NavigateToTab(string tabId)
    {
        ShowAndActivate();

        if (DataContext is MainViewModel mainVm)
        {
            var navItem = mainVm.NavigationItems.FirstOrDefault(n => n.Id == tabId);
            if (navItem != null)
            {
                mainVm.SelectedNavigationItem = navItem;
            }
        }
    }
}
