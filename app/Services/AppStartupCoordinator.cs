using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RegProbe.App.Diagnostics;
using RegProbe.App.ViewModels;
using RegProbe.Infrastructure;

namespace RegProbe.App.Services;

public sealed class AppStartupCoordinator
{
    private readonly MainWindowFactory _windowFactory = new();

    public async Task<MainWindow> CreateAndShowMainWindowAsync(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        AppDiagnostics.Log("[APP] OnStartup begin");

        var settings = await LoadSettingsAsync();
        AppDiagnostics.Log("[APP] Settings loaded");

        ApplyTheme(settings);
        ConfigureProcessRenderSettings();

        var mainWindow = _windowFactory.CreateMainWindow();
        ConfigureWindowRenderSettings(mainWindow);

        application.MainWindow = mainWindow;
        mainWindow.Show();
        await Dispatcher.Yield(DispatcherPriority.Render);
        mainWindow.Activate();
        return mainWindow;
    }

    public MainWindow CreateRecoveryWindow(Window? existingMainWindow)
    {
        if (existingMainWindow is MainWindow existingWindow)
        {
            return existingWindow;
        }

        return _windowFactory.CreateRecoveryWindow();
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

    private static void ConfigureProcessRenderSettings()
    {
        try
        {
            if (ShouldForceSoftwareRendering())
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                AppDiagnostics.Log("[APP] Software rendering enabled for compatibility.");
            }

            System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(System.Windows.Media.Animation.Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 60 });
        }
        catch (Exception ex)
        {
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
}
