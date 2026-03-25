using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace OpenTraceProject.App.Services.OsDetection;

public sealed class RegistryOsProvider
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    public Task<OsInfo> GetAsync()
    {
        var osInfo = new OsInfo
        {
            Source = "Registry"
        };

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey);
            if (key == null)
            {
                Debug.WriteLine("[RegistryOsProvider] Cannot open registry key");
                return Task.FromResult(osInfo);
            }

            osInfo.Caption = GetStringValue(key, "ProductName");
            osInfo.DisplayVersion = GetStringValue(key, "DisplayVersion");
            osInfo.ReleaseId = GetStringValue(key, "ReleaseId");
            osInfo.EditionId = GetStringValue(key, "EditionID");
            osInfo.InstallationType = GetStringValue(key, "InstallationType");
            osInfo.RegisteredUser = GetStringValue(key, "RegisteredOwner");
            osInfo.Organization = GetStringValue(key, "RegisteredOrganization");
            osInfo.Version = GetStringValue(key, "CurrentVersion");

            var buildText = GetStringValue(key, "CurrentBuild")
                ?? GetStringValue(key, "CurrentBuildNumber");

            if (!string.IsNullOrWhiteSpace(buildText) && int.TryParse(buildText, out var buildNumber))
            {
                osInfo.BuildNumber = buildText;
                osInfo.BuildNumberRaw = buildNumber;
            }

            var ubrText = GetStringValue(key, "UBR");
            if (!string.IsNullOrWhiteSpace(ubrText) && int.TryParse(ubrText, out var ubr))
            {
                osInfo.Ubr = ubr;
            }

            var installDateRaw = GetStringValue(key, "InstallDate");
            if (!string.IsNullOrWhiteSpace(installDateRaw))
            {
                if (int.TryParse(installDateRaw, out var unixTimestamp))
                {
                    osInfo.InstallDate = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                }
            }

            if (string.IsNullOrWhiteSpace(osInfo.Caption))
            {
                osInfo.Caption = BuildCaptionFromBuildNumber(osInfo.BuildNumberRaw, null);
            }

            DetectBiosMode(osInfo);

            Debug.WriteLine($"[RegistryOsProvider] Caption={osInfo.Caption}, Build={osInfo.BuildNumberRaw}, Source={osInfo.Source}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RegistryOsProvider] Error reading registry: {ex.Message}");
        }

        return Task.FromResult(osInfo);
    }

    private static string? GetStringValue(RegistryKey key, string valueName)
    {
        try
        {
            var value = key.GetValue(valueName);
            return value?.ToString()?.Trim();
        }
        catch
        {
            return null;
        }
    }

    internal static string BuildCaptionFromBuildNumber(int buildNumber, string? edition)
    {
        var osBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        return string.IsNullOrWhiteSpace(edition) ? osBase : $"{osBase} {edition}";
    }

    private static void DetectBiosMode(OsInfo osInfo)
    {
        try
        {
            using var secureBootKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\SecureBoot\State", false);

            if (secureBootKey != null)
            {
                osInfo.BiosMode = "UEFI";
                var secureBootValue = secureBootKey.GetValue("UEFISecureBootEnabled");
                if (secureBootValue != null && secureBootValue is int intValue)
                {
                    osInfo.SecureBootEnabled = intValue == 1;
                }
            }
            else
            {
                osInfo.BiosMode = "Legacy";
                osInfo.SecureBootEnabled = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RegistryOsProvider] BIOS mode detection failed: {ex.Message}");
            osInfo.BiosMode = "Unknown";
        }
    }
}
