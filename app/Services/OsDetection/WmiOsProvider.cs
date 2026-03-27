using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace RegProbe.App.Services.OsDetection;

public sealed class WmiOsProvider
{
    public async Task EnrichAsync(OsInfo osInfo)
    {
        try
        {
            var query = "SELECT Caption, OSArchitecture, LastBootUpTime, SystemDrive, WindowsDirectory, InstallDate FROM Win32_OperatingSystem";

            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in await Task.Run(() => searcher.Get()))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(osInfo.Architecture))
                    {
                        osInfo.Architecture = SafeGetString(obj, "OSArchitecture");
                    }

                    if (osInfo.LastBootTime == null)
                    {
                        var bootRaw = SafeGetString(obj, "LastBootUpTime");
                        if (!string.IsNullOrWhiteSpace(bootRaw))
                        {
                            var parsedBoot = ParseWmiDateTime(bootRaw);
                            if (parsedBoot.HasValue)
                            {
                                osInfo.LastBootTime = parsedBoot;
                                osInfo.UptimeFormatted = FormatUptime(parsedBoot.Value);
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(osInfo.SystemDrive))
                    {
                        osInfo.SystemDrive = SafeGetString(obj, "SystemDrive");
                    }

                    if (string.IsNullOrWhiteSpace(osInfo.WindowsDirectory))
                    {
                        osInfo.WindowsDirectory = SafeGetString(obj, "WindowsDirectory");
                    }

                    if (osInfo.InstallDate == null)
                    {
                        var installRaw = SafeGetString(obj, "InstallDate");
                        if (!string.IsNullOrWhiteSpace(installRaw))
                        {
                            osInfo.InstallDate = ParseWmiDateTime(installRaw);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(osInfo.Edition))
                    {
                        var caption = SafeGetString(obj, "Caption");
                        osInfo.Edition = ExtractEditionFromCaption(caption, osInfo.EditionId);
                    }

                    if (osInfo.Source == "Registry")
                    {
                        osInfo.Source = "Registry+WMI";
                    }

                    Debug.WriteLine($"[WmiOsProvider] Enriched: Architecture={osInfo.Architecture}, LastBoot={osInfo.LastBootTime}, Source={osInfo.Source}");
                }
                finally
                {
                    obj.Dispose();
                }

                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WmiOsProvider] WMI enrichment failed: {ex.Message}");
        }
    }

    private static string? SafeGetString(ManagementObject obj, string propertyName)
    {
        try
        {
            return obj[propertyName]?.ToString()?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ParseWmiDateTime(string wmiDate)
    {
        try
        {
            return ManagementDateTimeConverter.ToDateTime(wmiDate);
        }
        catch
        {
            return null;
        }
    }

    private static string? FormatUptime(DateTime lastBoot)
    {
        try
        {
            var uptime = DateTime.Now - lastBoot;
            if (uptime.TotalSeconds < 0) return null;

            var days = (int)uptime.TotalDays;
            var hours = uptime.Hours;
            var minutes = uptime.Minutes;

            if (days > 0)
            {
                return $"{days}d {hours}h {minutes}m";
            }
            if (hours > 0)
            {
                return $"{hours}h {minutes}m";
            }
            return $"{minutes}m";
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractEditionFromCaption(string? caption, string? editionId)
    {
        if (string.IsNullOrWhiteSpace(caption)) return editionId;

        if (caption.Contains("Pro", StringComparison.OrdinalIgnoreCase)) return "Pro";
        if (caption.Contains("Home", StringComparison.OrdinalIgnoreCase)) return "Home";
        if (caption.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)) return "Enterprise";
        if (caption.Contains("Education", StringComparison.OrdinalIgnoreCase)) return "Education";

        return editionId;
    }
}
