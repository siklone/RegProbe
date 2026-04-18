using System.Management;
using RegProbe.App.Diagnostics;

namespace RegProbe.App.Services;

internal static class OsWmiInfoReader
{
    public static void Populate(OsDetectionState state)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber, OSArchitecture, InstallDate, LastBootUpTime FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                ApplyOperatingSystemObject(state, obj);
                break;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[OsDetectionResolver] WMI cross-check failed: {ex.Message}");
        }
    }

    private static void ApplyOperatingSystemObject(OsDetectionState state, ManagementBaseObject obj)
    {
        var wmiCaption = obj["Caption"]?.ToString() ?? string.Empty;

        // Prefer WMI when registry is empty or stale after a Windows 10 -> 11 in-place upgrade.
        var registryIsStale = state.BuildNumber >= 22000 &&
                              !string.IsNullOrWhiteSpace(state.ProductName) &&
                              state.ProductName.Contains("Windows 10", StringComparison.OrdinalIgnoreCase) &&
                              !string.IsNullOrWhiteSpace(wmiCaption) &&
                              wmiCaption.Contains("Windows 11", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(state.ProductName) || registryIsStale)
        {
            state.ProductName = wmiCaption;
            if (!string.IsNullOrWhiteSpace(state.ProductName))
            {
                state.ProductNameSource = "WMI";
            }
        }

        if (string.IsNullOrWhiteSpace(state.Version))
        {
            state.Version = obj["Version"]?.ToString() ?? string.Empty;
        }

        if (state.BuildNumber <= 0 && int.TryParse(obj["BuildNumber"]?.ToString(), out var wmiBuild))
        {
            state.BuildNumber = wmiBuild;
            state.BuildNumberSource = "WMI";
        }

        state.Architecture = obj["OSArchitecture"]?.ToString() ?? string.Empty;
        state.InstallDate = ConvertWmiDate(obj["InstallDate"]?.ToString(), "yyyy-MM-dd");
        state.LastBootTime = ConvertWmiDate(obj["LastBootUpTime"]?.ToString(), "yyyy-MM-dd HH:mm");
    }

    private static string ConvertWmiDate(string? value, string format)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            return ManagementDateTimeConverter.ToDateTime(value).ToString(format);
        }
        catch
        {
            return value;
        }
    }
}
