using System;
using System.IO;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly ITweakLogStore _logStore;
    private int _totalTweaksAvailable;
    private int _tweaksApplied;
    private int _tweaksRolledBack;
    private long _logFileSizeBytes;

    public DashboardViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _logStore = new FileTweakLogStore(paths);

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

        // Count applied and rolled back tweaks from log store
        var allLogs = _logStore.GetAllLogs();
        var appliedCount = 0;
        var rolledBackCount = 0;

        foreach (var log in allLogs)
        {
            if (log.Action == Core.TweakAction.Apply && log.Outcome == Core.TweakOutcome.Success)
            {
                appliedCount++;
            }
            else if (log.Action == Core.TweakAction.Rollback && log.Outcome == Core.TweakOutcome.Success)
            {
                rolledBackCount++;
            }
        }

        TweaksApplied = appliedCount;
        TweaksRolledBack = rolledBackCount;

        // Get log file size
        var paths = AppPaths.FromEnvironment();
        if (File.Exists(paths.TweakLogFilePath))
        {
            var fileInfo = new FileInfo(paths.TweakLogFilePath);
            LogFileSizeBytes = fileInfo.Length;
        }
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
