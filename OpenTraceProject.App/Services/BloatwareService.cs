using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using OpenTraceProject.App.Models;

namespace OpenTraceProject.App.Services;

/// <summary>
/// Service for managing UWP/AppX packages with PowerShell and safety checks.
/// </summary>
public class BloatwareService
{
    private static readonly HashSet<string> CriticalPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Store & Framework
        "Microsoft.WindowsStore",
        "Microsoft.StorePurchaseApp",
        "Microsoft.DesktopAppInstaller",
        
        // Runtime Libraries (CRITICAL!)
        "Microsoft.VCLibs",
        "Microsoft.NET.Native.Framework",
        "Microsoft.NET.Native.Runtime",
        
        // UI Frameworks
        "Microsoft.UI.Xaml",
        
        // System Components
        "Microsoft.Windows.ShellExperienceHost",
        "Microsoft.Windows.StartMenuExperienceHost",
        "Microsoft.Windows.Cortana",
        "Microsoft.Windows.Search",
        "Microsoft.Windows.SecHealthUI",
        "Microsoft.Windows.CloudExperienceHost",
        
        // Core Apps
        "Microsoft.WindowsCalculator",
        "Microsoft.WindowsCamera",
        "Microsoft.WindowsAlarms",
        "Microsoft.ScreenSketch",
        "Microsoft.Windows.Photos",
        "Microsoft.WindowsSoundRecorder",
        
        // Edge
        "Microsoft.MicrosoftEdge",
        "Microsoft.MicrosoftEdge.Stable"
    };

    /// <summary>
    /// Gets installed UWP packages using PowerShell.
    /// </summary>
    public async Task<List<AppxPackageInfo>> GetInstalledAppsAsync()
    {
        return await Task.Run(() =>
        {
            var result = new List<AppxPackageInfo>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -Command \"Get-AppxPackage | Select-Object Name,PackageFullName,Publisher,Version,InstallLocation,IsFramework | ConvertTo-Json -Compress\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                if (process == null) return result;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (string.IsNullOrWhiteSpace(output)) return result;

                var packages = ParsePackagesJson(output);

                foreach (var pkg in packages)
                {
                    if (IsSystemCritical(pkg.PackageFullName) || pkg.IsFramework)
                        continue;
                    result.Add(pkg);
                }
            }
            catch { }

            return result.OrderBy(p => p.DisplayName).ToList();
        });
    }

    /// <summary>
    /// Uninstalls a package using PowerShell.
    /// </summary>
    public async Task<UninstallResult> UninstallAppAsync(string packageFullName)
    {
        if (IsSystemCritical(packageFullName))
        {
            return new UninstallResult(false, packageFullName, 
                "System-critical package protected.", new List<string>());
        }

        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Remove-AppxPackage -Package '{packageFullName}'\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return new UninstallResult(false, packageFullName, "PowerShell failed", new List<string>());
                }

                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && string.IsNullOrWhiteSpace(error))
                {
                    return new UninstallResult(true, packageFullName, string.Empty, new List<string>());
                }
                else
                {
                    return new UninstallResult(false, packageFullName, error, new List<string>());
                }
            }
            catch (Exception ex)
            {
                return new UninstallResult(false, packageFullName, ex.Message, new List<string>());
            }
        });
    }

    public PackageSafetyLevel GetSafetyLevel(string packageFullName)
    {
        if (IsSystemCritical(packageFullName))
            return PackageSafetyLevel.Critical;

        if (packageFullName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) &&
            !packageFullName.Contains("Bing", StringComparison.OrdinalIgnoreCase) &&
            !packageFullName.Contains("Xbox", StringComparison.OrdinalIgnoreCase) &&
            !packageFullName.Contains("Solitaire", StringComparison.OrdinalIgnoreCase))
        {
            return PackageSafetyLevel.Caution;
        }

        return PackageSafetyLevel.Safe;
    }

    private bool IsSystemCritical(string packageFullName)
    {
        return CriticalPackages.Any(critical => 
            packageFullName.Contains(critical, StringComparison.OrdinalIgnoreCase));
    }

    private List<AppxPackageInfo> ParsePackagesJson(string json)
    {
        var packages = new List<AppxPackageInfo>();

        try
        {
            var matches = Regex.Matches(json, @"\{[^\}]+\}");
            
            foreach (Match match in matches)
            {
                var pkgJson = match.Value;
                
                var name = ExtractJsonValue(pkgJson, "Name");
                var fullName = ExtractJsonValue(pkgJson, "PackageFullName");
                var publisher = ExtractJsonValue(pkgJson, "Publisher");
                var version = ExtractJsonValue(pkgJson, "Version");
                var installLocation = ExtractJsonValue(pkgJson, "InstallLocation");
                var isFramework = pkgJson.Contains("\"IsFramework\":true", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(fullName)) continue;

                long? size = null;
                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    try
                    {
                        size = GetDirectorySize(new DirectoryInfo(installLocation));
                    }
                    catch { }
                }

                packages.Add(new AppxPackageInfo(
                    fullName, name, publisher, version, 
                    installLocation, size, false, isFramework,
                    new List<string>()
                ));
            }
        }
        catch { }

        return packages;
    }

    private string ExtractJsonValue(string json, string key)
    {
        var pattern = $"\"{key}\"\\s*:\\s*\"([^\"]*)\"|" + $"\"{key}\"\\s*:\\s*null";
        var match = Regex.Match(json, pattern);
        return match.Success && match.Groups.Count > 1 ? match.Groups[1].Value : string.Empty;
    }

    private long GetDirectorySize(DirectoryInfo directory)
    {
        long size = 0;

        try
        {
            foreach (var file in directory.GetFiles())
            {
                size += file.Length;
            }

            foreach (var dir in directory.GetDirectories())
            {
                size += GetDirectorySize(dir);
            }
        }
        catch { }

        return size;
    }
}
