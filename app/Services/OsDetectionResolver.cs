using System;
using System.Management;
using Microsoft.Win32;
using RegProbe.App.Diagnostics;
using RegProbe.App.HardwareDb;

namespace RegProbe.App.Services;

public sealed record OsDetectionResult(
    string ProductName,
    string DisplayVersion,
    string ReleaseId,
    int BuildNumber,
    int Ubr,
    string EditionId,
    string InstallationType,
    string Version,
    string ProductNameSource,
    string DisplayVersionSource,
    string BuildNumberSource,
    string NormalizedName,
    string IconKey,
    string Architecture,
    string InstallDate,
    string LastBootTime);

public static class OsDetectionResolver
{
    public static OsDetectionResult Resolve(bool includeWmiCrossCheck)
    {
        var productName = string.Empty;
        var displayVersion = string.Empty;
        var releaseId = string.Empty;
        var editionId = string.Empty;
        var installType = string.Empty;
        var version = string.Empty;
        var architecture = string.Empty;
        var installDate = string.Empty;
        var lastBootTime = string.Empty;
        var buildNumber = 0;
        var ubr = 0;

        var productSource = "Unknown";
        var displaySource = "Unknown";
        var buildSource = "Unknown";

        try
        {
            using var currentVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (currentVersion != null)
            {
                productName = currentVersion.GetValue("ProductName")?.ToString() ?? string.Empty;
                displayVersion = currentVersion.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
                releaseId = currentVersion.GetValue("ReleaseId")?.ToString() ?? string.Empty;
                editionId = currentVersion.GetValue("EditionID")?.ToString() ?? string.Empty;
                installType = currentVersion.GetValue("InstallationType")?.ToString() ?? string.Empty;
                version = currentVersion.GetValue("CurrentVersion")?.ToString() ?? string.Empty;
                _ = int.TryParse(currentVersion.GetValue("UBR")?.ToString(), out ubr);

                var buildText = currentVersion.GetValue("CurrentBuild")?.ToString();
                if (string.IsNullOrWhiteSpace(buildText))
                {
                    buildText = currentVersion.GetValue("CurrentBuildNumber")?.ToString();
                }

                if (int.TryParse(buildText, out var parsedBuild))
                {
                    buildNumber = parsedBuild;
                    buildSource = "Registry";
                }

                if (!string.IsNullOrWhiteSpace(productName))
                {
                    productSource = "Registry";
                }

                if (!string.IsNullOrWhiteSpace(displayVersion))
                {
                    displaySource = "Registry";
                }
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Log($"[OsDetectionResolver] Registry read failed: {ex.Message}");
        }

        if (includeWmiCrossCheck)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber, OSArchitecture, InstallDate, LastBootUpTime FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var wmiCaption = obj["Caption"]?.ToString() ?? string.Empty;

                    // Always prefer the WMI Caption when:
                    //   a) registry had nothing, OR
                    //   b) the build number indicates Win11 but registry ProductName still says Win10
                    //      (common on in-place-upgraded systems where the registry key was never updated)
                    var registryIsStale = buildNumber >= 22000 &&
                                         !string.IsNullOrWhiteSpace(productName) &&
                                         productName.Contains("Windows 10", StringComparison.OrdinalIgnoreCase) &&
                                         !string.IsNullOrWhiteSpace(wmiCaption) &&
                                         wmiCaption.Contains("Windows 11", StringComparison.OrdinalIgnoreCase);

                    if (string.IsNullOrWhiteSpace(productName) || registryIsStale)
                    {
                        productName = wmiCaption;
                        if (!string.IsNullOrWhiteSpace(productName))
                        {
                            productSource = "WMI";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(version))
                    {
                        version = obj["Version"]?.ToString() ?? string.Empty;
                    }

                    if (buildNumber <= 0 && int.TryParse(obj["BuildNumber"]?.ToString(), out var wmiBuild))
                    {
                        buildNumber = wmiBuild;
                        buildSource = "WMI";
                    }

                    architecture = obj["OSArchitecture"]?.ToString() ?? string.Empty;

                    var installRaw = obj["InstallDate"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(installRaw))
                    {
                        try
                        {
                            installDate = ManagementDateTimeConverter.ToDateTime(installRaw).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            installDate = installRaw;
                        }
                    }

                    var bootRaw = obj["LastBootUpTime"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(bootRaw))
                    {
                        try
                        {
                            lastBootTime = ManagementDateTimeConverter.ToDateTime(bootRaw).ToString("yyyy-MM-dd HH:mm");
                        }
                        catch
                        {
                            lastBootTime = bootRaw;
                        }
                    }

                    break;
                }
            }
            catch (Exception ex)
            {
                AppDiagnostics.Log($"[OsDetectionResolver] WMI cross-check failed: {ex.Message}");
            }
        }

        if (buildNumber <= 0)
        {
            buildNumber = Environment.OSVersion.Version.Build;
            buildSource = "API";
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = Environment.OSVersion.VersionString;
            productSource = "API";
        }

        if (string.IsNullOrWhiteSpace(architecture))
        {
            architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        }

        // On Windows 11 systems that were upgraded in-place, the registry ReleaseId
        // may still hold an old Windows 10 value (e.g. "2009" = 20H2). Clear it so
        // the detail window shows the DisplayVersion (e.g. "25H2") instead.
        if (buildNumber >= 22000 && int.TryParse(releaseId, out var releaseIdNum) && releaseIdNum <= 2100)
        {
            AppDiagnostics.Log($"[OsDetectionResolver] Cleared stale Win10 ReleaseId '{releaseId}' on Win11 system (build {buildNumber})");
            releaseId = string.Empty;
        }

        var normalizedName = NormalizeName(buildNumber, editionId, displayVersion, releaseId);
        var iconKey = HardwareIconResolver.ResolveOsIconKey(normalizedName);

        AppDiagnostics.Log($"[OsDetectionResolver] BuildNumber={buildNumber}, ProductName={productName}, DisplayVersion={displayVersion}, FinalNormalizedName={normalizedName}, ChosenIcon={iconKey}");

        return new OsDetectionResult(
            ProductName: productName,
            DisplayVersion: displayVersion,
            ReleaseId: releaseId,
            BuildNumber: buildNumber,
            Ubr: ubr,
            EditionId: editionId,
            InstallationType: installType,
            Version: version,
            ProductNameSource: productSource,
            DisplayVersionSource: displaySource,
            BuildNumberSource: buildSource,
            NormalizedName: normalizedName,
            IconKey: iconKey,
            Architecture: architecture,
            InstallDate: installDate,
            LastBootTime: lastBootTime);
    }

    private static string NormalizeName(int buildNumber, string editionId, string displayVersion, string releaseId)
    {
        var osBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var edition = NormalizeEdition(editionId);
        var normalized = string.IsNullOrWhiteSpace(edition) ? osBase : $"{osBase} {edition}";

        var version = !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion : releaseId;
        if (!string.IsNullOrWhiteSpace(version))
        {
            normalized = $"{normalized} ({version})";
        }

        return normalized;
    }

    private static string NormalizeEdition(string editionId)
    {
        if (string.IsNullOrWhiteSpace(editionId))
        {
            return string.Empty;
        }

        return editionId.Trim() switch
        {
            "Professional" => "Pro",
            "Core" => "Home",
            "CoreSingleLanguage" => "Home Single Language",
            "EnterpriseS" => "Enterprise LTSC",
            _ => editionId.Trim()
        };
    }
}
