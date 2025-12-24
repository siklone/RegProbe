using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WindowsOptimizer.Infrastructure.Metrics;

public sealed class DiskMonitor
{
    private readonly Dictionary<string, (PerformanceCounter Read, PerformanceCounter Write)> _counters = new();

    public List<DiskInfo> GetDiskActivity()
    {
        var disks = new List<DiskInfo>();

        // Get all available PhysicalDisk instances
        PerformanceCounterCategory category;
        string[] instanceNames;

        try
        {
            category = new PerformanceCounterCategory("PhysicalDisk");
            instanceNames = category.GetInstanceNames();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get PhysicalDisk instances: {ex.Message}");
            return disks; // Return empty list if category is not available
        }

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                continue;

            var driveName = drive.Name.TrimEnd('\\'); // "C:"

            // Try multiple possible instance name formats
            var possibleInstanceNames = new[]
            {
                $"{drive.Name[0]}:",                    // "C:"
                $"{drive.Name[0]} {drive.Name[1]}",     // "C :"
                drive.Name.TrimEnd('\\'),               // "C:"
                "0",                                     // First physical disk
                "_Total"                                 // Total for all disks
            };

            // Find the first matching instance name
            string? matchingInstance = null;
            foreach (var possible in possibleInstanceNames)
            {
                if (instanceNames.Contains(possible))
                {
                    matchingInstance = possible;
                    break;
                }
            }

            // If no match found, skip this drive
            if (matchingInstance == null)
            {
                Debug.WriteLine($"No PhysicalDisk instance found for drive {driveName}");

                // Add drive info without I/O stats
                disks.Add(new DiskInfo
                {
                    DriveLetter = driveName,
                    TotalSizeGb = drive.TotalSize / (1024.0 * 1024 * 1024),
                    FreeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                    ReadBytesPerSec = 0,
                    WriteBytesPerSec = 0
                });
                continue;
            }

            if (!_counters.ContainsKey(driveName))
            {
                try
                {
                    _counters[driveName] = (
                        new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", matchingInstance),
                        new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", matchingInstance)
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create performance counters for {driveName}: {ex.Message}");

                    // Add drive info without I/O stats
                    disks.Add(new DiskInfo
                    {
                        DriveLetter = driveName,
                        TotalSizeGb = drive.TotalSize / (1024.0 * 1024 * 1024),
                        FreeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                        ReadBytesPerSec = 0,
                        WriteBytesPerSec = 0
                    });
                    continue;
                }
            }

            // Only add disk if counters were successfully created
            if (!_counters.ContainsKey(driveName))
                continue;

            var (readCounter, writeCounter) = _counters[driveName];

            float readRate = 0;
            float writeRate = 0;

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

            disks.Add(new DiskInfo
            {
                DriveLetter = driveName,
                TotalSizeGb = drive.TotalSize / (1024.0 * 1024 * 1024),
                FreeSpaceGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024),
                ReadBytesPerSec = readRate,
                WriteBytesPerSec = writeRate
            });
        }

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
}

public sealed class DiskInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public double TotalSizeGb { get; set; }
    public double FreeSpaceGb { get; set; }
    public float ReadBytesPerSec { get; set; }
    public float WriteBytesPerSec { get; set; }

    public double UsedSpaceGb => TotalSizeGb - FreeSpaceGb;
    public double UsedPercent => (UsedSpaceGb / TotalSizeGb) * 100.0;
    public double ReadMBps => ReadBytesPerSec / (1024 * 1024);
    public double WriteMBps => WriteBytesPerSec / (1024 * 1024);
}
