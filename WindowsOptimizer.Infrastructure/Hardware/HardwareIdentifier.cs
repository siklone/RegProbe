using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WindowsOptimizer.Infrastructure.Hardware;

/// <summary>
/// Identifies hardware components using multiple methods.
/// </summary>
public static class HardwareIdentifier
{
    #region CPU Identification

    /// <summary>
    /// Gets CPU identification information.
    /// </summary>
    public static CpuIdentity GetCpuId()
    {
        var identity = new CpuIdentity();

        // 1. Try WMI (most reliable on Windows)
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                identity.WmiName = obj["Name"]?.ToString()?.Trim();
                identity.ProcessorId = obj["ProcessorId"]?.ToString();
                identity.Manufacturer = obj["Manufacturer"]?.ToString();
                identity.Cores = Convert.ToInt32(obj["NumberOfCores"]);
                identity.Threads = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                identity.MaxClockSpeed = Convert.ToInt32(obj["MaxClockSpeed"]);
                identity.Family = Convert.ToInt32(obj["Family"]);
                identity.Architecture = obj["Architecture"]?.ToString() switch
                {
                    "0" => "x86",
                    "9" => "x64",
                    "12" => "ARM64",
                    _ => "Unknown"
                };
                break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HardwareIdentifier] WMI CPU query failed: {ex.Message}");
        }

        // 2. Fallback to Environment
        if (identity.Cores == 0)
        {
            identity.Cores = Environment.ProcessorCount / 2;
            identity.Threads = Environment.ProcessorCount;
        }

        // Build lookup key
        identity.LookupKey = !string.IsNullOrEmpty(identity.WmiName)
            ? NormalizeCpuName(identity.WmiName)
            : $"{identity.Manufacturer} Family {identity.Family}";

        return identity;
    }

    private static string NormalizeCpuName(string name)
    {
        // Remove extra spaces
        name = Regex.Replace(name, @"\s+", " ").Trim();

        // Remove frequency info (@ x.xx GHz)
        name = Regex.Replace(name, @"@\s*[\d.]+\s*GHz", "", RegexOptions.IgnoreCase).Trim();

        // Remove (R), (TM), (tm) marks
        name = Regex.Replace(name, @"\(R\)|\(TM\)|\(tm\)", "", RegexOptions.IgnoreCase);

        // Remove "CPU" suffix
        name = Regex.Replace(name, @"\s+CPU\s*$", "", RegexOptions.IgnoreCase);

        return name.Trim();
    }

    #endregion

    #region GPU Identification

    /// <summary>
    /// Gets GPU identification information.
    /// </summary>
    public static GpuIdentity GetGpuId()
    {
        var identity = new GpuIdentity();

        // 1. Try Registry (most reliable for device ID)
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000");
            if (key != null)
            {
                identity.DriverDesc = key.GetValue("DriverDesc")?.ToString();
                var matchingId = key.GetValue("MatchingDeviceId")?.ToString();

                // Parse VEN_XXXX&DEV_XXXX
                var match = Regex.Match(matchingId ?? "", @"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})",
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    identity.VendorId = match.Groups[1].Value.ToUpperInvariant();
                    identity.DeviceId = match.Groups[2].Value.ToUpperInvariant();
                    identity.PciId = $"{identity.VendorId}:{identity.DeviceId}";
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HardwareIdentifier] Registry GPU query failed: {ex.Message}");
        }

        // 2. WMI fallback
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                identity.WmiName = obj["Name"]?.ToString();
                identity.AdapterRam = Convert.ToInt64(obj["AdapterRAM"]);
                identity.DriverVersion = obj["DriverVersion"]?.ToString();
                identity.PnpDeviceId = obj["PNPDeviceID"]?.ToString();
                identity.VideoProcessor = obj["VideoProcessor"]?.ToString();

                // Parse PCI ID from PnpDeviceId if not already set
                if (string.IsNullOrEmpty(identity.PciId) && !string.IsNullOrEmpty(identity.PnpDeviceId))
                {
                    var match = Regex.Match(identity.PnpDeviceId, @"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})",
                        RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        identity.VendorId = match.Groups[1].Value.ToUpperInvariant();
                        identity.DeviceId = match.Groups[2].Value.ToUpperInvariant();
                        identity.PciId = $"{identity.VendorId}:{identity.DeviceId}";
                    }
                }
                break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HardwareIdentifier] WMI GPU query failed: {ex.Message}");
        }

        // Determine vendor name
        identity.VendorName = identity.VendorId switch
        {
            "10DE" => "NVIDIA",
            "1002" => "AMD",
            "8086" => "Intel",
            _ => "Unknown"
        };

        // Build lookup key
        identity.LookupKey = identity.PciId ?? identity.WmiName ?? "Unknown";

        return identity;
    }

    #endregion

    #region Motherboard Identification

    /// <summary>
    /// Gets motherboard identification information.
    /// </summary>
    public static MotherboardIdentity GetMotherboardId()
    {
        var identity = new MotherboardIdentity();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
            {
                identity.Manufacturer = obj["Manufacturer"]?.ToString();
                identity.Product = obj["Product"]?.ToString();
                identity.SerialNumber = obj["SerialNumber"]?.ToString();
                identity.Version = obj["Version"]?.ToString();
                break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HardwareIdentifier] WMI Motherboard query failed: {ex.Message}");
        }

        // Build lookup key
        identity.LookupKey = !string.IsNullOrEmpty(identity.Product)
            ? $"{identity.Manufacturer} {identity.Product}".Trim()
            : "Unknown";

        return identity;
    }

    #endregion

    #region RAM Identification

    /// <summary>
    /// Gets RAM identification information.
    /// </summary>
    public static RamIdentity GetRamId()
    {
        var identity = new RamIdentity();

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            foreach (var obj in searcher.Get())
            {
                var module = new RamModule
                {
                    Manufacturer = obj["Manufacturer"]?.ToString()?.Trim(),
                    PartNumber = obj["PartNumber"]?.ToString()?.Trim(),
                    SerialNumber = obj["SerialNumber"]?.ToString()?.Trim(),
                    CapacityBytes = Convert.ToInt64(obj["Capacity"]),
                    SpeedMHz = Convert.ToInt32(obj["Speed"]),
                    FormFactor = obj["FormFactor"]?.ToString(),
                    MemoryType = GetMemoryTypeName(Convert.ToInt32(obj["MemoryType"])),
                    BankLabel = obj["BankLabel"]?.ToString(),
                    DeviceLocator = obj["DeviceLocator"]?.ToString()
                };
                identity.Modules.Add(module);
                identity.TotalCapacityBytes += module.CapacityBytes;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HardwareIdentifier] WMI RAM query failed: {ex.Message}");
        }

        // Build summary
        if (identity.Modules.Count > 0)
        {
            var firstModule = identity.Modules[0];
            identity.LookupKey = $"{identity.TotalCapacityBytes / (1024 * 1024 * 1024)}GB " +
                                 $"{firstModule.MemoryType} " +
                                 $"{firstModule.SpeedMHz}MHz";
        }
        else
        {
            identity.LookupKey = "Unknown";
        }

        return identity;
    }

    private static string GetMemoryTypeName(int typeCode)
    {
        return typeCode switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => $"Type{typeCode}"
        };
    }

    #endregion
}

#region Identity Classes

/// <summary>
/// CPU identification information.
/// </summary>
public class CpuIdentity
{
    public string? WmiName { get; set; }
    public string? Manufacturer { get; set; }
    public string? ProcessorId { get; set; }
    public int Cores { get; set; }
    public int Threads { get; set; }
    public int MaxClockSpeed { get; set; }
    public int Family { get; set; }
    public string Architecture { get; set; } = "Unknown";
    public string LookupKey { get; set; } = "";

    public override string ToString() => WmiName ?? LookupKey;
}

/// <summary>
/// GPU identification information.
/// </summary>
public class GpuIdentity
{
    public string? VendorId { get; set; }
    public string? DeviceId { get; set; }
    public string? PciId { get; set; }
    public string? VendorName { get; set; }
    public string? DriverDesc { get; set; }
    public string? WmiName { get; set; }
    public long AdapterRam { get; set; }
    public string? DriverVersion { get; set; }
    public string? PnpDeviceId { get; set; }
    public string? VideoProcessor { get; set; }
    public string LookupKey { get; set; } = "";

    public double AdapterRamGB => AdapterRam / (1024.0 * 1024.0 * 1024.0);
    public override string ToString() => WmiName ?? DriverDesc ?? LookupKey;
}

/// <summary>
/// Motherboard identification information.
/// </summary>
public class MotherboardIdentity
{
    public string? Manufacturer { get; set; }
    public string? Product { get; set; }
    public string? SerialNumber { get; set; }
    public string? Version { get; set; }
    public string LookupKey { get; set; } = "";

    public override string ToString() => $"{Manufacturer} {Product}".Trim();
}

/// <summary>
/// RAM identification information.
/// </summary>
public class RamIdentity
{
    public List<RamModule> Modules { get; } = new();
    public long TotalCapacityBytes { get; set; }
    public string LookupKey { get; set; } = "";

    public double TotalCapacityGB => TotalCapacityBytes / (1024.0 * 1024.0 * 1024.0);
    public override string ToString() => LookupKey;
}

/// <summary>
/// Individual RAM module information.
/// </summary>
public class RamModule
{
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public long CapacityBytes { get; set; }
    public int SpeedMHz { get; set; }
    public string? FormFactor { get; set; }
    public string? MemoryType { get; set; }
    public string? BankLabel { get; set; }
    public string? DeviceLocator { get; set; }

    public double CapacityGB => CapacityBytes / (1024.0 * 1024.0 * 1024.0);
}

#endregion
