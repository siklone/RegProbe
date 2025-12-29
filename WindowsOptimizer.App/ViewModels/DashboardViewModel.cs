using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Core.Security;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Metrics;

namespace WindowsOptimizer.App.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly AppPaths _paths;
    private readonly VssSnapshotService _vssService = new();
    private readonly BootTimeTracker _bootTimeTracker;
    private readonly ITweakLogStore _logStore;
    private int _totalTweaksAvailable;
    private int _tweaksApplied;
    private int _tweaksRolledBack;
    private int _optimizationScore;
    private long _logFileSizeBytes;
    private int _healthTweaksTotal;
    private int _healthTweaksApplied;
    private bool _isScanning;
    private bool _isCreatingRestorePoint;
    private string _restorePointStatusMessage = string.Empty;
    private bool _vssServiceNeedsEnable;
    private IReadOnlyList<ActivityTimelineItem> _recentActivity = Array.Empty<ActivityTimelineItem>();
    private TweaksViewModel? _tweaksViewModel;

    // Navigation callback - set by MainViewModel
    public Action<string>? NavigateToCategoryRequested { get; set; }

    // Status filter callback - set by MainViewModel (for applied/rolled back filters)
    public Action<string>? NavigateToStatusFilterRequested { get; set; }

    public DashboardViewModel()
    {
        _paths = AppPaths.FromEnvironment();
        _bootTimeTracker = new BootTimeTracker(_paths);
        _bootTimeTracker.RecordCurrentBoot();
        _logStore = new FileTweakLogStore(_paths);
        LoadStatistics();
        _ = LoadRecentActivityAsync();
        ScanAllCommand = new RelayCommand(_ => ScanAllTweaksAsync(), _ => !IsScanning);
        RefreshActivityCommand = new RelayCommand(_ => _ = LoadRecentActivityAsync());

        // Category navigation commands
        NavigateToPrivacyCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("privacy"));
        NavigateToPowerCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("power"));
        NavigateToSystemCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("system"));
        NavigateToNetworkCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("network"));
        NavigateToSecurityCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("security"));
        NavigateToCleanupCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("cleanup"));
        NavigateToExplorerCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("explorer"));
        NavigateToAllTweaksCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke(""));

        // Stat card click commands
        NavigateToTotalTweaksCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke(""));
        NavigateToAppliedTweaksCommand = new RelayCommand(_ => NavigateToStatusFilterRequested?.Invoke("applied"));
        NavigateToRolledBackTweaksCommand = new RelayCommand(_ => NavigateToStatusFilterRequested?.Invoke("rolledback"));
        OpenLogFileCommand = new RelayCommand(_ => OpenLogFile(), _ => File.Exists(_paths.TweakLogFilePath));
        OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
        CreateRestorePointCommand = new RelayCommand(_ => CreateRestorePointAsync(), _ => !IsCreatingRestorePoint);
        EnableVssCommand = new RelayCommand(_ => EnableVssServiceAsync(), _ => VssServiceNeedsEnable && !IsCreatingRestorePoint);
    }

    public void SetTweaksViewModel(TweaksViewModel tweaksViewModel)
    {
        _tweaksViewModel = tweaksViewModel;
    }

    public ICommand ScanAllCommand { get; }
    public ICommand NavigateToPrivacyCommand { get; }
    public ICommand NavigateToPowerCommand { get; }
    public ICommand NavigateToSystemCommand { get; }
    public ICommand NavigateToNetworkCommand { get; }
    public ICommand NavigateToSecurityCommand { get; }
    public ICommand NavigateToCleanupCommand { get; }
    public ICommand NavigateToExplorerCommand { get; }
    public ICommand NavigateToAllTweaksCommand { get; }

    // Stat card click commands
    public ICommand NavigateToTotalTweaksCommand { get; }
    public ICommand NavigateToAppliedTweaksCommand { get; }
    public ICommand NavigateToRolledBackTweaksCommand { get; }
    public ICommand OpenLogFileCommand { get; }
    public ICommand OpenLogFolderCommand { get; }
    public ICommand CreateRestorePointCommand { get; }
    public ICommand EnableVssCommand { get; }

    public bool IsCreatingRestorePoint
    {
        get => _isCreatingRestorePoint;
        private set
        {
            if (SetProperty(ref _isCreatingRestorePoint, value))
            {
                ((RelayCommand)CreateRestorePointCommand).RaiseCanExecuteChanged();
                ((RelayCommand)EnableVssCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string RestorePointStatusMessage
    {
        get => _restorePointStatusMessage;
        private set => SetProperty(ref _restorePointStatusMessage, value);
    }

    public bool VssServiceNeedsEnable
    {
        get => _vssServiceNeedsEnable;
        private set
        {
            if (SetProperty(ref _vssServiceNeedsEnable, value))
            {
                ((RelayCommand)EnableVssCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                ((RelayCommand)ScanAllCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private async void ScanAllTweaksAsync()
    {
        if (_tweaksViewModel == null || IsScanning) return;

        IsScanning = true;
        try
        {
            await _tweaksViewModel.DetectAllTweaksAsync();
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async void CreateRestorePointAsync()
    {
        if (IsCreatingRestorePoint) return;

        IsCreatingRestorePoint = true;
        RestorePointStatusMessage = "Creating system restore point...";

        try
        {
            var description = $"Windows Optimizer - {DateTime.Now:yyyy-MM-dd HH:mm}";
            var result = await _vssService.CreateSnapshotAsync(description, CancellationToken.None);

            if (result.Success)
            {
                RestorePointStatusMessage = $"Restore point created: {result.Description}";
                VssServiceNeedsEnable = false;
            }
            else
            {
                RestorePointStatusMessage = $"Failed: {result.ErrorMessage}";
                // Check if VSS service needs to be enabled
                VssServiceNeedsEnable = result.ErrorMessage?.Contains("not available or disabled") == true;
            }
        }
        catch (Exception ex)
        {
            RestorePointStatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsCreatingRestorePoint = false;
        }
    }

    private async void EnableVssServiceAsync()
    {
        IsCreatingRestorePoint = true;
        RestorePointStatusMessage = "Enabling Volume Shadow Copy service...";

        try
        {
            // Start VSS service using sc.exe (requires admin)
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "start vss",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                RestorePointStatusMessage = "VSS service started. Try creating restore point again.";
                VssServiceNeedsEnable = false;
            }
            else
            {
                RestorePointStatusMessage = "Could not start VSS. Try: services.msc > Volume Shadow Copy > Start";
            }
        }
        catch (Exception ex)
        {
            RestorePointStatusMessage = $"Enable VSS failed: {ex.Message}";
        }
        finally
        {
            IsCreatingRestorePoint = false;
        }
    }

    public string Title => "Dashboard";

    public int TotalTweaksAvailable
    {
        get => _totalTweaksAvailable;
        set
        {
            if (SetProperty(ref _totalTweaksAvailable, value))
            {
                OnPropertyChanged(nameof(OptimizationScoreDetails));
            }
        }
    }

    public int TweaksApplied
    {
        get => _tweaksApplied;
        set
        {
            if (SetProperty(ref _tweaksApplied, value))
            {
                OnPropertyChanged(nameof(OptimizationScoreDetails));
            }
        }
    }

    public int HealthTweaksTotal
    {
        get => _healthTweaksTotal;
        set
        {
            if (SetProperty(ref _healthTweaksTotal, value))
            {
                OnPropertyChanged(nameof(OptimizationScoreDetails));
                OnPropertyChanged(nameof(HasHealthData));
                OnPropertyChanged(nameof(OptimizationScoreDisplayText));
            }
        }
    }

    public int HealthTweaksApplied
    {
        get => _healthTweaksApplied;
        set
        {
            if (SetProperty(ref _healthTweaksApplied, value))
            {
                OnPropertyChanged(nameof(OptimizationScoreDetails));
            }
        }
    }

    public int TweaksRolledBack
    {
        get => _tweaksRolledBack;
        set => SetProperty(ref _tweaksRolledBack, value);
    }

    public int OptimizationScore
    {
        get => _optimizationScore;
        set
        {
            if (SetProperty(ref _optimizationScore, value))
            {
                OnPropertyChanged(nameof(OptimizationScoreDisplayText));
            }
        }
    }

    public string OptimizationScoreDetails =>
        HealthTweaksTotal <= 0
            ? "Health is calculated from detected states. Run Detect to refresh current states."
            : $"{HealthTweaksApplied} / {HealthTweaksTotal} detected tweaks applied (Safe+Advanced; excludes Demo/Risky).";

    public bool HasHealthData => HealthTweaksTotal > 0;

    public string OptimizationScoreDisplayText => HasHealthData ? $"{OptimizationScore}%" : "—";

    public long LogFileSizeBytes
    {
        get => _logFileSizeBytes;
        set => SetProperty(ref _logFileSizeBytes, value);
    }

    public string LogFileSizeFormatted => FormatBytes(LogFileSizeBytes);

    public bool IsElevated => ProcessElevation.IsElevated();

    public string ElevationStatus => IsElevated ? "Running as Administrator" : "Running as Standard User";

    // Boot Time Tracking Properties
    public DateTime? LastBootTime => _bootTimeTracker.GetLastBootTime();

    public string LastBootTimeFormatted => LastBootTime?.ToString("MMM dd, yyyy HH:mm") ?? "Unknown";

    public TimeSpan? LastBootDuration => _bootTimeTracker.GetLastBootDuration();

    public string LastBootDurationFormatted => LastBootDuration.HasValue
        ? $"{LastBootDuration.Value.TotalSeconds:F1}s"
        : "Unknown";

    public TimeSpan? AverageBootDuration => _bootTimeTracker.GetAverageBootDuration();

    public string AverageBootDurationFormatted => AverageBootDuration.HasValue
        ? $"{AverageBootDuration.Value.TotalSeconds:F1}s"
        : "—";

    public double? BootTimeImprovement => _bootTimeTracker.GetImprovementPercentage();

    public string BootTimeImprovementFormatted => BootTimeImprovement.HasValue
        ? $"{(BootTimeImprovement > 0 ? "+" : "")}{BootTimeImprovement:F1}%"
        : "—";

    public bool HasBootTimeImprovement => BootTimeImprovement.HasValue && BootTimeImprovement > 0;

    public IReadOnlyList<double> RecentBootDurations => _bootTimeTracker.GetRecentBootDurations(10);

    public bool HasBootTimeHistory => RecentBootDurations.Count > 1;

    // System Information Properties
    public string OsVersion => GetOsVersion();
    public string OsBuild => Environment.OSVersion.Version.Build.ToString();
    public string MachineName => Environment.MachineName;
    public string UserName => Environment.UserName;
    public int ProcessorCount => Environment.ProcessorCount;
    public string SystemArchitecture => Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
    public string TotalMemoryFormatted => GetTotalMemoryFormatted();
    public string AvailableMemoryFormatted => GetAvailableMemoryFormatted();
    public string SystemUptime => GetSystemUptime();
    public string DotNetVersion => Environment.Version.ToString();

    [SupportedOSPlatform("windows")]
    private static string GetOsVersion()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                var caption = obj["Caption"]?.ToString();
                if (!string.IsNullOrEmpty(caption))
                {
                    // Remove "Microsoft " prefix for cleaner display
                    return caption.Replace("Microsoft ", "");
                }
            }
        }
        catch { }
        return $"Windows {Environment.OSVersion.Version.Major}";
    }

    [SupportedOSPlatform("windows")]
    private static string GetTotalMemoryFormatted()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                var totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                return FormatBytes((long)totalBytes);
            }
        }
        catch { }
        return "Unknown";
    }

    [SupportedOSPlatform("windows")]
    private static string GetAvailableMemoryFormatted()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                var freeKb = Convert.ToUInt64(obj["FreePhysicalMemory"]);
                return FormatBytes((long)(freeKb * 1024));
            }
        }
        catch { }
        return "Unknown";
    }

    private static string GetSystemUptime()
    {
        try
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            if (uptime.TotalDays >= 1)
                return $"{(int)uptime.TotalDays}d {uptime.Hours}h";
            if (uptime.TotalHours >= 1)
                return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
            return $"{uptime.Minutes}m";
        }
        catch { }
        return "Unknown";
    }

    // Recent Activity Timeline
    public ICommand RefreshActivityCommand { get; }

    public IReadOnlyList<ActivityTimelineItem> RecentActivity
    {
        get => _recentActivity;
        private set
        {
            if (SetProperty(ref _recentActivity, value))
            {
                OnPropertyChanged(nameof(HasRecentActivity));
            }
        }
    }

    public bool HasRecentActivity => RecentActivity.Count > 0;

    private async Task LoadRecentActivityAsync()
    {
        try
        {
            var entries = await _logStore.GetRecentHistoryAsync(10, CancellationToken.None);
            RecentActivity = entries
                .Select(e => new ActivityTimelineItem(
                    e.Timestamp.LocalDateTime,
                    e.TweakName,
                    e.Action.ToString(),
                    e.Status == Core.TweakStatus.Applied || e.Status == Core.TweakStatus.RolledBack,
                    e.Message))
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            RecentActivity = Array.Empty<ActivityTimelineItem>();
        }
    }

    private void LoadStatistics()
    {
        // These will be updated live by MainViewModel from TweaksViewModel.
        TotalTweaksAvailable = 0;

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

    private void OpenLogFile()
    {
        try
        {
            if (File.Exists(_paths.TweakLogFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _paths.TweakLogFilePath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // If we can't open the file directly, try opening the folder
            OpenLogFolder();
        }
    }

    private void OpenLogFolder()
    {
        try
        {
            var folder = Path.GetDirectoryName(_paths.TweakLogFilePath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    public string LogFilePath => _paths.TweakLogFilePath;

    public string LogFolderPath => Path.GetDirectoryName(_paths.TweakLogFilePath) ?? "";

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

/// <summary>
/// Represents a single activity item in the timeline.
/// </summary>
public sealed class ActivityTimelineItem
{
    public ActivityTimelineItem(DateTime timestamp, string tweakName, string action, bool success, string message)
    {
        Timestamp = timestamp;
        TweakName = tweakName;
        Action = action;
        Success = success;
        Message = message;
    }

    public DateTime Timestamp { get; }
    public string TweakName { get; }
    public string Action { get; }
    public bool Success { get; }
    public string Message { get; }

    public string TimestampFormatted => Timestamp.ToString("MMM dd, HH:mm");
    public string TimeAgo => FormatTimeAgo(DateTime.Now - Timestamp);

    public string ActionIcon => Action.ToLowerInvariant() switch
    {
        "apply" => "✓",
        "rollback" => "↩",
        "detect" => "🔍",
        "preview" => "👁",
        "verify" => "✔",
        _ => "•"
    };

    public string StatusColor => Success ? "#A3BE8C" : "#BF616A";

    private static string FormatTimeAgo(TimeSpan span)
    {
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return $"{(int)(span.TotalDays / 7)}w ago";
    }
}
