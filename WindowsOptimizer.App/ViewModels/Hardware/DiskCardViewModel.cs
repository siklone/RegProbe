using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WindowsOptimizer.Infrastructure.Hardware;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.ViewModels.Hardware;

/// <summary>
/// ViewModel for the Disk hardware card.
/// </summary>
public class DiskCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus? _bus;
    private readonly HardwareSpecsService _specsService = new();
    private List<StorageDriveInfo> _disks = new();
    private double? _readBytesPerSec;
    private double? _writeBytesPerSec;

    public DiskCardViewModel(MetricDataBus? bus = null)
    {
        _bus = bus;

        Icon = "\uEDA2"; // MDL2: HardDrive
        Title = "Storage";
        IconBackground = new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Orange
        PrimaryUnit = "";

        if (_bus != null)
        {
            _bus.MetricsUpdated += OnMetricsUpdated;
        }

        Task.Run(LoadSpecsAsync);
    }

    public List<StorageDriveInfo> Disks => _disks;

    private async Task LoadSpecsAsync()
    {
        try
        {
            var disks = new List<StorageDriveInfo>();

            await Task.Run(() =>
            {
                // Get physical disks
                try
                {
                    using var diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                    foreach (var disk in diskSearcher.Get())
                    {
                        var info = new StorageDriveInfo
                        {
                            Model = disk["Model"]?.ToString() ?? "Unknown",
                            SerialNumber = disk["SerialNumber"]?.ToString()?.Trim(),
                            InterfaceType = disk["InterfaceType"]?.ToString() ?? "Unknown",
                            MediaType = disk["MediaType"]?.ToString() ?? "Unknown",
                            SizeBytes = Convert.ToInt64(disk["Size"]),
                            Partitions = Convert.ToInt32(disk["Partitions"]),
                            DeviceId = disk["DeviceID"]?.ToString() ?? ""
                        };

                        // Determine if SSD or HDD
                        var mediaType = info.MediaType.ToLowerInvariant();
                        var model = info.Model.ToLowerInvariant();
                        info.IsSsd = mediaType.Contains("ssd") ||
                                     mediaType.Contains("solid") ||
                                     model.Contains("ssd") ||
                                     model.Contains("nvme") ||
                                     info.InterfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase);

                        disks.Add(info);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DiskCardViewModel] WMI DiskDrive failed: {ex.Message}");
                }

                // Get logical drives for space info
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        var logicalDisk = new LogicalStorageDriveInfo
                        {
                            DriveLetter = drive.Name.TrimEnd('\\'),
                            VolumeLabel = drive.VolumeLabel,
                            FileSystem = drive.DriveFormat,
                            TotalBytes = drive.TotalSize,
                            FreeBytes = drive.AvailableFreeSpace,
                            UsedBytes = drive.TotalSize - drive.AvailableFreeSpace
                        };

                        // Associate with physical disk (simplified - first disk gets all)
                        if (disks.Count > 0)
                        {
                            disks[0].LogicalDisks.Add(logicalDisk);
                        }
                    }
                }
            });

            await PopulateStorageSpecsAsync(disks);
            _disks = disks;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Calculate totals
                long totalSize = disks.Sum(d => d.SizeBytes);
                long totalUsed = disks.SelectMany(d => d.LogicalDisks).Sum(ld => ld.UsedBytes);
                long totalFree = disks.SelectMany(d => d.LogicalDisks).Sum(ld => ld.FreeBytes);

                if (disks.Count == 1)
                {
                    Subtitle = disks[0].Model;
                }
                else
                {
                    Subtitle = $"{disks.Count} drives";
                }

                // Primary value shows usage
                double usagePercent = totalSize > 0 ? (double)totalUsed / (totalUsed + totalFree) * 100 : 0;
                PrimaryValue = $"{usagePercent:F0}%";
                PrimaryUnit = " used";
                PrimaryValueColor = GetPercentageColor(usagePercent);

                // Secondary metrics
                SecondaryMetrics.Clear();
                SecondaryMetrics.Add(new MetricItem("Total", FormatSize(totalSize), ""));
                SecondaryMetrics.Add(new MetricItem("Used", FormatSize(totalUsed), ""));
                SecondaryMetrics.Add(new MetricItem("Free", FormatSize(totalFree), ""));

                if (disks.Count > 0)
                {
                    var types = disks.Select(d => d.Specs?.Type ?? (d.IsSsd ? "SSD" : "HDD")).Distinct();
                    SecondaryMetrics.Add(new MetricItem("Type", string.Join("/", types), ""));
                }

                var ratedRead = disks.Select(d => d.Specs?.SeqReadMbps ?? 0).Max();
                if (ratedRead > 0)
                {
                    SecondaryMetrics.Add(new MetricItem("Rated Read", ratedRead.ToString("F0"), "MB/s"));
                }

                var ratedWrite = disks.Select(d => d.Specs?.SeqWriteMbps ?? 0).Max();
                if (ratedWrite > 0)
                {
                    SecondaryMetrics.Add(new MetricItem("Rated Write", ratedWrite.ToString("F0"), "MB/s"));
                }

                HasSpecs = disks.Any(d => d.Specs?.IsFromDatabase == true);

                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DiskCardViewModel] Failed to load specs: {ex.Message}");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = "Disk Info Unavailable";
                IsLoading = false;
            });
        }
    }

    private async Task PopulateStorageSpecsAsync(List<StorageDriveInfo> disks)
    {
        var specsByModel = new Dictionary<string, StorageSpecs>(StringComparer.OrdinalIgnoreCase);
        foreach (var disk in disks)
        {
            var model = disk.Model ?? string.Empty;
            if (string.IsNullOrWhiteSpace(model))
            {
                continue;
            }

            if (!specsByModel.TryGetValue(model, out var specs))
            {
                specs = await _specsService.GetStorageSpecsAsync(model, CancellationToken.None);
                specsByModel[model] = specs;
            }

            disk.Specs = specs;
        }
    }

    private void OnMetricsUpdated(object? sender, MetricBatchEventArgs e)
    {
        if (e.TryGetValue<double>("disk.read.speed", out var readSpeed))
        {
            _readBytesPerSec = readSpeed;
            UpdateSecondaryMetric("Read", FormatSpeed(readSpeed), "/s");
            UpdateLiveSummary();
        }

        if (e.TryGetValue<double>("disk.write.speed", out var writeSpeed))
        {
            _writeBytesPerSec = writeSpeed;
            UpdateSecondaryMetric("Write", FormatSpeed(writeSpeed), "/s");
            UpdateLiveSummary();
        }

        if (e.TryGetValue<double>("disk.health", out var health))
        {
            StatusColor = health >= 90 ? Brushes.LimeGreen :
                          health >= 70 ? Brushes.Yellow :
                          health >= 50 ? Brushes.Orange : Brushes.Red;
        }
    }

    private void UpdateLiveSummary()
    {
        var parts = new List<string>();
        if (_readBytesPerSec.HasValue)
        {
            parts.Add($"Read {FormatSpeed(_readBytesPerSec.Value)}/s");
        }

        if (_writeBytesPerSec.HasValue)
        {
            parts.Add($"Write {FormatSpeed(_writeBytesPerSec.Value)}/s");
        }

        LiveSummary = parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
    }

    private static string FormatSize(long bytes)
    {
        const long kilo = 1024;
        const long mega = kilo * 1024;
        const long giga = mega * 1024;
        const long tera = giga * 1024;

        if (bytes >= tera)
            return $"{bytes / (double)tera:F1} TB";
        if (bytes >= giga)
            return $"{bytes / (double)giga:F0} GB";
        if (bytes >= mega)
            return $"{bytes / (double)mega:F0} MB";
        return $"{bytes / (double)kilo:F0} KB";
    }

    private static string FormatSpeed(double bytesPerSec)
    {
        const double kilo = 1024.0;
        const double mega = kilo * 1024.0;
        const double giga = mega * 1024.0;

        if (bytesPerSec >= giga)
            return $"{bytesPerSec / giga:F1} GB";
        if (bytesPerSec >= mega)
            return $"{bytesPerSec / mega:F0} MB";
        if (bytesPerSec >= kilo)
            return $"{bytesPerSec / kilo:F0} KB";
        return $"{bytesPerSec:F0} B";
    }

    public override void Dispose()
    {
        if (_bus != null)
        {
            _bus.MetricsUpdated -= OnMetricsUpdated;
        }
        base.Dispose();
    }
}

public class StorageDriveInfo
{
    public string Model { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string InterfaceType { get; set; } = "";
    public string MediaType { get; set; } = "";
    public long SizeBytes { get; set; }
    public int Partitions { get; set; }
    public string DeviceId { get; set; } = "";
    public bool IsSsd { get; set; }
    public StorageSpecs? Specs { get; set; }
    public List<LogicalStorageDriveInfo> LogicalDisks { get; } = new();

    public double SizeGB => SizeBytes / (1024.0 * 1024 * 1024);
}

public class LogicalStorageDriveInfo
{
    public string DriveLetter { get; set; } = "";
    public string? VolumeLabel { get; set; }
    public string FileSystem { get; set; } = "";
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }
    public long UsedBytes { get; set; }

    public double UsagePercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100 : 0;
    public double TotalGB => TotalBytes / (1024.0 * 1024 * 1024);
    public double FreeGB => FreeBytes / (1024.0 * 1024 * 1024);
    public double UsedGB => UsedBytes / (1024.0 * 1024 * 1024);
}
