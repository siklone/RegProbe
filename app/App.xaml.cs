using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using RegProbe.App.Diagnostics;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;
using WpfApplication = System.Windows.Application;

namespace RegProbe.App;

public partial class App : WpfApplication
{
    private static int _dispatcherErrorDialogShown;
    private readonly AppStartupCoordinator _startupCoordinator = new();
    private SingleInstanceManager? _singleInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new SingleInstanceManager();
        if (!_singleInstance.TryAcquire())
        {
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
            await _startupCoordinator.CreateAndShowMainWindowAsync(this);
            ApplyNavigationArguments(e.Args);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Startup sequence failed", ex);
            _ = CrashReportService.LogCrashAsync(ex, "Startup", true);

            var recoveryWindow = _startupCoordinator.CreateRecoveryWindow(MainWindow);
            MainWindow = recoveryWindow;
            recoveryWindow.Show();
            recoveryWindow.Activate();
        }
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

        // Keep the shell alive long enough for users to export logs or switch to recovery paths.
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

    private void OnArgumentsReceived(object? sender, string[] args)
    {
        ApplyNavigationArguments(args);
    }

    private void ApplyNavigationArguments(string[] args)
    {
        if (MainWindow?.DataContext is not MainViewModel mainViewModel)
        {
            return;
        }

        foreach (var arg in args)
        {
            if (arg.Equals("--tweaks", StringComparison.OrdinalIgnoreCase))
            {
                AppDiagnostics.Log("[App] Navigating to Tweaks via arg");
                mainViewModel.ShowConfigurationCommand.Execute(null);
                continue;
            }

            if (arg.Equals("--recovery", StringComparison.OrdinalIgnoreCase))
            {
                AppDiagnostics.Log("[App] Navigating to Recovery via arg");
                mainViewModel.ShowRepairsCommand.Execute(null);
                continue;
            }

            if (arg.Equals("--diagnostics", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("--about", StringComparison.OrdinalIgnoreCase))
            {
                AppDiagnostics.Log("[App] Navigating to Diagnostics via arg");
                mainViewModel.ShowAboutCommand.Execute(null);
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
