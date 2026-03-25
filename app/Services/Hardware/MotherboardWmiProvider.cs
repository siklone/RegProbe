using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace OpenTraceProject.App.Services.Hardware;

public sealed class MotherboardWmiProvider : IMotherboardProvider
{
    public async Task<MotherboardInfo> GetAsync()
    {
        var info = new MotherboardInfo();

        await Task.Run(() =>
        {
            QueryBaseBoard(info);
            QueryChassis(info);
            QueryBios(info);
            QueryBiosMode(info);
            QuerySystemSlots(info);
            QueryPhysicalMemoryArray(info);
            QueryPhysicalMemory(info);
        });

        Debug.WriteLine($"[MotherboardWmiProvider] Result: Manufacturer={info.Manufacturer}, Product={info.Product}, BiosMode={info.BiosMode}");
        return info;
    }

    private void QueryBaseBoard(MotherboardInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, Product, Version, SerialNumber, Tag, AssetTag, Replaceable FROM Win32_BaseBoard");

            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    info.Manufacturer = SafeGetString(obj, "Manufacturer");
                    info.Product = SafeGetString(obj, "Product");
                    info.Version = SafeGetString(obj, "Version");
                    info.SerialNumber = SafeGetString(obj, "SerialNumber");
                    info.Tag = SafeGetString(obj, "Tag");
                    info.AssetTag = SafeGetString(obj, "AssetTag");
                    info.Replaceable = SafeGetBool(obj, "Replaceable");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MotherboardWmiProvider] Win32_BaseBoard failed: {ex.Message}");
        }
    }

    private void QueryChassis(MotherboardInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure");
            foreach (ManagementObject obj in searcher.Get())
            {
                var types = obj["ChassisTypes"] as ushort[];
                if (types != null && types.Length > 0)
                {
                    info.ChassisType = ToChassisType(types[0]);
                }
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MotherboardWmiProvider] Win32_SystemEnclosure failed: {ex.Message}");
        }
    }

    private static string ToChassisType(int chassisCode)
    {
        return chassisCode switch
        {
            1 => "Other", 2 => "Unknown", 3 => "Desktop", 4 => "Low Profile Desktop",
            5 => "Pizza Box", 6 => "Mini Tower", 7 => "Tower", 8 => "Portable",
            9 => "Laptop", 10 => "Notebook", 11 => "Hand Held", 12 => "Docking Station",
            13 => "All in One", 14 => "Sub Notebook", 15 => "Space-saving", 16 => "Lunch Box",
            17 => "Main System Chassis", 18 => "Expansion Chassis", 19 => "SubChassis",
            20 => "Bus Expansion Chassis", 21 => "Peripheral Chassis", 22 => "Storage Chassis",
            23 => "Rack Mount Chassis", 24 => "Sealed-case PC", _ => "Unknown"
        };
    }

    private void QueryBios(MotherboardInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");

            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    info.BiosVendor = SafeGetString(obj, "Manufacturer");
                    info.BiosVersion = SafeGetString(obj, "SMBIOSBIOSVersion");

                    var releaseDateRaw = SafeGetString(obj, "ReleaseDate");
                    if (!string.IsNullOrWhiteSpace(releaseDateRaw))
                    {
                        try
                        {
                            info.BiosReleaseDate = ManagementDateTimeConverter.ToDateTime(releaseDateRaw);
                        }
                        catch
                        {
                            // Ignore parse errors
                        }
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MotherboardWmiProvider] Win32_BIOS failed: {ex.Message}");
        }
    }

    private void QueryBiosMode(MotherboardInfo info)
    {
        try
        {
            using var secureBootKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\SecureBoot\State", false);

            if (secureBootKey != null)
            {
                info.BiosMode = "UEFI";

                var secureBootValue = secureBootKey.GetValue("UEFISecureBootEnabled");
                if (secureBootValue != null)
                {
                    info.SecureBootEnabled = secureBootValue is int intValue && intValue == 1;
                }
            }
            else
            {
                info.BiosMode = "Legacy";
                info.SecureBootEnabled = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MotherboardWmiProvider] BIOS mode detection failed: {ex.Message}");
            info.BiosMode = "Unknown";
        }
    }

    private void QuerySystemSlots(MotherboardInfo info)
    {
        var pcieX16 = 0;
        var pcieX4 = 0;
        var m2 = 0;
        var dimm = 0;
        var sata = 0;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT SlotDesignation, CurrentUsage FROM Win32_SystemSlot");

            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    var designation = SafeGetString(obj, "SlotDesignation");
                    if (string.IsNullOrWhiteSpace(designation)) continue;

                    var desUpper = designation.ToUpperInvariant();

                    if (desUpper.Contains("PCI") || desUpper.Contains("PCIE") || desUpper.Contains("PCI EXPRESS"))
                    {
                        if (desUpper.Contains("X16") || desUpper.Contains("X 16"))
                        {
                            pcieX16++;
                        }
                        else if (desUpper.Contains("X4") || desUpper.Contains("X 4"))
                        {
                            pcieX4++;
                        }
                    }

                    if (desUpper.Contains("M.2") || desUpper.Contains("NGFF") || desUpper.Contains("M2"))
                    {
                        m2++;
                    }

                    if (desUpper.Contains("DIMM") || desUpper.Contains("MEMORY"))
                    {
                        dimm++;
                    }

                    if (desUpper.Contains("SATA"))
                    {
                        sata++;
                    }
                }
            }

            info.PcieX16Slots = pcieX16 > 0 ? pcieX16 : null;
            info.PcieX4Slots = pcieX4 > 0 ? pcieX4 : null;
            info.M2Slots = m2 > 0 ? m2 : null;
            info.DimmSlots = dimm > 0 ? dimm : null;
            info.SataPorts = sata > 0 ? sata : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MotherboardWmiProvider] Win32_SystemSlot failed: {ex.Message}");
        }
    }

    private void QueryPhysicalMemoryArray(MotherboardInfo info)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT MaxCapacity, MemoryDevices, MemoryErrorCorrection FROM Win32_PhysicalMemoryArray");

            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    var maxCapacity = SafeGetUInt32(obj, "MaxCapacity");
                    if (maxCapacity > 0)
                    {
                        info.MaxRamGb = (int)(maxCapacity / 1024 / 1024);
                    }

                    var memoryDevices = SafeGetUInt32(obj, "MemoryDevices");
                    if (memoryDevices > 0)
                    {
                        info.DimmSlotsTotal = (int)memoryDevices;
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MotherboardWmiProvider] Win32_PhysicalMemoryArray failed: {ex.Message}");
        }
    }

    private void QueryPhysicalMemory(MotherboardInfo info)
    {
        var speeds = new List<uint>();
        var populatedSlots = 0;
        uint? memoryType = null;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Speed, SMBIOSMemoryType FROM Win32_PhysicalMemory");

            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    populatedSlots++;

                    var speed = SafeGetUInt32(obj, "Speed");
                    if (speed > 0)
                    {
                        speeds.Add(speed);
                    }

                    var smbiosType = SafeGetUInt32(obj, "SMBIOSMemoryType");
                    if (smbiosType > 0 && memoryType == null)
                    {
                        memoryType = smbiosType;
                    }
                }
            }

            if (speeds.Count > 0)
            {
                info.MaxMemorySpeedMhz = (int)speeds.Max();
            }

            if (populatedSlots > 0)
            {
                info.DimmSlotsUsed = populatedSlots;
            }

            if (memoryType.HasValue)
            {
                info.MemoryType = MapMemoryType(memoryType.Value);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MotherboardWmiProvider] Win32_PhysicalMemory failed: {ex.Message}");
        }
    }

    private static string? SafeGetString(ManagementObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            return value?.ToString()?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static bool? SafeGetBool(ManagementObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null) return null;
            if (value is bool boolValue) return boolValue;
            if (bool.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static uint SafeGetUInt32(ManagementObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null) return 0;
            if (value is uint uintValue) return uintValue;
            if (uint.TryParse(value.ToString(), out var parsed)) return parsed;
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string MapMemoryType(uint smbiosType)
    {
        return smbiosType switch
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
}
