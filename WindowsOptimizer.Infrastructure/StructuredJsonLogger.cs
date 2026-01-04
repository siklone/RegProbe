using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowsOptimizer.Infrastructure;

/// <summary>
/// JSON Lines structured logger for machine-readable log analysis.
/// Writes logs in JSON Lines format (.jsonl) for easy parsing and aggregation.
/// Reference: https://jsonlines.org/
/// Reference: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to
/// </summary>
public sealed class StructuredJsonLogger : IAppLogger
{
    private const long MaxLogBytes = 50 * 1024 * 1024; // 50MB for JSON logs
    private const int MaxArchivedLogs = 3;
    private const string LogFileName = "app-structured.jsonl";
    
    private readonly object _sync = new();
    private readonly AppPaths _paths;
    private readonly string _logFilePath;
    private readonly string _applicationName;
    private readonly string _applicationVersion;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false // One line per entry (JSON Lines format)
    };

    public StructuredJsonLogger(AppPaths paths, string applicationName = "WindowsOptimizer", string? applicationVersion = null)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logFilePath = Path.Combine(paths.LogDirectory, LogFileName);
        _applicationName = applicationName;
        _applicationVersion = applicationVersion ?? GetAssemblyVersion();
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _paths.EnsureDirectories();

        var entry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level.ToString().ToUpperInvariant(),
            Message = message,
            Application = _applicationName,
            Version = _applicationVersion,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
            Exception = exception is not null ? new ExceptionData(exception) : null
        };

        var jsonLine = JsonSerializer.Serialize(entry, JsonOptions);

        lock (_sync)
        {
            RotateIfNeeded();
            File.AppendAllText(_logFilePath, jsonLine + Environment.NewLine);
        }
    }

    /// <summary>
    /// Logs a structured event with additional properties.
    /// </summary>
    public void LogEvent(LogLevel level, string eventName, object? properties = null, Exception? exception = null)
    {
        _paths.EnsureDirectories();

        var entry = new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = level.ToString().ToUpperInvariant(),
            Message = eventName,
            EventName = eventName,
            Application = _applicationName,
            Version = _applicationVersion,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
            Properties = properties,
            Exception = exception is not null ? new ExceptionData(exception) : null
        };

        var jsonLine = JsonSerializer.Serialize(entry, JsonOptions);

        lock (_sync)
        {
            RotateIfNeeded();
            File.AppendAllText(_logFilePath, jsonLine + Environment.NewLine);
        }
    }

    /// <summary>
    /// Logs a tweak execution event with structured data.
    /// </summary>
    public void LogTweakExecution(
        string tweakId,
        string tweakName,
        string action,
        string status,
        string? message = null,
        TimeSpan? duration = null,
        Exception? exception = null)
    {
        var properties = new TweakExecutionProperties
        {
            TweakId = tweakId,
            TweakName = tweakName,
            Action = action,
            Status = status,
            DurationMs = duration?.TotalMilliseconds
        };

        LogEvent(
            status == "Failed" ? LogLevel.Error : LogLevel.Info,
            "TweakExecution",
            properties,
            exception);
    }

    private void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(_logFilePath))
            {
                return;
            }

            var info = new FileInfo(_logFilePath);
            if (info.Length < MaxLogBytes)
            {
                return;
            }

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var archivePath = Path.Combine(_paths.LogDirectory, $"app-structured-{timestamp}.jsonl");
            File.Move(_logFilePath, archivePath);

            var archives = Directory.GetFiles(_paths.LogDirectory, "app-structured-*.jsonl")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ToList();

            foreach (var old in archives.Skip(MaxArchivedLogs))
            {
                old.Delete();
            }
        }
        catch
        {
            // Ignore rotation errors to avoid blocking log writes.
        }
    }

    private static string GetAssemblyVersion()
    {
        try
        {
            var assembly = typeof(StructuredJsonLogger).Assembly;
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }
}

/// <summary>
/// Structured log entry for JSON serialization.
/// </summary>
internal sealed class LogEntry
{
    /// <summary>ISO 8601 timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>Log level (DEBUG, INFO, WARN, ERROR).</summary>
    public string Level { get; set; } = string.Empty;
    
    /// <summary>Human-readable message.</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>Event name for structured events.</summary>
    public string? EventName { get; set; }
    
    /// <summary>Application name.</summary>
    public string? Application { get; set; }
    
    /// <summary>Application version.</summary>
    public string? Version { get; set; }
    
    /// <summary>Machine name.</summary>
    public string? MachineName { get; set; }
    
    /// <summary>Process ID.</summary>
    public int? ProcessId { get; set; }
    
    /// <summary>Thread ID.</summary>
    public int? ThreadId { get; set; }
    
    /// <summary>Additional structured properties.</summary>
    public object? Properties { get; set; }
    
    /// <summary>Exception details if present.</summary>
    public ExceptionData? Exception { get; set; }
}

/// <summary>
/// Structured exception data for JSON serialization.
/// </summary>
internal sealed class ExceptionData
{
    public string Type { get; set; }
    public string Message { get; set; }
    public string? StackTrace { get; set; }
    public ExceptionData? InnerException { get; set; }

    public ExceptionData(Exception ex)
    {
        Type = ex.GetType().FullName ?? ex.GetType().Name;
        Message = ex.Message;
        StackTrace = ex.StackTrace;
        InnerException = ex.InnerException is not null ? new ExceptionData(ex.InnerException) : null;
    }
}

/// <summary>
/// Structured properties for tweak execution events.
/// </summary>
internal sealed class TweakExecutionProperties
{
    public string TweakId { get; set; } = string.Empty;
    public string TweakName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? DurationMs { get; set; }
}
