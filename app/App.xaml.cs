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

        CrashReportService.Initialize(); // Initialize crash reporter first
        
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);

        SplashWindowHost? splashHost = null;
        try
        {
            AppDiagnostics.Log("[APP] OnStartup begin");

            var settings = await LoadSettingsAsync();
            AppDiagnostics.Log("[APP] Settings loaded");

            // Load saved theme preference before showing any windows.
            ApplyTheme(settings);
            ConfigureProcessRenderSettings();

            var showSplash = true;
            if (showSplash)
            {
                splashHost = new SplashWindowHost();
                await splashHost.ShowAsync();
                await Task.Delay(80);
            }

            var preloadProgress = showSplash
                ? new Progress<PreloadProgress>(progress => splashHost?.UpdatePreloadProgress(progress))
                : new Progress<PreloadProgress>(_ => { });

            var preloader = CreateStartupPreloader(preloadProgress);

            AppDiagnostics.Log("[APP] Calling PreloadAllAsync...");
            await Task.Run(() => preloader.RunAllAsync(CancellationToken.None));
            AppDiagnostics.Log("[APP] PreloadAllAsync done.");

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ConfigureWindowRenderSettings(mainWindow);

            mainWindow.Show();
            await Dispatcher.Yield(DispatcherPriority.Render);
            mainWindow.Activate();

            if (splashHost != null)
            {
                await splashHost.CompleteAndCloseAsync();
            }

            QueueDeferredStartupWork(settings, mainWindow);
        }
        catch (Exception ex)
        {
            splashHost?.CloseImmediately();
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

    private static PreloadManager CreateStartupPreloader(IProgress<PreloadProgress> progress)
    {
        var preloader = new PreloadManager(progress);
        RegisterCorePreloadTasks(preloader);
        return preloader;
    }

    private static PreloadManager CreateDeferredPreloader()
    {
        var preloader = new PreloadManager(new Progress<PreloadProgress>(_ => { }));
        preloader.RegisterTask("Analyze nohuto updates", async ct =>
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(12));

            var scanPaths = AppPaths.FromEnvironment();
            using var scanService = new NohutoRepoScanService(scanPaths);
            var result = await scanService.CheckAndAnalyzeAsync(timeoutCts.Token, TimeSpan.FromHours(2));
            AppDiagnostics.Log($"[NohutoScan] {result.Summary}");
        }, isCritical: false, priority: 40);

        return preloader;
    }

    private static void RegisterCorePreloadTasks(PreloadManager preloader)
    {
    }

    private void QueueDeferredStartupWork(AppSettings settings, MainWindow mainWindow)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            _ = RunDeferredPreloadAsync();

            if (settings.RunStartupScanOnLaunch && mainWindow.DataContext is MainViewModel mainVm)
            {
                _ = RunDeferredStartupScanAsync(mainVm);
            }
        }, DispatcherPriority.ContextIdle);
    }

    private static async Task RunDeferredPreloadAsync()
    {
        try
        {
            AppDiagnostics.Log("[APP] Starting deferred startup work");
            var preloader = CreateDeferredPreloader();
            await preloader.RunAllAsync(CancellationToken.None);
            AppDiagnostics.Log("[APP] Deferred startup work complete");
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Deferred startup work failed", ex);
        }
    }

    private static async Task RunDeferredStartupScanAsync(MainViewModel mainVm)
    {
        try
        {
            await Task.Delay(150);
            await mainVm.RunStartupScanAsync(null, CancellationToken.None);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Deferred startup scan failed", ex);
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
