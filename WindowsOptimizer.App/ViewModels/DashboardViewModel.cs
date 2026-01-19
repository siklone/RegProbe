using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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
using WindowsOptimizer.Core.Commands;
using WindowsOptimizer.Core.Security;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Elevation;
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
    private string _docsCoverageReportPath = string.Empty;
    private int _docsMissingCount;
    private string _docsCoverageSummary = "Docs report unavailable.";
    private int _scanCurrent;
    private int _scanTotal;
    private string _scanStatusText = "Ready to scan.";
    private string _scanDetailText = string.Empty;
    private string _lastScanSummary = "No scans yet.";
    private string _lastScanTimestamp = "Last scan: -";
    private DateTimeOffset? _lastScanAt;
    private TimeSpan _lastScanDuration = TimeSpan.Zero;
    private string _lastScanMode = "Quick scan";
    private int _lastScanDetected;
    private int _lastScanApplied;
    private int _lastScanTotal;
    private int _lastScanSkipped;
    private bool _isFullScanEnabled;
    private int _scanSkippedCount;
    private string _scanSkippedSummary = string.Empty;
    private string _osVersion = "Unknown";
    private string _osBuild = "Unknown";
    private string _osDisplayVersion = "Unknown";
    private string _machineName = "Unknown";
    private string _userName = "Unknown";
    private int _processorCount;
    private string _cpuName = "Unknown";
    private string _systemArchitecture = "Unknown";
    private string _totalMemoryFormatted = "Unknown";
    private string _availableMemoryFormatted = "Unknown";
    private string _systemUptime = "Unknown";
    private string _dotNetVersion = "Unknown";
    private string _gpuName = "Unknown";
    private string _gpuDriverVersion = "Unknown";
    private string _primaryDiskType = "Unknown";
    private string _biosVersion = "Unknown";
    private string _baseboardModel = "Unknown";
    private string _firmwareType = "Unknown";
    private string _secureBootStatus = "Unknown";
    private string _virtualizationStatus = "Unknown";
    private string _tpmStatus = "Unknown";
    private string _windowsUpdateStatus = "Unknown";
    private string _bitLockerStatus = "Unknown";
    private BootMetricsState _bootMetricsState = BootMetricsState.Unknown;
    private bool _isEnablingBootMetrics;
    private string _bootMetricsStatusMessage = string.Empty;
    private bool _showPreviewHint = true;
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
        ScanAllCommand = new AsyncRelayCommand(RunScanAsync, () => !IsScanning);
        RunFullScanCommand = new RelayCommand(_ => RunFullScan(), _ => !IsScanning); // Keep as RelayCommand as it calls RunScanAsync internally
        EnableBootMetricsCommand = new AsyncRelayCommand(EnableBootMetricsAsync, () => CanEnableBootMetrics && !IsEnablingBootMetrics);
        RefreshActivityCommand = new AsyncRelayCommand(LoadRecentActivityAsync);
        OpenActivityLinkCommand = new RelayCommand(param => OpenActivityLink(param as ActivityTimelineItem),
            param => param is ActivityTimelineItem item && item.HasLink);

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
        CreateRestorePointCommand = new AsyncRelayCommand(CreateRestorePointAsync, () => !IsCreatingRestorePoint);
        EnableVssCommand = new AsyncRelayCommand(EnableVssServiceAsync, () => VssServiceNeedsEnable && !IsCreatingRestorePoint);
        OpenDocsCoverageReportCommand = new RelayCommand(_ => OpenDocsCoverageReport(), _ => File.Exists(DocsCoverageReportPath));
        LoadDocsCoverageReport();
        LoadSystemSnapshot();
        _ = LoadUiSettingsAsync();
    }

    public void SetTweaksViewModel(TweaksViewModel tweaksViewModel)
    {
        _tweaksViewModel = tweaksViewModel;
    }

    public ICommand ScanAllCommand { get; }
    public ICommand RunFullScanCommand { get; }
    public ICommand EnableBootMetricsCommand { get; }
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
    public ICommand OpenDocsCoverageReportCommand { get; }

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
                OnPropertyChanged(nameof(IsScanIndeterminate));
            }
        }
    }

    public bool IsFullScanEnabled
    {
        get => _isFullScanEnabled;
        set
        {
            if (SetProperty(ref _isFullScanEnabled, value))
            {
                OnPropertyChanged(nameof(ScanModeLabel));
                OnPropertyChanged(nameof(ScanModeHint));
            }
        }
    }

    public string ScanModeLabel => IsFullScanEnabled ? "Full scan" : "Quick scan";

    public string ScanModeHint => IsFullScanEnabled
        ? "Includes admin and long-running checks. May show UAC prompts."
        : "Skips admin prompts and heavy checks for speed.";

    public int ScanSkippedCount
    {
        get => _scanSkippedCount;
        private set
        {
            if (SetProperty(ref _scanSkippedCount, value))
            {
                OnPropertyChanged(nameof(HasScanSkipped));
            }
        }
    }

    public bool HasScanSkipped => ScanSkippedCount > 0;

    public string ScanSkippedSummary
    {
        get => _scanSkippedSummary;
        private set => SetProperty(ref _scanSkippedSummary, value);
    }

    public int ScanCurrent
    {
        get => _scanCurrent;
        private set
        {
            if (SetProperty(ref _scanCurrent, value))
            {
                OnPropertyChanged(nameof(ScanProgressPercent));
                OnPropertyChanged(nameof(HasScanProgress));
                OnPropertyChanged(nameof(ScanProgressText));
            }
        }
    }

    public int ScanTotal
    {
        get => _scanTotal;
        private set
        {
            if (SetProperty(ref _scanTotal, value))
            {
                OnPropertyChanged(nameof(ScanProgressPercent));
                OnPropertyChanged(nameof(HasScanProgress));
                OnPropertyChanged(nameof(ScanProgressText));
                OnPropertyChanged(nameof(IsScanIndeterminate));
            }
        }
    }

    public double ScanProgressPercent => ScanTotal > 0
        ? Math.Round((double)ScanCurrent / ScanTotal * 100, 1)
        : 0;

    public bool HasScanProgress => ScanTotal > 0;

    public string ScanProgressText => ScanTotal > 0
        ? $"{ScanProgressPercent:F0}% ({ScanCurrent}/{ScanTotal})"
        : "Preparing...";

    public bool IsScanIndeterminate => IsScanning && ScanTotal == 0;

    public string ScanStatusText
    {
        get => _scanStatusText;
        private set => SetProperty(ref _scanStatusText, value);
    }

    public string ScanDetailText
    {
        get => _scanDetailText;
        private set => SetProperty(ref _scanDetailText, value);
    }

    public string LastScanSummary
    {
        get => _lastScanSummary;
        private set => SetProperty(ref _lastScanSummary, value);
    }

    public string LastScanTimestamp
    {
        get => _lastScanTimestamp;
        private set => SetProperty(ref _lastScanTimestamp, value);
    }

    public string LastScanMode
    {
        get => _lastScanMode;
        private set => SetProperty(ref _lastScanMode, value);
    }

    public TimeSpan LastScanDuration
    {
        get => _lastScanDuration;
        private set
        {
            if (SetProperty(ref _lastScanDuration, value))
            {
                OnPropertyChanged(nameof(LastScanDurationText));
            }
        }
    }

    public string LastScanDurationText => LastScanDuration == TimeSpan.Zero
        ? "-"
        : $"{LastScanDuration:mm\\:ss}";

    public int LastScanDetected
    {
        get => _lastScanDetected;
        private set => SetProperty(ref _lastScanDetected, value);
    }

    public int LastScanApplied
    {
        get => _lastScanApplied;
        private set => SetProperty(ref _lastScanApplied, value);
    }

    public int LastScanTotal
    {
        get => _lastScanTotal;
        private set => SetProperty(ref _lastScanTotal, value);
    }

    public int LastScanSkipped
    {
        get => _lastScanSkipped;
        private set => SetProperty(ref _lastScanSkipped, value);
    }

    public bool HasLastScanSummary => _lastScanAt.HasValue;

    public string LastScanTimeAgo => _lastScanAt.HasValue
        ? $"{FormatRelativeTime(DateTimeOffset.Now - _lastScanAt.Value)} ago"
        : string.Empty;

    public Task RunScanAsync()
    {
        return RunScanAsync(null, CancellationToken.None, false, true);
    }

    public async Task RunScanAsync(
        IProgress<StartupScanProgress>? progress,
        CancellationToken ct,
        bool isStartupScan,
        bool forceRedetect)
    {
        if (_tweaksViewModel == null || IsScanning)
        {
            return;
        }

        IsScanning = true;
        var useFullScan = IsFullScanEnabled && !isStartupScan;
        var scanStartedAt = DateTimeOffset.Now;
        ScanCurrent = 0;
        ScanTotal = 0;
        ScanStatusText = isStartupScan
            ? "Running quick scan..."
            : useFullScan
                ? "Running full scan..."
                : "Running quick scan...";
        ScanDetailText = "Preparing scan list...";
        ScanSkippedCount = 0;
        ScanSkippedSummary = string.Empty;

        var skipElevationPrompts = !useFullScan && !_tweaksViewModel.IsElevated;
        var skipExpensiveOperations = !useFullScan;
        var scanCandidateCount = _tweaksViewModel.Tweaks.Count(t =>
            (!isStartupScan || t.IsStartupScanEligible)
            && (!skipElevationPrompts || !t.WillPromptForElevation)
            && (!skipExpensiveOperations || t.IsScanFriendly));
        var skippedCount = Math.Max(0, _tweaksViewModel.Tweaks.Count - scanCandidateCount);
        ScanTotal = scanCandidateCount;
        ScanCurrent = 0;
        ScanStatusText = scanCandidateCount > 0
            ? $"Scanning {scanCandidateCount} tweaks..."
            : "No eligible tweaks found.";
        ScanDetailText = scanCandidateCount > 0
            ? $"Queued {scanCandidateCount} tweaks."
            : "Scan skipped.";
        try
        {
            var lastUiUpdate = DateTimeOffset.MinValue;
            void UpdateProgress(StartupScanProgress snapshot)
            {
                var now = DateTimeOffset.Now;
                if (snapshot.Current != snapshot.Total && now - lastUiUpdate < TimeSpan.FromMilliseconds(120))
                {
                    return;
                }

                lastUiUpdate = now;
                ScanCurrent = snapshot.Current;
                ScanTotal = snapshot.Total;
                ScanStatusText = snapshot.Total > 0
                    ? $"Scanning tweaks {snapshot.Current}/{snapshot.Total}"
                    : "Scanning tweaks...";

                if (!string.IsNullOrWhiteSpace(snapshot.CurrentName))
                {
                    ScanDetailText = $"Checking: {snapshot.CurrentName}";
                }

                progress?.Report(snapshot);
            }

            var progressRelay = new Progress<StartupScanProgress>(UpdateProgress);
            await _tweaksViewModel.DetectAllTweaksAsync(
                progressRelay,
                ct,
                isStartupScan,
                forceRedetect,
                skipElevationPrompts,
                skipExpensiveOperations);

            RefreshSystemSnapshot();

            var elapsed = DateTimeOffset.Now - scanStartedAt;
            var detected = _tweaksViewModel.ScorableTweaksMeasuredTotal;
            var totalScorable = _tweaksViewModel.ScorableTweaksTotal;
            var applied = _tweaksViewModel.ScorableTweaksApplied;
            var summary = $"Scan complete in {elapsed:mm\\:ss}. Detected {detected}/{totalScorable} (Safe+Advanced). Applied: {applied}.";
            if (skippedCount > 0)
            {
                summary += $" Skipped {skippedCount} admin-required or long-running tweaks.";
            }

            LastScanDuration = elapsed;
            LastScanMode = useFullScan ? "Full scan" : "Quick scan";
            LastScanDetected = detected;
            LastScanApplied = applied;
            LastScanTotal = totalScorable;
            LastScanSkipped = skippedCount;

            _lastScanAt = DateTimeOffset.Now;
            OnPropertyChanged(nameof(HasLastScanSummary));
            OnPropertyChanged(nameof(LastScanTimeAgo));

            LastScanSummary = summary;
            LastScanTimestamp = _lastScanAt.HasValue
                ? $"Last scan: {_lastScanAt.Value:HH:mm:ss} ({LastScanTimeAgo})"
                : "Last scan: -";
            ScanDetailText = string.Empty;
            ScanStatusText = "Scan complete.";

            ScanSkippedCount = skippedCount;
            if (skippedCount > 0)
            {
                ScanSkippedSummary = useFullScan
                    ? $"{skippedCount} tweaks were skipped due to scan filters."
                    : $"Skipped {skippedCount} admin/long tweaks. Enable Full scan to include them.";
            }
        }
        catch (OperationCanceledException)
        {
            ScanDetailText = string.Empty;
            ScanStatusText = "Scan cancelled.";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void RunFullScan()
    {
        if (IsScanning)
        {
            return;
        }

        IsFullScanEnabled = true;
        _ = RunScanAsync();
    }

    private async Task CreateRestorePointAsync()
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

    private async Task EnableVssServiceAsync()
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
                OnPropertyChanged(nameof(AppliedTweaksAngle));
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
                OnPropertyChanged(nameof(AppliedTweaksAngle));
            }
        }
    }

    public double AppliedTweaksAngle => TotalTweaksAvailable > 0 
        ? (double)TweaksApplied / TotalTweaksAvailable * 360.0 
        : 0;

    public int DocsMissingCount
    {
        get => _docsMissingCount;
        private set
        {
            if (SetProperty(ref _docsMissingCount, value))
            {
                OnPropertyChanged(nameof(DocsCoverageOk));
                OnPropertyChanged(nameof(DocsCoverageWarn));
                OnPropertyChanged(nameof(DocsCoverageCritical));
            }
        }
    }

    public string DocsCoverageSummary
    {
        get => _docsCoverageSummary;
        private set => SetProperty(ref _docsCoverageSummary, value);
    }

    public string DocsCoverageReportPath
    {
        get => _docsCoverageReportPath;
        private set
        {
            if (SetProperty(ref _docsCoverageReportPath, value))
            {
                ((RelayCommand)OpenDocsCoverageReportCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool DocsCoverageOk => DocsMissingCount == 0;

    public bool DocsCoverageWarn => DocsMissingCount > 0 && DocsMissingCount <= 10;

    public bool DocsCoverageCritical => DocsMissingCount > 10;

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
                OnPropertyChanged(nameof(OptimizationScoreAngle));
            }
        }
    }

    public double OptimizationScoreAngle => (double)OptimizationScore / 100.0 * 360.0;

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
        : ProcessElevation.IsElevated()
            ? "Unavailable"
            : "Requires admin";

    public bool IsBootDurationAvailable => LastBootDuration.HasValue;

    public string BootTimeTooltip => IsBootDurationAvailable
        ? LastBootTimeFormatted
        : $"{LastBootTimeFormatted} • Boot duration unavailable (run as admin / enable Diagnostics-Performance log).";

    public string BootDurationBadgeValue => IsBootDurationAvailable
        ? LastBootDurationFormatted
        : BootMetricsStatusMessage;

    public bool IsEnablingBootMetrics
    {
        get => _isEnablingBootMetrics;
        private set
        {
            if (SetProperty(ref _isEnablingBootMetrics, value))
            {
                ((RelayCommand)EnableBootMetricsCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanEnableBootMetrics => _bootMetricsState == BootMetricsState.Disabled;

    public string BootMetricsStatusMessage
    {
        get => _bootMetricsStatusMessage;
        private set
        {
            if (SetProperty(ref _bootMetricsStatusMessage, value))
            {
                OnPropertyChanged(nameof(BootDurationBadgeValue));
            }
        }
    }

    private enum BootMetricsState
    {
        Unknown,
        Enabled,
        Disabled,
        NotAvailable,
        RequiresAdmin
    }

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
    public string OsVersion => _osVersion;
    public string OsBuild => _osBuild;
    public string OsDisplayVersion => _osDisplayVersion;
    public string MachineName => _machineName;
    public string UserName => _userName;
    public int ProcessorCount => _processorCount;
    public string CpuName => _cpuName;
    public string SystemArchitecture => _systemArchitecture;
    public string TotalMemoryFormatted => _totalMemoryFormatted;
    public string AvailableMemoryFormatted => _availableMemoryFormatted;
    public string SystemUptime => _systemUptime;
    public string DotNetVersion => _dotNetVersion;
    public string GpuName => _gpuName;
    public string GpuDriverVersion => _gpuDriverVersion;
    public string PrimaryDiskType => _primaryDiskType;
    public string BiosVersion => _biosVersion;
    public string BaseboardModel => _baseboardModel;
    public string FirmwareType => _firmwareType;
    public string SecureBootStatus => _secureBootStatus;
    public string VirtualizationStatus => _virtualizationStatus;
    public string TpmStatus => _tpmStatus;
    public string WindowsUpdateStatus => _windowsUpdateStatus;
    public string BitLockerStatus => _bitLockerStatus;
    public bool ShowPreviewHint => _showPreviewHint;

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

    private static string GetOsDisplayVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var displayVersion = key?.GetValue("DisplayVersion")?.ToString();
            if (!string.IsNullOrWhiteSpace(displayVersion))
            {
                return displayVersion;
            }

            var releaseId = key?.GetValue("ReleaseId")?.ToString();
            if (!string.IsNullOrWhiteSpace(releaseId))
            {
                return releaseId;
            }
        }
        catch { }

        return "Unknown";
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
    private static string GetGpuDriverVersion()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DriverVersion, DriverDate FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                var version = obj["DriverVersion"]?.ToString();
                if (string.IsNullOrWhiteSpace(version))
                {
                    continue;
                }

                var dateText = obj["DriverDate"]?.ToString();
                if (!string.IsNullOrWhiteSpace(dateText))
                {
                    try
                    {
                        var driverDate = ManagementDateTimeConverter.ToDateTime(dateText);
                        return $"{version} ({driverDate:yyyy-MM-dd})";
                    }
                    catch
                    {
                        return version;
                    }
                }

                return version;
            }
        }
        catch { }

        return "Unknown";
    }

    [SupportedOSPlatform("windows")]
    private static string GetBiosVersion()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, BIOSVersion, Version FROM Win32_BIOS");
            foreach (var obj in searcher.Get())
            {
                var smbios = obj["SMBIOSBIOSVersion"]?.ToString();
                if (!string.IsNullOrWhiteSpace(smbios))
                {
                    return smbios;
                }

                if (obj["BIOSVersion"] is string[] versions && versions.Length > 0)
                {
                    return versions[0];
                }

                var version = obj["Version"]?.ToString();
                if (!string.IsNullOrWhiteSpace(version))
                {
                    return version;
                }
            }
        }
        catch { }

        return "Unknown";
    }

    [SupportedOSPlatform("windows")]
    private static string GetBaseboardModel()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
            {
                var manufacturer = obj["Manufacturer"]?.ToString();
                var product = obj["Product"]?.ToString();

                var combined = $"{manufacturer} {product}".Trim();
                if (!string.IsNullOrWhiteSpace(combined))
                {
                    return combined;
                }
            }
        }
        catch { }

        return "Unknown";
    }

    private static string GetCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var name = key?.GetValue("ProcessorNameString")?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }
        }
        catch { }

        return "Unknown";
    }

    private static string GetFirmwareType()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
            var value = key?.GetValue("PEFirmwareType");
            if (value is int intValue)
            {
                return intValue switch
                {
                    1 => "Legacy BIOS",
                    2 => "UEFI",
                    _ => "Unknown"
                };
            }

            if (int.TryParse(value?.ToString(), out var parsed))
            {
                return parsed switch
                {
                    1 => "Legacy BIOS",
                    2 => "UEFI",
                    _ => "Unknown"
                };
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
            var scope = TryConnectScope(
                @"\\.\root\Microsoft\Windows\Storage",
                "BootDiskStorage",
                logFailure: false)
                ?? TryConnectScope(
                @"root\Microsoft\Windows\Storage",
                "BootDiskStorage",
                logFailure: true);
            if (scope == null)
            {
                return null;
            }

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
            var scope = TryConnectScope(
                @"\\.\root\CIMV2\Security\MicrosoftTpm",
                "TPM",
                logFailure: false)
                ?? TryConnectScope(
                    @"root\CIMV2\Security\MicrosoftTpm",
                    "TPM",
                    logFailure: true);
            if (scope == null)
            {
                return "Not detected";
            }
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
        catch (Exception ex)
        {
            LogProbeException("TPM", ex);
        }

        return "Unknown";
    }

    private static string GetWindowsUpdateStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "Unavailable";
        }

        try
        {
            if (IsRebootPending())
            {
                return "Restart required";
            }

            var lastSuccess = GetWindowsUpdateLastSuccessTime();
            if (lastSuccess.HasValue)
            {
                return $"Last update {lastSuccess.Value:yyyy-MM-dd}";
            }
        }
        catch
        {
        }

        return "No pending restart";
    }

    private static DateTimeOffset? GetWindowsUpdateLastSuccessTime()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\Results\Install");
        var raw = key?.GetValue("LastSuccessTime");
        if (raw is DateTime dateTime)
        {
            return new DateTimeOffset(dateTime);
        }

        if (raw is string text && DateTime.TryParse(text, out var parsed))
        {
            return new DateTimeOffset(parsed);
        }

        return null;
    }

    private static bool IsRebootPending()
    {
        try
        {
            using var wuKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            if (wuKey != null)
            {
                return true;
            }

            using var cbsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
            if (cbsKey != null)
            {
                return true;
            }

            using var sessionManager = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager");
            if (sessionManager?.GetValue("PendingFileRenameOperations") != null)
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    [SupportedOSPlatform("windows")]
    private static string GetBitLockerStatus()
    {
        try
        {
            var systemDrive = (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\');
            var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftVolumeEncryption");
            scope.Connect();
            var query = new ObjectQuery("SELECT DriveLetter, ProtectionStatus FROM Win32_EncryptableVolume");
            using var searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject volume in searcher.Get())
            {
                var drive = volume["DriveLetter"]?.ToString();
                if (!string.Equals(drive, systemDrive, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var statusValue = volume["ProtectionStatus"];
                var status = Convert.ToInt32(statusValue ?? 2);
                return status switch
                {
                    0 => "Off",
                    1 => "On",
                    _ => "Unknown"
                };
            }
        }
        catch
        {
            return "Unavailable";
        }

        return "Unknown";
    }

    private static string FormatRelativeTime(TimeSpan span)
    {
        if (span.TotalSeconds < 60)
        {
            return "just now";
        }

        if (span.TotalMinutes < 60)
        {
            return $"{(int)span.TotalMinutes}m";
        }

        if (span.TotalHours < 24)
        {
            var hours = (int)span.TotalHours;
            var minutes = span.Minutes;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        return $"{(int)span.TotalDays}d";
    }

    [SupportedOSPlatform("windows")]
    private static ManagementScope? TryConnectScope(string scopePath, string label, bool logFailure)
    {
        Exception? lastError = null;
        foreach (var candidate in BuildScopeCandidates(scopePath))
        {
            try
            {
                var scope = new ManagementScope(candidate)
                {
                    Options =
                    {
                        EnablePrivileges = true,
                        Impersonation = ImpersonationLevel.Impersonate
                    }
                };
                scope.Connect();
                return scope;
            }
            catch (ManagementException ex) when (IsScopeError(ex))
            {
                lastError = ex;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        if (logFailure && lastError != null)
        {
            LogProbeException(label, lastError);
        }

        return null;
    }

    private static bool IsScopeError(ManagementException ex)
        => ex.ErrorCode is ManagementStatus.InvalidNamespace
            or ManagementStatus.InvalidClass
            or ManagementStatus.InvalidParameter;

    private static IEnumerable<string> BuildScopeCandidates(string scopePath)
    {
        var candidates = new List<string>();

        void AddCandidate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (candidates.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            candidates.Add(value);
        }

        AddCandidate(scopePath);

        if (TryExtractNamespace(scopePath, out var namespacePath))
        {
            AddCandidate($@"\\.\{namespacePath}");
            AddCandidate($@"\\{Environment.MachineName}\{namespacePath}");
            AddCandidate($@"\\localhost\{namespacePath}");
            AddCandidate(namespacePath);
        }

        return candidates;
    }

    private static bool TryExtractNamespace(string scopePath, out string namespacePath)
    {
        namespacePath = string.Empty;
        if (string.IsNullOrWhiteSpace(scopePath))
        {
            return false;
        }

        var trimmed = scopePath.Trim();
        if (trimmed.StartsWith(@"\\", StringComparison.Ordinal))
        {
            var parts = trimmed.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                namespacePath = string.Join('\\', parts.Skip(1));
                return true;
            }
        }

        if (trimmed.StartsWith("root\\", StringComparison.OrdinalIgnoreCase))
        {
            namespacePath = trimmed;
            return true;
        }

        return false;
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

    private static BootMetricsState GetBootMetricsState()
    {
        try
        {
            using var config = new EventLogConfiguration("Microsoft-Windows-Diagnostics-Performance/Operational");
            return config.IsEnabled ? BootMetricsState.Enabled : BootMetricsState.Disabled;
        }
        catch (EventLogNotFoundException)
        {
            return BootMetricsState.NotAvailable;
        }
        catch (UnauthorizedAccessException)
        {
            return BootMetricsState.RequiresAdmin;
        }
        catch
        {
            return BootMetricsState.Unknown;
        }
    }

    private static string BuildBootMetricsStatusMessage(BootMetricsState state)
    {
        return state switch
        {
            BootMetricsState.Disabled => "Boot metrics disabled",
            BootMetricsState.NotAvailable => "Boot metrics unavailable",
            BootMetricsState.RequiresAdmin => "Run as admin for boot metrics",
            BootMetricsState.Enabled => "Reboot to collect boot duration",
            _ => "Boot duration unavailable"
        };
    }

    private void LoadSystemSnapshot()
    {
        _osVersion = GetOsVersion();
        _osBuild = Environment.OSVersion.Version.Build.ToString();
        _osDisplayVersion = GetOsDisplayVersion();
        _machineName = Environment.MachineName;
        _userName = Environment.UserName;
        _processorCount = Environment.ProcessorCount;
        _cpuName = GetCpuName();
        _systemArchitecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        _totalMemoryFormatted = GetTotalMemoryFormatted();
        _availableMemoryFormatted = GetAvailableMemoryFormatted();
        _systemUptime = GetSystemUptime();
        _dotNetVersion = Environment.Version.ToString();
        _gpuName = GetGpuName();
        _gpuDriverVersion = GetGpuDriverVersion();
        _primaryDiskType = GetPrimaryDiskType();
        _biosVersion = GetBiosVersion();
        _baseboardModel = GetBaseboardModel();
        _firmwareType = GetFirmwareType();
        _secureBootStatus = GetSecureBootStatus();
        _virtualizationStatus = GetVirtualizationStatus();
        _tpmStatus = GetTpmStatus();
        _windowsUpdateStatus = GetWindowsUpdateStatus();
        _bitLockerStatus = GetBitLockerStatus();
        _bootMetricsState = GetBootMetricsState();
        BootMetricsStatusMessage = BuildBootMetricsStatusMessage(_bootMetricsState);
    }

    private void RefreshSystemSnapshot()
    {
        LoadSystemSnapshot();
        OnPropertyChanged(nameof(OsVersion));
        OnPropertyChanged(nameof(OsBuild));
        OnPropertyChanged(nameof(OsDisplayVersion));
        OnPropertyChanged(nameof(SystemArchitecture));
        OnPropertyChanged(nameof(MachineName));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(ProcessorCount));
        OnPropertyChanged(nameof(CpuName));
        OnPropertyChanged(nameof(TotalMemoryFormatted));
        OnPropertyChanged(nameof(AvailableMemoryFormatted));
        OnPropertyChanged(nameof(SystemUptime));
        OnPropertyChanged(nameof(DotNetVersion));
        OnPropertyChanged(nameof(GpuName));
        OnPropertyChanged(nameof(GpuDriverVersion));
        OnPropertyChanged(nameof(PrimaryDiskType));
        OnPropertyChanged(nameof(BiosVersion));
        OnPropertyChanged(nameof(BaseboardModel));
        OnPropertyChanged(nameof(FirmwareType));
        OnPropertyChanged(nameof(SecureBootStatus));
        OnPropertyChanged(nameof(VirtualizationStatus));
        OnPropertyChanged(nameof(TpmStatus));
        OnPropertyChanged(nameof(WindowsUpdateStatus));
        OnPropertyChanged(nameof(BitLockerStatus));
        OnPropertyChanged(nameof(LastBootTimeFormatted));
        OnPropertyChanged(nameof(LastBootDurationFormatted));
        OnPropertyChanged(nameof(AverageBootDurationFormatted));
        OnPropertyChanged(nameof(BootTimeImprovementFormatted));
        OnPropertyChanged(nameof(HasBootTimeImprovement));
        OnPropertyChanged(nameof(HasBootTimeHistory));
        OnPropertyChanged(nameof(RecentBootDurations));
        OnPropertyChanged(nameof(IsBootDurationAvailable));
        OnPropertyChanged(nameof(BootTimeTooltip));
        OnPropertyChanged(nameof(BootDurationBadgeValue));
        OnPropertyChanged(nameof(CanEnableBootMetrics));
        OnPropertyChanged(nameof(BootMetricsStatusMessage));
    }

    private async Task LoadUiSettingsAsync()
    {
        try
        {
            var settingsStore = new SettingsStore(_paths);
            var settings = await settingsStore.LoadAsync(CancellationToken.None);
            _showPreviewHint = settings.ShowPreviewHint;
            OnPropertyChanged(nameof(ShowPreviewHint));
        }
        catch
        {
            // Ignore settings load failures for UI hints
        }
    }

    private async Task EnableBootMetricsAsync()
    {
        if (!CanEnableBootMetrics || IsEnablingBootMetrics)
        {
            return;
        }

        IsEnablingBootMetrics = true;
        BootMetricsStatusMessage = "Enabling boot metrics...";

        try
        {
            var runner = CreateElevatedCommandRunner();
            var request = new CommandRequest(
                "wevtutil.exe",
                new[] { "sl", "Microsoft-Windows-Diagnostics-Performance/Operational", "/e:true" });
            var result = await runner.RunAsync(request, CancellationToken.None);

            if (result.ExitCode == 0)
            {
                _bootMetricsState = BootMetricsState.Enabled;
                BootMetricsStatusMessage = "Boot metrics enabled (reboot to collect).";
            }
            else
            {
                BootMetricsStatusMessage = "Failed to enable boot metrics.";
            }
        }
        catch (Exception ex)
        {
            BootMetricsStatusMessage = $"Enable failed: {ex.Message}";
        }
        finally
        {
            IsEnablingBootMetrics = false;
            OnPropertyChanged(nameof(CanEnableBootMetrics));
        }
    }

    private static ICommandRunner CreateElevatedCommandRunner()
    {
        var hostPath = ElevatedHostLocator.GetExecutablePath();
        var client = new ElevatedHostClient(new ElevatedHostClientOptions
        {
            HostExecutablePath = hostPath,
            PipeName = ElevatedHostDefaults.PipeName,
            ParentProcessId = Process.GetCurrentProcess().Id
        });
        return new ElevatedCommandRunner(client);
    }

    // Recent Activity Timeline
    public ICommand RefreshActivityCommand { get; }
    public ICommand OpenActivityLinkCommand { get; }

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
            var entries = await _logStore.GetRecentHistoryAsync(20, CancellationToken.None);
            var tweakItems = entries
                .Select(e => new ActivityTimelineItem(
                    e.Timestamp.LocalDateTime,
                    e.TweakName,
                    e.Action.ToString(),
                    e.Status == Core.TweakStatus.Applied || e.Status == Core.TweakStatus.RolledBack,
                    e.Message))
                .ToList();

            var appItems = ReadRecentAppActivity(20);
            RecentActivity = tweakItems
                .Concat(appItems)
                .OrderByDescending(item => item.Timestamp)
                .Take(10)
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            RecentActivity = Array.Empty<ActivityTimelineItem>();
        }
    }

    private IEnumerable<ActivityTimelineItem> ReadRecentAppActivity(int maxItems)
    {
        var logPath = _paths.LogFilePath;
        if (!File.Exists(logPath))
        {
            return Array.Empty<ActivityTimelineItem>();
        }

        var items = new List<ActivityTimelineItem>();
        foreach (var line in ReadTailLines(logPath, Math.Max(50, maxItems * 5)))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var timestampEnd = line.IndexOf(' ');
            if (timestampEnd <= 0)
            {
                continue;
            }

            var timestampText = line[..timestampEnd];
            if (!DateTimeOffset.TryParse(timestampText, out var timestamp))
            {
                continue;
            }

            var levelStart = line.IndexOf('[', timestampEnd);
            var levelEnd = levelStart >= 0 ? line.IndexOf(']', levelStart + 1) : -1;
            if (levelStart < 0 || levelEnd < 0)
            {
                continue;
            }

            var level = line.Substring(levelStart + 1, levelEnd - levelStart - 1);
            var message = line[(levelEnd + 1)..].Trim();
            if (!message.StartsWith("Activity:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = message["Activity:".Length..].Trim();
            if (string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            var source = "App";
            var detail = payload;
            var splitIndex = payload.IndexOf(" - ", StringComparison.Ordinal);
            if (splitIndex >= 0)
            {
                source = payload[..splitIndex].Trim();
                detail = payload[(splitIndex + 3)..].Trim();
            }

            var action = level.Equals("WARN", StringComparison.OrdinalIgnoreCase)
                ? "Warning"
                : level.Equals("ERROR", StringComparison.OrdinalIgnoreCase)
                    ? "Error"
                    : source;

            var success = !level.Equals("ERROR", StringComparison.OrdinalIgnoreCase);
            var linkPath = ResolveActivityLink(detail);
            items.Add(new ActivityTimelineItem(timestamp.LocalDateTime, source, action, success, detail, linkPath));
        }

        return items;
    }

    private string? ResolveActivityLink(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        var candidate = ExtractParenthetical(detail);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = ExtractPathToken(detail);
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        candidate = Environment.ExpandEnvironmentVariables(candidate.Trim().Trim('"'));
        if (Path.IsPathRooted(candidate))
        {
            return candidate;
        }

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var desktopPath = Path.Combine(desktop, candidate);
        if (File.Exists(desktopPath))
        {
            return desktopPath;
        }

        var logPath = Path.Combine(_paths.LogDirectory, candidate);
        if (File.Exists(logPath))
        {
            return logPath;
        }

        return null;
    }

    private static string? ExtractParenthetical(string detail)
    {
        var start = detail.LastIndexOf('(');
        var end = detail.LastIndexOf(')');
        if (start >= 0 && end > start)
        {
            return detail.Substring(start + 1, end - start - 1).Trim();
        }

        return null;
    }

    private static string? ExtractPathToken(string detail)
    {
        var tokens = new[] { " to ", " at ", " opened ", " saved " };
        foreach (var token in tokens)
        {
            var index = detail.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var candidate = detail[(index + token.Length)..].Trim();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }

        var driveIndex = detail.IndexOf(":\\", StringComparison.Ordinal);
        if (driveIndex > 0)
        {
            var start = driveIndex - 1;
            while (start > 0 && char.IsLetter(detail[start - 1]))
            {
                start--;
            }

            return detail[start..].Trim();
        }

        return null;
    }

    private static IEnumerable<string> ReadTailLines(string path, int maxLines)
    {
        var queue = new Queue<string>(maxLines);
        foreach (var line in File.ReadLines(path))
        {
            if (queue.Count >= maxLines)
            {
                queue.Dequeue();
            }

            queue.Enqueue(line);
        }

        return queue;
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

    private void LoadDocsCoverageReport()
    {
        try
        {
            var docsRoot = DocsLocator.TryFindDocsRoot();
            if (string.IsNullOrWhiteSpace(docsRoot))
            {
                DocsCoverageReportPath = string.Empty;
                DocsMissingCount = 0;
                DocsCoverageSummary = "Docs folder not found.";
                return;
            }

            var priorityHtml = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.html");
            var priorityMd = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.md");
            var priorityCsv = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.csv");
            var fallbackHtml = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.html");
            var fallbackMd = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.md");
            var fallbackCsv = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.csv");

            var reportPath = File.Exists(priorityHtml)
                ? priorityHtml
                : File.Exists(priorityMd)
                    ? priorityMd
                    : File.Exists(fallbackHtml)
                        ? fallbackHtml
                        : File.Exists(fallbackMd) ? fallbackMd : string.Empty;

            DocsCoverageReportPath = reportPath;

            var csvPath = File.Exists(priorityCsv)
                ? priorityCsv
                : File.Exists(fallbackCsv) ? fallbackCsv : string.Empty;

            if (!string.IsNullOrWhiteSpace(csvPath))
            {
                var lines = File.ReadAllLines(csvPath);
                DocsMissingCount = Math.Max(0, lines.Length - 1);
                DocsCoverageSummary = DocsMissingCount == 0 ? "All documented" : $"{DocsMissingCount} missing";
            }
            else
            {
                DocsMissingCount = 0;
                DocsCoverageSummary = string.IsNullOrWhiteSpace(reportPath)
                    ? "Docs report unavailable."
                    : "Docs report ready.";
            }
        }
        catch
        {
            DocsCoverageReportPath = string.Empty;
            DocsMissingCount = 0;
            DocsCoverageSummary = "Docs report unavailable.";
        }
    }

    private void OpenDocsCoverageReport()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DocsCoverageReportPath) || !File.Exists(DocsCoverageReportPath))
            {
                LoadDocsCoverageReport();
            }

            if (!string.IsNullOrWhiteSpace(DocsCoverageReportPath) && File.Exists(DocsCoverageReportPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DocsCoverageReportPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors
        }
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

    private void OpenActivityLink(ActivityTimelineItem? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.LinkPath))
        {
            return;
        }

        try
        {
            var target = item.LinkPath;
            if (File.Exists(target) || Directory.Exists(target) || Uri.IsWellFormedUriString(target, UriKind.Absolute))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore open failures
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
    public ActivityTimelineItem(DateTime timestamp, string tweakName, string action, bool success, string message, string? linkPath = null)
    {
        Timestamp = timestamp;
        TweakName = tweakName;
        Action = action;
        Success = success;
        Message = message;
        LinkPath = linkPath ?? string.Empty;
    }

    public DateTime Timestamp { get; }
    public string TweakName { get; }
    public string Action { get; }
    public bool Success { get; }
    public string Message { get; }
    public string LinkPath { get; }
    public bool HasLink => !string.IsNullOrWhiteSpace(LinkPath);

    public string TimestampFormatted => Timestamp.ToString("MMM dd, HH:mm");
    public string TimeAgo => FormatTimeAgo(DateTime.Now - Timestamp);

    public string ActionIcon => Action.ToLowerInvariant() switch
    {
        "apply" => "✓",
        "rollback" => "↩",
        "detect" => "🔍",
        "preview" => "👁",
        "verify" => "✔",
        "settings" => "⚙",
        "monitor" => "📈",
        "logs" => "🧾",
        "warning" => "⚠",
        "error" => "✖",
        _ => "•"
    };

    public string StatusColor => Action.ToLowerInvariant() switch
    {
        "warning" => "#EBCB8B",
        "error" => "#BF616A",
        _ => Success ? "#A3BE8C" : "#BF616A"
    };

    private static string FormatTimeAgo(TimeSpan span)
    {
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return $"{(int)(span.TotalDays / 7)}w ago";
    }
}
