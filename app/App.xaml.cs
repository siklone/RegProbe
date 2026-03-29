using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RegProbe.App.Diagnostics;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;
using RegProbe.Infrastructure;

namespace RegProbe.App;

public partial class App : Application
{
    private static int _dispatcherErrorDialogShown;
    private SingleInstanceManager? _singleInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // 1. Single instance check (before anything else)
        _singleInstance = new SingleInstanceManager();
        if (!_singleInstance.TryAcquire())
        {
            // Another instance is running - exit immediately
            Shutdown(0);
            return;
        }
        _singleInstance.ArgumentsReceived += OnArgumentsReceived;

        CrashReportService.Initialize();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);

        try
        {
            AppDiagnostics.Log("[APP] OnStartup begin");

            var settings = await LoadSettingsAsync();
            AppDiagnostics.Log("[APP] Settings loaded");

            // Load saved theme preference before showing any windows.
            ApplyTheme(settings);
            ConfigureProcessRenderSettings();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ConfigureWindowRenderSettings(mainWindow);

            mainWindow.Show();
            await Dispatcher.Yield(DispatcherPriority.Render);
            mainWindow.Activate();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Startup sequence failed", ex);
            _ = CrashReportService.LogCrashAsync(ex, "Startup", true);


            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            MainWindow.Opacity = 1;
            MainWindow.Show();
            MainWindow.Activate();
        }
    }

    private static async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            var paths = AppPaths.FromEnvironment();
            var settingsStore = new SettingsStore(paths);
            return await settingsStore.LoadAsync(CancellationToken.None);
        }
        catch
        {
            return new AppSettings();
        }
    }

    private static void ApplyTheme(AppSettings settings)
    {
        var themeManager = new ThemeManager();
        var palettes = new[]
        {
            ThemeManager.Nord,
            ThemeManager.ElectricPurple,
            ThemeManager.SunsetOrange,
            ThemeManager.CyberGreen,
            ThemeManager.RubyRed
        };

        var selectedPalette = System.Linq.Enumerable.FirstOrDefault(palettes, p => p.Name == settings.Theme) 
                              ?? ThemeManager.Nord;

        themeManager.ApplyTheme(selectedPalette);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppDiagnostics.LogException("DispatcherUnhandledException", e.Exception);
        _ = CrashReportService.LogCrashAsync(e.Exception, "DispatcherUnhandledException", false);

        if (Interlocked.Exchange(ref _dispatcherErrorDialogShown, 1) == 0)
        {
            try
            {
                MessageBox.Show(
                    $"Unexpected error: {e.Exception.Message}\n\nDetails were written to the application logs and CrashLogs.",
                    "RegProbe",
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

    private static void ConfigureProcessRenderSettings()
    {
        try
        {
            if (ShouldForceSoftwareRendering())
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                AppDiagnostics.Log("[APP] Software rendering enabled for compatibility.");
            }

            // Keep animation timing predictable across host and VM renderers.
            System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(System.Windows.Media.Animation.Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 60 });
        }
        catch (Exception ex)
        {
            // Non-critical, continue without optimization
            AppDiagnostics.LogException("Render settings configuration failed", ex);
        }
    }

    private static void ConfigureWindowRenderSettings(Window window)
    {
        try
        {
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(
                window,
                System.Windows.Media.BitmapScalingMode.LowQuality);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Window render settings configuration failed", ex);
        }
    }

    private static bool ShouldForceSoftwareRendering()
    {
        var overrideValue = Environment.GetEnvironmentVariable("REGPROBE_FORCE_SOFTWARE_RENDERING");
        if (!string.IsNullOrWhiteSpace(overrideValue))
        {
            return overrideValue.Equals("1", StringComparison.OrdinalIgnoreCase)
                   || overrideValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            return Process.GetProcessesByName("vmtoolsd").Length > 0
                   || Process.GetProcessesByName("vm3dservice").Length > 0;
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("VM compatibility detection failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Handles command-line arguments received from a second instance via IPC.
    /// </summary>
    private void OnArgumentsReceived(object? sender, string[] args)
    {
        // Example: Navigate to specific tab based on args
        if (MainWindow?.DataContext is MainViewModel mainVm)
        {
            foreach (var arg in args)
            {
                if (arg.Equals("--tweaks", StringComparison.OrdinalIgnoreCase))
                {
                    // Navigate to Tweaks tab
                    AppDiagnostics.Log("[App] Navigating to Tweaks via IPC arg");
                }
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
