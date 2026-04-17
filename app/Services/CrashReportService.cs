using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RegProbe.App.Services;

/// <summary>
/// Crash reporting service for logging unhandled exceptions.
/// Stores crash reports locally and optionally sends to remote endpoint.
/// </summary>
public class CrashReportService
{
    private static string? _remoteEndpoint;
    private static bool _isInitialized;

    /// <summary>
    /// Initialize crash reporting. Call this once at app startup.
    /// </summary>
    public static void Initialize(string? remoteEndpoint = null)
    {
        if (_isInitialized) return;

        _remoteEndpoint = remoteEndpoint;
        CrashReportStore.EnsureDirectory();

        // Hook into unhandled exceptions
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

    /// <summary>
    /// Log a crash report.
    /// </summary>
    public static async Task LogCrashAsync(Exception? exception, string source, bool isTerminating)
    {
        if (exception == null) return;

        var report = CrashReportFactory.Create(exception, source, isTerminating);

        // Save locally
        await CrashReportStore.SaveAsync(report);

        // Send to remote if configured
        if (!string.IsNullOrEmpty(_remoteEndpoint))
        {
            await CrashReportSender.SendAsync(report, _remoteEndpoint);
        }
    }

    /// <summary>
    /// Get all crash reports.
    /// </summary>
    public static IEnumerable<string> GetCrashReportFiles()
    {
        return CrashReportStore.GetReportFiles();
    }
}
