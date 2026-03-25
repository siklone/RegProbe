using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using OpenTraceProject.App.Diagnostics;
using OpenTraceProject.App.HardwareDb;
using OpenTraceProject.App.ViewModels;
using OpenTraceProject.App.ViewModels.Hardware;

namespace OpenTraceProject.App.Services;

public sealed class HardwarePreloadService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(60);
    private readonly SemaphoreSlim _preloadGate = new(1, 1);
    private HardwareDetailSnapshot _snapshot = new();
    private bool _isPreloaded;
    private DateTime _loadedAtUtc = DateTime.MinValue;
    private bool _backgroundTierStarted;
    private int _currentTier;

    public event Action<int>? SnapshotUpdated;

    public int CurrentTier => _currentTier;

    private HardwarePreloadService()
    {
    }

    public static HardwarePreloadService Instance { get; } = new();

    public async Task PreloadAsync(CancellationToken cancellationToken)
    {
        if (_isPreloaded && DateTime.UtcNow - _loadedAtUtc < CacheLifetime)
        {
            EnsureBackgroundTiers();
            return;
        }

        await _preloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isPreloaded && DateTime.UtcNow - _loadedAtUtc < CacheLifetime)
            {
                EnsureBackgroundTiers();
                return;
            }

            _snapshot = new HardwareDetailSnapshot
            {
                Os = LoadOsTier1Data(),
                Cpu = new CpuHardwareData(),
                Gpu = new GpuHardwareData(),
                Motherboard = new MotherboardHardwareData(),
                Memory = new MemoryHardwareData(),
                Storage = new StorageHardwareData(),
                Displays = new DisplayHardwareData(),
                Network = new NetworkHardwareData(),
                Usb = new UsbHardwareData()
            };

            _isPreloaded = true;
            _loadedAtUtc = DateTime.UtcNow;
            _currentTier = 1;
            PublishCache();
            // Allow external callers (tests / dev) to force a refresh by calling RefreshPublishedSnapshot
            _lastPublishedSnapshotToken = Guid.NewGuid();
            SnapshotUpdated?.Invoke(1);
            EnsureBackgroundTiers();
        }
        finally
        {
            _preloadGate.Release();
        }
    }

    public Task PreloadOsAsync(CancellationToken cancellationToken) => PreloadAsync(cancellationToken);

    public Task PreloadCpuAsync(CancellationToken cancellationToken) => PreloadAsync(cancellationToken);

    public Task PreloadGpuAsync(CancellationToken cancellationToken) => PreloadAsync(cancellationToken);

    public Task PreloadMotherboardAsync(CancellationToken cancellationToken) => PreloadAsync(cancellationToken);

    public Task PreloadMemoryAsync(CancellationToken cancellationToken) => PreloadAsync(cancellationToken);

    public Task PreloadStorageAsync(CancellationToken cancellationToken) => PreloadAsync(cancellationToken);

    public HardwareDetailSnapshot GetSnapshot()
    {
        return _snapshot;
    }

    private static OsHardwareData LoadOsTier1Data()
    {
        var resolved = OsDetectionResolver.Resolve(includeWmiCrossCheck: false);

        var data = new OsHardwareData
        {
            ProductName = resolved.ProductName,
            ProductNameSource = resolved.ProductNameSource,
            DisplayVersion = resolved.DisplayVersion,
            DisplayVersionSource = resolved.DisplayVersionSource,
            ReleaseId = resolved.ReleaseId,
            ReleaseIdSource = "Registry",
            BuildNumber = resolved.BuildNumber,
            BuildNumberSource = resolved.BuildNumberSource,
            Edition = resolved.EditionId,
            EditionSource = "Registry",
            InstallType = resolved.InstallationType,
            InstallTypeSource = "Registry",
            Version = !string.IsNullOrWhiteSpace(resolved.DisplayVersion) ? resolved.DisplayVersion : resolved.ReleaseId,
            VersionSource = !string.IsNullOrWhiteSpace(resolved.DisplayVersion) ? resolved.DisplayVersionSource : "Registry",
            Ubr = resolved.Ubr,
            NormalizedName = resolved.NormalizedName,
            IconKey = resolved.IconKey,
            Architecture = resolved.Architecture,
            ArchitectureSource = "API",
            Username = Environment.UserName,
            UsernameSource = "API"
        };

        var boot = DateTime.Now.AddMilliseconds(-Environment.TickCount64);
        data.LastBootTime = boot.ToString("yyyy-MM-dd HH:mm");
        data.LastBootTimeSource = "API";
        data.Uptime = BuildUptime(data.LastBootTime);
        data.UptimeSource = "API";

        return data;
    }

    private static OsHardwareData LoadOsTier2Data()
    {
        var data = new OsHardwareData
        {
            BiosMode = TryGetBiosMode(),
            BiosModeSource = "Registry",
            SecureBootState = TryGetSecureBootState(),
            SecureBootStateSource = "Registry",
            TpmVersion = TryGetTpmVersion(),
            TpmVersionSource = "WMI",
            BitLockerStatus = TryGetBitLockerStatus(),
            BitLockerStatusSource = "WMI",
            DeviceGuardStatus = TryGetDeviceGuardStatus(),
            DeviceGuardStatusSource = "WMI",
            CredentialGuardStatus = TryGetCredentialGuardStatus(),
            CredentialGuardStatusSource = "WMI",
            DefenderStatus = TryGetServiceStatus("WinDefend"),
            DefenderStatusSource = "API",
            FirewallStatus = TryGetServiceStatus("MpsSvc"),
            FirewallStatusSource = "API"
        };

        return data;
    }

    private static OsHardwareData MergeOs(OsHardwareData baseData, OsHardwareData update)
    {
        return new OsHardwareData
        {
            NormalizedName = FirstNotEmpty(baseData.NormalizedName, update.NormalizedName),
            IconKey = FirstNotEmpty(baseData.IconKey, update.IconKey),
            ProductName = FirstNotEmpty(baseData.ProductName, update.ProductName),
            Edition = FirstNotEmpty(baseData.Edition, update.Edition),
            DisplayVersion = FirstNotEmpty(baseData.DisplayVersion, update.DisplayVersion),
            ReleaseId = FirstNotEmpty(baseData.ReleaseId, update.ReleaseId),
            Version = FirstNotEmpty(baseData.Version, update.Version),
            BuildNumber = baseData.BuildNumber > 0 ? baseData.BuildNumber : update.BuildNumber,
            Ubr = baseData.Ubr > 0 ? baseData.Ubr : update.Ubr,
            Architecture = FirstNotEmpty(baseData.Architecture, update.Architecture),
            InstallDate = FirstNotEmpty(baseData.InstallDate, update.InstallDate),
            InstallType = FirstNotEmpty(baseData.InstallType, update.InstallType),
            RegisteredOwner = FirstNotEmpty(baseData.RegisteredOwner, update.RegisteredOwner),
            RegisteredOrganization = FirstNotEmpty(baseData.RegisteredOrganization, update.RegisteredOrganization),
            WindowsExperienceIndex = FirstNotEmpty(baseData.WindowsExperienceIndex, update.WindowsExperienceIndex),
            LastBootTime = FirstNotEmpty(baseData.LastBootTime, update.LastBootTime),
            Uptime = FirstNotEmpty(baseData.Uptime, update.Uptime),
            Username = FirstNotEmpty(baseData.Username, update.Username),
            SecureBootState = FirstNotEmpty(baseData.SecureBootState, update.SecureBootState),
            TpmVersion = FirstNotEmpty(baseData.TpmVersion, update.TpmVersion),
            BitLockerStatus = FirstNotEmpty(baseData.BitLockerStatus, update.BitLockerStatus),
            VirtualizationEnabled = FirstNotEmpty(baseData.VirtualizationEnabled, update.VirtualizationEnabled),
            HyperVInstalled = FirstNotEmpty(baseData.HyperVInstalled, update.HyperVInstalled),
            DefenderStatus = FirstNotEmpty(baseData.DefenderStatus, update.DefenderStatus),
            FirewallStatus = FirstNotEmpty(baseData.FirewallStatus, update.FirewallStatus),
            BiosMode = FirstNotEmpty(baseData.BiosMode, update.BiosMode),
            DeviceGuardStatus = FirstNotEmpty(baseData.DeviceGuardStatus, update.DeviceGuardStatus),
            CredentialGuardStatus = FirstNotEmpty(baseData.CredentialGuardStatus, update.CredentialGuardStatus),

            ProductNameSource = FirstNotEmpty(baseData.ProductNameSource, update.ProductNameSource),
            EditionSource = FirstNotEmpty(baseData.EditionSource, update.EditionSource),
            DisplayVersionSource = FirstNotEmpty(baseData.DisplayVersionSource, update.DisplayVersionSource),
            ReleaseIdSource = FirstNotEmpty(baseData.ReleaseIdSource, update.ReleaseIdSource),
            VersionSource = FirstNotEmpty(baseData.VersionSource, update.VersionSource),
            BuildNumberSource = FirstNotEmpty(baseData.BuildNumberSource, update.BuildNumberSource),
            ArchitectureSource = FirstNotEmpty(baseData.ArchitectureSource, update.ArchitectureSource),
            InstallDateSource = FirstNotEmpty(baseData.InstallDateSource, update.InstallDateSource),
            InstallTypeSource = FirstNotEmpty(baseData.InstallTypeSource, update.InstallTypeSource),
            RegisteredOwnerSource = FirstNotEmpty(baseData.RegisteredOwnerSource, update.RegisteredOwnerSource),
            RegisteredOrganizationSource = FirstNotEmpty(baseData.RegisteredOrganizationSource, update.RegisteredOrganizationSource),
            WindowsExperienceIndexSource = FirstNotEmpty(baseData.WindowsExperienceIndexSource, update.WindowsExperienceIndexSource),
            LastBootTimeSource = FirstNotEmpty(baseData.LastBootTimeSource, update.LastBootTimeSource),
            UptimeSource = FirstNotEmpty(baseData.UptimeSource, update.UptimeSource),
            UsernameSource = FirstNotEmpty(baseData.UsernameSource, update.UsernameSource),
            SecureBootStateSource = FirstNotEmpty(baseData.SecureBootStateSource, update.SecureBootStateSource),
            TpmVersionSource = FirstNotEmpty(baseData.TpmVersionSource, update.TpmVersionSource),
            BitLockerStatusSource = FirstNotEmpty(baseData.BitLockerStatusSource, update.BitLockerStatusSource),
            VirtualizationEnabledSource = FirstNotEmpty(baseData.VirtualizationEnabledSource, update.VirtualizationEnabledSource),
            HyperVInstalledSource = FirstNotEmpty(baseData.HyperVInstalledSource, update.HyperVInstalledSource),
            DefenderStatusSource = FirstNotEmpty(baseData.DefenderStatusSource, update.DefenderStatusSource),
            FirewallStatusSource = FirstNotEmpty(baseData.FirewallStatusSource, update.FirewallStatusSource),
            BiosModeSource = FirstNotEmpty(baseData.BiosModeSource, update.BiosModeSource),
            DeviceGuardStatusSource = FirstNotEmpty(baseData.DeviceGuardStatusSource, update.DeviceGuardStatusSource),
            CredentialGuardStatusSource = FirstNotEmpty(baseData.CredentialGuardStatusSource, update.CredentialGuardStatusSource)
        };
    }

    private static string? FirstNotEmpty(string? primary, string? fallback)
    {
        return string.IsNullOrWhiteSpace(primary) ? fallback : primary;
    }

    private void PublishCache()
    {
        MetricCacheService.Instance.Set("OS", _snapshot.Os);
        MetricCacheService.Instance.Set("CPU", _snapshot.Cpu);
        MetricCacheService.Instance.Set("GPU", _snapshot.Gpu);
        MetricCacheService.Instance.Set("Motherboard", _snapshot.Motherboard);
        MetricCacheService.Instance.Set("Memory", _snapshot.Memory);
        MetricCacheService.Instance.Set("Storage", _snapshot.Storage);
        MetricCacheService.Instance.Set("Displays", _snapshot.Displays);
        MetricCacheService.Instance.Set("Network", _snapshot.Network);
        MetricCacheService.Instance.Set("USB", _snapshot.Usb);
        MetricCacheService.Instance.Set("Audio", _snapshot.Audio);
    }

    private void EnsureBackgroundTiers()
    {
        if (_backgroundTierStarted)
        {
            return;
        }

        _backgroundTierStarted = true;
        _ = Task.Run(LoadBackgroundTiersAsync);
    }

    private Guid _lastPublishedSnapshotToken = Guid.Empty;

    /// <summary>
    /// Development helper: force re-publish of the current snapshot and notify subscribers.
    /// Safe to call from UI thread during debugging.
    /// </summary>
    public void RefreshPublishedSnapshot()
    {
        _loadedAtUtc = DateTime.UtcNow;
        _lastPublishedSnapshotToken = Guid.NewGuid();
        PublishCache();
        SnapshotUpdated?.Invoke(_currentTier);
    }

    private async Task LoadBackgroundTiersAsync()
    {
        try
        {
            var osTier2 = await Task.Run(LoadOsTier2Data).ConfigureAwait(false);
            _snapshot = new HardwareDetailSnapshot
            {
                Os = MergeOs(_snapshot.Os, osTier2),
                Cpu = _snapshot.Cpu,
                Gpu = _snapshot.Gpu,
                Motherboard = _snapshot.Motherboard,
                Memory = _snapshot.Memory,
                Storage = _snapshot.Storage,
                Displays = _snapshot.Displays,
                Network = _snapshot.Network,
                Usb = _snapshot.Usb,
                Audio = _snapshot.Audio
            };

            _currentTier = 2;
            PublishCache();
            SnapshotUpdated?.Invoke(2);

            var osTask = Task.Run(LoadOsData);
            var cpuTask = Task.Run(LoadCpuData);
            var gpuTask = Task.Run(LoadGpuData);
            var motherboardTask = Task.Run(LoadMotherboardData);
            var memoryTask = Task.Run(LoadMemoryData);
            var storageTask = Task.Run(LoadStorageData);
            var displayTask = Task.Run(LoadDisplayData);
            var networkTask = Task.Run(LoadNetworkData);
            var usbTask = Task.Run(LoadUsbData);
            var audioTask = Task.Run(LoadAudioData);

            await Task.WhenAll(osTask, cpuTask, gpuTask, motherboardTask, memoryTask, storageTask, displayTask, networkTask, usbTask, audioTask).ConfigureAwait(false);

            var validation = new HardwareValidationService();
            _snapshot = new HardwareDetailSnapshot
            {
                Os = validation.Validate(MergeOs(_snapshot.Os, osTask.Result)),
                Cpu = validation.Validate(cpuTask.Result),
                Gpu = validation.Validate(gpuTask.Result),
                Motherboard = validation.Validate(motherboardTask.Result),
                Memory = memoryTask.Result,
                Storage = storageTask.Result,
                Displays = displayTask.Result,
                Network = networkTask.Result,
                Usb = usbTask.Result,
                Audio = audioTask.Result
            };

            _currentTier = 3;
            PublishCache();
            SnapshotUpdated?.Invoke(3);
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] Tiered preload failed: {ex.Message}");
        }
    }

    private static OsHardwareData LoadOsData()
    {
        var resolved = OsDetectionResolver.Resolve(includeWmiCrossCheck: true);
        var data = new OsHardwareData
        {
            ProductName = resolved.ProductName,
            ProductNameSource = resolved.ProductNameSource,
            Edition = resolved.EditionId,
            EditionSource = "Registry",
            DisplayVersion = resolved.DisplayVersion,
            DisplayVersionSource = resolved.DisplayVersionSource,
            ReleaseId = resolved.ReleaseId,
            ReleaseIdSource = "Registry",
            Version = !string.IsNullOrWhiteSpace(resolved.DisplayVersion) ? resolved.DisplayVersion : resolved.ReleaseId,
            VersionSource = !string.IsNullOrWhiteSpace(resolved.DisplayVersion) ? resolved.DisplayVersionSource : "Registry",
            BuildNumber = resolved.BuildNumber,
            BuildNumberSource = resolved.BuildNumberSource,
            Ubr = resolved.Ubr,
            NormalizedName = resolved.NormalizedName,
            IconKey = resolved.IconKey,
            Architecture = resolved.Architecture,
            ArchitectureSource = GetArchitectureSource(resolved.Architecture),
            InstallDate = resolved.InstallDate,
            InstallDateSource = string.IsNullOrWhiteSpace(resolved.InstallDate) ? null : "WMI",
            LastBootTime = resolved.LastBootTime,
            LastBootTimeSource = string.IsNullOrWhiteSpace(resolved.LastBootTime) ? null : "WMI",
            InstallType = resolved.InstallationType,
            InstallTypeSource = "Registry"
        };

        try
        {
            using var currentVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (currentVersion != null)
            {
                data.RegisteredOwner = currentVersion.GetValue("RegisteredOwner")?.ToString();
                data.RegisteredOwnerSource = "Registry";
                data.RegisteredOrganization = currentVersion.GetValue("RegisteredOrganization")?.ToString();
                data.RegisteredOrganizationSource = "Registry";
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] OS owner registry read failed: {ex.Message}");
        }

        data.WindowsExperienceIndex = TryGetWindowsExperienceIndex();
        data.WindowsExperienceIndexSource = data.WindowsExperienceIndex == null ? "Unknown" : "WMI";

        data.SecureBootState = TryGetSecureBootState();
        data.SecureBootStateSource = "Registry";

        data.TpmVersion = TryGetTpmVersion();
        data.TpmVersionSource = data.TpmVersion == null ? "Unknown" : "WMI";

        data.BitLockerStatus = TryGetBitLockerStatus();
        data.BitLockerStatusSource = "WMI";

        data.VirtualizationEnabled = TryGetVirtualizationEnabled();
        data.VirtualizationEnabledSource = "WMI";

        data.HyperVInstalled = TryGetHyperVInstalled();
        data.HyperVInstalledSource = "WMI";

        data.DefenderStatus = TryGetServiceStatus("WinDefend");
        data.DefenderStatusSource = "API";

        data.FirewallStatus = TryGetServiceStatus("MpsSvc");
        data.FirewallStatusSource = "API";

        data.Uptime = BuildUptime(data.LastBootTime);
        data.UptimeSource = "API";

        AppDiagnostics.Log($"[HardwarePreloadService] OS detail sources: ProductName={data.ProductNameSource}, Build={data.BuildNumberSource}, DisplayVersion={data.DisplayVersionSource}, LastBoot={data.LastBootTimeSource}, Normalized={data.NormalizedName}, Icon={data.IconKey}");

        return data;
    }

    private static string GetArchitectureSource(string architecture)
    {
        return string.IsNullOrWhiteSpace(architecture) ? "API" : "WMI";
    }

    private static CpuHardwareData LoadCpuData()
    {
        var data = new CpuHardwareData();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                data.Name = GetValueSafe(obj, "Name")?.Trim();
                data.Manufacturer = GetValueSafe(obj, "Manufacturer")?.Trim();
                data.Cores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                data.Threads = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                data.MaxClockMhz = Convert.ToInt32(obj["MaxClockSpeed"] ?? 0);
                data.CurrentClockMhz = Convert.ToInt32(obj["CurrentClockSpeed"] ?? 0);
                data.BusSpeedMhz = Convert.ToInt32(obj["ExtClock"] ?? 0);
                data.Architecture = ToArchitecture(GetValueSafe(obj, "Architecture"));
                data.Description = GetValueSafe(obj, "Description");
                data.L2CacheKB = Convert.ToInt32(obj["L2CacheSize"] ?? 0);
                data.L3CacheKB = Convert.ToInt32(obj["L3CacheSize"] ?? 0);
                data.AddressWidth = Convert.ToInt32(obj["AddressWidth"] ?? 0);
                data.Socket = GetValueSafe(obj, "SocketDesignation");
                data.Caption = GetValueSafe(obj, "Caption");
                data.Revision = GetValueSafe(obj, "Revision");

                try { data.Virtualization = (bool?)obj["VirtualizationFirmwareEnabled"]; } catch { }

                data.HyperThreading = data.Threads > data.Cores;

                // Voltage: WMI returns in tenths of a volt (e.g. 12 = 1.2 V)
                var voltageTenths = Convert.ToInt32(obj["Voltage"] ?? 0);
                if (voltageTenths > 0)
                {
                    data.VoltageV = voltageTenths / 10.0;
                }

                // Parse Stepping / Family / Model from the Description string
                ParseCpuDescription(data.Description, out var family, out var model, out var stepping);
                data.CpuFamily = family;
                data.CpuModel = model;
                data.Stepping = stepping;

                // Microcode from Registry
                try
                {
                    using var cpuKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                    if (cpuKey != null)
                    {
                        var rev = cpuKey.GetValue("Update Revision");
                        if (rev is byte[] bytes && bytes.Length >= 4)
                        {
                            var revPart = bytes.Take(4).Reverse().ToArray();
                            data.Microcode = "0x" + BitConverter.ToString(revPart).Replace("-", "");
                        }
                    }
                }
                catch { }

                // L1 Cache from Win32_CacheMemory
                try
                {
                    using var cacheSearcher = new ManagementObjectSearcher("SELECT CacheType, InstalledSize, Level FROM Win32_CacheMemory");
                    int l1Size = 0;
                    foreach (ManagementObject cacheObj in cacheSearcher.Get())
                    {
                        var level = Convert.ToInt32(cacheObj["Level"] ?? 0);
                        var size = Convert.ToInt32(cacheObj["InstalledSize"] ?? 0);
                        if (level == 3) l1Size += size; // Level 3 in WMI is L1
                    }
                    data.L1CacheKB = l1Size;
                }
                catch { }
                break;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] CPU WMI read failed: {ex.Message}");
        }

        return data;
    }

    private static void ParseCpuDescription(string? description, out string? family, out string? model, out string? stepping)
    {
        family = null;
        model = null;
        stepping = null;
        if (string.IsNullOrWhiteSpace(description)) return;

        var familyMatch = Regex.Match(description, @"Family\s+(\d+)", RegexOptions.IgnoreCase);
        if (familyMatch.Success) family = familyMatch.Groups[1].Value;

        var modelMatch = Regex.Match(description, @"Model\s+(\d+)", RegexOptions.IgnoreCase);
        if (modelMatch.Success) model = modelMatch.Groups[1].Value;

        var steppingMatch = Regex.Match(description, @"Stepping\s+(\d+)", RegexOptions.IgnoreCase);
        if (steppingMatch.Success) stepping = steppingMatch.Groups[1].Value;
    }

    private static GpuHardwareData LoadGpuData()
    {
        var data = new GpuHardwareData();

        // Primary source for VRAM: DXGI adapter description.
        TryPopulateGpuFromDxgi(ref data);

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                data.Name ??= GetValueSafe(obj, "Name");
                data.Vendor ??= GetValueSafe(obj, "AdapterCompatibility");
                data.DriverVersion ??= GetValueSafe(obj, "DriverVersion");
                data.VideoProcessor ??= GetValueSafe(obj, "VideoProcessor");
                data.AdapterDacType ??= GetValueSafe(obj, "AdapterDACType");
                data.VideoModeDescription ??= GetValueSafe(obj, "VideoModeDescription");

                if (string.IsNullOrWhiteSpace(data.DriverDate))
                {
                    var driverDateRaw = GetValueSafe(obj, "DriverDate");
                    if (!string.IsNullOrWhiteSpace(driverDateRaw))
                    {
                        try
                        {
                            data.DriverDate = ManagementDateTimeConverter.ToDateTime(driverDateRaw).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            data.DriverDate = driverDateRaw;
                        }
                    }
                }

                if (data.CurrentHorizontalResolution <= 0)
                {
                    data.CurrentHorizontalResolution = Convert.ToInt32(obj["CurrentHorizontalResolution"] ?? 0);
                }

                if (data.CurrentVerticalResolution <= 0)
                {
                    data.CurrentVerticalResolution = Convert.ToInt32(obj["CurrentVerticalResolution"] ?? 0);
                }

                if (data.RefreshRateHz <= 0)
                {
                    data.RefreshRateHz = Convert.ToInt32(obj["CurrentRefreshRate"] ?? 0);
                }

                // WMI memory is fallback only when DXGI could not provide a usable value.
                if (data.AdapterRamBytes <= 0)
                {
                    data.AdapterRamBytes = Convert.ToInt64(obj["AdapterRAM"] ?? 0L);
                }

                var pnp = obj["PNPDeviceID"]?.ToString();
                data.PnpDeviceId ??= pnp;
                ParsePciVendorAndDevice(pnp, out var vendorId, out var deviceId);
                data.VendorId ??= vendorId;
                data.DeviceId ??= deviceId;

                break;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] GPU WMI read failed: {ex.Message}");
        }

        // Infer VRAM type heuristically from GPU name (WMI does not expose this)
        data.InferredVramType = HardwarePresentationFormatter.InferVramType(data.Name, data.VendorId);

        return data;
    }

    private static string? InferVramType(string? gpuName, string? vendorId)
    {
        if (string.IsNullOrWhiteSpace(gpuName)) return null;
        var name = gpuName.ToUpperInvariant();

        // NVIDIA RTX 40-series â†’ GDDR6X; RTX 30-series Ti/3090 â†’ GDDR6X; others â†’ GDDR6
        if (name.Contains("RTX 40") || name.Contains("RTX 4090") || name.Contains("RTX 4080") ||
            name.Contains("RTX 4070 TI") || name.Contains("RTX 3090") || name.Contains("RTX 3080"))
            return "GDDR6X";
        if (name.Contains("RTX") || name.Contains("GTX 16") || name.Contains("GTX 1660"))
            return "GDDR6";
        if (name.Contains("GTX 10") || name.Contains("GTX 1080") || name.Contains("GTX 1070"))
            return "GDDR5X";
        if (name.Contains("RX 7")) return "GDDR6";
        if (name.Contains("RX 6")) return "GDDR6";
        if (name.Contains("RX 5")) return "GDDR6";
        if (name.Contains("VEGA") || name.Contains("RX 590") || name.Contains("RX 580")) return "HBM2";
        if (name.Contains("IRIS") || name.Contains("UHD") || name.Contains("HD GRAPHICS")) return "Shared (DDR)";
        return null;
    }

    private static void TryPopulateGpuFromDxgi(ref GpuHardwareData data)
    {
        IntPtr factoryPtr = IntPtr.Zero;
        IDXGIFactory1? factory = null;

        try
        {
            var iid = typeof(IDXGIFactory1).GUID;
            var hr = CreateDXGIFactory1(ref iid, out factoryPtr);
            if (hr < 0 || factoryPtr == IntPtr.Zero)
            {
                return;
            }

            factory = (IDXGIFactory1)Marshal.GetObjectForIUnknown(factoryPtr);

            uint index = 0;
            while (factory.EnumAdapters1(index, out var adapter) >= 0)
            {
                try
                {
                    adapter.GetDesc1(out var desc);

                    var isSoftware = (desc.Flags & DxgiAdapterFlagSoftware) != 0;
                    if (!isSoftware)
                    {
                        data.Name ??= desc.Description;
                        data.AdapterRamBytes = desc.DedicatedVideoMemory > long.MaxValue
                            ? long.MaxValue
                            : (long)desc.DedicatedVideoMemory;

                        var vendorIdHex = desc.VendorId.ToString("X4");
                        var deviceIdHex = desc.DeviceId.ToString("X4");
                        data.VendorId ??= vendorIdHex;
                        data.DeviceId ??= deviceIdHex;
                        data.Vendor ??= MapVendorName(vendorIdHex);
                        return;
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(adapter);
                }

                index++;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] DXGI GPU read failed: {ex.Message}");
        }
        finally
        {
            if (factory != null)
            {
                Marshal.ReleaseComObject(factory);
            }

            if (factoryPtr != IntPtr.Zero)
            {
                Marshal.Release(factoryPtr);
            }
        }
    }

    private static string? MapVendorName(string? vendorId)
    {
        return (vendorId ?? string.Empty).ToUpperInvariant() switch
        {
            "10DE" => "NVIDIA",
            "1002" => "AMD",
            "8086" => "Intel",
            _ => null
        };
    }

    private static MotherboardHardwareData LoadMotherboardData()
    {
        var data = new MotherboardHardwareData();

        try
        {
            using var baseboardSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (ManagementObject obj in baseboardSearcher.Get())
            {
                data.Manufacturer = GetValueSafe(obj, "Manufacturer")?.Trim();
                data.Model = GetValueSafe(obj, "Model")?.Trim();
                data.Product = GetValueSafe(obj, "Product")?.Trim();
                data.Serial = GetValueSafe(obj, "SerialNumber")?.Trim();
                data.Version = GetValueSafe(obj, "Version")?.Trim();
                data.AssetTag = GetValueSafe(obj, "AssetTag")?.Trim();
                break;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] BaseBoard WMI read failed: {ex.Message}");
        }

        try
        {
            using var biosSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (ManagementObject obj in biosSearcher.Get())
            {
                data.BiosVendor = GetValueSafe(obj, "Manufacturer");
                data.BiosVersion = GetValueSafe(obj, "SMBIOSBIOSVersion");
                var biosDateRaw = GetValueSafe(obj, "ReleaseDate");
                if (!string.IsNullOrWhiteSpace(biosDateRaw))
                {
                    try
                    {
                        data.BiosDate = ManagementDateTimeConverter.ToDateTime(biosDateRaw).ToString("yyyy-MM-dd");
                    }
                    catch
                    {
                        data.BiosDate = biosDateRaw;
                    }
                }

                break;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] BIOS WMI read failed: {ex.Message}");
        }

        try
        {
            using var slotSearcher = new ManagementObjectSearcher("SELECT SlotDesignation FROM Win32_SystemSlot");
            var slotCount = 0;
            foreach (var _ in slotSearcher.Get())
            {
                slotCount++;
            }

            data.SystemSlotCount = slotCount;
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] SystemSlot WMI read failed: {ex.Message}");
        }

        try
        {
            using var chassisSearcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure");
            foreach (ManagementObject obj in chassisSearcher.Get())
            {
                var types = obj["ChassisTypes"] as ushort[];
                if (types != null && types.Length > 0)
                {
                    data.ChassisType = ToChassisType(types[0]);
                }
                break;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] SystemEnclosure WMI read failed: {ex.Message}");
        }

        data.Chipset = TryInferChipset(data.Product) ?? TryReadChipsetFromRegistry();

        return data;
    }

    private static MemoryHardwareData LoadMemoryData()
    {
        var data = new MemoryHardwareData();

        try
        {
            using var arraySearcher = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray");
            foreach (ManagementObject obj in arraySearcher.Get())
            {
                data.TotalSlotCount += GetIntSafe(obj, "MemoryDevices");
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] Memory array read failed: {ex.Message}");
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, PartNumber, SerialNumber, DeviceLocator, BankLabel, Capacity, Speed, ConfiguredClockSpeed, " +
                "SMBIOSMemoryType, FormFactor, MinVoltage FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in searcher.Get())
            {
                var capacityBytes = GetLongSafe(obj, "Capacity");
                var speedMhz = GetIntSafe(obj, "Speed");
                var configuredSpeedMhz = GetIntSafe(obj, "ConfiguredClockSpeed");
                var smbiosMemoryType = GetIntSafe(obj, "SMBIOSMemoryType");
                var formFactorCode = GetIntSafe(obj, "FormFactor");
                var minVoltageMv = GetIntSafe(obj, "MinVoltage");

                data.ModuleCount++;
                data.TotalBytes += capacityBytes;

                if (string.IsNullOrWhiteSpace(data.PrimaryManufacturer))
                {
                    data.PrimaryManufacturer = GetValueSafe(obj, "Manufacturer")?.Trim();
                }

                if (string.IsNullOrWhiteSpace(data.PrimaryModel))
                {
                    data.PrimaryModel = GetValueSafe(obj, "PartNumber")?.Trim();
                }

                if (data.SpeedMhz <= 0)
                {
                    data.SpeedMhz = speedMhz;
                }

                if (data.ConfiguredSpeedMhz <= 0)
                {
                    data.ConfiguredSpeedMhz = configuredSpeedMhz;
                }

                if (string.IsNullOrWhiteSpace(data.MemoryType))
                {
                    data.MemoryType = ToMemoryType(smbiosMemoryType);
                }

                if (string.IsNullOrWhiteSpace(data.FormFactor))
                {
                    data.FormFactor = ToFormFactor(formFactorCode);
                }

                if (data.MinVoltageMv <= 0)
                {
                    data.MinVoltageMv = minVoltageMv;
                }

                data.Modules.Add(new MemoryModuleData
                {
                    Slot = GetValueSafe(obj, "DeviceLocator")?.Trim(),
                    BankLabel = GetValueSafe(obj, "BankLabel")?.Trim(),
                    Manufacturer = GetValueSafe(obj, "Manufacturer")?.Trim(),
                    PartNumber = GetValueSafe(obj, "PartNumber")?.Trim(),
                    SerialNumber = GetValueSafe(obj, "SerialNumber")?.Trim(),
                    CapacityBytes = capacityBytes,
                    SpeedMhz = speedMhz,
                    ConfiguredSpeedMhz = configuredSpeedMhz,
                    MemoryType = ToMemoryType(smbiosMemoryType),
                    FormFactor = ToFormFactor(formFactorCode),
                    MinVoltageMv = minVoltageMv
                });
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] Memory WMI read failed: {ex.Message}");
        }

        if (data.SpeedMhz <= 0 && data.Modules.Count > 0)
        {
            data.SpeedMhz = data.Modules.Max(m => m.SpeedMhz);
        }

        if (data.ConfiguredSpeedMhz <= 0 && data.Modules.Count > 0)
        {
            data.ConfiguredSpeedMhz = data.Modules.Max(m => m.ConfiguredSpeedMhz);
        }

        if (data.MinVoltageMv <= 0 && data.Modules.Count > 0)
        {
            data.MinVoltageMv = data.Modules
                .Where(m => m.MinVoltageMv > 0)
                .Select(m => m.MinVoltageMv)
                .DefaultIfEmpty(0)
                .Min();
        }

        return data;
    }

    private static string ToFormFactor(int code) => code switch
    {
        8 => "DIMM",
        12 => "SO-DIMM",
        13 => "SO-DIMM",
        17 => "DIMM (FB-DIMM)",
        20 => "DIMM",
        21 => "SO-DIMM",
        _ => "Unknown"
    };

    private static StorageHardwareData LoadStorageData()
    {
        return HardwarePeripheralDataCollector.LoadStorageData();
    }

    private static string? InferMediaType(string? model, string? interfaceType)
    {
        if (string.IsNullOrWhiteSpace(model)) return null;
        var m = model.ToUpperInvariant();
        var iface = (interfaceType ?? string.Empty).ToUpperInvariant();

        // NVMe detection: either the interface says SCSI (Windows presents NVMe as SCSI) + model contains NVMe/SSD,
        // or the model name itself contains NVMe
        if (m.Contains("NVME") || m.Contains("NVM EXPRESS")) return "NVMe SSD";
        if (iface == "SCSI" && (m.Contains("SSD") || m.Contains("SOLID STATE"))) return "NVMe SSD";

        // SSD keywords
        if (m.Contains("SSD") || m.Contains("SOLID STATE")) return "SSD";

        // Model series known to be SSDs
        if (m.Contains("MX500") || m.Contains("MX300") || m.Contains("860 EVO") ||
            m.Contains("870 EVO") || m.Contains("970 EVO") || m.Contains("WD GREEN") ||
            m.Contains("WD BLUE SSD") || m.Contains("KINGSTON")) return "SSD";

        // Rotational
        if (m.Contains("HDD") || m.Contains("WD") || m.Contains("SEAGATE") ||
            m.Contains("HITACHI") || m.Contains("TOSHIBA") || m.Contains("HGST") ||
            iface == "IDE") return "HDD";

        return null;
    }

    private static DisplayHardwareData LoadDisplayData()
    {
        var data = new DisplayHardwareData
        {
            PrimaryWidth = (int)SystemParameters.PrimaryScreenWidth,
            PrimaryHeight = (int)SystemParameters.PrimaryScreenHeight,
            PrimaryRefreshRateHz = 0,
            PrimaryBitsPerPixel = 0,
            VirtualWidth = (int)SystemParameters.VirtualScreenWidth,
            VirtualHeight = (int)SystemParameters.VirtualScreenHeight,
            DisplayCount = 1
        };

        try
        {
            var resolvedDisplays = DisplayDetectionHelpers.ResolveConnectedDisplays();

            foreach (var displayInfo in resolvedDisplays)
            {
                data.Devices.Add(new DisplayDeviceData
                {
                    Name = displayInfo.Name,
                    DeviceName = displayInfo.DeviceName,
                    Manufacturer = ExpandDisplayManufacturer(displayInfo.Manufacturer),
                    Model = displayInfo.Model,
                    ProductCode = displayInfo.ProductCode,
                    Resolution = $"{displayInfo.Width} x {displayInfo.Height}",
                    Width = displayInfo.Width,
                    Height = displayInfo.Height,
                    RefreshRateHz = displayInfo.RefreshRateHz,
                    BitsPerPixel = displayInfo.BitsPerPixel,
                    IsPrimary = displayInfo.IsPrimary,
                    ConnectionType = displayInfo.ConnectionType,
                    IconLookupSeed = displayInfo.IconLookupSeed,
                    PhysicalWidthCm = displayInfo.PhysicalWidthCm ?? 0,
                    PhysicalHeightCm = displayInfo.PhysicalHeightCm ?? 0,
                    MatchMode = displayInfo.MatchMode,
                    MatchKey = displayInfo.MatchKey,
                    MatchedInstance = displayInfo.MatchedInstance
                });
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] Display inventory read failed: {ex.Message}");
        }

        if (data.Devices.Count > 0)
        {
            data.Devices = data.Devices
                .OrderByDescending(device => device.IsPrimary)
                .ThenBy(device => device.Name ?? device.DeviceName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var primaryDisplay = data.Devices.First();
            data.DisplayCount = data.Devices.Count;
            data.PrimaryMonitorName = primaryDisplay.Name;
            data.MonitorManufacturer = primaryDisplay.Manufacturer;
            data.MonitorModel = primaryDisplay.Model;

            if (primaryDisplay.Width > 0)
            {
                data.PrimaryWidth = primaryDisplay.Width;
            }

            if (primaryDisplay.Height > 0)
            {
                data.PrimaryHeight = primaryDisplay.Height;
            }

            if (primaryDisplay.RefreshRateHz > 0)
            {
                data.PrimaryRefreshRateHz = primaryDisplay.RefreshRateHz;
            }

            if (primaryDisplay.BitsPerPixel > 0)
            {
                data.PrimaryBitsPerPixel = primaryDisplay.BitsPerPixel;
            }
        }

        try
        {
            using var gpuSearcher = new ManagementObjectSearcher("SELECT Name, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate, CurrentBitsPerPixel FROM Win32_VideoController");
            foreach (ManagementObject obj in gpuSearcher.Get())
            {
                data.GpuOutput ??= obj["Name"]?.ToString();
                if (data.PrimaryRefreshRateHz <= 0)
                {
                    data.PrimaryRefreshRateHz = Convert.ToInt32(obj["CurrentRefreshRate"] ?? 0);
                }

                if (data.PrimaryBitsPerPixel <= 0)
                {
                    data.PrimaryBitsPerPixel = Convert.ToInt32(obj["CurrentBitsPerPixel"] ?? 0);
                }

                if (data.PrimaryWidth <= 0)
                {
                    data.PrimaryWidth = Convert.ToInt32(obj["CurrentHorizontalResolution"] ?? 0);
                }

                if (data.PrimaryHeight <= 0)
                {
                    data.PrimaryHeight = Convert.ToInt32(obj["CurrentVerticalResolution"] ?? 0);
                }

                break;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] Display video mode read failed: {ex.Message}");
        }

        return data;
    }

    private static string? ExpandDisplayManufacturer(string? manufacturer)
    {
        var value = manufacturer?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.ToUpperInvariant() switch
        {
            "ACI" or "AUS" => "ASUS",
            "ACR" => "Acer",
            "AOC" => "AOC",
            "BNQ" => "BenQ",
            "DEL" or "DELL" => "Dell",
            "GBT" => "Gigabyte",
            "GSM" or "LGD" => "LG Electronics",
            "MSI" => "MSI",
            "SAM" or "SEC" => "Samsung",
            "VSC" => "ViewSonic",
            _ => value
        };
    }

    private static NetworkHardwareData LoadNetworkData()
    {
        var data = new NetworkHardwareData();
        try
        {
            var adaptersById = new Dictionary<string, System.Net.NetworkInformation.NetworkInterface>(StringComparer.OrdinalIgnoreCase);

            foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback ||
                    adapter.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                var detail = CreateNetworkAdapterData(adapter);
                data.Adapters.Add(detail);
                if (!string.IsNullOrWhiteSpace(detail.AdapterId))
                {
                    adaptersById[detail.AdapterId] = adapter;
                }

                data.AdapterCount++;
                if (adapter.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    data.AdapterUpCount++;
                }

                if (adapter.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
                {
                    data.WirelessAdapterCount++;
                }
            }

            var primary = ChoosePrimaryNetworkAdapter(data.Adapters);
            if (primary != null)
            {
                primary.IsPrimary = true;
                ApplyPrimaryNetworkAdapter(data, primary);

                // WMI enrichment for DHCP and status
                try
                {
                    if (!adaptersById.TryGetValue(primary.AdapterId ?? string.Empty, out var preferred))
                    {
                        preferred = null;
                    }

                    if (preferred != null)
                    {
                        using var configSearcher = new ManagementObjectSearcher(
                            $"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE SettingID = '{preferred.Id}'");
                        foreach (ManagementObject config in configSearcher.Get())
                        {
                            data.DhcpEnabled = GetValueSafe(config, "DHCPEnabled") == "True" ? "Yes" : "No";
                            data.DhcpServer = GetValueSafe(config, "DHCPServer");
                            
                            var obtained = GetValueSafe(config, "DHCPLeaseObtained");
                            if (!string.IsNullOrWhiteSpace(obtained))
                                try { data.LeaseObtained = ManagementDateTimeConverter.ToDateTime(obtained).ToString("yyyy-MM-dd HH:mm"); } catch { }
                                
                            var expires = GetValueSafe(config, "DHCPLeaseExpires");
                            if (!string.IsNullOrWhiteSpace(expires))
                                try { data.LeaseExpires = ManagementDateTimeConverter.ToDateTime(expires).ToString("yyyy-MM-dd HH:mm"); } catch { }
                            break;
                        }

                        using var adapterSearcher = new ManagementObjectSearcher(
                            $"SELECT * FROM Win32_NetworkAdapter WHERE GUID = '{preferred.Id}'");
                        foreach (ManagementObject adapterObj in adapterSearcher.Get())
                        {
                            var status = GetValueSafe(adapterObj, "NetConnectionStatus");
                            data.NetConnectionStatus = MapNetConnectionStatus(status);
                            break;
                        }
                    }
                }
                catch { }

                if (primary != null)
                {
                    primary.DhcpEnabled = data.DhcpEnabled;
                    primary.DhcpServer = data.DhcpServer;
                    primary.LeaseObtained = data.LeaseObtained;
                    primary.LeaseExpires = data.LeaseExpires;
                    primary.Status = !string.IsNullOrWhiteSpace(data.NetConnectionStatus)
                        ? data.NetConnectionStatus
                        : primary.Status;
                }
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] Network load failed: {ex.Message}");
        }

        data.Adapters = data.Adapters
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(GetNetworkAdapterPriority)
            .ThenBy(a => a.Name)
            .ToList();

        data.AdapterCount = data.Adapters.Count;
        data.AdapterUpCount = data.Adapters.Count(a => string.Equals(a.Status, "Connected", StringComparison.OrdinalIgnoreCase) || string.Equals(a.Status, "Up", StringComparison.OrdinalIgnoreCase));
        data.WirelessAdapterCount = data.Adapters.Count(a =>
            !string.IsNullOrWhiteSpace(a.AdapterType) &&
            a.AdapterType.Contains("Wireless", StringComparison.OrdinalIgnoreCase));

        return data;
    }

    internal static NetworkAdapterData? ChoosePrimaryNetworkAdapter(IEnumerable<NetworkAdapterData> adapters)
    {
        return adapters
            .OrderByDescending(GetNetworkAdapterPriority)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static NetworkAdapterData CreateNetworkAdapterData(System.Net.NetworkInformation.NetworkInterface adapter)
    {
        var detail = new NetworkAdapterData
        {
            AdapterId = adapter.Id,
            Name = adapter.Name,
            Description = adapter.Description,
            AdapterType = adapter.NetworkInterfaceType.ToString(),
            Status = adapter.OperationalStatus.ToString(),
            MacAddress = adapter.GetPhysicalAddress().ToString(),
            LinkSpeed = FormatLinkSpeed(adapter.Speed)
        };

        var ipProps = adapter.GetIPProperties();
        foreach (var unicast in ipProps.UnicastAddresses)
        {
            if (unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                string.IsNullOrWhiteSpace(detail.Ipv4))
            {
                detail.Ipv4 = unicast.Address.ToString();
            }
            else if (unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                     !unicast.Address.IsIPv6LinkLocal &&
                     string.IsNullOrWhiteSpace(detail.Ipv6))
            {
                detail.Ipv6 = unicast.Address.ToString();
            }
        }

        detail.Gateway = ipProps.GatewayAddresses
            .FirstOrDefault(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
            .Address
            .ToString();

        var dnsList = ipProps.DnsAddresses
            .Where(d => d.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Take(3)
            .Select(d => d.ToString())
            .ToList();
        detail.Dns = dnsList.Count > 0 ? string.Join(", ", dnsList) : null;

        return detail;
    }

    private static int GetNetworkAdapterPriority(NetworkAdapterData adapter)
    {
        var score = 0;
        if (IsConnectedStatus(adapter.Status))
        {
            score += 500;
        }

        if (HasUsableIpv4(adapter.Ipv4))
        {
            score += 220;
        }

        if (HasUsableGateway(adapter.Gateway))
        {
            score += 180;
        }

        if (!IsVirtualNetworkAdapter(adapter))
        {
            score += 160;
        }

        if (IsPhysicalNetworkType(adapter.AdapterType))
        {
            score += 80;
        }

        score += GetLinkSpeedScore(adapter.LinkSpeed);
        return score;
    }

    private static bool IsConnectedStatus(string? status)
    {
        return string.Equals(status, "Up", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "Connected", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasUsableIpv4(string? ipv4)
    {
        return !string.IsNullOrWhiteSpace(ipv4) &&
               !ipv4.StartsWith("169.254.", StringComparison.OrdinalIgnoreCase) &&
               !ipv4.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasUsableGateway(string? gateway)
    {
        return !string.IsNullOrWhiteSpace(gateway) &&
               !gateway.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPhysicalNetworkType(string? adapterType)
    {
        if (string.IsNullOrWhiteSpace(adapterType))
        {
            return false;
        }

        return adapterType.Contains("Ethernet", StringComparison.OrdinalIgnoreCase) ||
               adapterType.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
               adapterType.Contains("Gigabit", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVirtualNetworkAdapter(NetworkAdapterData adapter)
    {
        var joined = $"{adapter.Name} {adapter.Description} {adapter.AdapterType}".Trim();
        if (string.IsNullOrWhiteSpace(joined))
        {
            return false;
        }

        var value = joined.ToLowerInvariant();
        return value.Contains("wireguard") ||
               value.Contains("wintun") ||
               value.Contains("tailscale") ||
               value.Contains("zerotier") ||
               value.Contains("hamachi") ||
               value.Contains("tap-windows") ||
               value.Contains("virtual") ||
               value.Contains("hyper-v") ||
               value.Contains("vmware") ||
               value.Contains("vpn") ||
               value.Contains("tunnel") ||
               value.Contains("loopback");
    }

    private static int GetLinkSpeedScore(string? linkSpeed)
    {
        if (string.IsNullOrWhiteSpace(linkSpeed))
        {
            return 0;
        }

        var match = Regex.Match(linkSpeed, @"(?<value>\d+(\.\d+)?)\s*(?<unit>Gbps|Mbps)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return 0;
        }

        var numeric = double.Parse(match.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var unit = match.Groups["unit"].Value;
        var mbps = unit.StartsWith("g", StringComparison.OrdinalIgnoreCase) ? numeric * 1000d : numeric;
        return (int)Math.Clamp(mbps / 100d, 0d, 60d);
    }

    private static string? GetLogicalDrivesForDisk(string deviceId)
    {
        var logicalDrives = new List<string>();
        try
        {
            using var partitionSearcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId.Replace("\\", "\\\\")}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");

            foreach (ManagementObject partition in partitionSearcher.Get())
            {
                using var logicalSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");

                foreach (ManagementObject logical in logicalSearcher.Get())
                {
                    var driveLetter = logical["DeviceID"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(driveLetter))
                    {
                        logicalDrives.Add(driveLetter);
                    }
                }
            }
        }
        catch
        {
            return null;
        }

        return logicalDrives.Count > 0 ? string.Join(", ", logicalDrives.Distinct(StringComparer.OrdinalIgnoreCase)) : null;
    }

    private static bool HasLogicalDriveLetter(string? logicalDrives, string driveLetter)
    {
        if (string.IsNullOrWhiteSpace(logicalDrives))
        {
            return false;
        }

        return logicalDrives
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(letter => string.Equals(letter, driveLetter, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyPrimaryNetworkAdapter(NetworkHardwareData data, NetworkAdapterData primary)
    {
        data.PrimaryAdapterName = primary.Name;
        data.PrimaryAdapterDescription = primary.Description;
        data.PrimaryAdapterType = primary.AdapterType;
        data.PrimaryMacAddress = primary.MacAddress;
        data.PrimaryLinkSpeed = primary.LinkSpeed;
        data.PrimaryIpv4 = primary.Ipv4;
        data.PrimaryIpv6 = primary.Ipv6;
        data.PrimaryGateway = primary.Gateway;
        data.PrimaryDns = primary.Dns;
        data.NetConnectionStatus = primary.Status;
    }

    private static string? MapNetConnectionStatus(string? status)
    {
        return status switch
        {
            "0" => "Disconnected",
            "1" => "Connecting",
            "2" => "Connected",
            "3" => "Disconnecting",
            "7" => "Hardware Not Present",
            _ => status
        };
    }

    private static UsbHardwareData LoadUsbData()
    {
        return HardwarePeripheralDataCollector.LoadUsbData();
    }

    private static AudioHardwareData LoadAudioData()
    {
        return HardwarePeripheralDataCollector.LoadAudioData();
    }

    private static string? FormatLinkSpeed(long speedBitsPerSecond)
    {
        if (speedBitsPerSecond <= 0)
        {
            return null;
        }

        const double giga = 1_000_000_000d;
        const double mega = 1_000_000d;

        if (speedBitsPerSecond >= giga)
        {
            return $"{speedBitsPerSecond / giga:F2} Gbps";
        }

        return $"{speedBitsPerSecond / mega:F0} Mbps";
    }

    private static void ParseUsbVidPid(string? deviceId, out string? vendorId, out string? productId)
    {
        vendorId = null;
        productId = null;

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        var match = Regex.Match(deviceId, @"VID_([0-9A-F]{4}).*PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return;
        }

        vendorId = match.Groups[1].Value.ToUpperInvariant();
        productId = match.Groups[2].Value.ToUpperInvariant();
    }

    private static string ToArchitecture(string? value)
    {
        return value switch
        {
            "0" => "x86",
            "5" => "ARM",
            "6" => "Itanium",
            "9" => "x64",
            "12" => "ARM64",
            _ => "Unknown"
        };
    }

    private static string? GetValueSafe(ManagementBaseObject obj, string propertyName)
    {
        try
        {
            return obj[propertyName]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static int GetIntSafe(ManagementBaseObject obj, string propertyName)
    {
        var raw = GetValueSafe(obj, propertyName);
        return int.TryParse(raw, out var value) ? value : 0;
    }

    private static long GetLongSafe(ManagementBaseObject obj, string propertyName)
    {
        var raw = GetValueSafe(obj, propertyName);
        return long.TryParse(raw, out var value) ? value : 0L;
    }

    private static string ToChassisType(int chassisCode)
    {
        return chassisCode switch
        {
            1 => "Other",
            2 => "Unknown",
            3 => "Desktop",
            4 => "Low Profile Desktop",
            5 => "Pizza Box",
            6 => "Mini Tower",
            7 => "Tower",
            8 => "Portable",
            9 => "Laptop",
            10 => "Notebook",
            11 => "Hand Held",
            12 => "Docking Station",
            13 => "All in One",
            14 => "Sub Notebook",
            15 => "Space-saving",
            16 => "Lunch Box",
            17 => "Main System Chassis",
            18 => "Expansion Chassis",
            19 => "SubChassis",
            20 => "Bus Expansion Chassis",
            21 => "Peripheral Chassis",
            22 => "Storage Chassis",
            23 => "Rack Mount Chassis",
            24 => "Sealed-case PC",
            _ => "Unknown"
        };
    }

    private static string ToMemoryType(int smbiosMemoryType)
    {
        return smbiosMemoryType switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => "Unknown"
        };
    }

    private static void ParsePciVendorAndDevice(string? pnpDeviceId, out string? vendorId, out string? deviceId)
    {
        vendorId = null;
        deviceId = null;

        if (string.IsNullOrWhiteSpace(pnpDeviceId))
        {
            return;
        }

        var match = Regex.Match(pnpDeviceId, @"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return;
        }

        vendorId = match.Groups[1].Value.ToUpperInvariant();
        deviceId = match.Groups[2].Value.ToUpperInvariant();
    }

    private static string DetectInstallType(RegistryKey? currentVersion)
    {
        try
        {
            if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\Setup\Upgrade") != null)
            {
                return "Upgrade";
            }

            var installType = currentVersion?.GetValue("InstallationType")?.ToString();
            return string.IsNullOrWhiteSpace(installType) ? "Clean" : "Clean";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string? TryGetWindowsExperienceIndex()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT WinSPRLevel FROM Win32_WinSAT");
            foreach (ManagementObject obj in searcher.Get())
            {
                var score = obj["WinSPRLevel"];
                if (score != null && double.TryParse(score.ToString(), out var value))
                {
                    return value.ToString("F1");
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string TryGetBiosMode()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
            var value = key?.GetValue("PEFirmwareType");
            if (value == null)
            {
                return "Unknown";
            }

            return Convert.ToInt32(value) == 2 ? "UEFI" : "Legacy";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string TryGetSecureBootState()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            var value = key?.GetValue("UEFISecureBootEnabled");
            if (value == null)
            {
                return "Unknown";
            }

            return Convert.ToInt32(value) > 0 ? "Enabled" : "Disabled";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string? TryGetTpmVersion()
    {
        try
        {
            var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftTpm");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT SpecVersion FROM Win32_Tpm"));
            foreach (ManagementObject obj in searcher.Get())
            {
                var spec = obj["SpecVersion"]?.ToString();
                if (!string.IsNullOrWhiteSpace(spec))
                {
                    return spec;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string TryGetBitLockerStatus()
    {
        try
        {
            var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftVolumeEncryption");
            scope.Connect();
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT ProtectionStatus FROM Win32_EncryptableVolume"));
            foreach (ManagementObject obj in searcher.Get())
            {
                var status = Convert.ToInt32(obj["ProtectionStatus"] ?? 0);
                if (status == 1)
                {
                    return "On";
                }
            }

            return "Off";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string TryGetDeviceGuardStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\DeviceGuard", "SELECT SecurityServicesRunning FROM Win32_DeviceGuard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var running = obj["SecurityServicesRunning"] as Array;
                return running != null && running.Length > 0 ? "Enabled" : "Disabled";
            }
        }
        catch
        {
        }

        return "Unknown";
    }

    private static string TryGetCredentialGuardStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\DeviceGuard", "SELECT SecurityServicesRunning FROM Win32_DeviceGuard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var running = obj["SecurityServicesRunning"] as ushort[];
                if (running == null)
                {
                    return "Disabled";
                }

                foreach (var item in running)
                {
                    if (item == 1)
                    {
                        return "Enabled";
                    }
                }

                return "Disabled";
            }
        }
        catch
        {
        }

        return "Unknown";
    }

    private static string TryGetVirtualizationEnabled()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT VirtualizationFirmwareEnabled FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                var enabled = obj["VirtualizationFirmwareEnabled"];
                if (enabled != null)
                {
                    return Convert.ToBoolean(enabled) ? "Enabled" : "Disabled";
                }
            }
        }
        catch
        {
        }

        return "Unknown";
    }

    private static string TryGetHyperVInstalled()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT InstallState FROM Win32_OptionalFeature WHERE Name='Microsoft-Hyper-V-All'");
            foreach (ManagementObject obj in searcher.Get())
            {
                var state = Convert.ToInt32(obj["InstallState"] ?? 0);
                return state == 1 ? "Yes" : "No";
            }
        }
        catch
        {
        }

        return "Unknown";
    }

    private static string TryGetServiceStatus(string serviceName)
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

    private static string BuildUptime(string? lastBootTime)
    {
        if (string.IsNullOrWhiteSpace(lastBootTime))
        {
            return "Unknown";
        }

        if (!DateTime.TryParse(lastBootTime, out var parsedBoot))
        {
            return "Unknown";
        }

        var span = DateTime.Now - parsedBoot;
        if (span.TotalMinutes < 0)
        {
            return "Unknown";
        }

        return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m";
    }

    private static string? TryInferChipset(string? boardProduct)
    {
        if (string.IsNullOrWhiteSpace(boardProduct))
        {
            return null;
        }

        var match = Regex.Match(boardProduct, @"\b([ABHXZQ]\d{2,3}|[A-Z]\d{3})\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static string? TryReadChipsetFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            var candidates = new[]
            {
                key?.GetValue("BaseBoardProduct")?.ToString(),
                key?.GetValue("SystemProductName")?.ToString(),
                key?.GetValue("BaseBoardVersion")?.ToString()
            };

            var chipsetCandidate = candidates.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
            return TryInferChipset(chipsetCandidate) ?? chipsetCandidate;
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePreloadService] Chipset registry fallback failed: {ex.Message}");
            return null;
        }
    }

    private const uint DxgiAdapterFlagSoftware = 0x2;

    [DllImport("dxgi.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int CreateDXGIFactory1(ref Guid riid, out IntPtr ppFactory);

    [ComImport]
    [Guid("770AAE78-F26F-4DBA-A829-253C83D1B387")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDXGIFactory1
    {
        [PreserveSig] int SetPrivateData(ref Guid name, uint dataSize, IntPtr data);
        [PreserveSig] int SetPrivateDataInterface(ref Guid name, [MarshalAs(UnmanagedType.IUnknown)] object unknown);
        [PreserveSig] int GetPrivateData(ref Guid name, ref uint dataSize, IntPtr data);
        [PreserveSig] int GetParent(ref Guid riid, out IntPtr parent);
        [PreserveSig] int EnumAdapters(uint adapter, out IDXGIAdapter adapterInterface);
        [PreserveSig] int MakeWindowAssociation(IntPtr windowHandle, uint flags);
        [PreserveSig] int GetWindowAssociation(out IntPtr windowHandle);
        [PreserveSig] int CreateSwapChain([MarshalAs(UnmanagedType.IUnknown)] object device, ref DXGI_SWAP_CHAIN_DESC desc, out IntPtr swapChain);
        [PreserveSig] int CreateSoftwareAdapter(IntPtr moduleHandle, out IDXGIAdapter adapterInterface);
        [PreserveSig] int EnumAdapters1(uint adapter, out IDXGIAdapter1 adapterInterface);
        [PreserveSig] bool IsCurrent();
    }

    [ComImport]
    [Guid("2411E7E1-12AC-4CCF-BD14-9798E8534DC0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDXGIAdapter
    {
        [PreserveSig] int SetPrivateData(ref Guid name, uint dataSize, IntPtr data);
        [PreserveSig] int SetPrivateDataInterface(ref Guid name, [MarshalAs(UnmanagedType.IUnknown)] object unknown);
        [PreserveSig] int GetPrivateData(ref Guid name, ref uint dataSize, IntPtr data);
        [PreserveSig] int GetParent(ref Guid riid, out IntPtr parent);
        [PreserveSig] int EnumOutputs(uint output, out IntPtr outputInterface);
        [PreserveSig] int GetDesc(out DXGI_ADAPTER_DESC desc);
        [PreserveSig] int CheckInterfaceSupport(ref Guid interfaceName, out long userModeVersion);
    }

    [ComImport]
    [Guid("29038F61-3839-4626-91FD-086879011A05")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDXGIAdapter1
    {
        [PreserveSig] int SetPrivateData(ref Guid name, uint dataSize, IntPtr data);
        [PreserveSig] int SetPrivateDataInterface(ref Guid name, [MarshalAs(UnmanagedType.IUnknown)] object unknown);
        [PreserveSig] int GetPrivateData(ref Guid name, ref uint dataSize, IntPtr data);
        [PreserveSig] int GetParent(ref Guid riid, out IntPtr parent);
        [PreserveSig] int EnumOutputs(uint output, out IntPtr outputInterface);
        [PreserveSig] int GetDesc(out DXGI_ADAPTER_DESC desc);
        [PreserveSig] int CheckInterfaceSupport(ref Guid interfaceName, out long userModeVersion);
        [PreserveSig] int GetDesc1(out DXGI_ADAPTER_DESC1 desc);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DXGI_ADAPTER_DESC
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public ulong DedicatedVideoMemory;
        public ulong DedicatedSystemMemory;
        public ulong SharedSystemMemory;
        public Luid AdapterLuid;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DXGI_ADAPTER_DESC1
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public ulong DedicatedVideoMemory;
        public ulong DedicatedSystemMemory;
        public ulong SharedSystemMemory;
        public Luid AdapterLuid;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_SWAP_CHAIN_DESC
    {
        public DXGI_MODE_DESC BufferDesc;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint BufferUsage;
        public uint BufferCount;
        public IntPtr OutputWindow;
        [MarshalAs(UnmanagedType.Bool)] public bool Windowed;
        public uint SwapEffect;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_MODE_DESC
    {
        public uint Width;
        public uint Height;
        public DXGI_RATIONAL RefreshRate;
        public uint Format;
        public uint ScanlineOrdering;
        public uint Scaling;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DXGI_SAMPLE_DESC
    {
        public uint Count;
        public uint Quality;
    }
}

public sealed class HardwareValidationService
{
    public OsHardwareData Validate(OsHardwareData os)
    {
        if (string.IsNullOrWhiteSpace(os.ProductName) ||
            !os.ProductName.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            AppDiagnostics.Log("[HardwareValidation] Dropped invalid OS ProductName (must contain 'Windows').");
            os.ProductName = null;
        }

        return os;
    }

    public CpuHardwareData Validate(CpuHardwareData cpu)
    {
        if (cpu.Cores <= 0)
        {
            AppDiagnostics.Log("[HardwareValidation] Dropped invalid CPU cores value (must be > 0).");
            cpu.Cores = 0;
        }

        return cpu;
    }

    public GpuHardwareData Validate(GpuHardwareData gpu)
    {
        if (string.IsNullOrWhiteSpace(gpu.VendorId) || string.IsNullOrWhiteSpace(gpu.Vendor))
        {
            return gpu;
        }

        var expected = gpu.VendorId.ToUpperInvariant() switch
        {
            "10DE" => "NVIDIA",
            "1002" => "AMD",
            "8086" => "INTEL",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(expected) &&
            !gpu.Vendor.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            AppDiagnostics.Log("[HardwareValidation] Dropped GPU vendor due to PCI vendor mismatch.");
            gpu.Vendor = null;
        }

        return gpu;
    }

    public MotherboardHardwareData Validate(MotherboardHardwareData motherboard)
    {
        if (!string.IsNullOrWhiteSpace(motherboard.Manufacturer) &&
            motherboard.Manufacturer.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            AppDiagnostics.Log("[HardwareValidation] Dropped invalid motherboard manufacturer (contains 'Windows').");
            motherboard.Manufacturer = null;
        }

        return motherboard;
    }
}

public sealed class HardwareDetailSnapshot
{
    public OsHardwareData Os { get; init; } = new();
    public CpuHardwareData Cpu { get; init; } = new();
    public GpuHardwareData Gpu { get; init; } = new();
    public MotherboardHardwareData Motherboard { get; init; } = new();
    public MemoryHardwareData Memory { get; init; } = new();
    public StorageHardwareData Storage { get; init; } = new();
    public DisplayHardwareData Displays { get; init; } = new();
    public NetworkHardwareData Network { get; init; } = new();
    public UsbHardwareData Usb { get; init; } = new();
    public AudioHardwareData Audio { get; init; } = new();
}

public sealed class OsHardwareData
{
    public string? NormalizedName { get; set; }
    public string? IconKey { get; set; }
    public string? ProductName { get; set; }
    public string? Edition { get; set; }
    public string? DisplayVersion { get; set; }
    public string? ReleaseId { get; set; }
    public string? Version { get; set; }
    public int BuildNumber { get; set; }
    public int Ubr { get; set; }
    public string? Architecture { get; set; }
    public string? InstallDate { get; set; }
    public string? InstallType { get; set; }
    public string? RegisteredOwner { get; set; }
    public string? RegisteredOrganization { get; set; }
    public string? WindowsExperienceIndex { get; set; }
    public string? LastBootTime { get; set; }
    public string? Uptime { get; set; }
    public string? Username { get; set; }
    public string? BiosMode { get; set; }
    public string? SecureBootState { get; set; }
    public string? TpmVersion { get; set; }
    public string? BitLockerStatus { get; set; }
    public string? DeviceGuardStatus { get; set; }
    public string? CredentialGuardStatus { get; set; }
    public string? VirtualizationEnabled { get; set; }
    public string? HyperVInstalled { get; set; }
    public string? DefenderStatus { get; set; }
    public string? FirewallStatus { get; set; }

    public string? ProductNameSource { get; set; }
    public string? EditionSource { get; set; }
    public string? DisplayVersionSource { get; set; }
    public string? ReleaseIdSource { get; set; }
    public string? VersionSource { get; set; }
    public string? BuildNumberSource { get; set; }
    public string? ArchitectureSource { get; set; }
    public string? InstallDateSource { get; set; }
    public string? InstallTypeSource { get; set; }
    public string? RegisteredOwnerSource { get; set; }
    public string? RegisteredOrganizationSource { get; set; }
    public string? WindowsExperienceIndexSource { get; set; }
    public string? LastBootTimeSource { get; set; }
    public string? UptimeSource { get; set; }
    public string? UsernameSource { get; set; }
    public string? BiosModeSource { get; set; }
    public string? SecureBootStateSource { get; set; }
    public string? TpmVersionSource { get; set; }
    public string? BitLockerStatusSource { get; set; }
    public string? DeviceGuardStatusSource { get; set; }
    public string? CredentialGuardStatusSource { get; set; }
    public string? VirtualizationEnabledSource { get; set; }
    public string? HyperVInstalledSource { get; set; }
    public string? DefenderStatusSource { get; set; }
    public string? FirewallStatusSource { get; set; }
}

public sealed class CpuHardwareData
{
    public string? Name { get; set; }
    public string? Manufacturer { get; set; }
    public int Cores { get; set; }
    public int Threads { get; set; }
    public int MaxClockMhz { get; set; }
    public int CurrentClockMhz { get; set; }
    public int BusSpeedMhz { get; set; }
    public string? Architecture { get; set; }
    public string? Description { get; set; }
    public int L1CacheKB { get; set; }
    public int L2CacheKB { get; set; }
    public int L3CacheKB { get; set; }
    public double VoltageV { get; set; }
    public int AddressWidth { get; set; }
    public string? Stepping { get; set; }
    public string? CpuFamily { get; set; }
    public string? CpuModel { get; set; }
    public string? Socket { get; set; }
    public string? Revision { get; set; }
    public string? Microcode { get; set; }
    public bool? HyperThreading { get; set; }
    public bool? Virtualization { get; set; }
    public string? Caption { get; set; }
}

public sealed class GpuHardwareData
{
    public string? Name { get; set; }
    public string? Vendor { get; set; }
    public string? VendorId { get; set; }
    public string? DeviceId { get; set; }
    public string? PnpDeviceId { get; set; }
    public string? DriverVersion { get; set; }
    public string? DriverDate { get; set; }
    public string? VideoProcessor { get; set; }
    public string? AdapterDacType { get; set; }
    public string? VideoModeDescription { get; set; }
    public string? InferredVramType { get; set; }
    public int CurrentHorizontalResolution { get; set; }
    public int CurrentVerticalResolution { get; set; }
    public int RefreshRateHz { get; set; }
    public long AdapterRamBytes { get; set; }
}

public sealed class MotherboardHardwareData
{
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Product { get; set; }
    public string? Serial { get; set; }
    public string? Version { get; set; }
    public string? BiosVendor { get; set; }
    public string? BiosVersion { get; set; }
    public string? BiosDate { get; set; }
    public string? Chipset { get; set; }
    public string? ChassisType { get; set; }
    public string? AssetTag { get; set; }
    public int SystemSlotCount { get; set; }
}

public sealed class MemoryHardwareData
{
    public long TotalBytes { get; set; }
    public int ModuleCount { get; set; }
    public int TotalSlotCount { get; set; }
    public string? PrimaryManufacturer { get; set; }
    public string? PrimaryModel { get; set; }
    public string? MemoryType { get; set; }
    public int SpeedMhz { get; set; }
    public int ConfiguredSpeedMhz { get; set; }
    public string? FormFactor { get; set; }
    public int MinVoltageMv { get; set; }
    public List<MemoryModuleData> Modules { get; set; } = new();
}

public sealed class MemoryModuleData
{
    public string? Slot { get; set; }
    public string? BankLabel { get; set; }
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public long CapacityBytes { get; set; }
    public int SpeedMhz { get; set; }
    public int ConfiguredSpeedMhz { get; set; }
    public string? MemoryType { get; set; }
    public string? FormFactor { get; set; }
    public int MinVoltageMv { get; set; }
}

public sealed class DiskDriveData
{
    public string? DeviceId { get; set; }
    public string? Model { get; set; }
    public string? InterfaceType { get; set; }
    public string? SerialNumber { get; set; }
    public string? MediaType { get; set; }
    public string? FirmwareRevision { get; set; }
    public string? LogicalDrives { get; set; }
    public long SizeBytes { get; set; }
    public long FreeBytes { get; set; }
    public int PartitionCount { get; set; }
    public int Index { get; set; }
    public int VolumeCount { get; set; }
    public string? Status { get; set; }
    public string? PnpDeviceId { get; set; }
    public string? InterfaceSummary { get; set; }
    public bool IsExternal { get; set; }
    public bool IsSystemDisk { get; set; }
    public List<StorageVolumeData> Volumes { get; set; } = new();
}

public sealed class StorageVolumeData
{
    public string? DriveLetter { get; set; }
    public string? Label { get; set; }
    public string? FileSystem { get; set; }
    public long SizeBytes { get; set; }
    public long FreeBytes { get; set; }
    public string? PartitionType { get; set; }
    public bool IsBoot { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsSystem { get; set; }
}

public sealed class StorageHardwareData
{
    public int DeviceCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long TotalFreeBytes { get; set; }
    public int VolumeCount { get; set; }
    public int ExternalDriveCount { get; set; }
    public int SystemDriveCount { get; set; }
    public List<DiskDriveData> Disks { get; set; } = new();
    
    // Legacy support for primary drive (first in list)
    public string? PrimaryModel => Disks.FirstOrDefault()?.Model;
    public string? PrimaryInterface => Disks.FirstOrDefault()?.InterfaceType;
    public string? PrimarySerialNumber => Disks.FirstOrDefault()?.SerialNumber;
    public string? PrimaryMediaType => Disks.FirstOrDefault()?.MediaType;
    public string? FirmwareRevision => Disks.FirstOrDefault()?.FirmwareRevision;
    public int PartitionCount => Disks.FirstOrDefault()?.PartitionCount ?? 0;
    public int Index => Disks.FirstOrDefault()?.Index ?? 0;
}

public sealed class DisplayHardwareData
{
    public int DisplayCount { get; set; }
    public int PrimaryWidth { get; set; }
    public int PrimaryHeight { get; set; }
    public int PrimaryRefreshRateHz { get; set; }
    public int PrimaryBitsPerPixel { get; set; }
    public int VirtualWidth { get; set; }
    public int VirtualHeight { get; set; }
    public string? PrimaryMonitorName { get; set; }
    public string? MonitorManufacturer { get; set; }
    public string? MonitorModel { get; set; }
    public string? GpuOutput { get; set; }
    public List<DisplayDeviceData> Devices { get; set; } = new();
}

public sealed class DisplayDeviceData
{
    public string? Name { get; set; }
    public string? DeviceName { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? ProductCode { get; set; }
    public string? Resolution { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int RefreshRateHz { get; set; }
    public int BitsPerPixel { get; set; }
    public int PhysicalWidthCm { get; set; }
    public int PhysicalHeightCm { get; set; }
    public bool IsPrimary { get; set; }
    public string? ConnectionType { get; set; }
    public string? IconLookupSeed { get; set; }
    public string? MatchMode { get; set; }
    public string? MatchKey { get; set; }
    public string? MatchedInstance { get; set; }
}

public sealed class NetworkHardwareData
{
    public int AdapterCount { get; set; }
    public int AdapterUpCount { get; set; }
    public int WirelessAdapterCount { get; set; }
    public string? PrimaryAdapterName { get; set; }
    public string? PrimaryAdapterDescription { get; set; }
    public string? PrimaryAdapterType { get; set; }
    public string? PrimaryMacAddress { get; set; }
    public string? PrimaryLinkSpeed { get; set; }
    public string? PrimaryIpv4 { get; set; }
    public string? PrimaryIpv6 { get; set; }
    public string? PrimaryGateway { get; set; }
    public string? PrimaryDns { get; set; }
    public string? DhcpEnabled { get; set; }
    public string? DhcpServer { get; set; }
    public string? LeaseObtained { get; set; }
    public string? LeaseExpires { get; set; }
    public string? NetConnectionStatus { get; set; }
    public List<NetworkAdapterData> Adapters { get; set; } = new();
}

public sealed class NetworkAdapterData
{
    public string? AdapterId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? AdapterType { get; set; }
    public string? Status { get; set; }
    public string? LinkSpeed { get; set; }
    public string? MacAddress { get; set; }
    public string? Ipv4 { get; set; }
    public string? Ipv6 { get; set; }
    public string? Gateway { get; set; }
    public string? Dns { get; set; }
    public string? DhcpEnabled { get; set; }
    public string? DhcpServer { get; set; }
    public string? LeaseObtained { get; set; }
    public string? LeaseExpires { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class UsbHardwareData
{
    public int RemovableDriveCount { get; set; }
    public int UsbControllerCount { get; set; }
    public int UsbDeviceCount { get; set; }
    public int HubCount { get; set; }
    public int InputDeviceCount { get; set; }
    public int AudioDeviceCount { get; set; }
    public int StorageDeviceCount { get; set; }
    public string? PrimaryControllerName { get; set; }
    public string? PrimaryUsbDeviceName { get; set; }
    public string? PrimaryManufacturer { get; set; }
    public string? PrimaryCategory { get; set; }
    public string? PrimaryVendorId { get; set; }
    public string? PrimaryProductId { get; set; }
    public string? PrimaryStatus { get; set; }
    public string? PrimaryDeviceId { get; set; }
    public List<UsbDeviceData> Devices { get; set; } = new();
}

public sealed class UsbDeviceData
{
    public string? Name { get; set; }
    public string? DeviceId { get; set; }
    public string? PnpDeviceId { get; set; }
    public string? Manufacturer { get; set; }
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? ClassName { get; set; }
    public string? Service { get; set; }
    public string? Category { get; set; }
    public bool IsController { get; set; }
    public bool IsHub { get; set; }
}

public sealed class AudioHardwareData
{
    public int DeviceCount { get; set; }
    public int PhysicalDeviceCount { get; set; }
    public int VirtualDeviceCount { get; set; }
    public string? PrimaryDeviceName { get; set; }
    public string? PrimaryManufacturer { get; set; }
    public string? PrimaryStatus { get; set; }
    public string? PrimaryDriverProvider { get; set; }
    public string? PrimaryDriverVersion { get; set; }
    public string? PrimaryDriverDate { get; set; }
    public List<string> AllDevices { get; set; } = new();
    public List<AudioDeviceData> Devices { get; set; } = new();
}

public sealed class AudioDeviceData
{
    public string? Name { get; set; }
    public string? Manufacturer { get; set; }
    public string? Status { get; set; }
    public string? DeviceId { get; set; }
    public string? DriverProvider { get; set; }
    public string? DriverVersion { get; set; }
    public string? DriverDate { get; set; }
    public string? InfName { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVirtual { get; set; }
}
