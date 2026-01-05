using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Service for viewing installed device drivers.
/// Uses WMI to query driver information.
/// </summary>
public class DriverService
{
    /// <summary>
    /// Get all signed device drivers.
    /// </summary>
    public async Task<IEnumerable<DriverInfo>> GetInstalledDriversAsync()
    {
        return await Task.Run(() =>
        {
            var drivers = new List<DriverInfo>();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPSignedDriver WHERE DriverName IS NOT NULL");

                foreach (ManagementObject mo in searcher.Get())
                {
                    drivers.Add(new DriverInfo
                    {
                        DeviceName = mo["DeviceName"]?.ToString() ?? "Unknown",
                        DriverVersion = mo["DriverVersion"]?.ToString() ?? "",
                        Manufacturer = mo["Manufacturer"]?.ToString() ?? "",
                        DriverDate = ParseDriverDate(mo["DriverDate"]?.ToString()),
                        DeviceClass = mo["DeviceClass"]?.ToString() ?? "",
                        DriverProviderName = mo["DriverProviderName"]?.ToString() ?? "",
                        IsSigned = (bool?)mo["IsSigned"] ?? false,
                        InfName = mo["InfName"]?.ToString() ?? "",
                        HardwareId = mo["HardWareID"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get drivers: {ex.Message}");
            }

            return drivers.OrderBy(d => d.DeviceClass).ThenBy(d => d.DeviceName);
        });
    }

    /// <summary>
    /// Get drivers grouped by device class.
    /// </summary>
    public async Task<Dictionary<string, List<DriverInfo>>> GetDriversByClassAsync()
    {
        var drivers = await GetInstalledDriversAsync();
        return drivers.GroupBy(d => d.DeviceClass)
                      .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Get outdated drivers (where driver date is older than 2 years).
    /// </summary>
    public async Task<IEnumerable<DriverInfo>> GetOutdatedDriversAsync(int olderThanYears = 2)
    {
        var drivers = await GetInstalledDriversAsync();
        var cutoffDate = DateTime.Now.AddYears(-olderThanYears);
        return drivers.Where(d => d.DriverDate < cutoffDate && d.DriverDate > DateTime.MinValue);
    }

    /// <summary>
    /// Check for unsigned drivers.
    /// </summary>
    public async Task<IEnumerable<DriverInfo>> GetUnsignedDriversAsync()
    {
        var drivers = await GetInstalledDriversAsync();
        return drivers.Where(d => !d.IsSigned);
    }

    private static DateTime ParseDriverDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString.Length < 8)
            return DateTime.MinValue;

        try
        {
            // WMI date format: yyyyMMddHHmmss.ffffff+zzz
            var year = int.Parse(dateString[..4]);
            var month = int.Parse(dateString[4..6]);
            var day = int.Parse(dateString[6..8]);
            return new DateTime(year, month, day);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}

/// <summary>
/// Information about a device driver.
/// </summary>
public class DriverInfo
{
    public string DeviceName { get; init; } = "";
    public string DriverVersion { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public DateTime DriverDate { get; init; }
    public string DeviceClass { get; init; } = "";
    public string DriverProviderName { get; init; } = "";
    public bool IsSigned { get; init; }
    public string InfName { get; init; } = "";
    public string HardwareId { get; init; } = "";

    public string DriverDateText => DriverDate > DateTime.MinValue ? DriverDate.ToString("d") : "Unknown";
    public string SignedText => IsSigned ? "Signed" : "Unsigned";
    public bool IsOutdated => DriverDate < DateTime.Now.AddYears(-2) && DriverDate > DateTime.MinValue;
}
