using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        LoadStatistics();
        ScanAllCommand = new RelayCommand(_ => ScanAllTweaksAsync(), _ => !IsScanning);

        // Category navigation commands
        NavigateToPrivacyCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("privacy"));
        NavigateToPowerCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("power"));
        NavigateToSystemCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("system"));
        NavigateToNetworkCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("network"));
        NavigateToSecurityCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke("security"));
        NavigateToAllTweaksCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke(""));

        // Stat card click commands
        NavigateToTotalTweaksCommand = new RelayCommand(_ => NavigateToCategoryRequested?.Invoke(""));
        NavigateToAppliedTweaksCommand = new RelayCommand(_ => NavigateToStatusFilterRequested?.Invoke("applied"));
        NavigateToRolledBackTweaksCommand = new RelayCommand(_ => NavigateToStatusFilterRequested?.Invoke("rolledback"));
        OpenLogFileCommand = new RelayCommand(_ => OpenLogFile(), _ => File.Exists(_paths.TweakLogFilePath));
        OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
        CreateRestorePointCommand = new RelayCommand(_ => CreateRestorePointAsync(), _ => !IsCreatingRestorePoint);
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
    public ICommand NavigateToAllTweaksCommand { get; }

    // Stat card click commands
    public ICommand NavigateToTotalTweaksCommand { get; }
    public ICommand NavigateToAppliedTweaksCommand { get; }
    public ICommand NavigateToRolledBackTweaksCommand { get; }
    public ICommand OpenLogFileCommand { get; }
    public ICommand OpenLogFolderCommand { get; }
    public ICommand CreateRestorePointCommand { get; }

    public bool IsCreatingRestorePoint
    {
        get => _isCreatingRestorePoint;
        private set
        {
            if (SetProperty(ref _isCreatingRestorePoint, value))
            {
                ((RelayCommand)CreateRestorePointCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string RestorePointStatusMessage
    {
        get => _restorePointStatusMessage;
        private set => SetProperty(ref _restorePointStatusMessage, value);
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
            }
            else
            {
                RestorePointStatusMessage = $"Failed: {result.ErrorMessage}";
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
