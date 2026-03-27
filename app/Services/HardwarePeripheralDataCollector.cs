using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using RegProbe.App.Diagnostics;

namespace RegProbe.App.Services;

internal static class HardwarePeripheralDataCollector
{
    public static StorageHardwareData LoadStorageData()
    {
        var data = new StorageHardwareData();
        var systemDrive = NormalizeDriveLetter(Environment.GetEnvironmentVariable("SystemDrive")) ?? "C:";

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Model, InterfaceType, SerialNumber, MediaType, FirmwareRevision, Size, Partitions, Index, Status, PNPDeviceID FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                data.Disks.Add(new DiskDriveData
                {
                    DeviceId = GetValueSafe(obj, "DeviceID"),
                    Model = GetValueSafe(obj, "Model"),
                    InterfaceType = GetValueSafe(obj, "InterfaceType"),
                    SerialNumber = GetValueSafe(obj, "SerialNumber")?.Trim(),
                    MediaType = GetValueSafe(obj, "MediaType"),
                    FirmwareRevision = GetValueSafe(obj, "FirmwareRevision"),
                    SizeBytes = GetLongSafe(obj, "Size"),
                    PartitionCount = GetIntSafe(obj, "Partitions"),
                    Index = GetIntSafe(obj, "Index"),
                    Status = GetValueSafe(obj, "Status"),
                    PnpDeviceId = GetValueSafe(obj, "PNPDeviceID")
                });
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] Storage read failed: {ex.Message}");
        }

        foreach (var disk in data.Disks)
        {
            if (string.IsNullOrWhiteSpace(disk.MediaType))
            {
                disk.MediaType = InferMediaType(disk.Model, disk.InterfaceType);
            }

            disk.IsExternal = IsExternalDisk(disk.InterfaceType, disk.MediaType);
            disk.InterfaceSummary = HardwarePresentationFormatter.BuildStorageInterfaceSummary(
                disk.InterfaceType,
                disk.MediaType,
                disk.IsExternal);

            if (!string.IsNullOrWhiteSpace(disk.DeviceId))
            {
                disk.Volumes = GetVolumesForDisk(disk.DeviceId!, systemDrive);
                disk.LogicalDrives = BuildLogicalDriveSummary(disk.Volumes);
            }

            disk.VolumeCount = disk.Volumes.Count;
            disk.FreeBytes = disk.Volumes.Sum(static volume => Math.Max(0L, volume.FreeBytes));
            disk.IsSystemDisk = disk.Volumes.Any(volume => volume.IsSystem || volume.IsBoot);

            data.TotalFreeBytes += disk.FreeBytes;
            data.VolumeCount += disk.VolumeCount;
            if (disk.IsExternal)
            {
                data.ExternalDriveCount++;
            }

            if (disk.IsSystemDisk)
            {
                data.SystemDriveCount++;
            }
        }

        data.Disks = data.Disks
            .OrderByDescending(static disk => disk.IsSystemDisk)
            .ThenByDescending(static disk => disk.SizeBytes)
            .ThenBy(static disk => disk.Index)
            .ToList();
        data.DeviceCount = data.Disks.Count;
        data.TotalSizeBytes = data.Disks.Sum(static disk => disk.SizeBytes);
        return data;
    }

    public static UsbHardwareData LoadUsbData()
    {
        var data = new UsbHardwareData();
        var devices = new Dictionary<string, UsbDeviceData>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var drive in System.IO.DriveInfo.GetDrives())
            {
                if (drive.DriveType == System.IO.DriveType.Removable)
                {
                    data.RemovableDriveCount++;
                }
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] Removable drive scan failed: {ex.Message}");
        }

        try
        {
            using var controllerSearcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer, Status, DeviceID, PNPDeviceID, Description FROM Win32_USBController");
            foreach (ManagementObject obj in controllerSearcher.Get())
            {
                UpsertUsbDevice(devices, new UsbDeviceData
                {
                    Name = GetValueSafe(obj, "Name"),
                    DeviceId = GetValueSafe(obj, "DeviceID"),
                    PnpDeviceId = GetValueSafe(obj, "PNPDeviceID"),
                    Manufacturer = GetValueSafe(obj, "Manufacturer"),
                    Status = GetValueSafe(obj, "Status"),
                    Description = GetValueSafe(obj, "Description"),
                    IsController = true
                });
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] USB controller read failed: {ex.Message}");
        }

        try
        {
            using var hubSearcher = new ManagementObjectSearcher(
                "SELECT Name, DeviceID, PNPDeviceID, Status, Description FROM Win32_USBHub");
            foreach (ManagementObject obj in hubSearcher.Get())
            {
                var deviceId = FirstNotEmpty(GetValueSafe(obj, "PNPDeviceID"), GetValueSafe(obj, "DeviceID"));
                UpsertUsbDevice(devices, new UsbDeviceData
                {
                    Name = GetValueSafe(obj, "Name"),
                    DeviceId = GetValueSafe(obj, "DeviceID"),
                    PnpDeviceId = GetValueSafe(obj, "PNPDeviceID"),
                    Status = GetValueSafe(obj, "Status"),
                    Description = GetValueSafe(obj, "Description"),
                    IsHub = true,
                    Category = "Hub",
                    VendorId = ParseUsbIdPart(deviceId, "VID"),
                    ProductId = ParseUsbIdPart(deviceId, "PID")
                });
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] USB hub read failed: {ex.Message}");
        }

        try
        {
            using var pnpSearcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer, Status, DeviceID, PNPClass, Service, Description FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");
            foreach (ManagementObject obj in pnpSearcher.Get())
            {
                var deviceId = GetValueSafe(obj, "DeviceID");
                var name = GetValueSafe(obj, "Name");
                var className = GetValueSafe(obj, "PNPClass");
                var service = GetValueSafe(obj, "Service");
                var isHub = ContainsAny(name, "hub", "root hub");
                UpsertUsbDevice(devices, new UsbDeviceData
                {
                    Name = name,
                    DeviceId = deviceId,
                    PnpDeviceId = deviceId,
                    Manufacturer = GetValueSafe(obj, "Manufacturer"),
                    Status = GetValueSafe(obj, "Status"),
                    Description = GetValueSafe(obj, "Description"),
                    ClassName = className,
                    Service = service,
                    IsHub = isHub,
                    VendorId = ParseUsbIdPart(deviceId, "VID"),
                    ProductId = ParseUsbIdPart(deviceId, "PID")
                });
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] USB PnP read failed: {ex.Message}");
        }

        data.Devices = devices.Values
            .Where(static device => !string.IsNullOrWhiteSpace(device.Name))
            .OrderByDescending(static device => device.IsController)
            .ThenByDescending(static device => device.IsHub)
            .ThenBy(static device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        data.UsbControllerCount = data.Devices.Count(static device => device.IsController);
        data.HubCount = data.Devices.Count(static device => device.IsHub);
        data.UsbDeviceCount = data.Devices.Count(static device => !device.IsController);
        data.InputDeviceCount = data.Devices.Count(static device => string.Equals(device.Category, "Input", StringComparison.OrdinalIgnoreCase));
        data.AudioDeviceCount = data.Devices.Count(static device => string.Equals(device.Category, "Audio", StringComparison.OrdinalIgnoreCase));
        data.StorageDeviceCount = data.Devices.Count(static device => string.Equals(device.Category, "Storage", StringComparison.OrdinalIgnoreCase));
        data.PrimaryControllerName = data.Devices.FirstOrDefault(static device => device.IsController)?.Name;

        var primaryDevice = data.Devices
            .Where(static device => !device.IsController)
            .OrderByDescending(ScoreUsbDevice)
            .FirstOrDefault();
        if (primaryDevice != null)
        {
            data.PrimaryUsbDeviceName = primaryDevice.Name;
            data.PrimaryVendorId = primaryDevice.VendorId;
            data.PrimaryProductId = primaryDevice.ProductId;
            data.PrimaryStatus = primaryDevice.Status;
            data.PrimaryDeviceId = primaryDevice.DeviceId;
            data.PrimaryManufacturer = primaryDevice.Manufacturer;
            data.PrimaryCategory = primaryDevice.Category;
        }

        return data;
    }

    public static AudioHardwareData LoadAudioData()
    {
        var data = new AudioHardwareData();
        var driverMap = LoadAudioDriverMap();
        var bestScore = int.MinValue;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer, Status, DeviceID, PNPDeviceID FROM Win32_SoundDevice");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = GetValueSafe(obj, "Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var manufacturer = GetValueSafe(obj, "Manufacturer");
                var status = GetValueSafe(obj, "Status");
                var deviceId = FirstNotEmpty(GetValueSafe(obj, "DeviceID"), GetValueSafe(obj, "PNPDeviceID"));
                var normalizedId = NormalizeDeviceId(deviceId);
                driverMap.TryGetValue(normalizedId, out var driverInfo);
                if (string.IsNullOrWhiteSpace(normalizedId))
                {
                    driverInfo = default;
                }
                else if (driverInfo == default)
                {
                    driverInfo = driverMap
                        .FirstOrDefault(pair => pair.Key.Contains(normalizedId, StringComparison.OrdinalIgnoreCase) ||
                                                normalizedId.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                        .Value;
                }

                var isVirtual = AudioDetectionHelpers.IsLikelyVirtualDevice(name, manufacturer, deviceId);
                var audioDevice = new AudioDeviceData
                {
                    Name = name,
                    Manufacturer = manufacturer,
                    Status = status,
                    DeviceId = deviceId,
                    DriverProvider = driverInfo.Provider,
                    DriverVersion = driverInfo.Version,
                    DriverDate = driverInfo.Date,
                    InfName = driverInfo.InfName,
                    IsVirtual = isVirtual
                };

                data.DeviceCount++;
                if (isVirtual)
                {
                    data.VirtualDeviceCount++;
                }
                else
                {
                    data.PhysicalDeviceCount++;
                }

                data.AllDevices.Add(name);
                data.Devices.Add(audioDevice);

                var score = AudioDetectionHelpers.ScoreDevice(name, manufacturer, status, deviceId);
                if (!string.IsNullOrWhiteSpace(driverInfo.Version))
                {
                    score += 5;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    audioDevice.IsPrimary = true;
                    data.PrimaryDeviceName = name;
                    data.PrimaryManufacturer = manufacturer;
                    data.PrimaryStatus = status;
                    data.PrimaryDriverProvider = driverInfo.Provider;
                    data.PrimaryDriverVersion = driverInfo.Version;
                    data.PrimaryDriverDate = driverInfo.Date;
                }
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] Audio read failed: {ex.Message}");
        }

        data.Devices = data.Devices
            .OrderByDescending(static device => device.IsPrimary)
            .ThenBy(static device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return data;
    }

    private static List<StorageVolumeData> GetVolumesForDisk(string deviceId, string systemDrive)
    {
        var volumes = new List<StorageVolumeData>();
        try
        {
            var escapedDeviceId = deviceId.Replace("\\", "\\\\");
            using var partitionSearcher = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{escapedDeviceId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
            foreach (ManagementObject partition in partitionSearcher.Get())
            {
                var partitionType = GetValueSafe(partition, "Type");
                var isBoot = GetBoolSafe(partition, "BootPartition");
                var isPrimary = GetBoolSafe(partition, "PrimaryPartition");
                var partitionDeviceId = GetValueSafe(partition, "DeviceID");
                if (string.IsNullOrWhiteSpace(partitionDeviceId))
                {
                    continue;
                }

                using var logicalSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionDeviceId}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                foreach (ManagementObject logical in logicalSearcher.Get())
                {
                    var driveLetter = NormalizeDriveLetter(GetValueSafe(logical, "DeviceID"));
                    if (string.IsNullOrWhiteSpace(driveLetter))
                    {
                        continue;
                    }

                    volumes.Add(new StorageVolumeData
                    {
                        DriveLetter = driveLetter,
                        Label = GetValueSafe(logical, "VolumeName"),
                        FileSystem = GetValueSafe(logical, "FileSystem"),
                        SizeBytes = GetLongSafe(logical, "Size"),
                        FreeBytes = GetLongSafe(logical, "FreeSpace"),
                        PartitionType = partitionType,
                        IsBoot = isBoot,
                        IsPrimary = isPrimary,
                        IsSystem = string.Equals(driveLetter, systemDrive, StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] Disk volume mapping failed: {ex.Message}");
        }

        return volumes
            .OrderBy(static volume => volume.DriveLetter, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, AudioDriverInfo> LoadAudioDriverMap()
    {
        var map = new Dictionary<string, AudioDriverInfo>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, DriverProviderName, DriverVersion, DriverDate, InfName, DeviceClass FROM Win32_PnPSignedDriver WHERE DeviceClass='MEDIA'");
            foreach (ManagementObject obj in searcher.Get())
            {
                var deviceId = NormalizeDeviceId(GetValueSafe(obj, "DeviceID"));
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    continue;
                }

                map[deviceId] = new AudioDriverInfo(
                    GetValueSafe(obj, "DriverProviderName"),
                    GetValueSafe(obj, "DriverVersion"),
                    FormatDriverDate(GetValueSafe(obj, "DriverDate")),
                    GetValueSafe(obj, "InfName"));
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[HardwarePeripheralDataCollector] Audio driver read failed: {ex.Message}");
        }

        return map;
    }

    private static void UpsertUsbDevice(IDictionary<string, UsbDeviceData> devices, UsbDeviceData candidate)
    {
        var key = NormalizeUsbKey(candidate.DeviceId, candidate.PnpDeviceId, candidate.Name);
        candidate.Category = HardwarePresentationFormatter.ClassifyUsbDeviceCategory(
            candidate.Name,
            candidate.ClassName,
            candidate.Service,
            candidate.IsController,
            candidate.IsHub);

        if (!devices.TryGetValue(key, out var existing))
        {
            devices[key] = candidate;
            return;
        }

        existing.Name = MergeString(existing.Name, candidate.Name);
        existing.DeviceId = MergeString(existing.DeviceId, candidate.DeviceId);
        existing.PnpDeviceId = MergeString(existing.PnpDeviceId, candidate.PnpDeviceId);
        existing.Manufacturer = MergeString(existing.Manufacturer, candidate.Manufacturer);
        existing.Status = MergeStatus(existing.Status, candidate.Status);
        existing.Description = MergeString(existing.Description, candidate.Description);
        existing.ClassName = MergeString(existing.ClassName, candidate.ClassName);
        existing.Service = MergeString(existing.Service, candidate.Service);
        existing.VendorId = MergeString(existing.VendorId, candidate.VendorId);
        existing.ProductId = MergeString(existing.ProductId, candidate.ProductId);
        existing.IsController |= candidate.IsController;
        existing.IsHub |= candidate.IsHub;
        existing.Category = HardwarePresentationFormatter.ClassifyUsbDeviceCategory(
            existing.Name,
            existing.ClassName,
            existing.Service,
            existing.IsController,
            existing.IsHub);
    }

    private static int ScoreUsbDevice(UsbDeviceData device)
    {
        var score = 0;

        if (!device.IsController)
        {
            score += 80;
        }

        if (!device.IsHub)
        {
            score += 40;
        }

        if (string.Equals(device.Status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(device.Manufacturer) &&
            !device.Manufacturer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) &&
            !device.Manufacturer.Contains("Generic", StringComparison.OrdinalIgnoreCase))
        {
            score += 15;
        }

        score += device.Category switch
        {
            "Audio" => 25,
            "Storage" => 20,
            "Input" => 15,
            _ => 0
        };

        if (!string.IsNullOrWhiteSpace(device.Name) &&
            device.Name.Contains("Composite", StringComparison.OrdinalIgnoreCase))
        {
            score -= 10;
        }

        return score;
    }

    private static string? InferMediaType(string? model, string? interfaceType)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var normalizedModel = model.ToUpperInvariant();
        var normalizedInterface = (interfaceType ?? string.Empty).ToUpperInvariant();

        if (normalizedModel.Contains("NVME", StringComparison.Ordinal) ||
            normalizedModel.Contains("NVM EXPRESS", StringComparison.Ordinal))
        {
            return "NVMe SSD";
        }

        if (normalizedInterface == "SCSI" &&
            (normalizedModel.Contains("SSD", StringComparison.Ordinal) ||
             normalizedModel.Contains("SOLID STATE", StringComparison.Ordinal)))
        {
            return "NVMe SSD";
        }

        if (normalizedModel.Contains("SSD", StringComparison.Ordinal) ||
            normalizedModel.Contains("SOLID STATE", StringComparison.Ordinal))
        {
            return "SSD";
        }

        if (normalizedModel.Contains("MX500", StringComparison.Ordinal) ||
            normalizedModel.Contains("MX300", StringComparison.Ordinal) ||
            normalizedModel.Contains("860 EVO", StringComparison.Ordinal) ||
            normalizedModel.Contains("870 EVO", StringComparison.Ordinal) ||
            normalizedModel.Contains("970 EVO", StringComparison.Ordinal) ||
            normalizedModel.Contains("WD GREEN", StringComparison.Ordinal) ||
            normalizedModel.Contains("WD BLUE SSD", StringComparison.Ordinal) ||
            normalizedModel.Contains("KINGSTON", StringComparison.Ordinal))
        {
            return "SSD";
        }

        if (normalizedModel.Contains("HDD", StringComparison.Ordinal) ||
            normalizedModel.Contains("SEAGATE", StringComparison.Ordinal) ||
            normalizedModel.Contains("HITACHI", StringComparison.Ordinal) ||
            normalizedModel.Contains("TOSHIBA", StringComparison.Ordinal) ||
            normalizedModel.Contains("HGST", StringComparison.Ordinal) ||
            normalizedInterface == "IDE")
        {
            return "HDD";
        }

        return null;
    }

    private static bool IsExternalDisk(string? interfaceType, string? mediaType)
    {
        return ContainsAny(interfaceType, "USB") ||
               ContainsAny(mediaType, "Removable", "External");
    }

    private static string BuildLogicalDriveSummary(IEnumerable<StorageVolumeData> volumes)
    {
        var drives = volumes
            .Select(static volume => NormalizeDriveLetter(volume.DriveLetter))
            .Where(static driveLetter => !string.IsNullOrWhiteSpace(driveLetter))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return drives.Count > 0 ? string.Join(", ", drives) : "N/A";
    }

    private static string NormalizeUsbKey(string? deviceId, string? pnpDeviceId, string? name)
    {
        var seed = FirstNotEmpty(deviceId, pnpDeviceId, name);
        return string.IsNullOrWhiteSpace(seed)
            ? Guid.NewGuid().ToString("N")
            : seed.Trim().ToUpperInvariant();
    }

    private static string NormalizeDeviceId(string? deviceId)
    {
        return string.IsNullOrWhiteSpace(deviceId)
            ? string.Empty
            : Regex.Replace(deviceId.Trim(), @"\s+", string.Empty).ToUpperInvariant();
    }

    private static string? NormalizeDriveLetter(string? driveLetter)
    {
        if (string.IsNullOrWhiteSpace(driveLetter))
        {
            return null;
        }

        return driveLetter.Trim().TrimEnd('\\').ToUpperInvariant();
    }

    private static string? ParseUsbIdPart(string? deviceId, string key)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var match = Regex.Match(
            deviceId,
            $@"{key}_([0-9A-F]{{4}})",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static string? FormatDriverDate(string? rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
        {
            return null;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(rawDate).ToString("yyyy-MM-dd");
        }
        catch
        {
            return rawDate.Trim();
        }
    }

    private static string? MergeString(string? current, string? candidate)
    {
        return string.IsNullOrWhiteSpace(current) ? candidate : current;
    }

    private static string? MergeStatus(string? current, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return candidate;
        }

        if (string.Equals(current, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        return string.Equals(candidate, "OK", StringComparison.OrdinalIgnoreCase) ? candidate : current;
    }

    private static bool ContainsAny(string? value, params string[] terms)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
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

    private static bool GetBoolSafe(ManagementBaseObject obj, string propertyName)
    {
        try
        {
            return obj[propertyName] is bool value && value;
        }
        catch
        {
            return false;
        }
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private readonly record struct AudioDriverInfo(
        string? Provider,
        string? Version,
        string? Date,
        string? InfName);
}
