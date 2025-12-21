using System;
using System.IO;
using System.Linq;
using System.Text;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly AppPaths _paths;
    private int _totalTweaksAvailable;
    private int _tweaksApplied;
    private int _tweaksRolledBack;
    private long _logFileSizeBytes;

    public DashboardViewModel()
    {
        _paths = AppPaths.FromEnvironment();
        LoadStatistics();
    }

    public string Title => "Dashboard";

    public int TotalTweaksAvailable
    {
        get => _totalTweaksAvailable;
        set => SetProperty(ref _totalTweaksAvailable, value);
    }

    public int TweaksApplied
    {
        get => _tweaksApplied;
        set => SetProperty(ref _tweaksApplied, value);
    }

    public int TweaksRolledBack
    {
        get => _tweaksRolledBack;
        set => SetProperty(ref _tweaksRolledBack, value);
    }

    public long LogFileSizeBytes
    {
        get => _logFileSizeBytes;
        set => SetProperty(ref _logFileSizeBytes, value);
    }

    public string LogFileSizeFormatted => FormatBytes(LogFileSizeBytes);

    public bool IsElevated => ProcessElevation.IsElevated();

    public string ElevationStatus => IsElevated ? "Running as Administrator" : "Running as Standard User";

    private void LoadStatistics()
    {
        TotalTweaksAvailable = 100; // This will be dynamic when we count actual tweaks

        // Count applied and rolled back tweaks from log file
        var appliedCount = 0;
        var rolledBackCount = 0;

        if (File.Exists(_paths.TweakLogFilePath))
        {
            try
            {
                var lines = File.ReadAllLines(_paths.TweakLogFilePath, Encoding.UTF8);

                // Skip header row
                foreach (var line in lines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = ParseCsvLine(line);
                    if (parts.Length >= 5)
                    {
                        var action = parts[3];
                        var status = parts[4];

                        if (action == "Apply" && status == "Applied")
                        {
                            appliedCount++;
                        }
                        else if (action == "Rollback" && status == "RolledBack")
                        {
                            rolledBackCount++;
                        }
                    }
                }

                // Get log file size
                var fileInfo = new FileInfo(_paths.TweakLogFilePath);
                LogFileSizeBytes = fileInfo.Length;
            }
            catch
            {
                // If we can't read the log file, just use default values
            }
        }

        TweaksApplied = appliedCount;
        TweaksRolledBack = rolledBackCount;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
