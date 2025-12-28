using System.ComponentModel;
using System.Linq;
using System.Windows;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();

        // Set tray tooltip data context to MainViewModel
        if (TrayIcon.TrayToolTip is FrameworkElement tooltip)
        {
            tooltip.DataContext = DataContext;
        }
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

    private void MenuItem_Monitor_Click(object sender, RoutedEventArgs e)
    {
        NavigateToTab("monitor");
    }

    private void MenuItem_ScanNow_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to dashboard and trigger scan
        NavigateToTab("dashboard");

        if (DataContext is MainViewModel mainVm)
        {
            var dashboardNav = mainVm.NavigationItems.FirstOrDefault(n => n.Id == "dashboard");
            if (dashboardNav?.ViewModel is DashboardViewModel dashboard && dashboard.ScanAllCommand.CanExecute(null))
            {
                dashboard.ScanAllCommand.Execute(null);
            }
        }
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
