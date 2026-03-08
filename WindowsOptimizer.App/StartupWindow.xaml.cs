using System.Windows;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
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
}
