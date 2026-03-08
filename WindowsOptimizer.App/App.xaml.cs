using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WindowsOptimizer.App.Diagnostics;
using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Services.Hardware;
using WindowsOptimizer.App.Services.OsDetection;
using WindowsOptimizer.App.ViewModels;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Hardware;

namespace WindowsOptimizer.App;

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

        // GPU hardware acceleration settings for smoother UI
        // Render settings configured later after MainWindow is created

        StartupWindow? splash = null;
        try
        {
            AppDiagnostics.Log("[APP] OnStartup begin");

            var settings = await LoadSettingsAsync();
            AppDiagnostics.Log("[APP] Settings loaded");

            // Load saved theme preference before showing any windows.
            ApplyTheme(settings);
            UiPreferences.Current.EnableCardShadows = settings.EnableCardShadows;

            var showSplash = settings.RunStartupScanOnLaunch;
            if (showSplash)
            {
                splash = new StartupWindow();
                splash.Show();
                await Dispatcher.Yield(DispatcherPriority.Render);
            }

            var preloadProgress = showSplash
                ? new Progress<PreloadProgress>(progress => splash?.UpdatePreloadProgress(progress))
                : new Progress<PreloadProgress>(_ => { });

            var preloader = CreateStartupPreloader(preloadProgress);

            AppDiagnostics.Log("[APP] Calling PreloadAllAsync...");
            await preloader.RunAllAsync(CancellationToken.None);
            AppDiagnostics.Log("[APP] PreloadAllAsync done.");

            var mainWindow = new MainWindow
            {
                Visibility = Visibility.Hidden
            };
            MainWindow = mainWindow;

            // GPU hardware acceleration settings
            ConfigureRenderSettings();

            mainWindow.Show();
            mainWindow.Activate();
            splash?.Close();

            QueueDeferredStartupWork(settings, mainWindow);
        }
        catch (Exception ex)
        {
            splash?.Close();
            AppDiagnostics.LogException("Startup sequence failed", ex);
            _ = CrashReportService.LogCrashAsync(ex, "Startup", true);


            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            MainWindow.Show();
        }
    }

    private static PreloadManager CreateStartupPreloader(IProgress<PreloadProgress> progress)
    {
        var preloader = new PreloadManager(progress);
        RegisterCorePreloadTasks(preloader);
        preloader.RegisterTask("Load hardware knowledge", async ct =>
        {
            await HardwareDbLoader.LoadAllAsync(ct);
        }, isCritical: true, priority: 90);
        return preloader;
    }

    private static PreloadManager CreateDeferredPreloader()
    {
        var preloader = new PreloadManager(new Progress<PreloadProgress>(_ => { }));
        preloader.RegisterTask("Hardware database", async ct =>
        {
            await HardwareDatabase.InitializeAsync(ct);
            if (HardwareDatabase.TryGetInstance(out var database) && database != null)
            {
                await database.CheckForUpdatesAsync(ct);
            }
        }, isCritical: false, priority: 90);

        preloader.RegisterTask("Start hardware metric cache", async ct =>
        {
            await HardwarePreloadService.Instance.PreloadAsync(ct);
        }, isCritical: false, priority: 70);

        preloader.RegisterTask("Warm hardware icon cache", _ =>
        {
            HardwareIconResolver.PreloadIcons();
            return Task.CompletedTask;
        }, isCritical: false, priority: 69);

        preloader.RegisterTask("Parallel hardware preloader", async ct =>
        {
            try
            {
                var hd = new HardwareDataPreloader(MetricCacheService.Instance, AppServices.OsDetectionService, AppServices.MotherboardProvider);
                await hd.PreloadAllAsync(new Progress<PreloadProgress>(_ => { }));
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogException("Parallel hardware preloader failed", ex);
            }
        }, isCritical: false, priority: 68);

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
        preloader.RegisterTask("Initialize threading", _ =>
        {
            AppServices.InitializeMetricThreading(action =>
            {
                var dispatcher = Current?.Dispatcher;
                if (dispatcher == null)
                {
                    action();
                }
                else
                {
                    dispatcher.InvokeAsync(action, DispatcherPriority.DataBind);
                }
            });
            return Task.CompletedTask;
        }, isCritical: true, priority: 100);
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
            ThemeManager.CyberGreen
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

    /// <summary>
    /// Configures WPF rendering settings for optimal GPU hardware acceleration and smooth animations.
    /// </summary>
    private static void ConfigureRenderSettings()
    {
        try
        {
            // Use high-quality but efficient bitmap scaling for images
            if (Current.MainWindow != null)
            {
                System.Windows.Media.RenderOptions.SetBitmapScalingMode(
                    Current.MainWindow, 
                    System.Windows.Media.BitmapScalingMode.LowQuality);
            }

            // Set animation frame rate (default is 60fps, adjust for performance)
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
                if (arg.Equals("--monitor", StringComparison.OrdinalIgnoreCase))
                {
                    // Navigate to Monitor tab
                    AppDiagnostics.Log("[App] Navigating to Monitor via IPC arg");
                }
                else if (arg.Equals("--tweaks", StringComparison.OrdinalIgnoreCase))
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
        AppServices.Dispose();
        base.OnExit(e);
    }
}
