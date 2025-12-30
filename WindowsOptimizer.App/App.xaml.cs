using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WindowsOptimizer.App.Diagnostics;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.Infrastructure;
using System.Threading;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App;

public partial class App : Application
{
    private static int _dispatcherErrorDialogShown;

    protected override async void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Load saved theme preference
        InitializeThemeAsync();

        base.OnStartup(e);

        StartupWindow? splash = null;
        try
        {
            splash = new StartupWindow();
            splash.Show();

            var mainWindow = new MainWindow
            {
                Visibility = Visibility.Hidden
            };
            MainWindow = mainWindow;

            if (mainWindow.DataContext is MainViewModel mainVm)
            {
                await mainVm.RunStartupScanAsync();
            }

            splash.Close();
            mainWindow.Show();
            mainWindow.Activate();
        }
        catch (Exception ex)
        {
            splash?.Close();
            AppDiagnostics.LogException("Startup sequence failed", ex);

            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            MainWindow.Show();
        }
    }

    private static async void InitializeThemeAsync()
    {
        try
        {
            var paths = AppPaths.FromEnvironment();
            var settingsStore = new SettingsStore(paths);
            var settings = await settingsStore.LoadAsync(CancellationToken.None);

            var theme = settings.Theme == "Light" ? AppTheme.Light : AppTheme.Dark;

            // Force apply theme on startup if not dark (default)
            if (theme == AppTheme.Light)
            {
                ThemeManager.SetTheme(AppTheme.Light, force: true);
            }
            else
            {
                ThemeManager.Initialize(theme);
            }
        }
        catch
        {
            // Use default dark theme on failure
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppDiagnostics.LogException("DispatcherUnhandledException", e.Exception);

        if (Interlocked.Exchange(ref _dispatcherErrorDialogShown, 1) == 0)
        {
            try
            {
                MessageBox.Show(
                    $"Unexpected error: {e.Exception.Message}\n\nDetails were written to %TEMP%\\WindowsOptimizer_Debug.log",
                    "Windows Optimizer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
            }
        }

        // Keep the app alive so the user can export logs / continue using other pages.
        e.Handled = true;
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppDiagnostics.LogException("AppDomain.UnhandledException", ex);
        }
        else
        {
            AppDiagnostics.Log($"AppDomain.UnhandledException: {e.ExceptionObject}");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppDiagnostics.LogException("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }
}
