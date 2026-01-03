using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using Microsoft.Win32;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Versioning;
using LibreHardwareMonitor.Hardware;

namespace WindowsOptimizer.Infrastructure.Metrics;

[SupportedOSPlatform("windows")]
public sealed class MetricProvider : IDisposable
{
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly PerformanceCounter? _diskReadCounter;
    private readonly PerformanceCounter? _diskWriteCounter;
    private readonly PerformanceCounter? _processCounter;
    private readonly PerformanceCounter? _threadCounter;
    private readonly PerformanceCounter? _handleCounter;
    private readonly PerformanceCounter? _committedBytesCounter;
    private readonly PerformanceCounter? _commitLimitCounter;
    private readonly PerformanceCounter? _cacheBytesCounter;
    private readonly PerformanceCounter? _pagedPoolBytesCounter;
    private readonly PerformanceCounter? _nonPagedPoolBytesCounter;
    private readonly Computer? _computer;
    private readonly double? _gpuMemoryTotalMb;
    private readonly double? _cpuBaseSpeedMhz;
    private readonly int? _cpuSockets;
    private readonly int? _cpuCores;
    private readonly int? _cpuLogicalProcessors;
    private readonly int? _cpuCacheL2Kb;
    private readonly int? _cpuCacheL3Kb;
    private readonly bool? _virtualizationEnabled;
    private readonly double? _memorySpeedMhz;
    private readonly int? _memorySlotsUsed;
    private readonly int? _memorySlotsTotal;
    private readonly string? _memoryFormFactor;
    private readonly double? _memoryHardwareReservedMb;
    private bool _disposed;

    public MetricProvider()
    {
        _cpuCounter = TryCreateCounter("Processor", "% Processor Time", "_Total");
        _ramCounter = TryCreateCounter("Memory", "Available MBytes");
        _diskReadCounter = TryCreateCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
        _diskWriteCounter = TryCreateCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        _processCounter = TryCreateCounter("System", "Processes");
        _threadCounter = TryCreateCounter("System", "Threads");
        _handleCounter = TryCreateCounter("System", "Handle Count");
        _committedBytesCounter = TryCreateCounter("Memory", "Committed Bytes");
        _commitLimitCounter = TryCreateCounter("Memory", "Commit Limit");
        _cacheBytesCounter = TryCreateCounter("Memory", "Cache Bytes");
        _pagedPoolBytesCounter = TryCreateCounter("Memory", "Pool Paged Bytes");
        _nonPagedPoolBytesCounter = TryCreateCounter("Memory", "Pool Nonpaged Bytes");

        // Initialize LibreHardwareMonitor for temperature sensors
        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true
            };
            _computer.Open();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize LibreHardwareMonitor: {ex.Message}");
            _computer = null;
        }

        _gpuMemoryTotalMb = TryGetGpuMemoryTotalMb();
        (_cpuBaseSpeedMhz, _cpuSockets, _cpuCores, _cpuLogicalProcessors, _cpuCacheL2Kb, _cpuCacheL3Kb, _virtualizationEnabled) = TryGetCpuStaticInfo();
        (_memorySpeedMhz, _memorySlotsUsed, _memorySlotsTotal, _memoryFormFactor, _memoryHardwareReservedMb) = TryGetMemoryStaticInfo();
    }

    public float GetCpuUsage() => _cpuCounter?.NextValue() ?? 0;
    
    public float GetAvailableRamMb() => _ramCounter?.NextValue() ?? 0;
    
    public float GetDiskIOBytesPerSec()
    {
        var read = _diskReadCounter?.NextValue() ?? 0;
        var write = _diskWriteCounter?.NextValue() ?? 0;
        return read + write;
    }

    public CpuPerformanceSnapshot GetCpuPerformanceSnapshot()
    {
        var currentSpeed = TryGetCpuCurrentSpeedMhz();
        var (processes, threads, handles) = TryGetSystemCounts();
        return new CpuPerformanceSnapshot(
            GetCpuUsage(),
            currentSpeed,
            _cpuBaseSpeedMhz,
            processes,
            threads,
            handles,
            _cpuSockets,
            _cpuCores,
            _cpuLogicalProcessors,
            _virtualizationEnabled,
            _cpuCacheL2Kb,
            _cpuCacheL3Kb);
    }

    public MemoryPerformanceSnapshot GetMemoryPerformanceSnapshot()
    {
        var totalGb = GetTotalRamGb();
        var availableGb = GetAvailableRamMb() / 1024.0;
        var usedGb = totalGb - availableGb;
        var committedGb = TryReadCounterGb(_committedBytesCounter);
        var commitLimitGb = TryReadCounterGb(_commitLimitCounter);
        var cacheGb = TryReadCounterGb(_cacheBytesCounter);
        var pagedPoolGb = TryReadCounterGb(_pagedPoolBytesCounter);
        var nonPagedPoolGb = TryReadCounterGb(_nonPagedPoolBytesCounter);

        return new MemoryPerformanceSnapshot(
            totalGb,
            availableGb,
            usedGb,
            committedGb,
            commitLimitGb,
            cacheGb,
            pagedPoolGb,
            nonPagedPoolGb,
            _memorySpeedMhz,
            _memorySlotsUsed,
            _memorySlotsTotal,
            _memoryFormFactor,
            _memoryHardwareReservedMb);
    }

    public IReadOnlyList<DiskPerformanceSnapshot> GetDiskPerformanceSnapshots()
    {
        var results = new List<DiskPerformanceSnapshot>();
        var perfByDrive = TryGetLogicalDiskPerf();
        var indexByDrive = TryGetDiskIndexByDrive();
        var identityByIndex = TryGetDiskIdentities()
            .Where(disk => disk.Index.HasValue)
            .GroupBy(disk => disk.Index!.Value)
            .ToDictionary(g => g.Key, g => g.First());
        var systemDrive = TryGetSystemDriveLetter();
        var pageFileDrives = TryGetPageFileDriveLetters();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable))
            {
                continue;
            }

            var driveLetter = drive.Name.TrimEnd('\\');
            if (!perfByDrive.TryGetValue(driveLetter, out var perf))
            {
                perf = null;
            }

            indexByDrive.TryGetValue(driveLetter, out var diskIndex);
            DiskIdentity? identity = null;
            if (diskIndex.HasValue && identityByIndex.TryGetValue(diskIndex.Value, out var found))
            {
                identity = found;
            }

            results.Add(new DiskPerformanceSnapshot(
                driveLetter,
                diskIndex,
                identity?.Model,
                identity?.MediaTypeLabel,
                identity?.InterfaceType,
                drive.TotalSize / (1024.0 * 1024 * 1024),
                drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                perf?.ActiveTimePercent,
                perf?.ReadMbps,
                perf?.WriteMbps,
                perf?.AvgResponseMs,
                perf?.QueueLength,
                systemDrive != null && driveLetter.Equals(systemDrive, StringComparison.OrdinalIgnoreCase),
                pageFileDrives.Contains(driveLetter)));
        }

        return results;
    }

    public double GetCpuTemperature()
    {
        if (_computer == null)
        {
            return TryGetCpuTemperatureFromWmi() ?? double.NaN;
        }

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu ||
                hardware.HardwareType == HardwareType.Motherboard ||
                hardware.HardwareType == HardwareType.SuperIO)
            {
                UpdateHardware(hardware);
                var temps = EnumerateSensors(hardware)
                    .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue)
                    .ToList();

                if (temps.Count == 0)
                {
                    continue;
                }

                var preferred = temps.FirstOrDefault(s => ContainsAny(s.Name, "Package", "CPU Package"))
                                ?? temps.FirstOrDefault(s => ContainsAny(s.Name, "Tctl", "Tdie", "Die"))
                                ?? temps.FirstOrDefault(s => ContainsAny(s.Name, "Core Average", "CCD", "SoC"))
                                ?? temps.FirstOrDefault(s => ContainsAny(s.Name, "CPU", "Core"));

                if (preferred?.Value is { } preferredValue)
                {
                    return preferredValue;
                }

                var coreTemps = temps
                    .Where(s => s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                                && !s.Name.Contains("Max", StringComparison.OrdinalIgnoreCase)
                                && !s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.Value!.Value)
                    .ToList();

                if (coreTemps.Count > 0)
                {
                    return coreTemps.Average();
                }

                return temps.Max(s => s.Value!.Value);
            }
        }

        return TryGetCpuTemperatureFromWmi() ?? double.NaN;
    }

    public double GetGpuTemperature()
    {
        if (_computer == null) return double.NaN;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                UpdateHardware(hardware);
                foreach (var sensor in EnumerateSensors(hardware))
                {
                    if (sensor.SensorType == SensorType.Temperature &&
                        (sensor.Name.Contains("Core") || sensor.Name.Contains("GPU")))
                        return sensor.Value ?? double.NaN;
                }
            }
        }
        return double.NaN;
    }

    public double GetGpuUsage()
    {
        if (_computer == null) return 0;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load &&
                        (sensor.Name.Contains("Core") || sensor.Name.Contains("GPU Core")))
                        return sensor.Value ?? 0;
                }
            }
        }
        return 0;
    }

    public GpuMemorySnapshot GetGpuMemorySnapshot()
    {
        if (_computer == null)
        {
            return new GpuMemorySnapshot(0, _gpuMemoryTotalMb ?? 0, 0, false);
        }

        double? usedMb = null;
        double? totalMb = null;
        double? usagePercent = null;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (!sensor.Value.HasValue)
                    {
                        continue;
                    }

                    var name = sensor.Name ?? string.Empty;

                    if (sensor.SensorType == SensorType.Load &&
                        name.Contains("Memory", StringComparison.OrdinalIgnoreCase))
                    {
                        usagePercent ??= sensor.Value.Value;
                    }

                    if (sensor.SensorType == SensorType.Data || sensor.SensorType == SensorType.SmallData)
                    {
                        if (name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("GPU Memory", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("VRAM", StringComparison.OrdinalIgnoreCase))
                        {
                            usedMb ??= sensor.SensorType == SensorType.Data
                                ? sensor.Value.Value * 1024
                                : sensor.Value.Value;
                        }

                        if (name.Contains("Memory Total", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Total Memory", StringComparison.OrdinalIgnoreCase))
                        {
                            totalMb ??= sensor.SensorType == SensorType.Data
                                ? sensor.Value.Value * 1024
                                : sensor.Value.Value;
                        }
                    }
                }

                break;
            }
        }

        totalMb ??= _gpuMemoryTotalMb;

        if (!usagePercent.HasValue && usedMb.HasValue && totalMb.HasValue && totalMb > 0)
        {
            usagePercent = usedMb.Value / totalMb.Value * 100.0;
        }

        if (!usedMb.HasValue && usagePercent.HasValue && totalMb.HasValue && totalMb > 0)
        {
            usedMb = totalMb.Value * usagePercent.Value / 100.0;
        }

        var isAvailable = usagePercent.HasValue || usedMb.HasValue;

        return new GpuMemorySnapshot(
            usedMb ?? 0,
            totalMb ?? 0,
            usagePercent ?? 0,
            isAvailable);
    }

    public GpuPerformanceSnapshot GetGpuPerformanceSnapshot()
    {
        string? name = null;
        double? dedicatedMb = null;
        double? sharedMb = null;
        double? totalMb = null;
        string? driverVersion = null;
        DateTime? driverDate = null;
        string? directXVersion = TryGetDirectXVersion();
        string? locationInfo = null;
        string? adapterCompatibility = null;
        string? videoProcessor = null;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, AdapterRAM, DedicatedVideoMemory, DedicatedSystemMemory, SharedSystemMemory, " +
                "DriverVersion, DriverDate, CurrentBitsPerPixel, LocationInformation, AdapterCompatibility, VideoProcessor " +
                "FROM Win32_VideoController");

            foreach (ManagementObject obj in searcher.Get())
            {
                var candidateName = obj["Name"]?.ToString()?.Trim();
                var candidateDedicated = TryReadDouble(obj, "DedicatedVideoMemory")
                                         ?? TryReadDouble(obj, "DedicatedSystemMemory")
                                         ?? TryReadDouble(obj, "AdapterRAM");
                var candidateShared = TryReadDouble(obj, "SharedSystemMemory");
                var candidateDriverVersion = obj["DriverVersion"]?.ToString();
                var candidateDriverDate = TryReadWmiDateTime(obj["DriverDate"]);
                var candidateLocation = obj["LocationInformation"]?.ToString();
                var candidateCompatibility = obj["AdapterCompatibility"]?.ToString();
                var candidateProcessor = obj["VideoProcessor"]?.ToString();
                var bitsPerPixel = TryReadInt(obj, "CurrentBitsPerPixel") ?? 0;

                var candidateDedicatedMb = NormalizeMemoryMb(candidateDedicated);
                var isActive = bitsPerPixel > 0;
                var prefer = name == null
                             || isActive
                             || (candidateDedicatedMb.HasValue && (!dedicatedMb.HasValue || candidateDedicatedMb.Value > dedicatedMb.Value));

                if (!prefer)
                {
                    continue;
                }

                name = candidateName ?? name;
                dedicatedMb = candidateDedicatedMb ?? dedicatedMb;
                sharedMb = NormalizeMemoryMb(candidateShared) ?? sharedMb;
                driverVersion = candidateDriverVersion ?? driverVersion;
                driverDate = candidateDriverDate ?? driverDate;
                locationInfo = candidateLocation ?? locationInfo;
                adapterCompatibility = candidateCompatibility ?? adapterCompatibility;
                videoProcessor = candidateProcessor ?? videoProcessor;

                if (isActive)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read GPU performance info: {ex.Message}");
        }

        dedicatedMb ??= _gpuMemoryTotalMb;

        if (!totalMb.HasValue)
        {
            if (dedicatedMb.HasValue && sharedMb.HasValue)
            {
                totalMb = dedicatedMb.Value + sharedMb.Value;
            }
            else if (dedicatedMb.HasValue)
            {
                totalMb = dedicatedMb.Value;
            }
            else if (sharedMb.HasValue)
            {
                totalMb = sharedMb.Value;
            }
        }

        var isAvailable = name != null
                          || dedicatedMb.HasValue
                          || sharedMb.HasValue
                          || driverVersion != null
                          || directXVersion != null;

        return new GpuPerformanceSnapshot(
            name,
            dedicatedMb,
            sharedMb,
            totalMb,
            driverVersion,
            driverDate,
            directXVersion,
            locationInfo,
            adapterCompatibility,
            videoProcessor,
            isAvailable);
    }

    public FanSpeedSnapshot GetFanSpeedSnapshot()
    {
        if (_computer == null)
        {
            return new FanSpeedSnapshot(double.NaN, double.NaN);
        }

        double? cpuRpm = null;
        double? cpuFallback = null;
        double? gpuRpm = null;

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu ||
                hardware.HardwareType == HardwareType.Motherboard ||
                hardware.HardwareType == HardwareType.SuperIO)
            {
                UpdateHardware(hardware);

                foreach (var sensor in EnumerateSensors(hardware))
                {
                    if (sensor.SensorType != SensorType.Fan || !sensor.Value.HasValue)
                    {
                        continue;
                    }

                    var name = sensor.Name ?? string.Empty;
                    if (IsCpuFanName(name))
                    {
                        cpuRpm = Math.Max(cpuRpm ?? 0, sensor.Value.Value);
                    }
                    else
                    {
                        cpuFallback = Math.Max(cpuFallback ?? 0, sensor.Value.Value);
                    }
                }
            }

            if (hardware.HardwareType == HardwareType.GpuNvidia ||
                hardware.HardwareType == HardwareType.GpuAmd ||
                hardware.HardwareType == HardwareType.GpuIntel)
            {
                UpdateHardware(hardware);

                foreach (var sensor in EnumerateSensors(hardware))
                {
                    if (sensor.SensorType != SensorType.Fan || !sensor.Value.HasValue)
                    {
                        continue;
                    }

                    gpuRpm = Math.Max(gpuRpm ?? 0, sensor.Value.Value);
                }
            }
        }

        cpuRpm ??= cpuFallback;
        return new FanSpeedSnapshot(cpuRpm ?? double.NaN, gpuRpm ?? double.NaN);
    }

    public DiskHealthSnapshot GetDiskHealthSnapshot()
    {
        double? healthPercent = null;
        bool? predictFailure = null;

        if (_computer != null)
        {
            var values = new List<double>();
            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Storage)
                {
                    continue;
                }

                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    if (!sensor.Value.HasValue)
                    {
                        continue;
                    }

                    if (sensor.SensorType == SensorType.Level &&
                        (sensor.Name.Contains("Health", StringComparison.OrdinalIgnoreCase) ||
                         sensor.Name.Contains("Remaining Life", StringComparison.OrdinalIgnoreCase)))
                    {
                        values.Add(sensor.Value.Value);
                    }
                }
            }

            if (values.Count > 0)
            {
                healthPercent = values.Min();
            }
        }

        if (!healthPercent.HasValue)
        {
            healthPercent = TryGetStorageReliabilityHealth();
        }

        if (!healthPercent.HasValue)
        {
            predictFailure = TryGetSmartPredictFailure();
        }

        if (!predictFailure.HasValue)
        {
            predictFailure = TryGetStorageHealthStatus();
        }

        if (!predictFailure.HasValue)
        {
            predictFailure = TryGetDiskStatusFromWin32DiskDrive();
        }

        return new DiskHealthSnapshot(healthPercent, predictFailure);
    }

    public IReadOnlyList<DiskHealthInfo> GetDiskHealthItems()
    {
        var items = new List<DiskHealthInfo>();
        var sensorHealth = TryGetStorageHealthFromSensors();
        var diskIdentities = TryGetDiskIdentities();
        var reliabilityByIndex = TryGetStorageReliabilityByIndex();
        var healthStatusByIndex = TryGetStorageHealthStatusByIndex();
        var smartPredictEntries = TryGetSmartPredictEntries();

        if (diskIdentities.Count == 0 && sensorHealth.Count > 0)
        {
            foreach (var entry in sensorHealth)
            {
                items.Add(new DiskHealthInfo(null, entry.Key, null, entry.Value, null, "LibreHardwareMonitor"));
            }

            return items;
        }

        foreach (var disk in diskIdentities)
        {
            double? healthPercent = null;
            if (disk.Index.HasValue && reliabilityByIndex.TryGetValue(disk.Index.Value, out var reliability))
            {
                healthPercent = reliability;
            }

            if (!healthPercent.HasValue)
            {
                healthPercent = TryMatchSensorHealth(sensorHealth, disk);
            }

            bool? predictFailure = null;
            if (disk.Index.HasValue)
            {
                if (TryMatchSmartPredict(smartPredictEntries, disk, out var smartPredict))
                {
                    predictFailure = smartPredict;
                }
                else if (healthStatusByIndex.TryGetValue(disk.Index.Value, out var healthStatus))
                {
                    predictFailure = healthStatus;
                }
            }

            var name = BuildDiskDisplayName(disk);
            var source = healthPercent.HasValue ? "SMART life" : predictFailure.HasValue ? "SMART status" : null;
            items.Add(new DiskHealthInfo(disk.Index, name, disk.MediaTypeLabel, healthPercent, predictFailure, source));
        }

        return items;
    }

    public string BuildSensorDiagnosticsReport()
    {
        var report = new StringBuilder();
        report.AppendLine("Windows Optimizer - Sensor Diagnostics");
        report.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        report.AppendLine("Summary");
        report.AppendLine($"CPU Temp: {FormatTemperature(GetCpuTemperature())}");
        report.AppendLine($"GPU Temp: {FormatTemperature(GetGpuTemperature())}");
        var fans = GetFanSpeedSnapshot();
        report.AppendLine($"CPU Fan: {FormatRpm(fans.CpuRpm)}");
        report.AppendLine($"GPU Fan: {FormatRpm(fans.GpuRpm)}");
        var diskHealth = GetDiskHealthSnapshot();
        report.AppendLine($"Disk Health (overall): {FormatDiskHealth(diskHealth.HealthPercent, diskHealth.PredictFailure)}");
        report.AppendLine();

        report.AppendLine("LibreHardwareMonitor Sensors");
        if (_computer == null)
        {
            report.AppendLine("  Not available.");
        }
        else
        {
            foreach (var hardware in _computer.Hardware)
            {
                UpdateHardware(hardware);
                report.AppendLine($"- {hardware.HardwareType}: {hardware.Name}");
                foreach (var sensor in EnumerateSensors(hardware))
                {
                    var value = sensor.Value.HasValue ? $"{sensor.Value.Value:F2}" : "N/A";
                    var unit = sensor.SensorType switch
                    {
                        SensorType.Temperature => "C",
                        SensorType.Load => "%",
                        SensorType.Fan => "RPM",
                        SensorType.Clock => "MHz",
                        SensorType.Power => "W",
                        SensorType.Data => "GB",
                        SensorType.SmallData => "MB",
                        SensorType.Level => "%",
                        _ => string.Empty
                    };
                    report.AppendLine($"  - {sensor.SensorType} {sensor.Name}: {value}{unit}");
                }
            }
        }

        AppendStorageDiagnostics(report);

        return report.ToString();
    }

    private Dictionary<string, double> TryGetStorageHealthFromSensors()
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (_computer == null)
        {
            return values;
        }

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Storage)
            {
                continue;
            }

            UpdateHardware(hardware);
            var sensorValues = new List<double>();

            foreach (var sensor in EnumerateSensors(hardware))
            {
                if (!sensor.Value.HasValue || sensor.SensorType != SensorType.Level)
                {
                    continue;
                }

                var name = sensor.Name ?? string.Empty;
                if (name.Contains("Health", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Remaining Life", StringComparison.OrdinalIgnoreCase))
                {
                    sensorValues.Add(sensor.Value.Value);
                }
            }

            if (sensorValues.Count > 0)
            {
                values[hardware.Name] = sensorValues.Min();
            }
        }

        return values;
    }

    private static double? TryMatchSensorHealth(
        IReadOnlyDictionary<string, double> sensorHealth,
        DiskIdentity disk)
    {
        if (sensorHealth.Count == 0)
        {
            return null;
        }

        foreach (var (name, value) in sensorHealth)
        {
            if (!string.IsNullOrWhiteSpace(disk.Model) &&
                (name.IndexOf(disk.Model, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 disk.Model.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return value;
            }

            if (!string.IsNullOrWhiteSpace(disk.FriendlyName) &&
                (name.IndexOf(disk.FriendlyName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 disk.FriendlyName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return value;
            }
        }

        return null;
    }

    private static List<DiskIdentity> TryGetDiskIdentities()
    {
        var disks = new List<DiskIdentity>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Index, Model, MediaType, InterfaceType, DeviceID, PNPDeviceID FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                var index = TryReadInt(obj, "Index");
                var model = obj["Model"]?.ToString()?.Trim();
                var mediaType = obj["MediaType"]?.ToString()?.Trim();
                var interfaceType = obj["InterfaceType"]?.ToString()?.Trim();
                var deviceId = obj["DeviceID"]?.ToString();
                var pnpDeviceId = obj["PNPDeviceID"]?.ToString();
                var mediaLabel = NormalizeMediaTypeLabel(mediaType, model, interfaceType);
                disks.Add(new DiskIdentity(index, model, null, mediaLabel, deviceId, pnpDeviceId, interfaceType));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read Win32_DiskDrive: {ex.Message}");
        }

        try
        {
            var scope = TryConnectStorageScope();
            if (scope != null)
            {
                using var searcher = new ManagementObjectSearcher(
                    scope,
                    new ObjectQuery("SELECT DeviceId, FriendlyName, MediaType FROM MSFT_PhysicalDisk"));

                foreach (ManagementObject obj in searcher.Get())
                {
                    var index = TryReadInt(obj, "DeviceId");
                    var friendlyName = obj["FriendlyName"]?.ToString()?.Trim();
                    var mediaLabel = NormalizeStorageMediaType(TryReadInt(obj, "MediaType"));

                    if (index.HasValue)
                    {
                        var existingIndex = disks.FindIndex(d => d.Index == index.Value);
                        if (existingIndex >= 0)
                        {
                            var updated = disks[existingIndex] with
                            {
                                FriendlyName = friendlyName ?? disks[existingIndex].FriendlyName,
                                MediaTypeLabel = mediaLabel ?? disks[existingIndex].MediaTypeLabel
                            };
                            disks[existingIndex] = updated;
                        }
                        else
                        {
                            disks.Add(new DiskIdentity(index, null, friendlyName, mediaLabel, null, null, null));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read MSFT_PhysicalDisk: {ex.Message}");
        }

        return disks;
    }

    private static Dictionary<int, double> TryGetStorageReliabilityByIndex()
    {
        var results = new Dictionary<int, double>();
        try
        {
            var scope = TryConnectStorageScope();
            if (scope == null)
            {
                return results;
            }

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT InstanceName, DiskNumber, DeviceId, PercentLifeUsed, RemainingLife, Wear FROM MSFT_StorageReliabilityCounter"));

            foreach (ManagementObject obj in searcher.Get())
            {
                var index = TryReadInt(obj, "DiskNumber")
                            ?? TryReadInt(obj, "DeviceId")
                            ?? TryParseDiskIndex(obj["InstanceName"]?.ToString());
                if (!index.HasValue)
                {
                    continue;
                }

                var percentUsed = NormalizePercent(TryReadDouble(obj, "PercentLifeUsed"), invert: true);
                var remainingLife = NormalizePercent(TryReadDouble(obj, "RemainingLife"), invert: false);
                var wear = NormalizePercent(TryReadDouble(obj, "Wear"), invert: true);

                var value = percentUsed ?? remainingLife ?? wear;
                if (value.HasValue)
                {
                    if (!results.TryGetValue(index.Value, out var existing))
                    {
                        results[index.Value] = value.Value;
                    }
                    else
                    {
                        results[index.Value] = Math.Min(existing, value.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read storage reliability counters: {ex.Message}");
        }

        return results;
    }

    private static Dictionary<int, bool?> TryGetStorageHealthStatusByIndex()
    {
        var results = new Dictionary<int, bool?>();

        try
        {
            var scope = TryConnectStorageScope();
            if (scope == null)
            {
                return results;
            }

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT DeviceId, HealthStatus FROM MSFT_PhysicalDisk"));

            foreach (ManagementObject obj in searcher.Get())
            {
                var index = TryReadInt(obj, "DeviceId");
                var healthStatus = TryReadInt(obj, "HealthStatus");
                if (!index.HasValue || !healthStatus.HasValue)
                {
                    continue;
                }

                results[index.Value] = MapHealthStatusToPredictFailure(healthStatus.Value);
            }

            using var diskSearcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT Number, HealthStatus FROM MSFT_Disk"));

            foreach (ManagementObject obj in diskSearcher.Get())
            {
                var index = TryReadInt(obj, "Number");
                var healthStatus = TryReadInt(obj, "HealthStatus");
                if (!index.HasValue || !healthStatus.HasValue)
                {
                    continue;
                }

                results[index.Value] = MapHealthStatusToPredictFailure(healthStatus.Value);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read storage health status: {ex.Message}");
        }

        return results;
    }

    private static List<SmartPredictEntry> TryGetSmartPredictEntries()
    {
        var results = new List<SmartPredictEntry>();
        try
        {
            var scope = new ManagementScope(@"\\.\root\WMI", new ConnectionOptions
            {
                EnablePrivileges = true,
                Impersonation = ImpersonationLevel.Impersonate
            });
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT InstanceName, PredictFailure FROM MSStorageDriver_FailurePredictStatus"));

            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["PredictFailure"] is not bool predicted)
                {
                    continue;
                }

                var instanceName = obj["InstanceName"]?.ToString();
                results.Add(new SmartPredictEntry(instanceName, predicted));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read SMART predict status: {ex.Message}");
        }

        return results;
    }

    private static bool TryMatchSmartPredict(
        IReadOnlyList<SmartPredictEntry> entries,
        DiskIdentity disk,
        out bool predictFailure)
    {
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.InstanceName))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(disk.PnpDeviceId) &&
                entry.InstanceName.IndexOf(disk.PnpDeviceId, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                predictFailure = entry.PredictFailure;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(disk.DeviceId) &&
                entry.InstanceName.IndexOf(disk.DeviceId, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                predictFailure = entry.PredictFailure;
                return true;
            }

            if (disk.Index.HasValue &&
                TryParseDiskIndex(entry.InstanceName) == disk.Index.Value)
            {
                predictFailure = entry.PredictFailure;
                return true;
            }
        }

        predictFailure = false;
        return false;
    }

    private static int? TryParseDiskIndex(string? instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            return null;
        }

        var match = Regex.Match(instanceName, @"(?i)(?:physicaldrive|disk)\s*([0-9]+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var index))
        {
            return index;
        }

        return null;
    }

    private static string BuildDiskDisplayName(DiskIdentity disk)
    {
        var baseName = disk.FriendlyName ?? disk.Model ?? "Disk";
        var label = disk.Index.HasValue ? $"Disk {disk.Index.Value}" : "Disk";

        if (!string.IsNullOrWhiteSpace(baseName))
        {
            if (!baseName.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                label = $"{label}: {baseName}";
            }
            else
            {
                label = baseName;
            }
        }

        if (!string.IsNullOrWhiteSpace(disk.MediaTypeLabel))
        {
            label = $"{label} ({disk.MediaTypeLabel})";
        }

        return label;
    }

    private static string? NormalizeStorageMediaType(int? mediaType)
    {
        return mediaType switch
        {
            3 => "HDD",
            4 => "SSD",
            5 => "SCM",
            _ => null
        };
    }

    private static string? NormalizeMediaTypeLabel(string? mediaType, string? model, string? interfaceType)
    {
        if (!string.IsNullOrWhiteSpace(interfaceType) &&
            interfaceType.IndexOf("NVMe", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "NVMe";
        }

        if (ContainsAny(mediaType, "SSD") || ContainsAny(model, "SSD"))
        {
            return "SSD";
        }

        if (ContainsAny(mediaType, "HDD", "Hard Disk") || ContainsAny(model, "HDD"))
        {
            return "HDD";
        }

        return string.IsNullOrWhiteSpace(mediaType) ? null : mediaType;
    }

    private static bool? MapHealthStatusToPredictFailure(int healthStatus)
    {
        return healthStatus switch
        {
            1 => false,
            2 => true,
            3 => true,
            _ => null
        };
    }

    private static string FormatTemperature(double value)
    {
        return double.IsFinite(value) && value > 0 ? $"{value:F1} C" : "N/A";
    }

    private static string FormatRpm(double value)
    {
        return double.IsFinite(value) && value > 0 ? $"{value:F0} RPM" : "N/A";
    }

    private static string FormatDiskHealth(double? percent, bool? predictFailure)
    {
        if (percent.HasValue)
        {
            return $"{percent.Value:F0}%";
        }

        if (predictFailure == true)
        {
            return "Warning";
        }

        if (predictFailure == false)
        {
            return "OK";
        }

        return "N/A";
    }

    private static void AppendStorageDiagnostics(StringBuilder report)
    {
        report.AppendLine("WMI: MSFT_PhysicalDisk");
        try
        {
            var scope = TryConnectStorageScope();
            if (scope == null)
            {
                report.AppendLine("  Not available.");
            }
            else
            {
                using var searcher = new ManagementObjectSearcher(
                    scope,
                    new ObjectQuery("SELECT DeviceId, FriendlyName, MediaType, HealthStatus, Size FROM MSFT_PhysicalDisk"));
                foreach (ManagementObject obj in searcher.Get())
                {
                    var index = TryReadInt(obj, "DeviceId");
                    var name = obj["FriendlyName"]?.ToString()?.Trim();
                    var media = NormalizeStorageMediaType(TryReadInt(obj, "MediaType")) ?? "Unknown";
                    var health = TryReadInt(obj, "HealthStatus");
                    var size = TryReadDouble(obj, "Size");
                    report.AppendLine($"  Disk {index}: {name} | {media} | HealthStatus={health} | Size={FormatSizeGb(size)}");
                }
            }
        }
        catch (Exception ex)
        {
            report.AppendLine($"  Error: {ex.Message}");
        }

        report.AppendLine();
        report.AppendLine("WMI: MSFT_StorageReliabilityCounter");
        try
        {
            var scope = TryConnectStorageScope();
            if (scope == null)
            {
                report.AppendLine("  Not available.");
            }
            else
            {
                using var searcher = new ManagementObjectSearcher(
                    scope,
                    new ObjectQuery("SELECT InstanceName, DiskNumber, DeviceId, PercentLifeUsed, RemainingLife, Wear FROM MSFT_StorageReliabilityCounter"));
                foreach (ManagementObject obj in searcher.Get())
                {
                    var instance = obj["InstanceName"]?.ToString();
                    var diskNumber = TryReadInt(obj, "DiskNumber") ?? TryReadInt(obj, "DeviceId");
                    var percentUsed = TryReadDouble(obj, "PercentLifeUsed");
                    var remainingLife = TryReadDouble(obj, "RemainingLife");
                    var wear = TryReadDouble(obj, "Wear");
                    report.AppendLine($"  {instance} (Disk {diskNumber}): PercentLifeUsed={percentUsed}, RemainingLife={remainingLife}, Wear={wear}");
                }
            }
        }
        catch (Exception ex)
        {
            report.AppendLine($"  Error: {ex.Message}");
        }

        report.AppendLine();
        report.AppendLine("WMI: MSStorageDriver_FailurePredictStatus");
        try
        {
            var scope = new ManagementScope(@"\\.\root\WMI", new ConnectionOptions
            {
                EnablePrivileges = true,
                Impersonation = ImpersonationLevel.Impersonate
            });
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT InstanceName, PredictFailure FROM MSStorageDriver_FailurePredictStatus"));
            foreach (ManagementObject obj in searcher.Get())
            {
                var instance = obj["InstanceName"]?.ToString();
                var predicted = obj["PredictFailure"]?.ToString();
                report.AppendLine($"  {instance}: PredictFailure={predicted}");
            }
        }
        catch (Exception ex)
        {
            report.AppendLine($"  Error: {ex.Message}");
        }

        report.AppendLine();
        report.AppendLine("WMI: Win32_DiskDrive");
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Index, Model, MediaType, InterfaceType, Status, PNPDeviceID, DeviceID FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                var index = TryReadInt(obj, "Index");
                var model = obj["Model"]?.ToString()?.Trim();
                var mediaType = obj["MediaType"]?.ToString();
                var interfaceType = obj["InterfaceType"]?.ToString();
                var status = obj["Status"]?.ToString();
                var deviceId = obj["DeviceID"]?.ToString();
                var pnp = obj["PNPDeviceID"]?.ToString();
                report.AppendLine($"  Disk {index}: {model} | {mediaType} | {interfaceType} | Status={status} | DeviceID={deviceId} | PNP={pnp}");
            }
        }
        catch (Exception ex)
        {
            report.AppendLine($"  Error: {ex.Message}");
        }
    }

    private static string FormatSizeGb(double? bytes)
    {
        if (!bytes.HasValue || bytes.Value <= 0)
        {
            return "N/A";
        }

        var gb = bytes.Value / (1024 * 1024 * 1024);
        return $"{gb:F1} GB";
    }

    public SystemInfo GetSystemInfo()
    {
        var info = new SystemInfo();

        try
        {
            // Get OS info
            using (var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.OsName = obj["Caption"]?.ToString() ?? "Unknown";
                    info.OsVersion = obj["Version"]?.ToString() ?? "Unknown";
                }
            }

            // Get CPU info
            using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.CpuName = obj["Name"]?.ToString()?.Trim() ?? "Unknown";
                    info.CpuCores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                    info.CpuThreads = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                    break; // Take first CPU
                }
            }

            // Get system uptime
            using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var bootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    info.Uptime = DateTime.Now - bootTime;
                }
            }

            // Get GPU name
            if (_computer != null)
            {
                foreach (var hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.GpuNvidia ||
                        hardware.HardwareType == HardwareType.GpuAmd ||
                        hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        info.GpuName = hardware.Name;
                        break;
                    }
                }
            }

            info.TotalRamGb = GetTotalRamGb();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get system info: {ex.Message}");
        }

        return info;
    }

    public double GetTotalRamGb()
    {
        var totalRam = 0.0;
        try
        {
            var query = new ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory");
            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get())
            {
                totalRam += Convert.ToDouble(obj["Capacity"]);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get total RAM: {ex.Message}");
        }
        return totalRam / (1024 * 1024 * 1024); // Convert to GB
    }

    public double GetUsedRamGb()
    {
        var availableMb = GetAvailableRamMb();
        var totalGb = GetTotalRamGb();
        return totalGb - (availableMb / 1024.0);
    }

    private static void UpdateHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware)
        {
            sub.Update();
        }
    }

    private static IEnumerable<ISensor> EnumerateSensors(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            yield return sensor;
        }

        foreach (var sub in hardware.SubHardware)
        {
            foreach (var sensor in sub.Sensors)
            {
                yield return sensor;
            }
        }
    }

    private static bool IsCpuFanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Contains("CPU", StringComparison.OrdinalIgnoreCase)
               || name.Contains("Pump", StringComparison.OrdinalIgnoreCase)
               || name.Contains("AIO", StringComparison.OrdinalIgnoreCase)
               || name.Contains("Water", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string? value, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var token in tokens)
        {
            if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static PerformanceCounter? TryCreateCounter(string category, string counter, string instance = "")
    {
        try
        {
            return string.IsNullOrWhiteSpace(instance)
                ? new PerformanceCounter(category, counter)
                : new PerformanceCounter(category, counter, instance);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create counter {category}:{counter} ({instance}): {ex.Message}");
            return null;
        }
    }

    private static double? TryReadCounterGb(PerformanceCounter? counter)
    {
        if (counter == null)
        {
            return null;
        }

        try
        {
            return counter.NextValue() / (1024 * 1024 * 1024);
        }
        catch
        {
            return null;
        }
    }

    private (int? Processes, int? Threads, int? Handles) TryGetSystemCounts()
    {
        int? processes = null;
        int? threads = null;
        int? handles = null;

        try
        {
            processes = _processCounter != null ? (int)_processCounter.NextValue() : null;
        }
        catch
        {
        }

        try
        {
            threads = _threadCounter != null ? (int)_threadCounter.NextValue() : null;
        }
        catch
        {
        }

        try
        {
            handles = _handleCounter != null ? (int)_handleCounter.NextValue() : null;
        }
        catch
        {
        }

        return (processes, threads, handles);
    }

    private static double? TryGetCpuCurrentSpeedMhz()
    {
        try
        {
            double? best = null;
            using var searcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentClockSpeed"] == null)
                {
                    continue;
                }

                var speed = Convert.ToDouble(obj["CurrentClockSpeed"]);
                if (speed <= 0)
                {
                    continue;
                }

                if (!best.HasValue || speed > best.Value)
                {
                    best = speed;
                }
            }

            return best;
        }
        catch
        {
            return null;
        }
    }

    private static (double? BaseSpeedMhz, int? Sockets, int? Cores, int? LogicalProcessors, int? L2CacheKb, int? L3CacheKb, bool? VirtualizationEnabled)
        TryGetCpuStaticInfo()
    {
        double? baseSpeed = null;
        int cores = 0;
        int logical = 0;
        int? l2Cache = null;
        int? l3Cache = null;
        bool? virtualization = null;
        var socketSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT MaxClockSpeed, NumberOfCores, NumberOfLogicalProcessors, L2CacheSize, L3CacheSize, SocketDesignation, VirtualizationFirmwareEnabled FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["MaxClockSpeed"] != null)
                {
                    var speed = Convert.ToDouble(obj["MaxClockSpeed"]);
                    if (speed > 0 && (!baseSpeed.HasValue || speed > baseSpeed.Value))
                    {
                        baseSpeed = speed;
                    }
                }

                cores += TryReadInt(obj, "NumberOfCores") ?? 0;
                logical += TryReadInt(obj, "NumberOfLogicalProcessors") ?? 0;

                var socket = obj["SocketDesignation"]?.ToString();
                if (!string.IsNullOrWhiteSpace(socket))
                {
                    socketSet.Add(socket);
                }

                if (obj["L2CacheSize"] != null)
                {
                    l2Cache ??= Convert.ToInt32(obj["L2CacheSize"]);
                }

                if (obj["L3CacheSize"] != null)
                {
                    l3Cache ??= Convert.ToInt32(obj["L3CacheSize"]);
                }

                if (obj["VirtualizationFirmwareEnabled"] != null)
                {
                    virtualization ??= Convert.ToBoolean(obj["VirtualizationFirmwareEnabled"]);
                }
            }
        }
        catch
        {
        }

        if (!virtualization.HasValue)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT HypervisorPresent FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["HypervisorPresent"] != null)
                    {
                        virtualization = Convert.ToBoolean(obj["HypervisorPresent"]);
                        break;
                    }
                }
            }
            catch
            {
            }
        }

        return (baseSpeed, socketSet.Count > 0 ? socketSet.Count : null, cores > 0 ? cores : null, logical > 0 ? logical : null, l2Cache, l3Cache, virtualization);
    }

    private static (double? SpeedMhz, int? SlotsUsed, int? SlotsTotal, string? FormFactor, double? HardwareReservedMb)
        TryGetMemoryStaticInfo()
    {
        double? speedMhz = null;
        int slotsUsed = 0;
        int? slotsTotal = null;
        string? formFactor = null;
        double? hardwareReserved = null;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Speed, ConfiguredClockSpeed, FormFactor FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in searcher.Get())
            {
                slotsUsed++;

                var configured = TryReadInt(obj, "ConfiguredClockSpeed");
                var speed = TryReadInt(obj, "Speed");
                var candidate = configured ?? speed;
                if (candidate.HasValue && candidate.Value > 0)
                {
                    if (!speedMhz.HasValue || candidate.Value > speedMhz.Value)
                    {
                        speedMhz = candidate.Value;
                    }
                }

                if (formFactor == null && obj["FormFactor"] != null)
                {
                    formFactor = MapMemoryFormFactor(Convert.ToInt32(obj["FormFactor"]));
                }
            }
        }
        catch
        {
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray");
            foreach (ManagementObject obj in searcher.Get())
            {
                slotsTotal = TryReadInt(obj, "MemoryDevices");
                if (slotsTotal.HasValue)
                {
                    break;
                }
            }
        }
        catch
        {
        }

        try
        {
            using var csSearcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            using var osSearcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            double? totalBytes = null;
            double? visibleBytes = null;

            foreach (ManagementObject obj in csSearcher.Get())
            {
                if (obj["TotalPhysicalMemory"] != null)
                {
                    totalBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                    break;
                }
            }

            foreach (ManagementObject obj in osSearcher.Get())
            {
                if (obj["TotalVisibleMemorySize"] != null)
                {
                    visibleBytes = Convert.ToDouble(obj["TotalVisibleMemorySize"]) * 1024.0;
                    break;
                }
            }

            if (totalBytes.HasValue && visibleBytes.HasValue && totalBytes.Value > visibleBytes.Value)
            {
                hardwareReserved = (totalBytes.Value - visibleBytes.Value) / (1024 * 1024);
            }
        }
        catch
        {
        }

        return (speedMhz, slotsUsed > 0 ? slotsUsed : null, slotsTotal, formFactor, hardwareReserved);
    }

    private static string? TryGetSystemDriveLetter()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SystemDrive FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var systemDrive = obj["SystemDrive"]?.ToString();
                if (!string.IsNullOrWhiteSpace(systemDrive))
                {
                    return systemDrive.Trim();
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static HashSet<string> TryGetPageFileDriveLetters()
    {
        var drives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PageFileUsage");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var drive = System.IO.Path.GetPathRoot(name)?.TrimEnd('\\');
                if (!string.IsNullOrWhiteSpace(drive))
                {
                    drives.Add(drive);
                }
            }
        }
        catch
        {
        }

        return drives;
    }

    private static Dictionary<string, DiskPerfData> TryGetLogicalDiskPerf()
    {
        var results = new Dictionary<string, DiskPerfData>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, PercentDiskTime, DiskReadBytesPerSec, DiskWriteBytesPerSec, AvgDiskSecPerTransfer, CurrentDiskQueueLength FROM Win32_PerfFormattedData_PerfDisk_LogicalDisk");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name) || name.Equals("_Total", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var active = TryReadDouble(obj, "PercentDiskTime");
                var readBytes = TryReadDouble(obj, "DiskReadBytesPerSec");
                var writeBytes = TryReadDouble(obj, "DiskWriteBytesPerSec");
                var responseSec = TryReadDouble(obj, "AvgDiskSecPerTransfer");
                var queue = TryReadDouble(obj, "CurrentDiskQueueLength");

                results[name] = new DiskPerfData(
                    active,
                    readBytes.HasValue ? readBytes.Value / (1024 * 1024) : null,
                    writeBytes.HasValue ? writeBytes.Value / (1024 * 1024) : null,
                    responseSec.HasValue ? responseSec.Value * 1000 : null,
                    queue);
            }
        }
        catch
        {
        }

        return results;
    }

    private static Dictionary<string, int?> TryGetDiskIndexByDrive()
    {
        var map = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var driveSearcher = new ManagementObjectSearcher("SELECT DeviceID, Index FROM Win32_DiskDrive");
            foreach (ManagementObject drive in driveSearcher.Get())
            {
                var index = TryReadInt(drive, "Index");
                var deviceId = drive["DeviceID"]?.ToString();
                if (!index.HasValue || string.IsNullOrWhiteSpace(deviceId))
                {
                    continue;
                }

                var escapedDeviceId = deviceId.Replace("\\", "\\\\");
                using var partitionSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{escapedDeviceId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    var partitionId = partition["DeviceID"]?.ToString();
                    if (string.IsNullOrWhiteSpace(partitionId))
                    {
                        continue;
                    }

                    var escapedPartitionId = partitionId.Replace("\\", "\\\\");
                    using var logicalSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{escapedPartitionId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                    foreach (ManagementObject logical in logicalSearcher.Get())
                    {
                        var name = logical["Name"]?.ToString();
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        map[name] = index;
                    }
                }
            }
        }
        catch
        {
        }

        return map;
    }

    private static string? MapMemoryFormFactor(int formFactor)
    {
        return formFactor switch
        {
            8 => "DIMM",
            12 => "SODIMM",
            13 => "SRIMM",
            14 => "SMD",
            16 => "SLIMM",
            17 => "MicroDIMM",
            _ => null
        };
    }

    [SupportedOSPlatform("windows")]
    private static double? TryGetStorageReliabilityHealth()
    {
        try
        {
            var scope = TryConnectStorageScope();
            if (scope == null)
            {
                return null;
            }

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT PercentLifeUsed, RemainingLife, Wear FROM MSFT_StorageReliabilityCounter"));

            var values = new List<double>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var percentUsed = NormalizePercent(TryReadDouble(obj, "PercentLifeUsed"), invert: true);
                var remainingLife = NormalizePercent(TryReadDouble(obj, "RemainingLife"), invert: false);
                var wear = NormalizePercent(TryReadDouble(obj, "Wear"), invert: true);

                if (percentUsed.HasValue)
                {
                    values.Add(percentUsed.Value);
                }
                else if (remainingLife.HasValue)
                {
                    values.Add(remainingLife.Value);
                }
                else if (wear.HasValue)
                {
                    values.Add(wear.Value);
                }
            }

            return values.Count > 0 ? values.Min() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read storage reliability counters: {ex.Message}");
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool? TryGetStorageHealthStatus()
    {
        try
        {
            var scope = TryConnectStorageScope();
            if (scope == null)
            {
                return null;
            }

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT HealthStatus FROM MSFT_PhysicalDisk"));

            bool? anyHealthy = null;
            foreach (ManagementObject obj in searcher.Get())
            {
                var healthStatus = TryReadInt(obj, "HealthStatus");
                if (!healthStatus.HasValue)
                {
                    continue;
                }

                switch (healthStatus.Value)
                {
                    case 1:
                        anyHealthy = anyHealthy ?? true;
                        break;
                    case 2:
                    case 3:
                        return true;
                }
            }

            if (anyHealthy.HasValue)
            {
                return false;
            }

            using var diskSearcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT HealthStatus FROM MSFT_Disk"));
            foreach (ManagementObject obj in diskSearcher.Get())
            {
                var diskHealth = TryReadInt(obj, "HealthStatus");
                if (!diskHealth.HasValue)
                {
                    continue;
                }

                switch (diskHealth.Value)
                {
                    case 1:
                        anyHealthy = anyHealthy ?? true;
                        break;
                    case 2:
                    case 3:
                        return true;
                }
            }

            return anyHealthy.HasValue ? false : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read physical disk health status: {ex.Message}");
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool? TryGetSmartPredictFailure()
    {
        try
        {
            var scope = new ManagementScope(@"\\.\root\WMI", new ConnectionOptions
            {
                EnablePrivileges = true,
                Impersonation = ImpersonationLevel.Impersonate
            });
            scope.Connect();

            using var searcher = new ManagementObjectSearcher(
                scope,
                new ObjectQuery("SELECT PredictFailure FROM MSStorageDriver_FailurePredictStatus"));

            bool? anyHealthy = null;
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["PredictFailure"] is not bool predicted)
                {
                    continue;
                }

                if (predicted)
                {
                    return true;
                }

                anyHealthy = anyHealthy ?? true;
            }

            return anyHealthy.HasValue ? false : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read disk SMART status: {ex.Message}");
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool? TryGetDiskStatusFromWin32DiskDrive()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Status, StatusInfo FROM Win32_DiskDrive");
            bool? anyHealthy = null;

            foreach (ManagementObject obj in searcher.Get())
            {
                var status = obj["Status"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status.Equals("OK", StringComparison.OrdinalIgnoreCase))
                    {
                        anyHealthy = anyHealthy ?? true;
                        continue;
                    }

                    if (status.IndexOf("Pred", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        status.IndexOf("Degrad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        status.IndexOf("Fail", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                var statusInfo = TryReadInt(obj, "StatusInfo");
                if (statusInfo.HasValue)
                {
                    switch (statusInfo.Value)
                    {
                        case 3: // enabled
                            anyHealthy = anyHealthy ?? true;
                            break;
                        case 4: // disabled
                        case 5: // not applicable
                        case 6: // unknown
                            break;
                        default:
                            return true;
                    }
                }
            }

            return anyHealthy.HasValue ? false : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read disk status from Win32_DiskDrive: {ex.Message}");
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static double? TryGetCpuTemperatureFromWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            var values = new List<double>();

            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentTemperature"] == null)
                {
                    continue;
                }

                var raw = Convert.ToDouble(obj["CurrentTemperature"]);
                var celsius = (raw / 10.0) - 273.15;
                if (celsius > 0 && celsius < 150)
                {
                    values.Add(celsius);
                }
            }

            return values.Count > 0 ? values.Max() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read CPU temperature from WMI: {ex.Message}");
            return null;
        }
    }

    private static double? NormalizePercent(double? value, bool invert)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var raw = value.Value;
        if (double.IsNaN(raw) || double.IsInfinity(raw) || raw < 0)
        {
            return null;
        }

        if (raw > 1000)
        {
            return null;
        }

        var normalized = invert ? 100.0 - raw : raw;
        if (normalized < 0) normalized = 0;
        if (normalized > 100) normalized = 100;
        return normalized;
    }

    private static double? TryReadDouble(ManagementBaseObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null)
            {
                return null;
            }

            return value switch
            {
                double d => d,
                float f => f,
                decimal m => (double)m,
                ushort us => us,
                short s => s,
                uint ui => ui,
                int i => i,
                long l => l,
                _ => double.TryParse(value.ToString(), out var parsed) ? parsed : null
            };
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? TryReadWmiDateTime(object? value)
    {
        if (value == null)
        {
            return null;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(value.ToString());
        }
        catch
        {
            return null;
        }
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

            return value switch
            {
                int i => i,
                uint ui => (int)ui,
                ushort us => us,
                short s => s,
                byte b => b,
                long l => (int)l,
                _ => int.TryParse(value.ToString(), out var parsed) ? parsed : null
            };
        }
        catch
        {
            return null;
        }
    }

    private static double? NormalizeMemoryMb(double? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return null;
        }

        if (value.Value < 1024 * 1024)
        {
            return value.Value;
        }

        return value.Value / (1024 * 1024);
    }

    [SupportedOSPlatform("windows")]
    private static ManagementScope? TryConnectStorageScope()
    {
        var options = new ConnectionOptions
        {
            EnablePrivileges = true,
            Impersonation = ImpersonationLevel.Impersonate
        };

        foreach (var path in new[] { @"\\.\root\Microsoft\Windows\Storage", @"root\Microsoft\Windows\Storage" })
        {
            try
            {
                var scope = new ManagementScope(path, options);
                scope.Connect();
                return scope;
            }
            catch
            {
                // Try next candidate.
            }
        }

        return null;
    }

    private sealed record DiskIdentity(
        int? Index,
        string? Model,
        string? FriendlyName,
        string? MediaTypeLabel,
        string? DeviceId,
        string? PnpDeviceId,
        string? InterfaceType);

    private sealed record DiskPerfData(
        double? ActiveTimePercent,
        double? ReadMbps,
        double? WriteMbps,
        double? AvgResponseMs,
        double? QueueLength);

    private sealed record SmartPredictEntry(string? InstanceName, bool PredictFailure);

    public void Dispose()
    {
        if (!_disposed)
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _diskReadCounter?.Dispose();
            _diskWriteCounter?.Dispose();
            _processCounter?.Dispose();
            _threadCounter?.Dispose();
            _handleCounter?.Dispose();
            _committedBytesCounter?.Dispose();
            _commitLimitCounter?.Dispose();
            _cacheBytesCounter?.Dispose();
            _pagedPoolBytesCounter?.Dispose();
            _nonPagedPoolBytesCounter?.Dispose();
            _computer?.Close();
            _disposed = true;
        }
    }

    private static double? TryGetGpuMemoryTotalMb()
    {
        try
        {
            double? best = null;
            using var searcher = new ManagementObjectSearcher("SELECT AdapterRAM FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["AdapterRAM"] == null)
                {
                    continue;
                }

                var bytes = Convert.ToDouble(obj["AdapterRAM"]);
                if (bytes <= 0)
                {
                    continue;
                }

                var mb = bytes / (1024 * 1024);
                if (!best.HasValue || mb > best.Value)
                {
                    best = mb;
                }
            }

            return best;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read GPU memory total: {ex.Message}");
            return null;
        }
    }

    private static string? TryGetDirectXVersion()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX");
            return key?.GetValue("Version")?.ToString();
        }
        catch
        {
            return null;
        }
    }
}

public sealed class SystemInfo
{
    public string OsName { get; set; } = "Unknown";
    public string OsVersion { get; set; } = "Unknown";
    public string CpuName { get; set; } = "Unknown";
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public string GpuName { get; set; } = "Unknown";
    public double TotalRamGb { get; set; }
    public TimeSpan Uptime { get; set; }

    public string UptimeFormatted => $"{(int)Uptime.TotalDays}d {Uptime.Hours}h {Uptime.Minutes}m";
}

public readonly record struct GpuMemorySnapshot(double UsedMb, double TotalMb, double UsagePercent, bool IsAvailable);

public readonly record struct FanSpeedSnapshot(double CpuRpm, double GpuRpm);

public readonly record struct DiskHealthSnapshot(double? HealthPercent, bool? PredictFailure);

public sealed record DiskHealthInfo(
    int? DiskIndex,
    string Name,
    string? MediaType,
    double? HealthPercent,
    bool? PredictFailure,
    string? Source);
