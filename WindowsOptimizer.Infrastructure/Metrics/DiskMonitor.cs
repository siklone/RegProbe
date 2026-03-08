using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class DiskMonitor : IDisposable
{
    private readonly Dictionary<string, (PerformanceCounter Read, PerformanceCounter Write)> _counters = new();

    public List<DiskInfo> GetDiskActivity()
    {
        var disks = new List<DiskInfo>();

        // Get per-volume disk activity. LogicalDisk instances map directly to drive letters (C:, D: ...),
        // unlike PhysicalDisk which uses "0 C:" style instance names.
        string[] instanceNames = Array.Empty<string>();
        var perfCountersAvailable = false;

        try
        {
            var category = new PerformanceCounterCategory("LogicalDisk");
            instanceNames = category.GetInstanceNames();
            perfCountersAvailable = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get LogicalDisk instances: {ex.Message}");
        }

        var activeDriveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            if (drive.DriveType == DriveType.CDRom || drive.DriveType == DriveType.NoRootDirectory)
            {
                continue;
            }

            var driveName = drive.Name.TrimEnd('\\'); // "C:"
            activeDriveNames.Add(driveName);

            if (perfCountersAvailable && !_counters.ContainsKey(driveName))
            {
                var instanceName = instanceNames.FirstOrDefault(name =>
                    name.Equals(driveName, StringComparison.OrdinalIgnoreCase));
                if (instanceName is null)
                {
                    Debug.WriteLine($"No LogicalDisk instance found for drive {driveName}");
                }
                else
                {
                    try
                    {
                        _counters[driveName] = (
                            new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", instanceName),
                            new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", instanceName)
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to create performance counters for {driveName}: {ex.Message}");
                    }
                }
            }

            float readRate = 0;
            float writeRate = 0;

            if (_counters.TryGetValue(driveName, out var counters))
            {
                var (readCounter, writeCounter) = counters;
                try
                {
                    readRate = readCounter.NextValue();
                    writeRate = writeCounter.NextValue();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read performance counters for {driveName}: {ex.Message}");
                    // Continue with 0 values instead of skipping
                }
            }

            disks.Add(new DiskInfo
            {
                DriveLetter = driveName,
                TotalSizeGb = drive.TotalSize / (1024.0 * 1024 * 1024),
                FreeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                ReadBytesPerSec = readRate,
                WriteBytesPerSec = writeRate
            });
        }

        CleanupInactive(activeDriveNames);
        return disks;
    }

    public void Dispose()
    {
        foreach (var (read, write) in _counters.Values)
        {
            read?.Dispose();
            write?.Dispose();
        }
    }

    private void CleanupInactive(HashSet<string> activeDriveNames)
    {
        foreach (var driveName in _counters.Keys.Where(name => !activeDriveNames.Contains(name)).ToList())
        {
            if (_counters.Remove(driveName, out var counters))
            {
                counters.Read.Dispose();
                counters.Write.Dispose();
            }
        }
    }
}

public sealed class DiskInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public int? DiskIndex { get; set; }
    public string? Model { get; set; }
    public string? MediaType { get; set; }
    public string? InterfaceType { get; set; }
    public string? BusType { get; set; }
    public bool? IsExternal { get; set; }
    public bool? IsSystemDisk { get; set; }
    public bool? HasPageFile { get; set; }
    public double TotalSizeGb { get; set; }
    public double FreeSpaceGb { get; set; }
    public float ReadBytesPerSec { get; set; }
    public float WriteBytesPerSec { get; set; }

    public double UsedSpaceGb => TotalSizeGb - FreeSpaceGb;
    public double UsedPercent => (UsedSpaceGb / TotalSizeGb) * 100.0;
    public double ReadMBps => ReadBytesPerSec / (1024 * 1024);
    public double WriteMBps => WriteBytesPerSec / (1024 * 1024);
    public string DescriptorText
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(MediaType))
            {
                parts.Add(MediaType);
            }

            if (!string.IsNullOrWhiteSpace(BusType))
            {
                parts.Add(BusType);
            }

            if (IsExternal == true)
            {
                parts.Add("External");
            }
            else if (IsExternal == false)
            {
                parts.Add("Internal");
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : "Unknown";
        }
    }
}
