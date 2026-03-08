using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Services.Hardware;
using WindowsOptimizer.App.Services.OsDetection;

namespace WindowsOptimizer.App.Services;

public interface IHardwareDataPreloader
{
    Task PreloadAllAsync(IProgress<PreloadProgress>? progress = null);
    bool IsPreloadComplete { get; }
    DateTime? LastPreloadTime { get; }
}

public sealed class HardwareDataPreloader : IHardwareDataPreloader
{
    private readonly IMetricCacheService _cache;
    private readonly WindowsOptimizer.App.Services.OsDetection.IOsDetectionService _osService;
    private readonly WindowsOptimizer.App.Services.Hardware.IMotherboardProvider _motherboardProvider;

    private bool _isPreloadComplete;
    private DateTime? _lastPreloadTime;

    public bool IsPreloadComplete => _isPreloadComplete;
    public DateTime? LastPreloadTime => _lastPreloadTime;

    public HardwareDataPreloader(
        IMetricCacheService cache,
        WindowsOptimizer.App.Services.OsDetection.IOsDetectionService osService,
        WindowsOptimizer.App.Services.Hardware.IMotherboardProvider motherboardProvider)
    {
        _cache = cache;
        _osService = osService;
        _motherboardProvider = motherboardProvider;
    }

    public async Task PreloadAllAsync(IProgress<PreloadProgress>? progress = null)
    {
        Debug.WriteLine("[HardwareDataPreloader] Starting parallel preload...");

        var tasks = new List<(string Key, string Name, Func<Task<object>> Provider)>
        {
            (CacheKeys.Os, "Operating System", LoadOsAsync),
            (CacheKeys.Cpu, "CPU", LoadCpuAsync),
            (CacheKeys.Gpu, "GPU", LoadGpuAsync),
            (CacheKeys.Motherboard, "Motherboard", LoadMotherboardAsync),
            (CacheKeys.Memory, "Memory", LoadMemoryAsync),
            (CacheKeys.Storage, "Storage", LoadStorageAsync),
            (CacheKeys.Network, "Network", LoadNetworkAsync),
        };

        var total = tasks.Count;
        var completed = 0;
        var ttl = TimeSpan.FromMinutes(5);

        progress?.Report(new PreloadProgress(0, total, "Starting...", PreloadState.Running));

        var results = new Dictionary<string, object?>();
        var lockObj = new object();

        await Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                var result = await task.Provider();
                lock (lockObj)
                {
                    results[task.Key] = result;
                    completed++;
                    progress?.Report(new PreloadProgress(completed, total, task.Name, PreloadState.Running));
                }
                Debug.WriteLine($"[HardwareDataPreloader] Loaded {task.Name}");
            }
            catch (Exception ex)
            {
                lock (lockObj)
                {
                    results[task.Key] = null;
                    completed++;
                    progress?.Report(new PreloadProgress(completed, total, task.Name, PreloadState.Failed, ex.Message));
                }
                Debug.WriteLine($"[HardwareDataPreloader] Failed to load {task.Name}: {ex.Message}");
            }
        }));

        foreach (var kvp in results)
        {
            if (kvp.Value != null && _cache is MetricCacheService cacheImpl)
            {
                cacheImpl.SetRaw(kvp.Key, kvp.Value, ttl);
            }
        }

        _isPreloadComplete = true;
        _lastPreloadTime = DateTime.UtcNow;

        progress?.Report(new PreloadProgress(total, total, "Complete", PreloadState.Completed));
        Debug.WriteLine($"[HardwareDataPreloader] Preload complete at {_lastPreloadTime}");
        Debug.WriteLine($"[PRELOAD] Complete. IsPreloadComplete={IsPreloadComplete}");
    }

    private async Task<object> LoadOsAsync() => await _osService.DetectAsync(includeActivation: true);

    private async Task<object> LoadCpuAsync()
    {
        return await Task.Run(() =>
        {
            var data = new CpuHardwareData();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, CurrentClockSpeed, ExtClock, VoltageCaps, CurrentVoltage, AddressWidth, Architecture, Description, L2CacheSize, L3CacheSize FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        data.Name = SafeGetString(obj, "Name");
                        data.Manufacturer = SafeGetString(obj, "Manufacturer");
                        data.Cores = SafeGetInt(obj, "NumberOfCores");
                        data.Threads = SafeGetInt(obj, "NumberOfLogicalProcessors");
                        data.MaxClockMhz = SafeGetInt(obj, "MaxClockSpeed");
                        data.CurrentClockMhz = SafeGetInt(obj, "CurrentClockSpeed");
                        data.BusSpeedMhz = SafeGetInt(obj, "ExtClock");
                        
                        var voltage = SafeGetInt(obj, "CurrentVoltage");
                        data.VoltageV = voltage > 0 ? voltage / 10.0 : 0;
                        
                        data.AddressWidth = SafeGetInt(obj, "AddressWidth");
                        data.Architecture = SafeGetString(obj, "Architecture");
                        data.Description = SafeGetString(obj, "Description");
                        data.L2CacheKB = SafeGetInt(obj, "L2CacheSize");
                        data.L3CacheKB = SafeGetInt(obj, "L3CacheSize");

                        if (!string.IsNullOrWhiteSpace(data.Description))
                        {
                            var parsed = ParseCpuDescription(data.Description);
                            data.Stepping = parsed.Stepping;
                            data.CpuFamily = parsed.Family;
                            data.CpuModel = parsed.Model;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[HardwareDataPreloader] CPU: {ex.Message}"); }
            return data;
        });
    }

    private async Task<object> LoadGpuAsync()
    {
        return await Task.Run(() =>
        {
            var data = new GpuHardwareData();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, AdapterCompatibility, VideoProcessor, AdapterRAM, DriverVersion, DriverDate, VideoModeDescription, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        data.Name = SafeGetString(obj, "Name");
                        data.Vendor = SafeGetString(obj, "AdapterCompatibility");
                        data.VideoProcessor = SafeGetString(obj, "VideoProcessor");
                        data.AdapterRamBytes = SafeGetLong(obj, "AdapterRAM");
                        data.DriverVersion = SafeGetString(obj, "DriverVersion");
                        data.DriverDate = SafeGetString(obj, "DriverDate");
                        data.VideoModeDescription = SafeGetString(obj, "VideoModeDescription");
                        data.CurrentHorizontalResolution = SafeGetInt(obj, "CurrentHorizontalResolution");
                        data.CurrentVerticalResolution = SafeGetInt(obj, "CurrentVerticalResolution");
                        data.RefreshRateHz = SafeGetInt(obj, "CurrentRefreshRate");
                        
                        if (!string.IsNullOrWhiteSpace(data.Name))
                        {
                            data.InferredVramType = InferVramType(data.Name);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[HardwareDataPreloader] GPU: {ex.Message}"); }
            return data;
        });
    }

    private async Task<object> LoadMotherboardAsync() => await _motherboardProvider.GetAsync();

    private async Task<object> LoadMemoryAsync()
    {
        return await Task.Run(() =>
        {
            var data = new MemoryHardwareData();
            try
            {
                long totalBytes = 0;
                var modules = 0;
                var speeds = new List<int>();
                var configuredSpeeds = new List<int>();
                var voltages = new List<int>();
                var formFactors = new List<int>();

                string? primaryManufacturer = null;
                string? primaryModel = null;
                string? memoryType = null;

                using var searcher = new ManagementObjectSearcher(
                    "SELECT Capacity, Manufacturer, PartNumber, Speed, ConfiguredClockSpeed, ConfiguredVoltage, MinVoltage, FormFactor, SMBIOSMemoryType FROM Win32_PhysicalMemory");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        modules++;
                        totalBytes += SafeGetLong(obj, "Capacity");
                        primaryManufacturer ??= SafeGetString(obj, "Manufacturer");
                        primaryModel ??= SafeGetString(obj, "PartNumber");
                        
                        var speed = SafeGetInt(obj, "Speed");
                        if (speed > 0) speeds.Add(speed);
                        
                        var confSpeed = SafeGetInt(obj, "ConfiguredClockSpeed");
                        if (confSpeed > 0) configuredSpeeds.Add(confSpeed);

                        var volt = SafeGetInt(obj, "ConfiguredVoltage");
                        if (volt > 0) voltages.Add(volt);

                        var ff = SafeGetInt(obj, "FormFactor");
                        if (ff > 0) formFactors.Add(ff);

                        if (memoryType == null)
                        {
                            memoryType = SafeGetInt(obj, "SMBIOSMemoryType") switch
                            {
                                26 => "DDR4",
                                34 => "DDR5",
                                24 => "DDR3",
                                _ => "Unknown"
                            };
                        }
                    }
                }
                data.TotalBytes = totalBytes;
                data.ModuleCount = modules;
                data.PrimaryManufacturer = primaryManufacturer;
                data.PrimaryModel = primaryModel;
                data.MemoryType = memoryType;
                data.SpeedMhz = speeds.Count > 0 ? speeds.Max() : 0;
                data.ConfiguredSpeedMhz = configuredSpeeds.Count > 0 ? configuredSpeeds.Max() : 0;
                data.MinVoltageMv = voltages.Count > 0 ? voltages.Min() : 0;
                data.FormFactor = formFactors.Count > 0 ? ToFormFactor(formFactors[0]) : "Unknown";
            }
            catch (Exception ex) { Debug.WriteLine($"[HardwareDataPreloader] Memory: {ex.Message}"); }
            return data;
        });
    }

    private async Task<object> LoadStorageAsync()
    {
        return await Task.Run(() =>
        {
            var data = new StorageHardwareData();
            try
            {
                var deviceCount = 0;
                long totalSize = 0;

                using var searcher = new ManagementObjectSearcher(
                    "SELECT Model, Size, InterfaceType, SerialNumber FROM Win32_DiskDrive");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        var model = SafeGetString(obj, "Model");
                        var interfaceType = SafeGetString(obj, "InterfaceType");
                        var serialNumber = SafeGetString(obj, "SerialNumber");
                        var sizeBytes = SafeGetLong(obj, "Size");

                        deviceCount++;
                        totalSize += sizeBytes;
                        data.Disks.Add(new DiskDriveData
                        {
                            Model = model,
                            InterfaceType = interfaceType,
                            SerialNumber = serialNumber,
                            MediaType = string.IsNullOrWhiteSpace(model) ? null : InferMediaType(model, interfaceType),
                            SizeBytes = sizeBytes
                        });
                    }
                }
                data.DeviceCount = deviceCount;
                data.TotalSizeBytes = totalSize;
            }
            catch (Exception ex) { Debug.WriteLine($"[HardwareDataPreloader] Storage: {ex.Message}"); }
            return data;
        });
    }

    private async Task<object> LoadNetworkAsync()
    {
        return await Task.Run(() =>
        {
            var data = new NetworkHardwareData();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, Description, NetConnectionStatus, AdapterType, MACAddress, Speed FROM Win32_NetworkAdapter WHERE NetConnectionStatus IS NOT NULL");
                var adapters = 0;
                var upCount = 0;
                var wirelessCount = 0;
                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        adapters++;
                        if (SafeGetInt(obj, "NetConnectionStatus") == 2) upCount++;
                        var name = SafeGetString(obj, "Name") ?? "";
                        var desc = SafeGetString(obj, "Description") ?? "";
                        if (name.Contains("Wireless", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                            desc.Contains("Wireless", StringComparison.OrdinalIgnoreCase))
                            wirelessCount++;
                        
                        if (data.PrimaryAdapterName == null)
                        {
                            data.PrimaryAdapterName = name;
                            data.PrimaryAdapterDescription = desc;
                            data.PrimaryAdapterType = SafeGetString(obj, "AdapterType");
                            data.PrimaryMacAddress = SafeGetString(obj, "MACAddress");
                            var speedBits = SafeGetLong(obj, "Speed");
                            data.PrimaryLinkSpeed = speedBits > 0
                                ? speedBits / 1_000_000 >= 1000
                                    ? $"{speedBits / 1_000_000_000.0:F1} Gbps"
                                    : $"{speedBits / 1_000_000} Mbps"
                                : null;
                        }
                    }
                }
                data.AdapterCount = adapters;
                data.AdapterUpCount = upCount;
                data.WirelessAdapterCount = wirelessCount;

                // IPv4 and IPv6
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    var props = ni.GetIPProperties();
                    foreach (var ip in props.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork && data.PrimaryIpv4 == null)
                            data.PrimaryIpv4 = ip.Address.ToString();
                        else if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6 && data.PrimaryIpv6 == null && !ip.Address.IsIPv6LinkLocal)
                            data.PrimaryIpv6 = ip.Address.ToString();
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[HardwareDataPreloader] Network: {ex.Message}"); }
            return data;
        });
    }

    private (string? Family, string? Model, string? Stepping) ParseCpuDescription(string desc)
    {
        var match = Regex.Match(desc, @"Family\s+(\d+)\s+Model\s+(\d+)\s+Stepping\s+(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
        }
        return (null, null, null);
    }

    private string InferVramType(string gpuName)
    {
        var name = gpuName.ToUpperInvariant();
        if (name.Contains("RTX 40") || name.Contains("RTX 30")) return "GDDR6X";
        if (name.Contains("RTX 20") || name.Contains("GTX 16") || name.Contains("RX 6") || name.Contains("RX 5")) return "GDDR6";
        if (name.Contains("GTX 10") || name.Contains("RX 4")) return "GDDR5";
        return "Unknown";
    }

    private string ToFormFactor(int ff) => ff switch
    {
        8 => "DIMM", 12 => "SO-DIMM", 13 => "SIMM", 14 => "DIMM", 15 => "SDRAM", 
        _ => "DIMM" // Heuristic default
    };

    private string InferMediaType(string model, string? interfaceType)
    {
        var m = model.ToUpperInvariant();
        if (m.Contains("NVME") || m.Contains("PCI-E")) return "NVMe SSD";
        if (m.Contains("SSD")) return "SATA SSD";
        if (interfaceType?.Contains("USB") == true) return "Removable USB";
        return "Hard Disk Drive";
    }

    private static string? SafeGetString(ManagementObject obj, string prop) =>
        obj[prop]?.ToString()?.Trim();
    private static int SafeGetInt(ManagementObject obj, string prop) =>
        obj[prop] is int i ? i : int.TryParse(obj[prop]?.ToString(), out var v) ? v : 0;
    private static long SafeGetLong(ManagementObject obj, string prop) =>
        obj[prop] is long l ? l : obj[prop] is int i ? i : long.TryParse(obj[prop]?.ToString(), out var v) ? v : 0;
}
