using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Crash reporting service for logging unhandled exceptions.
/// Stores crash reports locally and optionally sends to remote endpoint.
/// </summary>
public class CrashReportService
{
    private static readonly string CrashLogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowsOptimizer", "CrashLogs");

    private static string? _remoteEndpoint;
    private static bool _isInitialized;

    /// <summary>
    /// Initialize crash reporting. Call this once at app startup.
    /// </summary>
    public static void Initialize(string? remoteEndpoint = null)
    {
        if (_isInitialized) return;

        _remoteEndpoint = remoteEndpoint;
        Directory.CreateDirectory(CrashLogDirectory);

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

        var report = CreateReport(exception, source, isTerminating);
        
        // Save locally
        await SaveReportLocallyAsync(report);
        
        // Send to remote if configured
        if (!string.IsNullOrEmpty(_remoteEndpoint))
        {
            await SendReportToRemoteAsync(report);
        }
    }

    private static CrashReport CreateReport(Exception exception, string source, bool isTerminating)
    {
        return new CrashReport
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTime.UtcNow,
            Source = source,
            IsTerminating = isTerminating,
            ExceptionType = exception.GetType().FullName ?? "Unknown",
            Message = exception.Message,
            StackTrace = exception.StackTrace ?? "",
            InnerException = exception.InnerException?.Message,
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            OsVersion = Environment.OSVersion.ToString(),
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet / 1024 / 1024, // MB
            AdditionalData = new Dictionary<string, string>
            {
                ["IsAdmin"] = IsRunningAsAdmin().ToString(),
                ["Culture"] = System.Globalization.CultureInfo.CurrentCulture.Name
            }
        };
    }

    private static async Task SaveReportLocallyAsync(CrashReport report)
    {
        try
        {
            var fileName = $"crash_{report.Timestamp:yyyyMMdd_HHmmss}_{report.Id[..8]}.json";
            var filePath = Path.Combine(CrashLogDirectory, fileName);
            
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
            
            Debug.WriteLine($"Crash report saved: {filePath}");
            
            // Cleanup old reports (keep last 50)
            CleanupOldReports(50);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save crash report: {ex.Message}");
        }
    }

    private static async Task SendReportToRemoteAsync(CrashReport report)
    {
        if (string.IsNullOrEmpty(_remoteEndpoint)) return;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = JsonSerializer.Serialize(report);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            await client.PostAsync(_remoteEndpoint, content);
            Debug.WriteLine("Crash report sent to remote");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send crash report: {ex.Message}");
        }
    }

    private static void CleanupOldReports(int keepCount)
    {
        try
        {
            var files = Directory.GetFiles(CrashLogDirectory, "crash_*.json")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Skip(keepCount)
                .ToList();

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        catch { /* Ignore cleanup errors */ }
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get all crash reports.
    /// </summary>
    public static IEnumerable<string> GetCrashReportFiles()
    {
        if (!Directory.Exists(CrashLogDirectory))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(CrashLogDirectory, "crash_*.json")
            .OrderByDescending(f => File.GetCreationTime(f));
    }
}

/// <summary>
/// Crash report data model.
/// </summary>
public class CrashReport
{
    public string Id { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string Source { get; init; } = "";
    public bool IsTerminating { get; init; }
    public string ExceptionType { get; init; } = "";
    public string Message { get; init; } = "";
    public string StackTrace { get; init; } = "";
    public string? InnerException { get; init; }
    public string AppVersion { get; init; } = "";
    public string OsVersion { get; init; } = "";
    public string MachineName { get; init; } = "";
    public string UserName { get; init; } = "";
    public int ProcessorCount { get; init; }
    public long WorkingSet { get; init; }
    public Dictionary<string, string> AdditionalData { get; init; } = new();
}
