using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace RegProbe.App.Services.OsDetection;

public sealed class OsActivationChecker
{
    public async Task<string> GetActivationStatusAsync()
    {
        try
        {
            var query = "SELECT LicenseStatus FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL";

            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in await Task.Run(() => searcher.Get()))
            {
                try
                {
                    var licenseStatus = SafeGetInt(obj, "LicenseStatus");
                    var status = MapLicenseStatus(licenseStatus);
                    Debug.WriteLine($"[OsActivationChecker] LicenseStatus={licenseStatus} â†’ {status}");
                    return status;
                }
                finally
                {
                    obj.Dispose();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"[OsActivationChecker] Error checking activation: {ex.Message}");
        }

        return "Unknown";
    }

    private static int SafeGetInt(ManagementObject obj, string propertyName)
    {
        try
        {
            var value = obj[propertyName];
            if (value == null) return 0;

            if (value is int intValue) return intValue;
            if (int.TryParse(value.ToString(), out var parsed)) return parsed;

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string MapLicenseStatus(int status)
    {
        return status switch
        {
            0 => "Unlicensed",
            1 => "Activated",
            2 => "Out-of-Box Grace",
            3 => "Out-of-Tolerance Grace",
            4 => "Non-Genuine Grace",
            5 => "Notification",
            6 => "Extended Grace",
            _ => "Unknown"
        };
    }
}
