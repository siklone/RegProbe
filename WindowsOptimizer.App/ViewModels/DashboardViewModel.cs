using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using WindowsOptimizer.App.Models;
using WindowsOptimizer.App.Diagnostics;
using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.App.ViewModels.Hardware;

namespace WindowsOptimizer.App.ViewModels;

/// <summary>
/// Comprehensive Dashboard showing detailed System Information similar to msinfo32.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DashboardViewModel : ViewModelBase
{
    private bool _isLoading = true;

    // System Summary
    private string _osName = "Loading...";
    private string _osVersion = "Loading...";
    private string _osBuild = "Loading...";
    private string _osArchitecture = "Loading...";
    private string _osInstallDate = "Loading...";
    private string _systemName = "Loading...";
    private string _systemManufacturer = "Loading...";
    private string _systemModel = "Loading...";
    private string _systemType = "Loading...";
    private string _systemSku = "Loading...";

    // BIOS Info
    private string _biosMode = "Loading...";
    private string _biosVersion = "Loading...";
    private string _biosDate = "Loading...";
    private string _smbiosVersion = "Loading...";
    private string _secureBootState = "Loading...";
    private string _tpmVersion = "Loading...";
    private string _bitLockerStatus = "Loading...";

    // Motherboard
    // Motherboard
    private MotherboardDetailedModel _motherboardDetails = new();
    public MotherboardDetailedModel MotherboardDetails
    {
        get => _motherboardDetails;
        set => SetProperty(ref _motherboardDetails, value);
    }
    
    // Legacy mapping
    public string BaseboardManufacturer => _motherboardDetails.Manufacturer;
    public string BaseboardProduct => _motherboardDetails.Model;
    public string BaseboardVersion => _motherboardDetails.Version;
    public string BaseboardSerialNumber { get => _baseboardSerialNumber; private set => SetProperty(ref _baseboardSerialNumber, value); }
    private string _baseboardSerialNumber = "Unknown";
    public string BaseboardStatus { get => _baseboardStatus; private set => SetProperty(ref _baseboardStatus, value); }
    private string _baseboardStatus = "Loading...";

    // Processor
    // Processor
    private CpuDetailedModel _cpuDetails = new CpuDetailedModel();
    public CpuDetailedModel CpuDetails
    {
        get => _cpuDetails;
        set => SetProperty(ref _cpuDetails, value);
    }


    // Memory Summary
    private string _totalPhysicalMemory = "Loading...";
    private string _availablePhysicalMemory = "Loading...";
    private string _totalVirtualMemory = "Loading...";
    private string _availableVirtualMemory = "Loading...";
    private string _pageFileSpace = "Loading...";
    private string _memorySlots = "Loading...";
    
    // Memory Detailed
    private MemoryDetailedModel _memoryDetails = new();
    public MemoryDetailedModel MemoryDetails
    {
        get => _memoryDetails;
        set => SetProperty(ref _memoryDetails, value);
    }

    // Graphics
    private string _gpuName = "Loading...";
    private string _gpuDriverVersion = "Loading...";
    private string _gpuDriverDate = "Loading...";
    private string _gpuVideoMemory = "Loading...";
    private string _gpuResolution = "Loading...";
    private string _gpuRefreshRate = "Loading...";
    private string _gpuSearchUrl = "";

    // System Info
    private string _windowsDirectory = "Loading...";
    private string _systemDirectory = "Loading...";
    private string _bootDevice = "Loading...";
    private string _locale = "Loading...";
    private string _timeZone = "Loading...";
    private string _userName = "Loading...";
    private string _uptime = "Loading...";
    private string _windowsBuildUrl = "";
    private string _motherboardDriverUrl = "";
    private string _chipsetDriverUrl = "";
    private string _osIconKey = GetRuntimeOsDefaultIconKey();
    private string _cpuIconKey = "cpu_default";
    private string _gpuIconKey = "gpu_default";
    private string _memoryIconKey = "memory_default";
    private string _storageIconKey = "storage_default";
    private string _motherboardIconKey = "chipset_default";
    private string _displayIconKey = "display_default";
    private string _networkIconKey = "network_default";
    private string _usbIconKey = "usb_default";
    private string _audioIconKey = "audio_default";
    private ImageSource _osIconSource = HardwareIconResolver.ResolveOsIcon(GetRuntimeOsDefaultName());
    private ImageSource _cpuIconSource = HardwareIconResolver.ResolveIcon("cpu_default", "cpu_default");
    private ImageSource _gpuIconSource = HardwareIconResolver.ResolveIcon("gpu_default", "gpu_default");
    private ImageSource _memoryIconSource = HardwareIconResolver.ResolveIcon("memory_default", "memory_default");
    private ImageSource _storageIconSource = HardwareIconResolver.ResolveIcon("storage_default", "storage_default");
    private ImageSource _motherboardIconSource = HardwareIconResolver.ResolveIcon("chipset_default", "chipset_default");
    private ImageSource _displayIconSource = HardwareIconResolver.ResolveIcon("display_default", "display_default");
    private ImageSource _networkIconSource = HardwareIconResolver.ResolveIcon("network_default", "network_default");
    private ImageSource _usbIconSource = HardwareIconResolver.ResolveIcon("usb_default", "usb_default");
    private ImageSource _audioIconSource = HardwareIconResolver.ResolveIcon("audio_default", "audio_default");

    // Security
    private string _kernelDmaProtection = "Loading...";
    private string _virtualizationSecurity = "Loading...";
    private string _deviceGuard = "Loading...";
    private string _hypervisorEnforced = "Loading...";
    private string _credentialGuard = "Loading...";
    private string _defenderStatus = "Loading...";
    private string _firewallStatus = "Loading...";
    private string _primaryAudioDevice = "Loading...";
    private string _primaryAudioManufacturer = "Loading...";
    private string _primaryAudioStatus = "Loading...";
    private int _audioDeviceCount;

    // Collections
    private ObservableCollection<RamModuleModel> _ramModules = new();
    private ObservableCollection<DiskDriveModel> _diskDrives = new();
    private ObservableCollection<UsbDeviceModel> _usbDevices = new();
    private ObservableCollection<NetworkAdapterModel> _networkAdapters = new();
    private ObservableCollection<MonitorModel> _monitors = new();
    private readonly RelayCommand _openAuditReportCommand;
    private string _auditScore = "Pending";
    private string _auditStatus = "Hardware audit pending";
    private string _auditDetail = "The audit report will appear after the first snapshot is validated.";
    private string _auditReportPath = string.Empty;
    private int _auditIssueCount;
    private int _auditWarningCount;
    private int _auditErrorCount;
    private ObservableCollection<string> _auditHighlights = new();


    public DashboardViewModel()
    {
        OpenUrlCommand = new RelayCommand(OpenUrl);
        OpenDetailCommand = new RelayCommand(OpenDetail, _ => true);
        _openAuditReportCommand = new RelayCommand(OpenAuditReport, _ => !string.IsNullOrWhiteSpace(AuditReportPath));
        OpenAuditReportCommand = _openAuditReportCommand;
        _ = LoadSystemInfoAsync();
    }

    public ICommand OpenUrlCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand OpenAuditReportCommand { get; }

    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }

    // System Summary
    public string OsName { get => _osName; private set => SetProperty(ref _osName, value); }
    public string OsVersion { get => _osVersion; private set => SetProperty(ref _osVersion, value); }
    public string OsBuild { get => _osBuild; private set => SetProperty(ref _osBuild, value); }
    public string OsArchitecture { get => _osArchitecture; private set => SetProperty(ref _osArchitecture, value); }
    public string OsInstallDate { get => _osInstallDate; private set => SetProperty(ref _osInstallDate, value); }
    public string SystemName { get => _systemName; private set => SetProperty(ref _systemName, value); }
    public string SystemManufacturer { get => _systemManufacturer; private set => SetProperty(ref _systemManufacturer, value); }
    public string SystemModel { get => _systemModel; private set => SetProperty(ref _systemModel, value); }
    public string SystemType { get => _systemType; private set => SetProperty(ref _systemType, value); }
    public string SystemSku { get => _systemSku; private set => SetProperty(ref _systemSku, value); }

    // BIOS
    public string BiosMode { get => _biosMode; private set => SetProperty(ref _biosMode, value); }
    public string BiosVersion { get => _biosVersion; private set => SetProperty(ref _biosVersion, value); }
    public string BiosDate { get => _biosDate; private set => SetProperty(ref _biosDate, value); }
    public string SmbiosVersion { get => _smbiosVersion; private set => SetProperty(ref _smbiosVersion, value); }
    public string SecureBootState { get => _secureBootState; private set => SetProperty(ref _secureBootState, value); }
    public string TpmVersion { get => _tpmVersion; private set => SetProperty(ref _tpmVersion, value); }
    public string BitLockerStatus { get => _bitLockerStatus; private set => SetProperty(ref _bitLockerStatus, value); }

    // Motherboard
    // Motherboard (Mapped to MotherboardDetails now)
    // removed backing fields, properties are above

    // Processor
    // Processor
    public string ProcessorName
    {
        get => _cpuDetails.Name;
        set { _cpuDetails.Name = value; OnPropertyChanged(); }
    }
    public string ProcessorSpeed
    {
        get => _cpuDetails.CoreSpeed;
        set { _cpuDetails.CoreSpeed = value; OnPropertyChanged(); }
    }
    public string ProcessorCores
    {
        get => _cpuDetails.Cores;
        set { _cpuDetails.Cores = value; OnPropertyChanged(); }
    }
    public string ProcessorThreads
    {
        get => _cpuDetails.Threads;
        set { _cpuDetails.Threads = value; OnPropertyChanged(); }
    }
    public string ProcessorSocket
    {
        get => _cpuDetails.Package;
        set { _cpuDetails.Package = value; OnPropertyChanged(); }
    }
    public string ProcessorL2Cache
    {
        get => _cpuDetails.L2Cache;
        set { _cpuDetails.L2Cache = value; OnPropertyChanged(); }
    }
    public string ProcessorL3Cache
    {
        get => _cpuDetails.L3Cache;
        set { _cpuDetails.L3Cache = value; OnPropertyChanged(); }
    }
    public string ProcessorSearchUrl 
    { 
        get => _cpuDetails.SearchUrl; 
        set { _cpuDetails.SearchUrl = value; OnPropertyChanged(); } 
    }

    // Memory
    public string TotalPhysicalMemory { get => _totalPhysicalMemory; private set => SetProperty(ref _totalPhysicalMemory, value); }
    public string AvailablePhysicalMemory { get => _availablePhysicalMemory; private set => SetProperty(ref _availablePhysicalMemory, value); }
    public string TotalVirtualMemory { get => _totalVirtualMemory; private set => SetProperty(ref _totalVirtualMemory, value); }
    public string AvailableVirtualMemory { get => _availableVirtualMemory; private set => SetProperty(ref _availableVirtualMemory, value); }
    public string PageFileSpace { get => _pageFileSpace; private set => SetProperty(ref _pageFileSpace, value); }
    public string MemorySlots { get => _memorySlots; private set => SetProperty(ref _memorySlots, value); }

    // Graphics
    private GpuDetailedModel _gpuDetails = new();
    public GpuDetailedModel GpuDetails { get => _gpuDetails; private set => SetProperty(ref _gpuDetails, value); }
    public string GpuName { get => _gpuName; private set => SetProperty(ref _gpuName, value); }
    public string GpuDriverVersion { get => _gpuDriverVersion; private set => SetProperty(ref _gpuDriverVersion, value); }
    public string GpuDriverDate { get => _gpuDriverDate; private set => SetProperty(ref _gpuDriverDate, value); }
    public string GpuVideoMemory { get => _gpuVideoMemory; private set => SetProperty(ref _gpuVideoMemory, value); }
    public string GpuResolution { get => _gpuResolution; private set => SetProperty(ref _gpuResolution, value); }
    public string GpuRefreshRate { get => _gpuRefreshRate; private set => SetProperty(ref _gpuRefreshRate, value); }
    public string GpuSearchUrl { get => _gpuSearchUrl; private set => SetProperty(ref _gpuSearchUrl, value); }

    // System Info
    public string WindowsDirectory { get => _windowsDirectory; private set => SetProperty(ref _windowsDirectory, value); }
    public string SystemDirectory { get => _systemDirectory; private set => SetProperty(ref _systemDirectory, value); }
    public string BootDevice { get => _bootDevice; private set => SetProperty(ref _bootDevice, value); }
    public string Locale { get => _locale; private set => SetProperty(ref _locale, value); }
    public string TimeZone { get => _timeZone; private set => SetProperty(ref _timeZone, value); }
    public string UserName { get => _userName; private set => SetProperty(ref _userName, value); }
    public string Uptime { get => _uptime; private set => SetProperty(ref _uptime, value); }
    public string WindowsBuildUrl { get => _windowsBuildUrl; private set => SetProperty(ref _windowsBuildUrl, value); }
    public string MotherboardDriverUrl { get => _motherboardDriverUrl; private set => SetProperty(ref _motherboardDriverUrl, value); }
    public string ChipsetDriverUrl { get => _chipsetDriverUrl; private set => SetProperty(ref _chipsetDriverUrl, value); }
    public string OsIconKey { get => _osIconKey; private set => SetProperty(ref _osIconKey, value); }
    public string CpuIconKey { get => _cpuIconKey; private set => SetProperty(ref _cpuIconKey, value); }
    public string GpuIconKey { get => _gpuIconKey; private set => SetProperty(ref _gpuIconKey, value); }
    public string MemoryIconKey { get => _memoryIconKey; private set => SetProperty(ref _memoryIconKey, value); }
    public string StorageIconKey { get => _storageIconKey; private set => SetProperty(ref _storageIconKey, value); }
    public string MotherboardIconKey { get => _motherboardIconKey; private set => SetProperty(ref _motherboardIconKey, value); }
    public string DisplayIconKey { get => _displayIconKey; private set => SetProperty(ref _displayIconKey, value); }
    public string NetworkIconKey { get => _networkIconKey; private set => SetProperty(ref _networkIconKey, value); }
    public string UsbIconKey { get => _usbIconKey; private set => SetProperty(ref _usbIconKey, value); }
    public string AudioIconKey { get => _audioIconKey; private set => SetProperty(ref _audioIconKey, value); }
    public ImageSource OsIconSource { get => _osIconSource; private set => SetProperty(ref _osIconSource, value); }
    public ImageSource CpuIconSource { get => _cpuIconSource; private set => SetProperty(ref _cpuIconSource, value); }
    public ImageSource GpuIconSource { get => _gpuIconSource; private set => SetProperty(ref _gpuIconSource, value); }
    public ImageSource MemoryIconSource { get => _memoryIconSource; private set => SetProperty(ref _memoryIconSource, value); }
    public ImageSource StorageIconSource { get => _storageIconSource; private set => SetProperty(ref _storageIconSource, value); }
    public ImageSource MotherboardIconSource { get => _motherboardIconSource; private set => SetProperty(ref _motherboardIconSource, value); }
    public ImageSource DisplayIconSource { get => _displayIconSource; private set => SetProperty(ref _displayIconSource, value); }
    public ImageSource NetworkIconSource { get => _networkIconSource; private set => SetProperty(ref _networkIconSource, value); }
    public ImageSource UsbIconSource { get => _usbIconSource; private set => SetProperty(ref _usbIconSource, value); }
    public ImageSource AudioIconSource { get => _audioIconSource; private set => SetProperty(ref _audioIconSource, value); }

    // Security
    public string KernelDmaProtection { get => _kernelDmaProtection; private set => SetProperty(ref _kernelDmaProtection, value); }
    public string VirtualizationSecurity { get => _virtualizationSecurity; private set => SetProperty(ref _virtualizationSecurity, value); }
    public string DeviceGuard { get => _deviceGuard; private set => SetProperty(ref _deviceGuard, value); }
    public string HypervisorEnforced { get => _hypervisorEnforced; private set => SetProperty(ref _hypervisorEnforced, value); }
    public string CredentialGuard { get => _credentialGuard; private set => SetProperty(ref _credentialGuard, value); }
    public string DefenderStatus { get => _defenderStatus; private set => SetProperty(ref _defenderStatus, value); }
    public string FirewallStatus { get => _firewallStatus; private set => SetProperty(ref _firewallStatus, value); }
    public string PrimaryAudioDevice { get => _primaryAudioDevice; private set => SetProperty(ref _primaryAudioDevice, value); }
    public string PrimaryAudioManufacturer { get => _primaryAudioManufacturer; private set => SetProperty(ref _primaryAudioManufacturer, value); }
    public string PrimaryAudioStatus { get => _primaryAudioStatus; private set => SetProperty(ref _primaryAudioStatus, value); }
    public int AudioDeviceCount { get => _audioDeviceCount; private set => SetProperty(ref _audioDeviceCount, value); }


    // Collections
    public ObservableCollection<RamModuleModel> RamModules { get => _ramModules; private set => SetProperty(ref _ramModules, value); }
    public ObservableCollection<DiskDriveModel> DiskDrives { get => _diskDrives; private set => SetProperty(ref _diskDrives, value); }
    public ObservableCollection<UsbDeviceModel> UsbDevices { get => _usbDevices; private set => SetProperty(ref _usbDevices, value); }
    public ObservableCollection<NetworkAdapterModel> NetworkAdapters { get => _networkAdapters; private set => SetProperty(ref _networkAdapters, value); }
    public ObservableCollection<MonitorModel> Monitors { get => _monitors; private set => SetProperty(ref _monitors, value); }
    public string AuditScore { get => _auditScore; private set => SetProperty(ref _auditScore, value); }
    public string AuditStatus { get => _auditStatus; private set => SetProperty(ref _auditStatus, value); }
    public string AuditDetail { get => _auditDetail; private set => SetProperty(ref _auditDetail, value); }
    public string PlatformIdentity => JoinNonEmpty(SystemManufacturer, SystemModel);
    public string OsCompactSummary => JoinNonEmpty(OsVersion, HasValue(OsBuild) ? $"Build {OsBuild}" : null, OsArchitecture);
    public string SystemEnvironmentSummary => JoinNonEmpty(
        HasValue(SystemType) ? SystemType : null,
        NormalizeOemValue(SystemSku),
        FormatInstalledDate(OsInstallDate),
        Uptime);
    public string ProcessorCompactSummary => JoinNonEmpty(
        HasValue(ProcessorCores) && HasValue(ProcessorThreads) ? $"{ProcessorCores}C / {ProcessorThreads}T" : null,
        ProcessorSpeed,
        ProcessorSocket);
    public string MemoryCompactSummary => JoinNonEmpty(
        TotalPhysicalMemory,
        MemoryDetails.Type,
        MemoryDetails.Frequency,
        HasValue(MemorySlots) ? $"{MemorySlots} modules" : null);
    public string MemoryRuntimeSummary => JoinNonEmpty(
        HasValue(AvailablePhysicalMemory) ? $"Free {AvailablePhysicalMemory}" : null,
        HasValue(AvailableVirtualMemory) ? $"Virtual {AvailableVirtualMemory}" : null,
        HasValue(PageFileSpace) ? $"Page {PageFileSpace}" : null);
    public string GraphicsCompactSummary => JoinNonEmpty(GpuVideoMemory, GpuResolution, GpuRefreshRate);
    public string FirmwareCompactSummary => JoinNonEmpty(BiosVersion, BiosDate, BiosMode);
    public string FirmwarePlatformSummary => JoinNonEmpty(
        MotherboardDetails.BiosVendor,
        HasValue(SmbiosVersion) ? $"SMBIOS {SmbiosVersion}" : null,
        MotherboardDetails.Chipset);
    public string MotherboardCompactSummary => JoinNonEmpty(
        BaseboardManufacturer,
        MotherboardDetails.Chipset,
        NormalizeOemValue(BaseboardVersion));
    public string StorageCompactSummary
    {
        get
        {
            var firstDisk = GetPrimaryDiskDrive();
            return JoinNonEmpty(
                DiskDrives.Count > 0 ? $"{DiskDrives.Count} drives" : null,
                firstDisk?.InterfacePretty,
                firstDisk?.DisplayCapacity,
                firstDisk?.Model);
        }
    }
    public string StorageCapacitySummary
    {
        get
        {
            var totalBytes = DiskDrives.Sum(static disk => Math.Max(0, disk.SizeBytes));
            var primaryDisk = GetPrimaryDiskDrive();
            return JoinNonEmpty(
                totalBytes > 0 ? $"{FormatBytes(totalBytes)} total" : null,
                primaryDisk?.LogicalDrives,
                primaryDisk?.Model);
        }
    }
    public string StorageIdentitySummary
    {
        get
        {
            var primaryDisk = GetPrimaryDiskDrive();
            return JoinNonEmpty(
                primaryDisk?.InterfacePretty,
                primaryDisk?.LogicalDrives,
                HasValue(primaryDisk?.FirmwareRevision) ? $"FW {primaryDisk!.FirmwareRevision}" : null);
        }
    }
    public string PrimaryDisplaySummary
    {
        get
        {
            var primaryDisplay = GetPrimaryMonitor();
            return JoinNonEmpty(
                primaryDisplay?.Name,
                primaryDisplay?.Resolution,
                primaryDisplay?.RefreshRate,
                primaryDisplay?.ConnectionType);
        }
    }
    public string PrimaryDisplayTechSummary
    {
        get
        {
            var primaryDisplay = GetPrimaryMonitor();
            return JoinNonEmpty(
                primaryDisplay?.PhysicalSize,
                primaryDisplay?.BitsPerPixel,
                primaryDisplay?.ConnectionType);
        }
    }
    public string PrimaryNetworkSummary
    {
        get
        {
            var primaryNetwork = GetPrimaryNetworkAdapter();
            return JoinNonEmpty(
                primaryNetwork?.Name,
                primaryNetwork?.Speed,
                primaryNetwork?.Status,
                primaryNetwork?.Type);
        }
    }
    public string NetworkEndpointSummary
    {
        get
        {
            var primaryNetwork = GetPrimaryNetworkAdapter();
            return JoinNonEmpty(
                CleanNetworkValue(primaryNetwork?.IpAddress),
                PrefixValue(CleanNetworkValue(primaryNetwork?.DefaultGateway), "GW "),
                PrefixValue(FirstDnsServer(primaryNetwork), "DNS "));
        }
    }
    public string NetworkIdentitySummary
    {
        get
        {
            var primaryNetwork = GetPrimaryNetworkAdapter();
            return JoinNonEmpty(
                PrefixValue(CleanNetworkValue(primaryNetwork?.MacAddress), "MAC "),
                HasValue(primaryNetwork?.DhcpEnabled) ? $"DHCP {primaryNetwork!.DhcpEnabled}" : null,
                primaryNetwork?.Type);
        }
    }
    public string UsbCompactSummary
    {
        get
        {
            var controllerCount = UsbDevices.Count(static device => device.IsController);
            var primaryDevice = UsbDevices.FirstOrDefault(static device => !device.IsController);
            return JoinNonEmpty(
                UsbDevices.Count > 0 ? $"{UsbDevices.Count} devices" : null,
                controllerCount > 0 ? $"{controllerCount} controllers" : null,
                primaryDevice?.Manufacturer,
                primaryDevice?.Name);
        }
    }
    public string AudioCompactSummary => JoinNonEmpty(
        AudioDeviceCount > 0 ? $"{AudioDeviceCount} devices" : null,
        PrimaryAudioManufacturer,
        PrimaryAudioDevice);
    public string SecurityCompactSummary => JoinNonEmpty(
        HasValue(SecureBootState) ? $"Secure Boot {SecureBootState}" : null,
        HasValue(TpmVersion) ? $"TPM {TpmVersion}" : null,
        HasValue(BitLockerStatus) ? $"BitLocker {BitLockerStatus}" : null);
    public string SecurityPlatformSummary => JoinNonEmpty(
        HasValue(VirtualizationSecurity) && !VirtualizationSecurity.StartsWith("Not", StringComparison.OrdinalIgnoreCase) ? $"VBS {VirtualizationSecurity}" : null,
        HasValue(DeviceGuard) && !DeviceGuard.StartsWith("Not", StringComparison.OrdinalIgnoreCase) ? $"DG {DeviceGuard}" : null,
        HasValue(CredentialGuard) && !CredentialGuard.StartsWith("Not", StringComparison.OrdinalIgnoreCase) ? $"CG {CredentialGuard}" : null,
        string.Equals(HypervisorEnforced, "Yes", StringComparison.OrdinalIgnoreCase) ? "HVCI" : null);
    public string SecurityServiceSummary => JoinNonEmpty(
        HasValue(DefenderStatus) ? $"Defender {DefenderStatus}" : null,
        HasValue(FirewallStatus) ? $"Firewall {FirewallStatus}" : null,
        HasValue(KernelDmaProtection) && !KernelDmaProtection.Equals("Off", StringComparison.OrdinalIgnoreCase) ? $"DMA {KernelDmaProtection}" : null);
    public string AuditCompactSummary => JoinNonEmpty(
        AuditScore,
        AuditErrorCount > 0 ? $"{AuditErrorCount} errors" : null,
        AuditWarningCount > 0 ? $"{AuditWarningCount} warnings" : AuditErrorCount == 0 ? "clean" : null);
    public string AuditReportPath
    {
        get => _auditReportPath;
        private set
        {
            if (SetProperty(ref _auditReportPath, value))
            {
                _openAuditReportCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public int AuditIssueCount { get => _auditIssueCount; private set => SetProperty(ref _auditIssueCount, value); }
    public int AuditWarningCount { get => _auditWarningCount; private set => SetProperty(ref _auditWarningCount, value); }
    public int AuditErrorCount { get => _auditErrorCount; private set => SetProperty(ref _auditErrorCount, value); }
    public ObservableCollection<string> AuditHighlights { get => _auditHighlights; private set => SetProperty(ref _auditHighlights, value); }

    private void OpenUrl(object? parameter)
    {
        var url = parameter as string;
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }

    private void OpenAuditReport(object? _)
    {
        if (string.IsNullOrWhiteSpace(AuditReportPath) || !File.Exists(AuditReportPath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(AuditReportPath) { UseShellExecute = true });
        }
        catch
        {
            // Ignore shell launch failures on the dashboard.
        }
    }

    public void OpenDetail(object? parameter)
    {
        if (parameter is not string section)
        {
            return;
        }

        try
        {
            Window? window = section.ToLowerInvariant() switch
            {
                "os" => new Views.OsDetailWindow(),
                "cpu" => new Views.CpuDetailWindow(),
                "gpu" => new Views.GpuDetailWindow(),
                "memory" or "ram" => new Views.MemoryDetailWindow(),
                "disk" or "storage" => new Views.StorageDetailWindow(),
                "network" => new Views.NetworkDetailWindow(),
                "usb" => new Views.UsbDetailWindow(),
                "audio" => new Views.AudioDetailWindow(),
                "motherboard" or "mobo" => new Views.MotherboardDetailWindow(),
                "displays" or "display" => new Views.DisplaysDetailWindow(),
                _ => null
            };

            if (window == null)
            {
                return;
            }

            window.Owner = System.Windows.Application.Current?.MainWindow;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException($"Dashboard detail launch failed for '{section}'", ex);
        }
    }

    private async Task LoadSystemInfoAsync()
    {
        HardwareDetailSnapshot? snapshot = null;
        try
        {
            await HardwareDbLoader.LoadAllAsync(CancellationToken.None);
            await HardwarePreloadService.Instance.PreloadAsync(CancellationToken.None);
            snapshot = HardwarePreloadService.Instance.GetSnapshot();

            await Task.Run(() =>
            {
                TryLoadDashboardSection("OS", LoadOperatingSystemInfo);
                TryLoadDashboardSection("System", LoadSystemInfo);
                TryLoadDashboardSection("BIOS", LoadBiosInfo);
                TryLoadDashboardSection("Motherboard", LoadMotherboardInfo);
                TryLoadDashboardSection("CPU", LoadProcessorInfo);
                TryLoadDashboardSection("Memory", LoadMemoryInfo);
                TryLoadDashboardSection("RAM Modules", LoadRamModulesInfo);
                TryLoadDashboardSection("GPU", LoadGraphicsInfo);
                TryLoadDashboardSection("Displays", LoadMonitorsInfo);
                TryLoadDashboardSection("Storage", LoadDiskDrivesInfo);
                TryLoadDashboardSection("USB", LoadUsbDevicesInfo);
                TryLoadDashboardSection("Network", LoadNetworkInfo);
                TryLoadDashboardSection("Audio", LoadAudioInfo);
                TryLoadDashboardSection("Security", LoadSecurityInfo);
            });

            snapshot = HardwarePreloadService.Instance.GetSnapshot();
            ApplySnapshotCorrections(snapshot);
            RunHardwareAudit(snapshot);
            NotifyCompactSummaryPropertiesChanged();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Dashboard load failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static void TryLoadDashboardSection(string sectionName, Action loader)
    {
        try
        {
            loader();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException($"Dashboard {sectionName} load failed", ex);
        }
    }

    private void ApplySnapshotCorrections(HardwareDetailSnapshot snapshot)
    {
        OsName = PreferBetter(OsName, snapshot.Os.NormalizedName);
        OsVersion = PreferBetter(OsVersion, FirstNonEmpty(snapshot.Os.DisplayVersion, snapshot.Os.Version, snapshot.Os.ReleaseId));
        if (snapshot.Os.BuildNumber > 0)
        {
            OsBuild = snapshot.Os.BuildNumber.ToString();
        }
        OsArchitecture = PreferBetter(OsArchitecture, snapshot.Os.Architecture);
        OsInstallDate = PreferBetter(OsInstallDate, snapshot.Os.InstallDate);
        Uptime = PreferBetter(Uptime, snapshot.Os.Uptime);
        UserName = PreferBetter(UserName, snapshot.Os.Username);
        SecureBootState = PreferBetter(SecureBootState, snapshot.Os.SecureBootState);
        TpmVersion = PreferBetter(TpmVersion, snapshot.Os.TpmVersion);
        BitLockerStatus = PreferBetter(BitLockerStatus, snapshot.Os.BitLockerStatus);
        BiosMode = PreferBetter(BiosMode, snapshot.Os.BiosMode);
        CredentialGuard = PreferBetter(CredentialGuard, snapshot.Os.CredentialGuardStatus);
        DeviceGuard = PreferBetter(DeviceGuard, snapshot.Os.DeviceGuardStatus);
        DefenderStatus = PreferBetter(DefenderStatus, snapshot.Os.DefenderStatus);
        FirewallStatus = PreferBetter(FirewallStatus, snapshot.Os.FirewallStatus);
        if (!string.IsNullOrWhiteSpace(snapshot.Os.VirtualizationEnabled))
        {
            VirtualizationSecurity = PreferBetter(VirtualizationSecurity, snapshot.Os.VirtualizationEnabled);
        }
        if (!string.IsNullOrWhiteSpace(snapshot.Os.HyperVInstalled))
        {
            HypervisorEnforced = PreferBetter(HypervisorEnforced, snapshot.Os.HyperVInstalled);
        }
        if (!string.IsNullOrWhiteSpace(snapshot.Os.IconKey))
        {
            OsIconKey = snapshot.Os.IconKey;
            OsIconSource = HardwareIconService.ResolveByIconKey(HardwareType.Os, snapshot.Os.IconKey, snapshot.Os.NormalizedName);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Cpu.Name))
        {
            var matchedCpu = HardwareKnowledgeDbService.Instance.MatchCpuDetailed(snapshot.Cpu.Name);
            var cpuResolution = HardwareIconService.ResolveResult(HardwareType.Cpu, snapshot.Cpu.Name, matchedCpu.Model);
            ProcessorName = PreferBetter(ProcessorName, snapshot.Cpu.Name);
            if (snapshot.Cpu.Cores > 0)
            {
                ProcessorCores = snapshot.Cpu.Cores.ToString();
            }
            if (snapshot.Cpu.Threads > 0)
            {
                ProcessorThreads = snapshot.Cpu.Threads.ToString();
            }
            if (snapshot.Cpu.MaxClockMhz > 0)
            {
                ProcessorSpeed = $"{snapshot.Cpu.MaxClockMhz} MHz";
            }
            ProcessorSocket = PreferBetter(ProcessorSocket, snapshot.Cpu.Socket);
            if (snapshot.Cpu.L1CacheKB > 0)
            {
                CpuDetails.L1Cache = FormatBytes(snapshot.Cpu.L1CacheKB * 1024L);
            }
            if (snapshot.Cpu.L2CacheKB > 0)
            {
                CpuDetails.L2Cache = FormatBytes(snapshot.Cpu.L2CacheKB * 1024L);
                OnPropertyChanged(nameof(ProcessorL2Cache));
            }
            if (snapshot.Cpu.L3CacheKB > 0)
            {
                CpuDetails.L3Cache = FormatBytes(snapshot.Cpu.L3CacheKB * 1024L);
                OnPropertyChanged(nameof(ProcessorL3Cache));
            }
            CpuDetails.CodeName = PreferBetter(CpuDetails.CodeName, matchedCpu.Model?.Codename);
            CpuDetails.Technology = PreferBetter(CpuDetails.Technology, matchedCpu.Model?.ProcessNode);
            CpuIconKey = cpuResolution.IconKey;
            CpuIconSource = HardwareIconService.Resolve(cpuResolution);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Gpu.Name))
        {
            var gpuLookupSeed = HardwareIconService.BuildGpuLookupSeed(snapshot.Gpu.Name, snapshot.Gpu.Vendor, snapshot.Gpu.PnpDeviceId);
            var matchedGpu = HardwareKnowledgeDbService.Instance.MatchGpuDetailed(gpuLookupSeed);
            var gpuResolution = HardwareIconService.ResolveResult(HardwareType.Gpu, gpuLookupSeed, matchedGpu.Model);
            GpuName = PreferBetter(GpuName, snapshot.Gpu.Name);
            GpuDetails.CodeName = PreferBetter(GpuDetails.CodeName, matchedGpu.Model?.Codename);
            GpuDetails.Technology = PreferBetter(GpuDetails.Technology, matchedGpu.Model?.ProcessNode);
            GpuDetails.MemoryType = PreferBetter(GpuDetails.MemoryType, snapshot.Gpu.InferredVramType);
            if (string.IsNullOrWhiteSpace(GpuDetails.Shaders) && matchedGpu.Model?.Units > 0)
            {
                GpuDetails.Shaders = matchedGpu.Model.Units.ToString();
            }
            if (snapshot.Gpu.AdapterRamBytes > 0)
            {
                var vram = FormatBytes(snapshot.Gpu.AdapterRamBytes);
                GpuVideoMemory = vram;
                GpuDetails.MemorySize = vram;
            }
            if (snapshot.Gpu.CurrentHorizontalResolution > 0 && snapshot.Gpu.CurrentVerticalResolution > 0)
            {
                GpuResolution = $"{snapshot.Gpu.CurrentHorizontalResolution} x {snapshot.Gpu.CurrentVerticalResolution}";
            }
            if (snapshot.Gpu.RefreshRateHz > 0)
            {
                GpuRefreshRate = $"{snapshot.Gpu.RefreshRateHz} Hz";
            }
            GpuIconKey = gpuResolution.IconKey;
            GpuIconSource = HardwareIconService.Resolve(gpuResolution);
        }

        if (!HardwareAuditHeuristics.IsPlaceholderValue(snapshot.Motherboard.Manufacturer) ||
            !HardwareAuditHeuristics.IsPlaceholderValue(snapshot.Motherboard.Product) ||
            !HardwareAuditHeuristics.IsPlaceholderValue(snapshot.Motherboard.Model))
        {
            MotherboardDetails.Manufacturer = PreferBetter(MotherboardDetails.Manufacturer, snapshot.Motherboard.Manufacturer);
            MotherboardDetails.Model = PreferBetter(MotherboardDetails.Model, FirstNonEmpty(snapshot.Motherboard.Product, snapshot.Motherboard.Model));
            MotherboardDetails.Version = PreferBetter(MotherboardDetails.Version, snapshot.Motherboard.Version);
            MotherboardDetails.BiosVendor = PreferBetter(MotherboardDetails.BiosVendor, snapshot.Motherboard.BiosVendor);
            MotherboardDetails.BiosVersion = PreferBetter(MotherboardDetails.BiosVersion, snapshot.Motherboard.BiosVersion);
            MotherboardDetails.BiosDate = PreferBetter(MotherboardDetails.BiosDate, snapshot.Motherboard.BiosDate);
            MotherboardDetails.Chipset = PreferBetter(MotherboardDetails.Chipset, snapshot.Motherboard.Chipset);
            BaseboardSerialNumber = PreferBetter(BaseboardSerialNumber, snapshot.Motherboard.Serial);

            var boardLookupSeed = HardwareIconService.BuildMotherboardLookupSeed(
                snapshot.Motherboard.Manufacturer,
                snapshot.Motherboard.Product,
                snapshot.Motherboard.Model,
                snapshot.Motherboard.Version,
                snapshot.Motherboard.BiosVendor,
                snapshot.Motherboard.Chipset);
            var boardResolution = HardwareIconService.ResolveResult(HardwareType.Motherboard, boardLookupSeed);
            MotherboardIconKey = boardResolution.IconKey;
            MotherboardIconSource = HardwareIconService.Resolve(boardResolution);
            OnPropertyChanged(nameof(BaseboardManufacturer));
            OnPropertyChanged(nameof(BaseboardProduct));
            OnPropertyChanged(nameof(BaseboardVersion));
        }

        if (snapshot.Memory.TotalBytes > 0)
        {
            TotalPhysicalMemory = FormatBytes(snapshot.Memory.TotalBytes);
        }
        if (snapshot.Memory.ModuleCount > 0)
        {
            MemorySlots = snapshot.Memory.ModuleCount.ToString();
        }
        if (!string.IsNullOrWhiteSpace(snapshot.Memory.MemoryType))
        {
            MemoryDetails.Type = snapshot.Memory.MemoryType;
        }
        if (snapshot.Memory.SpeedMhz > 0)
        {
            MemoryDetails.Frequency = $"{snapshot.Memory.SpeedMhz:F1} MHz";
            MemoryDetails.DRAMFrequency = $"{snapshot.Memory.SpeedMhz / 2.0:F1} MHz";
        }
        if (snapshot.Memory.Modules.Count > 0 && RamModules.Count == 0)
        {
            RamModules = new ObservableCollection<RamModuleModel>(snapshot.Memory.Modules.Select(module => new RamModuleModel
            {
                Slot = module.Slot ?? string.Empty,
                Bank = module.BankLabel ?? string.Empty,
                Manufacturer = module.Manufacturer ?? string.Empty,
                PartNumber = module.PartNumber ?? string.Empty,
                Capacity = FormatBytes(module.CapacityBytes),
                Speed = module.ConfiguredSpeedMhz > 0 ? $"{module.ConfiguredSpeedMhz} MHz" : module.SpeedMhz > 0 ? $"{module.SpeedMhz} MHz" : string.Empty,
                FormFactor = module.FormFactor ?? string.Empty,
                MemoryType = module.MemoryType ?? string.Empty,
                SerialNumber = module.SerialNumber ?? string.Empty,
                SearchUrl = GenerateSearchUrl($"{module.Manufacturer} {module.PartNumber}".Trim(), "ram")
            }));
        }

        if (snapshot.Storage.DeviceCount > 0)
        {
            if (DiskDrives.Count == 0)
            {
                DiskDrives = new ObservableCollection<DiskDriveModel>(snapshot.Storage.Disks.Select(disk => new DiskDriveModel
                {
                    Model = disk.Model ?? string.Empty,
                    Size = FormatBytes(disk.SizeBytes),
                    SizeBytes = disk.SizeBytes,
                    InterfaceType = disk.InterfaceType ?? string.Empty,
                    MediaType = disk.MediaType ?? string.Empty,
                    SerialNumber = disk.SerialNumber ?? string.Empty,
                    FirmwareRevision = disk.FirmwareRevision ?? string.Empty,
                    Partitions = disk.PartitionCount.ToString(),
                    Status = string.Empty,
                    SearchUrl = GenerateSearchUrl(disk.Model ?? string.Empty, "storage"),
                    InterfacePretty = SimplifyStorageInterface(disk.InterfaceType, disk.MediaType),
                    DisplayCapacity = $"[{FormatBytes(disk.SizeBytes).Replace(" ", string.Empty)}]"
                }));
            }

            var storageLookup = snapshot.Storage.PrimaryModel ?? DiskDrives.FirstOrDefault()?.Model;
            var storageResolution = HardwareIconService.ResolveResult(HardwareType.Storage, storageLookup);
            StorageIconKey = storageResolution.IconKey;
            StorageIconSource = HardwareIconService.Resolve(storageResolution);
        }

        if (snapshot.Displays.Devices.Count > 0 && Monitors.Count == 0)
        {
            Monitors = new ObservableCollection<MonitorModel>(snapshot.Displays.Devices.Select(device => new MonitorModel
            {
                Name = device.Name ?? string.Empty,
                DeviceName = device.DeviceName ?? string.Empty,
                Resolution = !string.IsNullOrWhiteSpace(device.Resolution) ? device.Resolution : $"{device.Width} x {device.Height}",
                RefreshRate = device.RefreshRateHz > 0 ? $"{device.RefreshRateHz} Hz" : string.Empty,
                BitsPerPixel = device.BitsPerPixel > 0 ? $"{device.BitsPerPixel}-bit" : string.Empty,
                IsPrimary = device.IsPrimary,
                ConnectionType = device.ConnectionType ?? string.Empty,
                PhysicalSize = device.PhysicalWidthCm > 0 && device.PhysicalHeightCm > 0 ? $"{device.PhysicalWidthCm} x {device.PhysicalHeightCm} cm" : string.Empty,
                MatchMode = device.MatchMode ?? string.Empty,
                MatchKey = device.MatchKey ?? string.Empty,
                MatchedInstance = device.MatchedInstance ?? string.Empty,
                SearchUrl = GenerateSearchUrl(device.Name ?? string.Empty, "monitor"),
                IconLookupSeed = device.IconLookupSeed ?? string.Empty
            }));
        }
        if (snapshot.Displays.Devices.Count > 0)
        {
            var primaryDisplay = snapshot.Displays.Devices.FirstOrDefault(static device => device.IsPrimary) ?? snapshot.Displays.Devices.FirstOrDefault();
            if (primaryDisplay != null)
            {
                var displayResolution = HardwareIconService.ResolveResult(HardwareType.Display, primaryDisplay.IconLookupSeed ?? primaryDisplay.Name);
                DisplayIconKey = displayResolution.IconKey;
                DisplayIconSource = HardwareIconService.Resolve(displayResolution);
            }
        }

        if (snapshot.Network.Adapters.Count > 0 && NetworkAdapters.Count == 0)
        {
            NetworkAdapters = new ObservableCollection<NetworkAdapterModel>(snapshot.Network.Adapters.Select(adapter => new NetworkAdapterModel
            {
                Name = adapter.Name ?? string.Empty,
                Description = adapter.Description ?? string.Empty,
                Type = adapter.AdapterType ?? string.Empty,
                Status = adapter.Status ?? string.Empty,
                Speed = adapter.LinkSpeed ?? string.Empty,
                MacAddress = FormatMacAddress(adapter.MacAddress ?? string.Empty),
                IpAddress = adapter.Ipv4 ?? string.Empty,
                SubnetMask = string.Empty,
                DefaultGateway = adapter.Gateway ?? string.Empty,
                DnsServers = adapter.Dns ?? string.Empty,
                DhcpEnabled = adapter.DhcpEnabled ?? string.Empty,
                DriverUrl = GenerateNetworkDriverUrl(adapter.Description ?? adapter.Name ?? string.Empty)
            }));
        }
        if (snapshot.Network.AdapterCount > 0)
        {
            var networkLookup = FirstNonEmpty(snapshot.Network.PrimaryAdapterDescription, snapshot.Network.PrimaryAdapterName);
            var networkResolution = HardwareIconService.ResolveResult(HardwareType.Network, networkLookup);
            NetworkIconKey = networkResolution.IconKey;
            NetworkIconSource = HardwareIconService.Resolve(networkResolution);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Usb.PrimaryControllerName))
        {
            var usbResolution = HardwareIconService.ResolveResult(HardwareType.Usb, snapshot.Usb.PrimaryControllerName);
            UsbIconKey = usbResolution.IconKey;
            UsbIconSource = HardwareIconService.Resolve(usbResolution);
        }

        if (snapshot.Audio.DeviceCount > 0)
        {
            AudioDeviceCount = Math.Max(AudioDeviceCount, snapshot.Audio.DeviceCount);
        }
        PrimaryAudioDevice = PreferBetter(PrimaryAudioDevice, snapshot.Audio.PrimaryDeviceName);
        PrimaryAudioManufacturer = PreferBetter(PrimaryAudioManufacturer, snapshot.Audio.PrimaryManufacturer);
        PrimaryAudioStatus = PreferBetter(PrimaryAudioStatus, snapshot.Audio.PrimaryStatus);
        if (!string.IsNullOrWhiteSpace(snapshot.Audio.PrimaryDeviceName))
        {
            var audioLookup = JoinNonEmpty(snapshot.Audio.PrimaryManufacturer, snapshot.Audio.PrimaryDeviceName);
            var audioResolution = HardwareIconService.ResolveResult(HardwareType.Audio, audioLookup);
            AudioIconKey = audioResolution.IconKey;
            AudioIconSource = HardwareIconService.Resolve(audioResolution);
        }

        NotifyCompactSummaryPropertiesChanged();
    }

    private void RunHardwareAudit(HardwareDetailSnapshot snapshot)
    {
        var report = HardwareAuditService.Instance.CreateReport(snapshot);
        var savedPath = HardwareAuditService.Instance.SaveReport(report);
        AuditScore = $"{report.Score}/100";
        AuditIssueCount = report.Issues.Count;
        AuditWarningCount = report.WarningCount;
        AuditErrorCount = report.ErrorCount;
        AuditReportPath = savedPath;
        AuditStatus = report.ErrorCount > 0
            ? "Detection issues need attention"
            : report.WarningCount > 0
                ? "Hardware snapshot is usable with a few weak spots"
                : "Hardware snapshot looks clean";
        AuditDetail = report.ErrorCount > 0 || report.WarningCount > 0
            ? $"{report.ErrorCount} errors, {report.WarningCount} warnings. Last report saved locally."
            : "No suspicious records were found in the current snapshot.";
        AuditHighlights = new ObservableCollection<string>(
            report.Issues
                .OrderByDescending(static issue => issue.Severity)
                .ThenBy(static issue => issue.Section)
                .Take(4)
                .Select(static issue => $"{issue.Section}: {issue.Message}"));
    }

    private static string PreferBetter(string currentValue, string? candidateValue)
    {
        if (string.IsNullOrWhiteSpace(candidateValue))
        {
            return currentValue;
        }

        return string.IsNullOrWhiteSpace(currentValue) ||
               currentValue.StartsWith("Loading", StringComparison.OrdinalIgnoreCase) ||
               HardwareAuditHeuristics.IsPlaceholderValue(currentValue)
            ? candidateValue.Trim()
            : currentValue;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static bool HasValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && !HardwareAuditHeuristics.IsPlaceholderValue(value);
    }

    private static string JoinNonEmpty(params string?[] values)
    {
        return string.Join(" · ", values.Where(HasValue));
    }

    private static string GetRuntimeOsDefaultName()
    {
        return Environment.OSVersion.Version.Build >= 22000 ? "Windows 11" : "Windows 10";
    }

    private static string GetRuntimeOsDefaultIconKey()
    {
        return HardwareIconResolver.ResolveOsIconKey(GetRuntimeOsDefaultName());
    }

    private static string? NormalizeOemValue(string? value)
    {
        if (!HasValue(value))
        {
            return null;
        }

        return value!.Contains("To Be Filled", StringComparison.OrdinalIgnoreCase) ? null : value;
    }

    private static string? FormatInstalledDate(string? value)
    {
        if (!HasValue(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return $"Installed {parsed:yyyy-MM-dd}";
        }

        return $"Installed {value}";
    }

    private static string? PrefixValue(string? value, string prefix)
    {
        return HasValue(value) ? $"{prefix}{value}" : null;
    }

    private static string? CleanNetworkValue(string? value)
    {
        if (!HasValue(value))
        {
            return null;
        }

        return value!.Equals("N/A", StringComparison.OrdinalIgnoreCase) ? null : value;
    }

    private static string? FirstDnsServer(NetworkAdapterModel? adapter)
    {
        if (adapter == null || string.IsNullOrWhiteSpace(adapter.DnsServers))
        {
            return null;
        }

        return adapter.DnsServers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private MonitorModel? GetPrimaryMonitor()
    {
        return Monitors.FirstOrDefault(static monitor => monitor.IsPrimary) ?? Monitors.FirstOrDefault();
    }

    private NetworkAdapterModel? GetPrimaryNetworkAdapter()
    {
        return NetworkAdapters
            .OrderByDescending(adapter => IsConnectedNetworkStatus(adapter.Status))
            .ThenByDescending(adapter => !IsVirtualNetworkType(adapter.Type))
            .ThenByDescending(adapter => !string.IsNullOrWhiteSpace(CleanNetworkValue(adapter.IpAddress)))
            .ThenBy(adapter => adapter.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private DiskDriveModel? GetPrimaryDiskDrive()
    {
        return DiskDrives
            .OrderByDescending(disk => ContainsIgnoreCase(BootDevice, disk.Model) || ContainsIgnoreCase(disk.LogicalDrives, "C:"))
            .ThenByDescending(static disk => disk.SizeBytes)
            .FirstOrDefault();
    }

    private static bool IsConnectedNetworkStatus(string? status)
    {
        return !string.IsNullOrWhiteSpace(status) &&
               (status.Contains("up", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("connected", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsVirtualNetworkType(string? type)
    {
        return !string.IsNullOrWhiteSpace(type) &&
               (type.Contains("loopback", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("tunnel", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsIgnoreCase(string? source, string? value)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               !string.IsNullOrWhiteSpace(value) &&
               source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private void NotifyCompactSummaryPropertiesChanged()
    {
        OnPropertyChanged(nameof(PlatformIdentity));
        OnPropertyChanged(nameof(OsCompactSummary));
        OnPropertyChanged(nameof(SystemEnvironmentSummary));
        OnPropertyChanged(nameof(ProcessorCompactSummary));
        OnPropertyChanged(nameof(MemoryCompactSummary));
        OnPropertyChanged(nameof(MemoryRuntimeSummary));
        OnPropertyChanged(nameof(GraphicsCompactSummary));
        OnPropertyChanged(nameof(FirmwareCompactSummary));
        OnPropertyChanged(nameof(FirmwarePlatformSummary));
        OnPropertyChanged(nameof(MotherboardCompactSummary));
        OnPropertyChanged(nameof(StorageCompactSummary));
        OnPropertyChanged(nameof(StorageCapacitySummary));
        OnPropertyChanged(nameof(StorageIdentitySummary));
        OnPropertyChanged(nameof(PrimaryDisplaySummary));
        OnPropertyChanged(nameof(PrimaryDisplayTechSummary));
        OnPropertyChanged(nameof(PrimaryNetworkSummary));
        OnPropertyChanged(nameof(NetworkEndpointSummary));
        OnPropertyChanged(nameof(NetworkIdentitySummary));
        OnPropertyChanged(nameof(UsbCompactSummary));
        OnPropertyChanged(nameof(AudioCompactSummary));
        OnPropertyChanged(nameof(SecurityCompactSummary));
        OnPropertyChanged(nameof(SecurityPlatformSummary));
        OnPropertyChanged(nameof(SecurityServiceSummary));
        OnPropertyChanged(nameof(AuditCompactSummary));
    }

    private static string SimplifyStorageInterface(string? interfaceType, string? mediaType)
    {
        if (!string.IsNullOrWhiteSpace(mediaType) &&
            mediaType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
        {
            return "NVMe";
        }

        if (!string.IsNullOrWhiteSpace(interfaceType))
        {
            if (interfaceType.Contains("USB", StringComparison.OrdinalIgnoreCase))
            {
                return "USB";
            }

            if (interfaceType.Contains("SATA", StringComparison.OrdinalIgnoreCase) ||
                interfaceType.Contains("IDE", StringComparison.OrdinalIgnoreCase))
            {
                return "SATA";
            }

            if (interfaceType.Contains("SCSI", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(mediaType) &&
                mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase))
            {
                return "NVMe";
            }

            return interfaceType;
        }

        return string.Empty;
    }

    private void LoadOperatingSystemInfo()
    {
        var resolved = OsDetectionResolver.Resolve(includeWmiCrossCheck: true);
        OsName = string.IsNullOrWhiteSpace(resolved.NormalizedName) ? "Unknown" : resolved.NormalizedName;
        OsVersion = !string.IsNullOrWhiteSpace(resolved.DisplayVersion)
            ? resolved.DisplayVersion
            : (!string.IsNullOrWhiteSpace(resolved.Version) ? resolved.Version : "Unknown");
        OsBuild = resolved.BuildNumber > 0 ? resolved.BuildNumber.ToString() : "Unknown";
        OsIconKey = resolved.IconKey;
        OsIconSource = HardwareIconResolver.ResolveIcon(resolved.IconKey, HardwareIconResolver.GetFallbackKey("os"));

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                OsArchitecture = obj["OSArchitecture"]?.ToString() ?? "Unknown";
                SystemName = obj["CSName"]?.ToString() ?? Environment.MachineName;

                var installDateStr = obj["InstallDate"]?.ToString();
                if (!string.IsNullOrEmpty(installDateStr))
                {
                    var installDate = ManagementDateTimeConverter.ToDateTime(installDateStr);
                    OsInstallDate = installDate.ToString("yyyy-MM-dd HH:mm:ss");
                }

                var lastBootStr = obj["LastBootUpTime"]?.ToString();
                if (!string.IsNullOrEmpty(lastBootStr))
                {
                    var bootTime = ManagementDateTimeConverter.ToDateTime(lastBootStr);
                    var uptime = DateTime.Now - bootTime;
                    Uptime = FormatUptime(uptime);
                }

                TotalPhysicalMemory = FormatBytes(Convert.ToInt64(obj["TotalVisibleMemorySize"] ?? 0) * 1024);
                AvailablePhysicalMemory = FormatBytes(Convert.ToInt64(obj["FreePhysicalMemory"] ?? 0) * 1024);
                TotalVirtualMemory = FormatBytes(Convert.ToInt64(obj["TotalVirtualMemorySize"] ?? 0) * 1024);
                AvailableVirtualMemory = FormatBytes(Convert.ToInt64(obj["FreeVirtualMemory"] ?? 0) * 1024);

                WindowsDirectory = obj["WindowsDirectory"]?.ToString() ?? "Unknown";
                SystemDirectory = obj["SystemDirectory"]?.ToString() ?? "Unknown";
                BootDevice = obj["BootDevice"]?.ToString() ?? "Unknown";
                Locale = obj["Locale"]?.ToString() ?? "Unknown";
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(OsArchitecture))
        {
            OsArchitecture = resolved.Architecture;
        }

        TimeZone = System.TimeZoneInfo.Local.StandardName;
        UserName = $"{Environment.UserDomainName}\\{Environment.UserName}";
        
        // Generate Windows build URL
        WindowsBuildUrl = GenerateWindowsBuildUrl(OsBuild, OsName);
    }

    private void LoadSystemInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                SystemManufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                SystemModel = obj["Model"]?.ToString() ?? "Unknown";
                SystemType = obj["SystemType"]?.ToString() ?? "Unknown";
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct");
            foreach (ManagementObject obj in searcher.Get())
            {
                SystemSku = obj["SKUNumber"]?.ToString() ?? "To Be Filled By O.E.M.";
            }
        }
        catch { }
    }

    private void LoadBiosInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? "Unknown";
                var smbiosMajor = obj["SMBIOSMajorVersion"];
                var smbiosMinor = obj["SMBIOSMinorVersion"];
                SmbiosVersion = smbiosMajor != null && smbiosMinor != null
                    ? $"{smbiosMajor}.{smbiosMinor}"
                    : "Unknown";

                var releaseDateStr = obj["ReleaseDate"]?.ToString();
                if (!string.IsNullOrEmpty(releaseDateStr))
                {
                    var releaseDate = ManagementDateTimeConverter.ToDateTime(releaseDateStr);
                    BiosDate = releaseDate.ToString("yyyy-MM-dd");
                }
            }
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            if (key != null)
            {
                BiosMode = "UEFI";
                var secureBootEnabled = (int?)key.GetValue("UEFISecureBootEnabled") ?? 0;
                SecureBootState = secureBootEnabled == 1 ? "On" : "Off";
            }
            else
            {
                BiosMode = "Legacy";
                SecureBootState = "Not Supported";
            }
        }
        catch
        {
            BiosMode = "Unknown";
            SecureBootState = "Unknown";
        }
    }

    private void LoadMotherboardInfo()
    {
        try
        {
            var details = new MotherboardDetailedModel();
            
            // BIOS Info first (for details)
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    details.BiosVendor = obj["Manufacturer"]?.ToString() ?? "";
                    details.BiosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? "";
                    var releaseDateStr = obj["ReleaseDate"]?.ToString();
                    if (!string.IsNullOrEmpty(releaseDateStr))
                    {
                        var releaseDate = ManagementDateTimeConverter.ToDateTime(releaseDateStr);
                        details.BiosDate = releaseDate.ToString("MM/dd/yyyy");
                    }
                    
                    // Populate legacy BIOS props for other views if needed
                    BiosVersion = details.BiosVersion; 
                    BiosDate = details.BiosDate;
                }
            }

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                     details.Manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                     details.Model = obj["Product"]?.ToString() ?? "Unknown";
                     details.Version = obj["Version"]?.ToString() ?? "Unknown";
                     BaseboardSerialNumber = obj["SerialNumber"]?.ToString() ?? "Unknown";
                     BaseboardStatus = obj["Status"]?.ToString() ?? "OK";
                     
                     // Attempt to get Chipset via PnP (Heuristic)
                     // Usually found in Win32_PnPEntity where Name contains "Chipset" or "LPC Controller"
                }
            }
            
            // Improved Chipset Detection
            details.Chipset = GetChipsetName();
            
            // Fallback: If PnP failed, try to infer from Model name (e.g. B550 Pro4 -> B550)
            if (string.IsNullOrEmpty(details.Chipset) || details.Chipset == "Unknown")
            {
                var match = System.Text.RegularExpressions.Regex.Match(details.Model, @"([A-Z][0-9]{3}|[X,Z,B,H,Q][0-9]{2,3})");
                if (match.Success)
                {
                    details.Chipset = match.Value + " (Inferred)";
                }
            }
            
            details.GraphicInterface = string.Empty;
            details.GraphicVersion = string.Empty;
            details.LinkWidth = string.Empty;

            MotherboardDetails = details;
            var boardLookupSeed = HardwareIconService.BuildMotherboardLookupSeed(
                details.Manufacturer,
                details.Model,
                details.Version,
                details.BiosVendor,
                details.Chipset);
            var boardIconResolution = HardwareIconService.ResolveResult(HardwareType.Motherboard, boardLookupSeed);
            MotherboardIconKey = boardIconResolution.IconKey;
            MotherboardIconSource = HardwareIconService.Resolve(boardIconResolution);

            // Generate motherboard driver URL
            MotherboardDriverUrl = GenerateMotherboardUrl(details.Manufacturer, details.Model);
            ChipsetDriverUrl = GenerateChipsetDriverUrl(details.Manufacturer, details.Model);
            
             // Trigger updates for legacy props
             OnPropertyChanged(nameof(BaseboardManufacturer));
             OnPropertyChanged(nameof(BaseboardProduct));
             OnPropertyChanged(nameof(BaseboardVersion));
        }
        catch { }
    }

    private void LoadProcessorInfo()
    {
        try
        {
            var details = new CpuDetailedModel();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    details.Name = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
                    var matchedCpu = HardwareKnowledgeDbService.Instance.MatchCpuDetailed(details.Name);
                    var cpuResolution = HardwareIconService.ResolveResult(HardwareType.Cpu, details.Name, matchedCpu.Model);
                    CpuIconKey = cpuResolution.IconKey;
                    CpuIconSource = HardwareIconService.Resolve(cpuResolution);
                    details.Specification = details.Name;
                    
                    var maxClock = obj["MaxClockSpeed"];
                    details.CoreSpeed = maxClock != null ? $"{maxClock} MHz" : "Unknown";
                    details.Cores = obj["NumberOfCores"]?.ToString() ?? "0";
                    details.Threads = obj["NumberOfLogicalProcessors"]?.ToString() ?? "0";
                    details.Package = obj["SocketDesignation"]?.ToString() ?? "Unknown";
                    
                    // Parse Description for Family/Model/Stepping
                    // Example: "Intel64 Family 6 Model 158 Stepping 10"
                    var desc = obj["Description"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(desc))
                    {
                        var familyMatch = Regex.Match(desc, @"Family (\d+)");
                        var modelMatch = Regex.Match(desc, @"Model (\d+)");
                        var steppingMatch = Regex.Match(desc, @"Stepping (\d+)");
                        
                        if (familyMatch.Success) details.Family = familyMatch.Groups[1].Value;
                        if (modelMatch.Success) details.Model = modelMatch.Groups[1].Value;
                        if (steppingMatch.Success) details.Stepping = steppingMatch.Groups[1].Value;
                        
                        // Revision is often same as Stepping or requires detailed lookup
                        details.Revision = details.Stepping;
                    }

                    // Cache
                    var l2 = Convert.ToInt64(obj["L2CacheSize"] ?? 0) * 1024;
                    var l3 = Convert.ToInt64(obj["L3CacheSize"] ?? 0) * 1024;
                    
                    // Format caches to look like "8 x 32 KBytes" if we could iterate per core, 
                    // but WMI gives total. We'll stick to total for now or simple format.
                    details.L2Cache = FormatBytes(l2);
                    details.L3Cache = FormatBytes(l3);
                    
                    details.CodeName = matchedCpu.Model?.Codename ?? string.Empty;
                    details.Technology = matchedCpu.Model?.ProcessNode ?? string.Empty;
                    details.Instructions = string.Empty;

                    var extClock = Convert.ToInt32(obj["ExtClock"] ?? 0);
                    details.BusSpeed = extClock > 0 ? $"{extClock:F1} MHz" : string.Empty;
                    if (extClock > 0 && decimal.TryParse(obj["MaxClockSpeed"]?.ToString(), out decimal maxClockVal))
                    {
                        details.Multiplier = $"x {maxClockVal / extClock:F1}";
                    }
                    else
                    {
                        details.Multiplier = string.Empty;
                    }

                    ProcessorSearchUrl = GenerateSearchUrl(details.Name, "cpu");
                }
            }

            // Fetch L1 Cache separately if possible
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_CacheMemory WHERE Level=3"); // Often maps to L1 in some WMI versions, inconsistent
                // Actually Level 3 is L2 usually? 
                // Let's just try to find small caches
            }
            catch {}
            
            // If L1 is missing, estimate logic
            if (string.IsNullOrEmpty(details.L1Cache))
            {
                 int.TryParse(details.Cores, out int cores);
                if (cores > 0) details.L1Cache = $"{cores} x 32 KB";
                 else details.L1Cache = string.Empty;
            }

            CpuDetails = details;
            
            // Trigger updates for bound properties
            OnPropertyChanged(nameof(ProcessorName));
            OnPropertyChanged(nameof(ProcessorSpeed));
            OnPropertyChanged(nameof(ProcessorCores));
            OnPropertyChanged(nameof(ProcessorThreads));
            OnPropertyChanged(nameof(ProcessorSocket));
            OnPropertyChanged(nameof(ProcessorL2Cache));
            OnPropertyChanged(nameof(ProcessorL3Cache));
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("Dashboard CPU load failed", ex);
        }
    }

    private void LoadMemoryInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PageFileUsage");
            var pageFileSizes = new List<long>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var allocated = Convert.ToInt64(obj["AllocatedBaseSize"] ?? 0) * 1024 * 1024;
                pageFileSizes.Add(allocated);
            }

            PageFileSpace = FormatBytes(DashboardInfoHelpers.SumPageFileBytes(pageFileSizes));
        }
        catch { }

        // Count memory slots
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemoryArray");
            var slotCounts = new List<int?>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var slotsValue = obj["MemoryDevices"];
                if (slotsValue != null &&
                    int.TryParse(slotsValue.ToString(), out var parsed) &&
                    parsed > 0)
                {
                    slotCounts.Add(parsed);
                }
                else
                {
                    slotCounts.Add(null);
                }
            }

            var totalSlots = DashboardInfoHelpers.SumPositiveInts(slotCounts);
            MemorySlots = totalSlots.HasValue ? totalSlots.Value.ToString() : "Unknown";
        }
        catch { }
    }

    private void LoadRamModulesInfo()
    {
        var modules = new List<RamModuleModel>();
        var memDetails = new MemoryDetailedModel();
        
        long totalCapacity = 0;
        int moduleCount = 0;
        double maxFreq = 0;
        
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            int slotIndex = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                moduleCount++;
                var manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "Unknown";
                var partNumber = obj["PartNumber"]?.ToString()?.Trim() ?? "Unknown";
                var capacity = Convert.ToInt64(obj["Capacity"] ?? 0);
                totalCapacity += capacity;
                
                var speed = obj["Speed"]?.ToString() ?? "Unknown";
                var configuredSpeed = obj["ConfiguredClockSpeed"]?.ToString();
                
                // Fallback logic for speed
                var finalSpeed = speed;
                if (int.TryParse(configuredSpeed, out int confSpeedVal) && confSpeedVal > 0)
                {
                    finalSpeed = configuredSpeed;
                    if (confSpeedVal > maxFreq) maxFreq = confSpeedVal;
                }
                else if (int.TryParse(speed, out int speedVal) && speedVal > 0)
                {
                     if (speedVal > maxFreq) maxFreq = speedVal;
                }
                
                var formFactorInt = Convert.ToInt32(obj["FormFactor"] ?? 0);
                var memoryTypeInt = Convert.ToInt32(obj["SMBIOSMemoryType"] ?? 0);
                
                var bankLabel = obj["BankLabel"]?.ToString() ?? $"Bank {slotIndex}";
                var deviceLocator = obj["DeviceLocator"]?.ToString() ?? $"DIMM {slotIndex}";
                var serialNumber = obj["SerialNumber"]?.ToString()?.Trim() ?? "Unknown";
                
                var formFactorStr = GetMemoryFormFactor(formFactorInt);
                var memoryTypeStr = GetMemoryType(memoryTypeInt);

                // Add to Legacy List
                modules.Add(new RamModuleModel
                {
                    Slot = deviceLocator,
                    Bank = bankLabel,
                    Manufacturer = manufacturer,
                    PartNumber = partNumber,
                    Capacity = FormatBytes(capacity),
                    Speed = $"{finalSpeed} MHz",
                    FormFactor = formFactorStr,
                    MemoryType = memoryTypeStr,
                    SerialNumber = serialNumber,
                    SearchUrl = GenerateSearchUrl($"{manufacturer} {partNumber}", "ram")
                });
                
                // Add to Detailed List
                var detailMod = new MemoryModuleDetailedModel
                {
                    Slot = $"#{moduleCount} [{deviceLocator}]",
                    Size = FormatBytes(capacity),
                    Type = memoryTypeStr, // e.g. DDR4
                    Manufacturer = manufacturer,
                    PartNumber = partNumber,
                    SerialNumber = serialNumber,
                    WeekYear = string.Empty
                };
                memDetails.Modules.Add(detailMod);

                // Set General Info on first module (assuming homogenous)
                if (moduleCount == 1)
                {
                    memDetails.Type = memoryTypeStr;
                }
                
                slotIndex++;
            }
            
            // Fill General Memory Details
            memDetails.Size = FormatBytes(totalCapacity);
            memDetails.Frequency = $"{maxFreq:F1} MHz";
            double realFreq = maxFreq / 2.0; // DDR double rate
            memDetails.DRAMFrequency = $"{realFreq:F1} MHz";
            memDetails.FSB_DRAM = string.Empty;
            memDetails.Channels = (moduleCount >= 2) ? "Dual" : "Single";
            if (moduleCount >= 4) memDetails.Channels = "Quad";
            
            MemoryDetails = memDetails;
            var memoryBrand = memDetails.Modules.FirstOrDefault()?.Manufacturer ?? "";
            var matchedMemory = HardwareKnowledgeDbService.Instance.MatchMemory($"{memoryBrand} {memDetails.Type}");
            MemoryIconKey = matchedMemory?.IconKey
                ?? HardwareIconResolver.ResolveIconKey("memory", $"{memoryBrand} {memDetails.Type}", HardwareIconResolver.GetFallbackKey("memory"));
            MemoryIconSource = HardwareIconResolver.ResolveIcon(MemoryIconKey, HardwareIconResolver.GetFallbackKey("memory"));
        }
        catch { }
        
        RamModules = new ObservableCollection<RamModuleModel>(modules);
    }


    private void LoadGraphicsInfo()
    {
        var details = new GpuDetailedModel();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var gpuName = obj["Name"]?.ToString() ?? "Unknown";
                var pnpDeviceId = obj["PNPDeviceID"]?.ToString();
                
                details.Name = gpuName;
                details.Vendor = obj["AdapterCompatibility"]?.ToString() ?? "Unknown";
                details.Manufacturer = details.Vendor;
                var driverDateStr = obj["DriverDate"]?.ToString();
                var driverVersion = obj["DriverVersion"]?.ToString() ?? "Unknown";
                
                details.DriverVersion = driverVersion;
                if (!string.IsNullOrEmpty(driverDateStr))
                {
                    try {
                        var driverDate = ManagementDateTimeConverter.ToDateTime(driverDateStr);
                        details.DriverDate = driverDate.ToString("MMM dd, yyyy");
                    } catch { details.DriverDate = driverDateStr; }
                }

                // Try to get VRAM from AdapterRAM first
                var adapterRam = Convert.ToInt64(obj["AdapterRAM"] ?? 0);
                
                // AdapterRAM is often capped at 4GB (uint max), try registry fallback for modern GPUs
                if (adapterRam <= 4L * 1024 * 1024 * 1024)
                {
                    var registryVram = GetGpuVramFromRegistry(gpuName, pnpDeviceId);
                    if (registryVram > adapterRam) adapterRam = registryVram;
                }

                if (adapterRam <= 4L * 1024 * 1024 * 1024)
                {
                    var knownVram = GetKnownGpuVram(gpuName);
                    if (knownVram > 0) adapterRam = knownVram;
                }
                
                details.MemorySize = FormatBytes(adapterRam);
                
                // Legacy Props Update
                GpuName = gpuName;
                GpuDriverVersion = driverVersion;
                GpuDriverDate = details.DriverDate;
                GpuVideoMemory = details.MemorySize;

                var hRes = Convert.ToInt32(obj["CurrentHorizontalResolution"] ?? 0);
                var vRes = Convert.ToInt32(obj["CurrentVerticalResolution"] ?? 0);
                GpuResolution = (hRes > 0 && vRes > 0) ? $"{hRes} x {vRes}" : "Unknown";

                var refreshRate = Convert.ToInt32(obj["CurrentRefreshRate"] ?? 0);
                
                // Generate search URL
                details.SearchUrl = GenerateGpuUrl(gpuName);
                GpuSearchUrl = details.SearchUrl;

                var gpuLookupSeed = HardwareIconService.BuildGpuLookupSeed(
                    gpuName,
                    details.Vendor,
                    details.Manufacturer,
                    details.SubVendor,
                    details.CodeName,
                    pnpDeviceId);
                var matchedGpu = HardwareKnowledgeDbService.Instance.MatchGpuDetailed(gpuLookupSeed);
                var gpuResolution = HardwareIconService.ResolveResult(HardwareType.Gpu, gpuLookupSeed, matchedGpu.Model);
                GpuIconKey = gpuResolution.IconKey;
                GpuIconSource = HardwareIconService.Resolve(gpuResolution);
                details.CodeName = matchedGpu.Model?.Codename ?? string.Empty;
                details.Technology = matchedGpu.Model?.ProcessNode ?? string.Empty;
                details.MemoryType = string.Empty;
                details.BusInterface = string.Empty;
                details.BusWidth = string.Empty;
                details.Bandwidth = string.Empty;
                details.Rops = string.Empty;
                details.Tmus = string.Empty;
                details.GpuClock = string.Empty;
                details.MemoryClock = string.Empty;
                details.BoostClock = string.Empty;
                details.Shaders = matchedGpu.Model?.Units > 0 ? matchedGpu.Model.Units.ToString() : string.Empty;
                
                break; // Only first GPU
            }
        }
        catch { }
        
        GpuDetails = details;
    }

    private void LoadMonitorsInfo()
    {
        var monitors = new List<MonitorModel>();
        try
        {
            var resolvedDisplays = DisplayDetectionHelpers.ResolveConnectedDisplays();
            monitors = resolvedDisplays.Select(display => new MonitorModel
            {
                Name = display.Name,
                DeviceName = display.DeviceName,
                Resolution = $"{display.Width} x {display.Height}",
                RefreshRate = display.RefreshRateHz > 0 ? $"{display.RefreshRateHz} Hz" : "Unknown",
                BitsPerPixel = display.BitsPerPixel > 0 ? $"{display.BitsPerPixel}-bit" : "Unknown",
                IsPrimary = display.IsPrimary,
                ConnectionType = string.IsNullOrWhiteSpace(display.ConnectionType) ? "Unknown" : display.ConnectionType,
                SearchUrl = GenerateSearchUrl(display.Name, "monitor"),
                IconLookupSeed = display.IconLookupSeed,
                PhysicalSize = FormatDisplaySize(display.PhysicalWidthCm, display.PhysicalHeightCm),
                MatchMode = display.MatchMode,
                MatchKey = display.MatchKey ?? string.Empty,
                MatchedInstance = display.MatchedInstance ?? string.Empty
            }).ToList();

            LogMonitorMatchReport(resolvedDisplays.Select(BuildMonitorReportLine).ToList());
        }
        catch { }

        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
        {
            Monitors = new ObservableCollection<MonitorModel>(monitors);
            var primaryMonitor = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
            var displayLookupSeed = primaryMonitor?.IconLookupSeed;
            if (string.IsNullOrWhiteSpace(displayLookupSeed))
            {
                displayLookupSeed = primaryMonitor?.Name;
            }

            var displayIconResolution = HardwareIconService.ResolveResult(HardwareType.Display, displayLookupSeed);
            DisplayIconKey = displayIconResolution.IconKey;
            DisplayIconSource = HardwareIconService.Resolve(displayIconResolution);
        });
    }

    private static string BuildMonitorReportLine(DisplayDetectionHelpers.ResolvedDisplayInfo display)
    {
        var sb = new StringBuilder();
        sb.Append("[Dashboard][MonitorMatch] ");
        sb.Append($"DeviceName='{display.DeviceName}' ");
        if (!string.IsNullOrWhiteSpace(display.DeviceString))
        {
            sb.Append($"DeviceString='{display.DeviceString}' ");
        }
        if (!string.IsNullOrWhiteSpace(display.DeviceId))
        {
            sb.Append($"DeviceID='{display.DeviceId}' ");
        }
        if (!string.IsNullOrWhiteSpace(display.InstanceId))
        {
            sb.Append($"Instance='{display.InstanceId}' ");
        }
        if (!string.IsNullOrWhiteSpace(display.PrefixId))
        {
            sb.Append($"Prefix='{display.PrefixId}' ");
            if (display.PrefixMatchCount > 0)
            {
                sb.Append($"PrefixMatches={display.PrefixMatchCount} ");
            }
        }
        sb.Append($"MatchMode='{display.MatchMode}' ");
        if (!string.IsNullOrWhiteSpace(display.MatchKey))
        {
            sb.Append($"MatchKey='{display.MatchKey}' ");
        }
        if (!string.IsNullOrWhiteSpace(display.MatchedInstance))
        {
            sb.Append($"MatchedInstance='{display.MatchedInstance}' ");
        }
        if (display.RegistryEdidInfo.HasValue)
        {
            AppendEdidDetails(sb, "EdidReg", display.RegistryEdidInfo.Value, null);
        }
        if (display.MatchedEdidInfo != null)
        {
            AppendEdidDetails(sb, "EdidWmi", display.MatchedEdidInfo.EdidInfo, display.MatchedEdidInfo.UserFriendlyName);
        }
        sb.Append($"Name='{display.Name}' ");
        sb.Append($"Connection='{display.ConnectionType}'");
        return sb.ToString();
    }

    private static string FormatDisplaySize(int? widthCm, int? heightCm)
    {
        return widthCm.HasValue && heightCm.HasValue
            ? $"{widthCm.Value} x {heightCm.Value} cm"
            : string.Empty;
    }

    private static void AppendEdidDetails(
        StringBuilder sb,
        string label,
        DashboardInfoHelpers.EdidInfo info,
        string? fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(info.ManufacturerId) ||
            !string.IsNullOrWhiteSpace(info.ProductCodeHex) ||
            !string.IsNullOrWhiteSpace(info.SerialNumber))
        {
            sb.Append($"{label}='{info.ManufacturerId}|{info.ProductCodeHex}|{info.SerialNumber}' ");
        }

        var displayName = !string.IsNullOrWhiteSpace(info.MonitorName) ? info.MonitorName : fallbackName;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            sb.Append($"{label}Name='{displayName}' ");
        }

        if (info.HorizontalSizeCm.HasValue && info.VerticalSizeCm.HasValue)
        {
            sb.Append($"{label}Size='{info.HorizontalSizeCm.Value}x{info.VerticalSizeCm.Value}cm' ");
        }

        if (info.MinVerticalHz.HasValue && info.MaxVerticalHz.HasValue &&
            info.MinHorizontalKHz.HasValue && info.MaxHorizontalKHz.HasValue &&
            info.MaxPixelClockMHz.HasValue)
        {
            sb.Append($"{label}Range='V{info.MinVerticalHz.Value}-{info.MaxVerticalHz.Value} ");
            sb.Append($"H{info.MinHorizontalKHz.Value}-{info.MaxHorizontalKHz.Value} ");
            sb.Append($"P{info.MaxPixelClockMHz.Value}' ");
        }
    }

    private static void LogMonitorMatchReport(IReadOnlyList<string> reportLines)
    {
        if (reportLines == null || reportLines.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[Dashboard] Monitor match report");
        foreach (var line in reportLines)
        {
            sb.AppendLine(line);
        }

        AppDiagnostics.Log(sb.ToString().TrimEnd());
    }

    private long GetGpuVramFromRegistry(string gpuName, string? pnpDeviceId)
    {
        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            if (classKey == null)
            {
                return 0;
            }

            long bestMatch = 0;
            foreach (var subKeyName in classKey.GetSubKeyNames())
            {
                using var subKey = classKey.OpenSubKey(subKeyName);
                if (subKey == null)
                {
                    continue;
                }

                if (!IsMatchingGpuKey(subKey, gpuName, pnpDeviceId))
                {
                    continue;
                }

                var vram = ReadGpuVramBytes(subKey);
                if (vram > bestMatch)
                {
                    bestMatch = vram;
                }
            }

            return bestMatch;
        }
        catch { }

        return 0;
    }

    private static bool IsMatchingGpuKey(RegistryKey key, string gpuName, string? pnpDeviceId)
    {
        var matchingId = key.GetValue("MatchingDeviceId")?.ToString();
        if (!string.IsNullOrWhiteSpace(pnpDeviceId) &&
            !string.IsNullOrWhiteSpace(matchingId) &&
            pnpDeviceId.Contains(matchingId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var driverDesc = key.GetValue("DriverDesc")?.ToString();
        if (!string.IsNullOrWhiteSpace(driverDesc) &&
            gpuName.Contains(driverDesc, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var adapterString = key.GetValue("HardwareInformation.AdapterString")?.ToString();
        if (!string.IsNullOrWhiteSpace(adapterString) &&
            gpuName.Contains(adapterString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static long ReadGpuVramBytes(RegistryKey key)
    {
        var qwMemorySize = key.GetValue("HardwareInformation.qwMemorySize");
        if (qwMemorySize is long memSize)
        {
            return memSize;
        }

        if (qwMemorySize is byte[] bytes && bytes.Length >= 8)
        {
            return BitConverter.ToInt64(bytes, 0);
        }

        return 0;
    }

    private static long GetKnownGpuVram(string gpuName)
    {
        // Known GPU VRAM database (in bytes)
        var gpuDatabase = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            // NVIDIA RTX 50 Series
            { "RTX 5090", 32L * 1024 * 1024 * 1024 },
            { "RTX 5080", 16L * 1024 * 1024 * 1024 },
            { "RTX 5070 Ti", 16L * 1024 * 1024 * 1024 },
            { "RTX 5070", 12L * 1024 * 1024 * 1024 },
            { "RTX 5060 Ti", 16L * 1024 * 1024 * 1024 },
            { "RTX 5060", 8L * 1024 * 1024 * 1024 },
            
            // NVIDIA RTX 40 Series
            { "RTX 4090", 24L * 1024 * 1024 * 1024 },
            { "RTX 4080 Super", 16L * 1024 * 1024 * 1024 },
            { "RTX 4080", 16L * 1024 * 1024 * 1024 },
            { "RTX 4070 Ti Super", 16L * 1024 * 1024 * 1024 },
            { "RTX 4070 Ti", 12L * 1024 * 1024 * 1024 },
            { "RTX 4070 Super", 12L * 1024 * 1024 * 1024 },
            { "RTX 4070", 12L * 1024 * 1024 * 1024 },
            { "RTX 4060 Ti 16GB", 16L * 1024 * 1024 * 1024 },
            { "RTX 4060 Ti", 8L * 1024 * 1024 * 1024 },
            { "RTX 4060", 8L * 1024 * 1024 * 1024 },
            
            // NVIDIA RTX 30 Series
            { "RTX 3090 Ti", 24L * 1024 * 1024 * 1024 },
            { "RTX 3090", 24L * 1024 * 1024 * 1024 },
            { "RTX 3080 Ti", 12L * 1024 * 1024 * 1024 },
            { "RTX 3080 12GB", 12L * 1024 * 1024 * 1024 },
            { "RTX 3080", 10L * 1024 * 1024 * 1024 },
            { "RTX 3070 Ti", 8L * 1024 * 1024 * 1024 },
            { "RTX 3070", 8L * 1024 * 1024 * 1024 },
            { "RTX 3060 Ti", 8L * 1024 * 1024 * 1024 },
            { "RTX 3060 12GB", 12L * 1024 * 1024 * 1024 },
            { "RTX 3060", 12L * 1024 * 1024 * 1024 },
            { "RTX 3050", 8L * 1024 * 1024 * 1024 },
            
            // AMD RX 9000 Series
            { "RX 9070 XT", 16L * 1024 * 1024 * 1024 },
            { "RX 9070", 16L * 1024 * 1024 * 1024 },
            
            // AMD RX 7000 Series
            { "RX 7900 XTX", 24L * 1024 * 1024 * 1024 },
            { "RX 7900 XT", 20L * 1024 * 1024 * 1024 },
            { "RX 7900 GRE", 16L * 1024 * 1024 * 1024 },
            { "RX 7800 XT", 16L * 1024 * 1024 * 1024 },
            { "RX 7700 XT", 12L * 1024 * 1024 * 1024 },
            { "RX 7600 XT", 16L * 1024 * 1024 * 1024 },
            { "RX 7600", 8L * 1024 * 1024 * 1024 },
            
            // AMD RX 6000 Series
            { "RX 6950 XT", 16L * 1024 * 1024 * 1024 },
            { "RX 6900 XT", 16L * 1024 * 1024 * 1024 },
            { "RX 6800 XT", 16L * 1024 * 1024 * 1024 },
            { "RX 6800", 16L * 1024 * 1024 * 1024 },
            { "RX 6750 XT", 12L * 1024 * 1024 * 1024 },
            { "RX 6700 XT", 12L * 1024 * 1024 * 1024 },
            { "RX 6650 XT", 8L * 1024 * 1024 * 1024 },
            { "RX 6600 XT", 8L * 1024 * 1024 * 1024 },
            { "RX 6600", 8L * 1024 * 1024 * 1024 },
            { "RX 6500 XT", 4L * 1024 * 1024 * 1024 },
            
            // Intel Arc
            { "Arc A770", 16L * 1024 * 1024 * 1024 },
            { "Arc A750", 8L * 1024 * 1024 * 1024 },
            { "Arc A580", 8L * 1024 * 1024 * 1024 },
            { "Arc A380", 6L * 1024 * 1024 * 1024 },
        };

        foreach (var kvp in gpuDatabase)
        {
            if (gpuName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return 0;
    }

    private void LoadDiskDrivesInfo()
    {
        var drives = new List<DiskDriveModel>();
        var msftDisks = LoadMsftPhysicalDiskInfo();
        try
        {
            // Get physical disk info
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString() ?? "Unknown";
                var matchedStorage = HardwareKnowledgeDbService.Instance.MatchStorage(model);
                StorageIconKey = matchedStorage?.IconKey
                    ?? HardwareIconResolver.ResolveIconKey("storage", model, HardwareIconResolver.GetFallbackKey("storage"));
                StorageIconSource = HardwareIconResolver.ResolveIcon(StorageIconKey, HardwareIconResolver.GetFallbackKey("storage"));
                var size = Convert.ToInt64(obj["Size"] ?? 0);
                var interfaceType = obj["InterfaceType"]?.ToString() ?? "Unknown"; // e.g. SCSI, IDE
                var mediaType = obj["MediaType"]?.ToString() ?? "Unknown";
                var serialNumber = obj["SerialNumber"]?.ToString()?.Trim() ?? "Unknown";
                var firmwareRevision = obj["FirmwareRevision"]?.ToString() ?? "Unknown";
                var partitions = obj["Partitions"]?.ToString() ?? "0";
                var deviceId = obj["DeviceID"]?.ToString() ?? "";
                var diskIndex = TryGetUInt(obj["Index"]);

                // Determine if external or internal
                var isExternal = interfaceType.Contains("USB", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Contains("Removable", StringComparison.OrdinalIgnoreCase) ||
                                 mediaType.Contains("External", StringComparison.OrdinalIgnoreCase);

                // Get disk type (SSD/HDD/NVMe)
                var diskType = GetDiskType(msftDisks, diskIndex, model, interfaceType);

                // Get associated logical drives
                var logicalDrives = GetLogicalDrivesForDisk(deviceId);

                var interfacePretty = SimplifyStorageInterface(interfaceType, diskType);

                var formattedSize = FormatBytes(size);

                drives.Add(new DiskDriveModel
                {
                    Model = model,
                    Size = formattedSize,
                    SizeBytes = size,
                    InterfaceType = interfaceType,
                    MediaType = diskType,
                    SerialNumber = serialNumber,
                    FirmwareRevision = firmwareRevision,
                    Partitions = partitions,
                    IsExternal = isExternal,
                    LogicalDrives = logicalDrives,
                    Status = obj["Status"]?.ToString() ?? "Unknown",
                    SearchUrl = GenerateSearchUrl(model, "storage"),
                    InterfacePretty = interfacePretty,
                    DisplayCapacity = $"[{formattedSize.Replace(" ", "")}]" // e.g. [500GB]
                });
            }
        }
        catch { }

        System.Windows.Application.Current?.Dispatcher?.Invoke(() => DiskDrives = new ObservableCollection<DiskDriveModel>(drives));
    }

    private static Dictionary<uint, (int mediaType, int busType)> LoadMsftPhysicalDiskInfo()
    {
        var map = new Dictionary<uint, (int mediaType, int busType)>();
        try
        {
            var scope = new ManagementScope(@"root\Microsoft\Windows\Storage");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DeviceId, MediaType, BusType FROM MSFT_PhysicalDisk"));
            foreach (ManagementObject obj in searcher.Get())
            {
                var deviceId = TryGetUInt(obj["DeviceId"]);
                if (!deviceId.HasValue)
                {
                    continue;
                }

                var mediaType = Convert.ToInt32(obj["MediaType"] ?? 0);
                var busType = Convert.ToInt32(obj["BusType"] ?? 0);
                map[deviceId.Value] = (mediaType, busType);
            }
        }
        catch { }

        return map;
    }

    private static uint? TryGetUInt(object? value)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            return Convert.ToUInt32(value);
        }
        catch
        {
            return null;
        }
    }

    private static string GetDiskType(
        IReadOnlyDictionary<uint, (int mediaType, int busType)> msftDisks,
        uint? diskIndex,
        string model,
        string interfaceType)
    {
        return DashboardInfoHelpers.ResolveDiskType(msftDisks, diskIndex, model, interfaceType);
    }

    private string GetLogicalDrivesForDisk(string deviceId)
    {
        var logicalDrives = new List<string>();
        try
        {
            // Map physical disk to partitions to logical drives
            using var partitionSearcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId.Replace("\\", "\\\\")}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
            
            foreach (ManagementObject partition in partitionSearcher.Get())
            {
                using var logicalSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                
                foreach (ManagementObject logical in logicalSearcher.Get())
                {
                    var driveLetter = logical["DeviceID"]?.ToString();
                    if (!string.IsNullOrEmpty(driveLetter))
                    {
                        logicalDrives.Add(driveLetter);
                    }
                }
            }
        }
        catch { }

        return logicalDrives.Count > 0 ? string.Join(", ", logicalDrives) : "N/A";
    }

    private void LoadUsbDevicesInfo()
    {
        var devices = new List<UsbDeviceModel>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBHub");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "Unknown USB Device";
                var deviceId = obj["DeviceID"]?.ToString() ?? "";
                var pnpDeviceId = obj["PNPDeviceID"]?.ToString() ?? "";
                var status = obj["Status"]?.ToString() ?? "Unknown";

                // Parse VID and PID from DeviceID
                var (vid, pid) = ParseVidPid(pnpDeviceId);

                devices.Add(new UsbDeviceModel
                {
                    Name = name,
                    DeviceId = deviceId,
                    VendorId = vid,
                    ProductId = pid,
                    Status = status,
                    SearchUrl = !string.IsNullOrEmpty(vid) ? $"https://devicehunt.com/view/type/usb/vendor/{vid}" : ""
                });
            }

            // Also get USB controllers
            using var controllerSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBController");
            foreach (ManagementObject obj in controllerSearcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "USB Controller";
                var manufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                var deviceId = obj["DeviceID"]?.ToString() ?? "";
                var status = obj["Status"]?.ToString() ?? "Unknown";

                devices.Add(new UsbDeviceModel
                {
                    Name = $"[Controller] {name}",
                    DeviceId = deviceId,
                    Manufacturer = manufacturer,
                    Status = status,
                    IsController = true
                });
            }

            // Get connected USB devices
            using var pnpSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");
            foreach (ManagementObject obj in pnpSearcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "USB Device";
                var deviceId = obj["DeviceID"]?.ToString() ?? "";
                var manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                var status = obj["Status"]?.ToString() ?? "Unknown";
                
                // Skip if already added
                if (devices.Any(d => d.DeviceId == deviceId))
                    continue;

                // Skip generic hubs and controllers
                if (name.Contains("Hub", StringComparison.OrdinalIgnoreCase) && 
                    !name.Contains("Keyboard", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("Mouse", StringComparison.OrdinalIgnoreCase))
                    continue;

                var (vid, pid) = ParseVidPid(deviceId);

                devices.Add(new UsbDeviceModel
                {
                    Name = name,
                    DeviceId = deviceId,
                    Manufacturer = manufacturer,
                    VendorId = vid,
                    ProductId = pid,
                    Status = status,
                    SearchUrl = !string.IsNullOrEmpty(vid) ? $"https://devicehunt.com/view/type/usb/vendor/{vid}" : ""
                });
            }
        }
        catch { }

        // Sort: controllers first, then by name
        var sorted = devices
            .Where(d => !string.IsNullOrEmpty(d.Name))
            .DistinctBy(d => d.DeviceId)
            .OrderByDescending(d => d.IsController)
            .ThenBy(d => d.Name)
            .ToList();

        var primaryUsbName = sorted.FirstOrDefault()?.Name;
        var matchedUsbController = HardwareKnowledgeDbService.Instance.MatchUsb(primaryUsbName ?? string.Empty);

        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
        {
            UsbDevices = new ObservableCollection<UsbDeviceModel>(sorted);
            UsbIconKey = matchedUsbController?.IconKey
                ?? HardwareIconResolver.ResolveIconKey("usb", primaryUsbName, HardwareIconResolver.GetFallbackKey("usb"));
            UsbIconSource = HardwareIconResolver.ResolveIcon(UsbIconKey, HardwareIconResolver.GetFallbackKey("usb"));
        });
    }

    private static (string vid, string pid) ParseVidPid(string deviceId)
    {
        var vidMatch = Regex.Match(deviceId, @"VID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
        var pidMatch = Regex.Match(deviceId, @"PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);

        return (
            vidMatch.Success ? vidMatch.Groups[1].Value : "",
            pidMatch.Success ? pidMatch.Groups[1].Value : ""
        );
    }

    private void LoadNetworkInfo()
    {
        var adapters = new List<NetworkAdapterModel>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProps = nic.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                adapters.Add(new NetworkAdapterModel
                {
                    Name = nic.Name,
                    Description = nic.Description,
                    Type = nic.NetworkInterfaceType.ToString(),
                    Status = nic.OperationalStatus.ToString(),
                    Speed = FormatNetworkSpeed(nic.Speed),
                    MacAddress = FormatMacAddress(nic.GetPhysicalAddress().ToString()),
                    IpAddress = ipv4?.Address.ToString() ?? "N/A",
                    SubnetMask = ipv4?.IPv4Mask?.ToString() ?? "N/A",
                    DefaultGateway = ipProps.GatewayAddresses.FirstOrDefault()?.Address?.ToString() ?? "N/A",
                    DnsServers = string.Join(", ", ipProps.DnsAddresses.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(a => a.ToString())),
                    DhcpEnabled = ipProps.GetIPv4Properties()?.IsDhcpEnabled == true ? "On" : "Off",
                    DriverUrl = GenerateNetworkDriverUrl(nic.Description)
                });
            }
        }
        catch { }

        var primaryNetworkDesc = adapters.FirstOrDefault()?.Description;
        var matchedNetwork = HardwareKnowledgeDbService.Instance.MatchNetworkAdapter(primaryNetworkDesc ?? string.Empty);

        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
        {
            NetworkAdapters = new ObservableCollection<NetworkAdapterModel>(adapters);
            var primaryNetworkName = adapters.FirstOrDefault()?.Description ?? adapters.FirstOrDefault()?.Name;
            NetworkIconKey = matchedNetwork?.IconKey
                ?? HardwareIconResolver.ResolveIconKey("network", primaryNetworkName, HardwareIconResolver.GetFallbackKey("network"));
            NetworkIconSource = HardwareIconResolver.ResolveIcon(NetworkIconKey, HardwareIconResolver.GetFallbackKey("network"));
        });
    }

    private void LoadAudioInfo()
    {
        var deviceCount = 0;
        var primaryDevice = string.Empty;
        var primaryManufacturer = string.Empty;
        var primaryStatus = string.Empty;
        var bestScore = int.MinValue;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, Status, DeviceID FROM Win32_SoundDevice");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                deviceCount++;
                var manufacturer = obj["Manufacturer"]?.ToString() ?? string.Empty;
                var status = obj["Status"]?.ToString() ?? string.Empty;
                var deviceId = obj["DeviceID"]?.ToString();
                var score = AudioDetectionHelpers.ScoreDevice(name, manufacturer, status, deviceId);
                if (score > bestScore)
                {
                    bestScore = score;
                    primaryDevice = name;
                    primaryManufacturer = manufacturer;
                    primaryStatus = status;
                }
            }
        }
        catch
        {
        }

        AudioDeviceCount = Math.Max(AudioDeviceCount, deviceCount);
        PrimaryAudioDevice = PreferBetter(PrimaryAudioDevice, primaryDevice);
        PrimaryAudioManufacturer = PreferBetter(PrimaryAudioManufacturer, primaryManufacturer);
        PrimaryAudioStatus = PreferBetter(PrimaryAudioStatus, primaryStatus);
        if (!string.IsNullOrWhiteSpace(primaryDevice))
        {
            var audioLookup = JoinNonEmpty(primaryManufacturer, primaryDevice);
            var audioResolution = HardwareIconService.ResolveResult(HardwareType.Audio, audioLookup);
            AudioIconKey = audioResolution.IconKey;
            AudioIconSource = HardwareIconService.Resolve(audioResolution);
        }
    }

    private static string GenerateNetworkDriverUrl(string description)
    {
        // Universal search - works for ALL network adapters (Realtek, Intel, Broadcom, Marvell, Aquantia, etc.)
        var searchQuery = Uri.EscapeDataString(description + " driver download");
        return $"https://www.google.com/search?q={searchQuery}";
    }

    private void LoadSecurityInfo()
    {
        // Kernel DMA Protection - multiple sources
        KernelDmaProtection = GetKernelDmaProtectionStatus();
        
        // Hypervisor and VBS - multiple sources
        LoadVirtualizationSecurityInfo();
        
        // Credential Guard
        CredentialGuard = GetCredentialGuardStatus();
        DefenderStatus = GetServiceStatus("WinDefend");
        FirewallStatus = GetServiceStatus("MpsSvc");
    }

    private string GetKernelDmaProtectionStatus()
    {
        // Method 1: Registry
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Kernel DMA Protection");
            if (key != null)
            {
                var enabled = (int?)key.GetValue("DeviceEnumerationPolicy") ?? -1;
                if (enabled == 0) return "On";
            }
        }
        catch { }

        // Method 2: Check via msinfo32's registry key
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DmaSecurity");
            if (key != null)
            {
                var policy = key.GetValue("DmaGuardPolicy");
                if (policy is int val && val == 1) return "On";
            }
        }
        catch { }

        // Method 3: SystemInfo query
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var hwSec = obj["HypervisorPresent"];
                // If hypervisor is present, DMA protection might be active
            }
        }
        catch { }

        return "Off";
    }

    private void LoadVirtualizationSecurityInfo()
    {
        VirtualizationSecurity = "Not enabled";
        DeviceGuard = "Not enabled";
        HypervisorEnforced = "No";

        try
        {
            // Method 1: Win32_DeviceGuard WMI
            using var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\DeviceGuard", "SELECT * FROM Win32_DeviceGuard");
            var results = searcher.Get();
            foreach (ManagementObject obj in results)
            {
                var vbsStatus = obj["VirtualizationBasedSecurityStatus"]?.ToString() ?? "0";
                VirtualizationSecurity = vbsStatus switch
                {
                    "0" => "Not enabled",
                    "1" => "Enabled but not running",
                    "2" => "Running",
                    _ => "Unknown"
                };

                var sgServices = obj["SecurityServicesRunning"] as uint[];
                DeviceGuard = (sgServices != null && sgServices.Length > 0) ? "Enabled" : "Not enabled";
                
                // Check hypervisor enforcement
                var hypervisorPol = obj["RequiredSecurityProperties"] as uint[];
                HypervisorEnforced = (hypervisorPol != null && hypervisorPol.Length > 0) ? "Yes" : "No";
                return;
            }
        }
        catch { }

        // Method 2: Fallback - Registry check
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard");
            if (key != null)
            {
                var vbs = key.GetValue("EnableVirtualizationBasedSecurity");
                if (vbs is int v && v == 1)
                {
                    VirtualizationSecurity = "Enabled";
                    DeviceGuard = "Enabled";
                    HypervisorEnforced = "Unknown";
                }
            }
        }
        catch { }
    }

    private string GetCredentialGuardStatus()
    {
        // Method 1: Registry
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\LSA");
            if (key != null)
            {
                var lsaCfg = key.GetValue("LsaCfgFlags");
                if (lsaCfg is int flag)
                {
                    return flag switch
                    {
                        1 => "Enabled with UEFI lock",
                        2 => "Enabled without lock",
                        _ => "Not enabled"
                    };
                }
            }
        }
        catch { }

        // Method 2: WMI fallback
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\DeviceGuard", "SELECT * FROM Win32_DeviceGuard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var services = obj["SecurityServicesConfigured"] as uint[];
                if (services != null && services.Contains(1u))
                {
                    return "Configured";
                }
            }
        }
        catch { }

        return "Not enabled";
    }

    private static string GetServiceStatus(string serviceName)
    {
        try
        {
            using var service = new ServiceController(serviceName);
            return service.Status == ServiceControllerStatus.Running ? "Running" : "Stopped";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetChipsetName()
    {
        try 
        {
             // Search System Devices for likely chipset/LPC controllers
             // This is the most reliable way to identify the chipset on Windows
             using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PnPEntity WHERE ClassGuid='{4d36e97d-e325-11ce-bfc1-08002be10318}'");
             foreach (ManagementObject obj in searcher.Get())
             {
                 var name = obj["Name"]?.ToString();
                 if (string.IsNullOrEmpty(name)) continue;

                 // Common patterns for Chipset strings
                 if (name.Contains("LPC Controller", StringComparison.OrdinalIgnoreCase) || 
                     name.Contains("Chipset", StringComparison.OrdinalIgnoreCase))
                 {
                     // Exclude generic or irrelevant controllers
                     if (name.Contains("EC", StringComparison.OrdinalIgnoreCase) || 
                         name.Contains("Sio", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Interface", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Root Complex", StringComparison.OrdinalIgnoreCase) ||
                         name.Length > 60) continue;

                     // Clean string
                     var clean = name.Replace("Interface", "")
                                     .Replace("Controller", "")
                                     .Replace("LPC", "")
                                     .Replace("Chipset", "")
                                     .Replace("(R)", "")
                                     .Replace("Standard", "")
                                     .Replace("PCI", "")
                                     .Replace("ISA", "")
                                     .Replace("bridge", "")
                                     .Trim();
                     
                     // If we have a decent string left (e.g. "AMD B550", "Intel Z690")
                     if (clean.Length > 3) return clean;
                 }
             }
        }
        catch {}
        return "Unknown";
    }

    private static string GenerateSearchUrl(string query, string category)
    {
        var encoded = Uri.EscapeDataString(query);
        return category switch
        {
            "cpu" => $"https://www.google.com/search?q={encoded}+specifications",
            "ram" => $"https://www.google.com/search?q={encoded}+specs",
            "storage" => $"https://www.google.com/search?q={encoded}+specifications",
            "usb" => $"https://www.google.com/search?q={encoded}",
            _ => $"https://www.google.com/search?q={encoded}"
        };
    }

    private static string GenerateGpuUrl(string gpuName)
    {
        var encoded = Uri.EscapeDataString(gpuName + " specifications");
        return $"https://www.google.com/search?q={encoded}";
    }

    private static string GenerateWindowsBuildUrl(string buildNumber, string osName)
    {
        // Microsoft Windows release information - this one stays specific
        if (osName.Contains("11", StringComparison.OrdinalIgnoreCase))
        {
            return "https://learn.microsoft.com/en-us/windows/release-health/windows11-release-information";
        }
        return "https://learn.microsoft.com/en-us/windows/release-health/release-information";
    }

    private static string GenerateMotherboardUrl(string manufacturer, string product)
    {
        // Universal search - works for ALL manufacturers (ASRock, ASUS, Gigabyte, MSI, Intel, Foxconn, Supermicro, Dell, HP, etc.)
        var searchQuery = Uri.EscapeDataString(manufacturer + " " + product + " support drivers");
        return $"https://www.google.com/search?q={searchQuery}";
    }

    private static string GenerateChipsetDriverUrl(string manufacturer, string product)
    {
        // Universal search - works for all chipsets (AMD, Intel, VIA, SiS, etc.)
        var searchQuery = Uri.EscapeDataString(product + " chipset driver download");
        return $"https://www.google.com/search?q={searchQuery}";
    }


    private static string GetMemoryFormFactor(int code)
    {
        return code switch
        {
            8 => "DIMM",
            12 => "SO-DIMM",
            _ => "Unknown"
        };
    }

    private static string GetMemoryType(int code)
    {
        return code switch
        {
            20 => "DDR",
            21 => "DDR2",
            22 => "DDR2 FB-DIMM",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => "Unknown"
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.Days > 0)
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.Hours > 0)
            return $"{uptime.Hours}h {uptime.Minutes}m";
        return $"{uptime.Minutes}m";
    }

    private static string FormatNetworkSpeed(long speedBps)
    {
        if (speedBps <= 0) return "Unknown";
        if (speedBps >= 1_000_000_000)
            return $"{speedBps / 1_000_000_000.0:0.#} Gbps";
        if (speedBps >= 1_000_000)
            return $"{speedBps / 1_000_000.0:0.#} Mbps";
        return $"{speedBps / 1_000.0:0.#} Kbps";
    }

    private static string FormatMacAddress(string mac)
    {
        if (string.IsNullOrEmpty(mac) || mac.Length < 12) return "N/A";
        return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
    }
}


// Models

public class RamModuleModel
{
    public string Slot { get; set; } = "";
    public string Bank { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string Capacity { get; set; } = "";
    public string Speed { get; set; } = "";
    public string FormFactor { get; set; } = "";
    public string MemoryType { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string SearchUrl { get; set; } = "";
}

public class DiskDriveModel
{
    public string Model { get; set; } = "";
    public string Size { get; set; } = "";
    public long SizeBytes { get; set; }
    public string InterfaceType { get; set; } = "";
    public string MediaType { get; set; } = ""; // SSD, HDD
    public string SerialNumber { get; set; } = "";
    public string FirmwareRevision { get; set; } = "";
    public string Partitions { get; set; } = "";
    public bool IsExternal { get; set; }
    public string LogicalDrives { get; set; } = "";
    public string Status { get; set; } = "";
    public string SearchUrl { get; set; } = "";
    
    // New fields for detailed view
    public string InterfacePretty { get; set; } = ""; // e.g. "NVMe 16.0 GT/s"
    public string DisplayCapacity { get; set; } = ""; // e.g. "[500 GB]"
}

public class UsbDeviceModel
{
    public string Name { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsController { get; set; }
    public string SearchUrl { get; set; } = "";
}

public class NetworkAdapterModel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Speed { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string SubnetMask { get; set; } = "";
    public string DefaultGateway { get; set; } = "";
    public string DnsServers { get; set; } = "";
    public string DhcpEnabled { get; set; } = "";
    public string DriverUrl { get; set; } = "";
}

public class MonitorModel
{
    public string Name { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string Resolution { get; set; } = "";
    public string RefreshRate { get; set; } = "";
    public string BitsPerPixel { get; set; } = "";
    public bool IsPrimary { get; set; }
    public string ConnectionType { get; set; } = "";
    public string PhysicalSize { get; set; } = "";
    public string MatchMode { get; set; } = "";
    public string MatchKey { get; set; } = "";
    public string MatchedInstance { get; set; } = "";
    public string SearchUrl { get; set; } = "";
    public string IconLookupSeed { get; set; } = "";
}

public class GpuDetailedModel
{
    public string Name { get; set; } = "Unknown"; // e.g. NVIDIA GeForce RTX 3080
    public string Manufacturer { get; set; } = ""; // e.g. NVIDIA (Chipset Vendor)
    public string Vendor { get; set; } = ""; // e.g. MSI (Card Vendor)
    public string SubVendor { get; set; } = ""; // e.g. PNY (Subvendor from pci db)
    public string CodeName { get; set; } = ""; // e.g. GA102-200
    public string Technology { get; set; } = ""; // e.g. 8 nm
    public string BusInterface { get; set; } = ""; // e.g. PCIe x16 4.0
    
    public string MemorySize { get; set; } = ""; // e.g. 10 GB
    public string MemoryType { get; set; } = ""; // e.g. GDDR6X
    public string BusWidth { get; set; } = ""; // e.g. 320-bit
    public string Bandwidth { get; set; } = ""; // e.g. 760 GB/s
    
    public string Rops { get; set; } = ""; // e.g. 96
    public string Shaders { get; set; } = ""; // e.g. 8704
    public string Tmus { get; set; } = ""; // e.g. 272
    
    public string GpuClock { get; set; } = ""; // e.g. 1440 MHz
    public string MemoryClock { get; set; } = ""; // e.g. 1188 MHz
    public string BoostClock { get; set; } = ""; // e.g. 1710 MHz

    public string DriverVersion { get; set; } = "";
    public string DriverDate { get; set; } = "";
    
    public string SearchUrl { get; set; } = "";
}

