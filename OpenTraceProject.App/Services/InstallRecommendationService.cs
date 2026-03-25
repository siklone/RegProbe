using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace OpenTraceProject.App.Services;

public sealed class InstallRecommendationService
{
    private const string MicrosoftVcRedistInfoUrl = "https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist";
    private const string MicrosoftDirectXRuntimeUrl = "https://www.microsoft.com/en-us/download/details.aspx?id=35";
    private const string NvidiaDriversUrl = "https://www.nvidia.com/en-us/drivers/";
    private const string AmdSupportUrl = "https://www.amd.com/en/support";
    private const string IntelDsaUrl = "https://www.intel.com/content/www/us/en/support/intel-driver-support-assistant.html";

    private static readonly string[] VcRedistX64BundleNames =
    {
        "Microsoft Visual C++ 2015-2022 Redistributable (x64)",
        "Microsoft Visual C++ 2015-2019 Redistributable (x64)",
        "Microsoft Visual C++ v14 Redistributable (x64)"
    };

    private static readonly string[] VcRedistX64ComponentNames =
    {
        "Microsoft Visual C++ 2022 X64 Minimum Runtime",
        "Microsoft Visual C++ 2022 X64 Additional Runtime"
    };

    private static readonly string[] VcRedistX86BundleNames =
    {
        "Microsoft Visual C++ 2015-2022 Redistributable (x86)",
        "Microsoft Visual C++ 2015-2019 Redistributable (x86)",
        "Microsoft Visual C++ v14 Redistributable (x86)"
    };

    private static readonly string[] VcRedistX86ComponentNames =
    {
        "Microsoft Visual C++ 2022 X86 Minimum Runtime",
        "Microsoft Visual C++ 2022 X86 Additional Runtime"
    };

    public static InstallRecommendationService Instance { get; } = new();

    public IReadOnlyList<InstallRecommendation> GetRecommendations(InstallRecommendationContext context)
    {
        return BuildRecommendations(context, ProbeInstalledState());
    }

    internal IReadOnlyList<InstallRecommendation> BuildRecommendations(
        InstallRecommendationContext context,
        InstallProbeState probeState)
    {
        var recommendations = new List<InstallRecommendation>();

        if (LooksLikeNvidiaGpu(context.GpuName))
        {
            recommendations.Add(new InstallRecommendation
            {
                Id = "driver.nvidia",
                Category = "Driver",
                Title = "NVIDIA Driver",
                Description = "Official GeForce driver hub.",
                Reason = CompactHardwareName(context.GpuName) ?? string.Empty,
                CurrentState = JoinNonEmpty(
                    context.GpuDriverVersion,
                    HasValue(context.GpuDriverDate) ? context.GpuDriverDate : null),
                StatusLabel = "Vendor page",
                SourceName = "NVIDIA",
                SourceUrl = NvidiaDriversUrl,
                Priority = 10
            });
        }

        if (LooksLikeAmdPlatform(context.CpuName, context.MotherboardModel, context.MotherboardChipset))
        {
            recommendations.Add(new InstallRecommendation
            {
                Id = "driver.amd-chipset",
                Category = "Driver",
                Title = "AMD Chipset",
                Description = "Recommended for Ryzen AM4/AM5 platforms.",
                Reason = JoinNonEmpty(
                    CompactHardwareName(context.CpuName),
                    CompactHardwareName(context.MotherboardChipset)),
                CurrentState = CompactHardwareName(FirstNonEmpty(context.MotherboardModel, context.MotherboardChipset)) ?? string.Empty,
                StatusLabel = "Vendor page",
                SourceName = "AMD Support",
                SourceUrl = AmdSupportUrl,
                Priority = 20
            });
        }

        var intelReason = GetIntelHardwareReason(context);
        if (intelReason != null && !probeState.IntelDsaInstalled)
        {
            recommendations.Add(new InstallRecommendation
            {
                Id = "tool.intel-dsa",
                Category = "Utility",
                Title = "Intel Driver Assistant",
                Description = "Useful for Intel Wi-Fi, Bluetooth and graphics stacks.",
                Reason = intelReason,
                CurrentState = probeState.HasWinget
                    ? "Available through winget."
                    : "Open the official Intel assistant page.",
                StatusLabel = probeState.HasWinget ? "Optional install" : "Official source",
                SourceName = "Intel",
                SourceUrl = IntelDsaUrl,
                InstallCommand = probeState.HasWinget
                    ? "winget install --id 'Intel.IntelDriverAndSupportAssistant' --exact --accept-package-agreements --accept-source-agreements"
                    : null,
                Priority = 30
            });
        }

        if (context.Is64BitOs)
        {
            recommendations.Add(CreateVcRuntimeRecommendation(
                id: "runtime.vcredist-x64",
                title: "VC++ Runtime (x64)",
                reason: "Games, launchers and middleware.",
                installed: probeState.VcRedistX64Installed,
                hasWinget: probeState.HasWinget,
                wingetId: "Microsoft.VCRedist.2015+.x64",
                priority: 40));
        }

        recommendations.Add(CreateVcRuntimeRecommendation(
            id: "runtime.vcredist-x86",
            title: "VC++ Runtime (x86)",
            reason: "Older games and 32-bit launchers.",
            installed: probeState.VcRedistX86Installed,
            hasWinget: probeState.HasWinget,
            wingetId: "Microsoft.VCRedist.2015+.x86",
            priority: 50));

        recommendations.Add(new InstallRecommendation
        {
            Id = "runtime.directx-legacy",
            Category = "Runtime",
            Title = "DirectX Legacy",
            Description = "Legacy DirectX helper DLL set.",
            Reason = "Covers older game dependencies.",
            CurrentState = probeState.DirectXLegacyRuntimeInstalled
                ? "Ready locally."
                : "Legacy files not found.",
            StatusLabel = probeState.DirectXLegacyRuntimeInstalled ? "Ready" : probeState.HasWinget ? "Missing" : "Official source",
            SourceName = "Microsoft",
            SourceUrl = MicrosoftDirectXRuntimeUrl,
            InstallCommand = !probeState.DirectXLegacyRuntimeInstalled && probeState.HasWinget
                ? "winget install --id 'Microsoft.DirectX' --exact --accept-package-agreements --accept-source-agreements"
                : null,
            IsInstalled = probeState.DirectXLegacyRuntimeInstalled,
            Priority = 60
        });

        return recommendations
            .OrderBy(static item => item.Priority)
            .ThenBy(static item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal InstallProbeState ProbeInstalledState()
    {
        var installedApps = GetInstalledDisplayNames();

        return new InstallProbeState
        {
            HasWinget = HasWingetAvailable(),
            VcRedistX64Installed = HasVcRedistInstalled(installedApps, isX64: true),
            VcRedistX86Installed = HasVcRedistInstalled(installedApps, isX64: false),
            DirectXLegacyRuntimeInstalled = HasDirectXLegacyRuntimeFiles(),
            IntelDsaInstalled = installedApps.Any(name =>
                name.Contains("Intel Driver & Support Assistant", StringComparison.OrdinalIgnoreCase))
        };
    }

    private static InstallRecommendation CreateVcRuntimeRecommendation(
        string id,
        string title,
        string reason,
        bool installed,
        bool hasWinget,
        string wingetId,
        int priority)
    {
        return new InstallRecommendation
        {
            Id = id,
            Category = "Runtime",
            Title = title,
            Description = "Microsoft Visual C++ redistributable.",
            Reason = reason,
            CurrentState = installed
                ? "Ready locally."
                : "Missing locally.",
            StatusLabel = installed ? "Ready" : hasWinget ? "Missing" : "Official source",
            SourceName = "Microsoft Learn",
            SourceUrl = MicrosoftVcRedistInfoUrl,
            InstallCommand = !installed && hasWinget
                ? $"winget install --id '{wingetId}' --exact --accept-package-agreements --accept-source-agreements"
                : null,
            IsInstalled = installed,
            Priority = priority
        };
    }

    private static List<string> GetInstalledDisplayNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (uninstallKey == null)
                    {
                        continue;
                    }

                    foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                    {
                        using var appKey = uninstallKey.OpenSubKey(subKeyName);
                        var displayName = appKey?.GetValue("DisplayName")?.ToString();
                        if (!string.IsNullOrWhiteSpace(displayName))
                        {
                            names.Add(displayName.Trim());
                        }
                    }
                }
                catch
                {
                    // Best-effort inventory only.
                }
            }
        }

        return names.ToList();
    }

    internal static bool HasVcRedistInstalled(IEnumerable<string> installedApps, bool isX64)
    {
        var bundleNames = isX64 ? VcRedistX64BundleNames : VcRedistX86BundleNames;
        var componentNames = isX64 ? VcRedistX64ComponentNames : VcRedistX86ComponentNames;

        if (ContainsAnyInstalledName(installedApps, bundleNames))
        {
            return true;
        }

        return componentNames.All(componentName =>
            installedApps.Any(installed =>
                installed.Contains(componentName, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool ContainsAnyInstalledName(IEnumerable<string> installedApps, IEnumerable<string> candidates)
    {
        return installedApps.Any(installed => candidates.Any(candidate =>
            installed.Contains(candidate, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasDirectXLegacyRuntimeFiles()
    {
        try
        {
            var systemDirectory = Environment.SystemDirectory;
            var wow64Directory = Path.Combine(
                Path.GetDirectoryName(systemDirectory) ?? systemDirectory,
                "SysWOW64");

            var requiredFiles = new[]
            {
                "d3dx9_43.dll",
                "d3dcompiler_43.dll",
                "xinput1_3.dll"
            };

            var systemFilesPresent = requiredFiles.All(file => File.Exists(Path.Combine(systemDirectory, file)));
            if (!Environment.Is64BitOperatingSystem)
            {
                return systemFilesPresent;
            }

            var wow64FilesPresent = requiredFiles.All(file => File.Exists(Path.Combine(wow64Directory, file)));
            return systemFilesPresent && wow64FilesPresent;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasWingetAvailable()
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathSegments = pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var segment in pathSegments)
        {
            try
            {
                if (File.Exists(Path.Combine(segment, "winget.exe")))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore malformed path segments.
            }
        }

        var localWindowsApps = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WindowsApps",
            "winget.exe");
        return File.Exists(localWindowsApps);
    }

    private static bool LooksLikeNvidiaGpu(string? value)
    {
        return ContainsAny(value, "nvidia", "geforce", "rtx", "gtx", "quadro");
    }

    private static bool LooksLikeAmdPlatform(string? cpuName, string? motherboardModel, string? motherboardChipset)
    {
        return ContainsAny(cpuName, "amd", "ryzen", "threadripper", "epyc") ||
               ContainsAny(motherboardChipset, "am4", "am5", "a620", "a520", "b450", "b550", "b650", "x570", "x670") ||
               ContainsAny(motherboardModel, "x570", "x670", "b450", "b550", "b650", "a620", "a520");
    }

    private static string? GetIntelHardwareReason(InstallRecommendationContext context)
    {
        var cpuReason = GetIntelCpuReason(context.CpuName);
        if (cpuReason != null)
        {
            return cpuReason;
        }

        var gpuReason = GetIntelGpuReason(context.GpuName);
        if (gpuReason != null)
        {
            return gpuReason;
        }

        return GetIntelNetworkReason(context.NetworkHints);
    }

    private static string? GetIntelCpuReason(string? cpuName)
    {
        if (!HasValue(cpuName))
        {
            return null;
        }

        if (ContainsAny(cpuName, "intel", "core ultra", "xeon", "pentium", "celeron"))
        {
            return $"CPU: {CompactHardwareName(cpuName)}";
        }

        return null;
    }

    private static string? GetIntelGpuReason(string? gpuName)
    {
        if (!HasValue(gpuName))
        {
            return null;
        }

        if (ContainsAny(gpuName, "intel", "arc", "iris", "uhd graphics"))
        {
            return $"GPU: {CompactHardwareName(gpuName)}";
        }

        return null;
    }

    private static string? GetIntelNetworkReason(IEnumerable<string> networkHints)
    {
        foreach (var networkHint in networkHints.Where(HasValue))
        {
            if (!ContainsAny(networkHint, "intel"))
            {
                continue;
            }

            if (ContainsAny(networkHint, "wi-fi", "wifi", "wireless", "bluetooth", "be200", "be202", "ax200", "ax210", "ax211"))
            {
                return $"Wireless: {CompactHardwareName(networkHint)}";
            }
        }

        return null;
    }

    private static string? CompactHardwareName(string? value)
    {
        if (!HasValue(value))
        {
            return null;
        }

        var compact = string.Join(
            " ",
            value!.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return compact.Length <= 72
            ? compact
            : $"{compact[..69]}...";
    }

    private static bool ContainsAny(string? value, params string[] fragments)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string JoinNonEmpty(params string?[] values)
    {
        return string.Join(" | ", values.Where(HasValue));
    }

    private static bool HasValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (HasValue(value))
            {
                return value!.Trim();
            }
        }

        return null;
    }
}

public sealed class InstallRecommendationContext
{
    public bool Is64BitOs { get; set; }
    public string? CpuName { get; set; }
    public string? MotherboardModel { get; set; }
    public string? MotherboardChipset { get; set; }
    public string? GpuName { get; set; }
    public string? GpuDriverVersion { get; set; }
    public string? GpuDriverDate { get; set; }
    public IReadOnlyList<string> NetworkHints { get; set; } = Array.Empty<string>();
}

public sealed class InstallProbeState
{
    public bool HasWinget { get; set; }
    public bool VcRedistX64Installed { get; set; }
    public bool VcRedistX86Installed { get; set; }
    public bool DirectXLegacyRuntimeInstalled { get; set; }
    public bool IntelDsaInstalled { get; set; }
}

public sealed class InstallRecommendation
{
    public string Id { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string CurrentState { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public string SourceUrl { get; init; } = string.Empty;
    public string? InstallCommand { get; init; }
    public bool IsInstalled { get; init; }
    public int Priority { get; init; }
}
