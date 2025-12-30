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
using Microsoft.Win32;
using WindowsOptimizer.App.Diagnostics;
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
        ScanAllCommand = new RelayCommand(_ => _ = RunScanAsync(), _ => !IsScanning);
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

    public Task RunScanAsync()
    {
        return RunScanAsync(null, CancellationToken.None);
    }

    public async Task RunScanAsync(IProgress<StartupScanProgress>? progress, CancellationToken ct)
    {
        if (_tweaksViewModel == null || IsScanning)
        {
            return;
        }

        IsScanning = true;
        try
        {
            await _tweaksViewModel.DetectAllTweaksAsync(progress, ct);
            RefreshSystemSnapshot();
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
    public string GpuName => GetGpuName();
    public string PrimaryDiskType => GetPrimaryDiskType();
    public string SecureBootStatus => GetSecureBootStatus();
    public string VirtualizationStatus => GetVirtualizationStatus();
    public string TpmStatus => GetTpmStatus();

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

    [SupportedOSPlatform("windows")]
    private static string GetGpuName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }
        catch { }
        return "Unknown";
    }

    [SupportedOSPlatform("windows")]
    private static string GetPrimaryDiskType()
    {
        try
        {
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
            var bootDiskNumber = TryGetBootDiskNumber();
            if (!bootDiskNumber.HasValue)
            {
                LogProbe("BootDisk", $"Could not map SystemDrive='{systemDrive}' to a disk index.");
            }

            var storageType = TryGetDiskTypeFromStorage(bootDiskNumber);
            if (!string.IsNullOrWhiteSpace(storageType))
            {
                return storageType;
            }

            var legacyType = TryGetDiskTypeFromWin32(bootDiskNumber);
            if (!string.IsNullOrWhiteSpace(legacyType))
            {
                return legacyType;
            }

            LogProbe("BootDisk", $"Unknown. SystemDrive='{systemDrive}', DiskIndex='{bootDiskNumber?.ToString() ?? "null"}'.");
        }
        catch (Exception ex)
        {
            LogProbeException("BootDisk", ex);
        }

        return "Unknown";
    }

    [SupportedOSPlatform("windows")]
    private static int? TryGetBootDiskNumber()
    {
        try
        {
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
            var driveId = systemDrive.TrimEnd('\\');
            using var partitionSearcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{driveId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
            foreach (ManagementObject partition in partitionSearcher.Get())
            {
                var partitionId = partition["DeviceID"]?.ToString();
                if (string.IsNullOrWhiteSpace(partitionId))
                {
                    continue;
                }

                using var diskSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
                foreach (ManagementObject disk in diskSearcher.Get())
                {
                    if (disk["Index"] is uint index)
                    {
                        return (int)index;
                    }

                    if (int.TryParse(disk["Index"]?.ToString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogProbeException("BootDiskLookup", ex);
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string? TryGetDiskTypeFromStorage(int? bootDiskNumber)
    {
        if (!bootDiskNumber.HasValue)
        {
            return null;
        }

        try
        {
            var scope = new ManagementScope(@"root\\Microsoft\\Windows\\Storage");
            scope.Connect();

            string? diskFriendlyName = null;
            int? diskBusType = null;
            using (var diskSearcher = new ManagementObjectSearcher(
                       scope,
                       new ObjectQuery($"SELECT Number, BusType, FriendlyName FROM MSFT_Disk WHERE Number = {bootDiskNumber.Value}")))
            {
                foreach (var obj in diskSearcher.Get())
                {
                    diskFriendlyName = obj["FriendlyName"]?.ToString();
                    diskBusType = TryReadInt(obj, "BusType");
                }
            }

            using var physicalSearcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT DeviceId, MediaType, BusType, FriendlyName FROM MSFT_PhysicalDisk"));
            foreach (var obj in physicalSearcher.Get())
            {
                var deviceId = obj["DeviceId"]?.ToString();
                var friendlyName = obj["FriendlyName"]?.ToString();
                if (!string.Equals(deviceId, bootDiskNumber.Value.ToString(), StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(friendlyName, diskFriendlyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var mediaType = TryReadInt(obj, "MediaType");
                var busType = diskBusType ?? TryReadInt(obj, "BusType");
                return FormatDiskType(mediaType, busType, friendlyName);
            }

            if (diskBusType.HasValue || !string.IsNullOrWhiteSpace(diskFriendlyName))
            {
                return FormatDiskType(null, diskBusType, diskFriendlyName);
            }
        }
        catch (Exception ex)
        {
            LogProbeException("BootDiskStorage", ex);
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string? TryGetDiskTypeFromWin32(int? bootDiskNumber)
    {
        if (!bootDiskNumber.HasValue)
        {
            return null;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT Model, MediaType, InterfaceType FROM Win32_DiskDrive WHERE Index = {bootDiskNumber.Value}");
            foreach (var obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString() ?? string.Empty;
                var mediaType = obj["MediaType"]?.ToString() ?? string.Empty;
                var interfaceType = obj["InterfaceType"]?.ToString() ?? string.Empty;
                var fingerprint = $"{model} {mediaType} {interfaceType}".ToLowerInvariant();
                if (fingerprint.Contains("nvme"))
                {
                    return "NVMe SSD";
                }
                if (fingerprint.Contains("ssd") || fingerprint.Contains("solid state"))
                {
                    return "SSD";
                }
                if (fingerprint.Contains("hdd") || fingerprint.Contains("hard disk"))
                {
                    return "HDD";
                }
                if (fingerprint.Contains("usb"))
                {
                    return "USB";
                }
            }
        }
        catch (Exception ex)
        {
            LogProbeException("BootDiskWin32", ex);
        }

        return null;
    }

    private static string? FormatDiskType(int? mediaType, int? busType, string? friendlyName)
    {
        if (busType == 17)
        {
            return "NVMe SSD";
        }

        if (mediaType == 4)
        {
            return "SSD";
        }

        if (mediaType == 3)
        {
            return "HDD";
        }

        if (!string.IsNullOrWhiteSpace(friendlyName))
        {
            var name = friendlyName.ToLowerInvariant();
            if (name.Contains("nvme"))
            {
                return "NVMe SSD";
            }
            if (name.Contains("ssd"))
            {
                return "SSD";
            }
        }

        return null;
    }

    private static int? TryReadInt(ManagementBaseObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                return null;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (value is uint uintValue)
            {
                return (int)uintValue;
            }

            if (value is ushort ushortValue)
            {
                return ushortValue;
            }

            if (int.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }
        }
        catch { }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string GetSecureBootStatus()
    {
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SYSTEM\\CurrentControlSet\\Control\\SecureBoot\\State");
            if (key?.GetValue("UEFISecureBootEnabled") is int enabled)
            {
                return enabled == 1 ? "Enabled" : "Disabled";
            }
        }
        catch { }

        return "Unsupported";
    }

    [SupportedOSPlatform("windows")]
    private static string GetVirtualizationStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT VirtualizationFirmwareEnabled FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                if (obj["VirtualizationFirmwareEnabled"] is bool enabled)
                {
                    return enabled ? "Enabled" : "Disabled";
                }
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT HypervisorPresent FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                if (obj["HypervisorPresent"] is bool present)
                {
                    return present ? "Enabled" : "Disabled";
                }
            }
        }
        catch { }

        return "Unknown";
    }

    [SupportedOSPlatform("windows")]
    private static string GetTpmStatus()
    {
        try
        {
            var scope = new ManagementScope(@"root\\CIMV2\\Security\\MicrosoftTpm")
            {
                Options =
                {
                    EnablePrivileges = true,
                    Impersonation = ImpersonationLevel.Impersonate
                }
            };
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT IsEnabled_InitialValue, IsActivated_InitialValue, SpecVersion FROM Win32_Tpm"));
            foreach (var obj in searcher.Get())
            {
                var enabled = TryReadBool(obj, "IsEnabled_InitialValue");
                var activated = TryReadBool(obj, "IsActivated_InitialValue");
                if (enabled == true && activated == true)
                {
                    return "Enabled";
                }
                if (enabled == true)
                {
                    return "Present (Disabled)";
                }
                return "Present";
            }

            return "Not detected";
        }
        catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace || ex.ErrorCode == ManagementStatus.InvalidClass)
        {
            LogProbeException("TPM", ex);
            return "Not detected";
        }
        catch (Exception ex)
        {
            LogProbeException("TPM", ex);
        }

        return "Unknown";
    }

    private static bool? TryReadBool(ManagementBaseObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                return null;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is int intValue)
            {
                return intValue != 0;
            }

            if (value is uint uintValue)
            {
                return uintValue != 0;
            }

            if (value is ushort ushortValue)
            {
                return ushortValue != 0;
            }

            if (bool.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }
        }
        catch { }

        return null;
    }

    private static void LogProbe(string label, string message)
    {
        AppDiagnostics.Log($"[SystemProbe:{label}] {message}");
    }

    private static void LogProbeException(string label, Exception exception)
    {
        AppDiagnostics.LogException($"SystemProbe:{label}", exception);
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

    private void RefreshSystemSnapshot()
    {
        OnPropertyChanged(nameof(OsVersion));
        OnPropertyChanged(nameof(OsBuild));
        OnPropertyChanged(nameof(SystemArchitecture));
        OnPropertyChanged(nameof(MachineName));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(ProcessorCount));
        OnPropertyChanged(nameof(TotalMemoryFormatted));
        OnPropertyChanged(nameof(AvailableMemoryFormatted));
        OnPropertyChanged(nameof(SystemUptime));
        OnPropertyChanged(nameof(GpuName));
        OnPropertyChanged(nameof(PrimaryDiskType));
        OnPropertyChanged(nameof(SecureBootStatus));
        OnPropertyChanged(nameof(VirtualizationStatus));
        OnPropertyChanged(nameof(TpmStatus));
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
