using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RegProbe.App.Services;

public class CrashReportService
{
    private static string? _remoteEndpoint;
    private static bool _isInitialized;

    public static void Initialize(string? remoteEndpoint = null)
    {
        if (_isInitialized) return;

        _remoteEndpoint = remoteEndpoint;
        CrashReportStore.EnsureDirectory();

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _isInitialized = true;
        Debug.WriteLine("CrashReportService initialized");
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _ = LogCrashAsync(exception, "UnhandledException", e.IsTerminating);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _ = LogCrashAsync(e.Exception, "UnobservedTaskException", false);
        e.SetObserved();
    }

    public static async Task LogCrashAsync(Exception? exception, string source, bool isTerminating)
    {
        if (exception == null) return;

        var report = CrashReportFactory.Create(exception, source, isTerminating);

        await CrashReportStore.SaveAsync(report);

        if (!string.IsNullOrEmpty(_remoteEndpoint))
        {
            await CrashReportSender.SendAsync(report, _remoteEndpoint);
        }
    }

    public static IEnumerable<string> GetCrashReportFiles()
    {
        return CrashReportStore.GetReportFiles();
    }
}
