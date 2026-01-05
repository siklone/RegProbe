using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Audit logging service for tracking all user actions and system changes.
/// Provides immutable, timestamped logs for compliance and troubleshooting.
/// </summary>
public class AuditLogService : IDisposable
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowsOptimizer", "AuditLogs");

    private readonly string _logFilePath;
    private readonly object _lock = new();
    private bool _isDisposed;

    public AuditLogService()
    {
        Directory.CreateDirectory(LogDirectory);
        _logFilePath = Path.Combine(LogDirectory, $"audit_{DateTime.Now:yyyyMM}.jsonl");
    }

    /// <summary>
    /// Log an audit event.
    /// </summary>
    public void Log(AuditAction action, string description, Dictionary<string, string>? metadata = null)
    {
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = action,
            Description = description,
            User = Environment.UserName,
            Machine = Environment.MachineName,
            SessionId = Environment.ProcessId,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        WriteEntry(entry);
    }

    /// <summary>
    /// Log an audit event asynchronously.
    /// </summary>
    public async Task LogAsync(AuditAction action, string description, Dictionary<string, string>? metadata = null)
    {
        await Task.Run(() => Log(action, description, metadata));
    }

    /// <summary>
    /// Log a tweak application.
    /// </summary>
    public void LogTweakApplied(string tweakId, string tweakName, bool success)
    {
        Log(AuditAction.TweakApplied, $"Applied tweak: {tweakName}", new Dictionary<string, string>
        {
            ["TweakId"] = tweakId,
            ["Success"] = success.ToString()
        });
    }

    /// <summary>
    /// Log a tweak revert.
    /// </summary>
    public void LogTweakReverted(string tweakId, string tweakName, bool success)
    {
        Log(AuditAction.TweakReverted, $"Reverted tweak: {tweakName}", new Dictionary<string, string>
        {
            ["TweakId"] = tweakId,
            ["Success"] = success.ToString()
        });
    }

    /// <summary>
    /// Log a preset application.
    /// </summary>
    public void LogPresetApplied(string presetName, int tweaksApplied)
    {
        Log(AuditAction.PresetApplied, $"Applied preset: {presetName}", new Dictionary<string, string>
        {
            ["TweaksApplied"] = tweaksApplied.ToString()
        });
    }

    /// <summary>
    /// Log a DNS change.
    /// </summary>
    public void LogDnsChanged(string provider)
    {
        Log(AuditAction.SettingsChanged, $"DNS changed to: {provider}", new Dictionary<string, string>
        {
            ["Setting"] = "DNS",
            ["NewValue"] = provider
        });
    }

    /// <summary>
    /// Get all audit entries for a date range.
    /// </summary>
    public async Task<IEnumerable<AuditEntry>> GetEntriesAsync(DateTime from, DateTime to)
    {
        return await Task.Run(() =>
        {
            var entries = new List<AuditEntry>();

            try
            {
                var files = Directory.GetFiles(LogDirectory, "audit_*.jsonl");
                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        var entry = JsonSerializer.Deserialize<AuditEntry>(line);
                        if (entry != null && entry.Timestamp >= from && entry.Timestamp <= to)
                        {
                            entries.Add(entry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read audit logs: {ex.Message}");
            }

            return entries;
        });
    }

    private void WriteEntry(AuditEntry entry)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry);
            lock (_lock)
            {
                File.AppendAllText(_logFilePath, json + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write audit log: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Types of audit actions.
/// </summary>
public enum AuditAction
{
    AppStarted,
    AppClosed,
    TweakApplied,
    TweakReverted,
    PresetApplied,
    PresetReverted,
    StartupItemDisabled,
    StartupItemEnabled,
    AppUninstalled,
    SettingsChanged,
    ConfigExported,
    ConfigImported,
    RestorePointCreated,
    ScheduledTaskModified
}

/// <summary>
/// Audit log entry.
/// </summary>
public class AuditEntry
{
    public DateTime Timestamp { get; init; }
    public AuditAction Action { get; init; }
    public string Description { get; init; } = "";
    public string User { get; init; } = "";
    public string Machine { get; init; } = "";
    public int SessionId { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}
