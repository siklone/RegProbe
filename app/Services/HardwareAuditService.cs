using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenTraceProject.App.Diagnostics;
using OpenTraceProject.App.HardwareDb;
using OpenTraceProject.App.Utilities;

namespace OpenTraceProject.App.Services;

public enum HardwareAuditSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public sealed class HardwareAuditIssue
{
    public string Section { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public HardwareAuditSeverity Severity { get; set; }
    public string RawValue { get; set; } = string.Empty;
}

public sealed class HardwareAuditComponent
{
    public string Section { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
    public string LookupValue { get; set; } = string.Empty;
    public string IconKey { get; set; } = string.Empty;
    public string IconSource { get; set; } = string.Empty;
    public string MatchLabel { get; set; } = string.Empty;
    public string MatchedModelName { get; set; } = string.Empty;
    public string MatchedBrand { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public List<HardwareAuditIssue> Issues { get; set; } = new();
}

public sealed class HardwareAuditReport
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public int Score { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public List<HardwareAuditComponent> Components { get; set; } = new();
    public List<HardwareAuditIssue> Issues { get; set; } = new();
}

public static class HardwareAuditHeuristics
{
    private static readonly string[] PlaceholderTokens =
    {
        "unknown",
        "loading",
        "to be filled by o e m",
        "to be filled by oem",
        "default string",
        "n a",
        "n/a",
        "generic",
        "not available"
    };

    public static bool IsPlaceholderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = HardwareNameNormalizer.Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        return PlaceholderTokens.Any(token => normalized.Contains(token, StringComparison.Ordinal));
    }

    public static bool IsGenericDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Contains("Generic PnP Monitor", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Default Monitor", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("PnP Monitor", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class HardwareAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private HardwareAuditService()
    {
    }

    public static HardwareAuditService Instance { get; } = new();

    public HardwareAuditReport CreateReport(HardwareDetailSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var components = new List<HardwareAuditComponent>
        {
            AuditOs(snapshot.Os),
            AuditCpu(snapshot.Cpu),
            AuditGpu(snapshot.Gpu),
            AuditMotherboard(snapshot.Motherboard),
            AuditMemory(snapshot.Memory),
            AuditStorage(snapshot.Storage),
            AuditDisplays(snapshot.Displays),
            AuditNetwork(snapshot.Network),
            AuditUsb(snapshot.Usb),
            AuditAudio(snapshot.Audio)
        };

        var issues = components.SelectMany(static component => component.Issues).ToList();
        var report = new HardwareAuditReport
        {
            Components = components,
            Issues = issues,
            ErrorCount = issues.Count(static issue => issue.Severity == HardwareAuditSeverity.Error),
            WarningCount = issues.Count(static issue => issue.Severity == HardwareAuditSeverity.Warning),
            InfoCount = issues.Count(static issue => issue.Severity == HardwareAuditSeverity.Info)
        };

        report.Score = Math.Clamp(100 - (report.ErrorCount * 18) - (report.WarningCount * 8) - (report.InfoCount * 2), 0, 100);
        return report;
    }

    public string SaveReport(HardwareAuditReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        Directory.CreateDirectory(ApplicationPaths.AuditLogDirectory);
        var filePath = Path.Combine(
            ApplicationPaths.AuditLogDirectory,
            $"hardware_audit_{report.GeneratedAtUtc:yyyyMMdd_HHmmss}.json");

        report.FilePath = filePath;
        File.WriteAllText(filePath, JsonSerializer.Serialize(report, JsonOptions));
        CleanupOldReports(25);
        AppDiagnostics.Log($"[HardwareAudit] Score={report.Score} Errors={report.ErrorCount} Warnings={report.WarningCount} Path='{filePath}'");
        return filePath;
    }

    private static void CleanupOldReports(int keepCount)
    {
        try
        {
            var files = Directory
                .EnumerateFiles(ApplicationPaths.AuditLogDirectory, "hardware_audit_*.json", SearchOption.TopDirectoryOnly)
                .Select(static path => new FileInfo(path))
                .OrderByDescending(static file => file.CreationTimeUtc)
                .ToList();

            foreach (var file in files.Skip(keepCount))
            {
                file.Delete();
            }
        }
        catch
        {
            // Audit report persistence must not break the UI.
        }
    }

    private static HardwareAuditComponent AuditOs(OsHardwareData os)
    {
        var raw = FirstNonEmpty(os.NormalizedName, os.ProductName);
        var component = CreateComponent("OS", HardwareType.Os, raw, raw);
        var isWindows11 = raw.Contains("Windows 11", StringComparison.OrdinalIgnoreCase);

        AddIf(component, string.IsNullOrWhiteSpace(os.ProductName), "ProductName", "OS product name was not resolved.", HardwareAuditSeverity.Error, os.ProductName);
        AddIf(component, os.BuildNumber <= 0, "BuildNumber", "OS build number is missing.", HardwareAuditSeverity.Warning, os.BuildNumber.ToString());
        AddIf(component, string.IsNullOrWhiteSpace(os.Architecture), "Architecture", "OS architecture is missing.", HardwareAuditSeverity.Warning, os.Architecture);
        AddIf(component, string.IsNullOrWhiteSpace(os.BiosMode), "BiosMode", "OS BIOS mode is missing.", HardwareAuditSeverity.Info, os.BiosMode);
        AddIf(component, string.IsNullOrWhiteSpace(os.SecureBootState), "SecureBootState", "Secure Boot state is missing.", isWindows11 ? HardwareAuditSeverity.Warning : HardwareAuditSeverity.Info, os.SecureBootState);
        AddIf(component, isWindows11 && string.IsNullOrWhiteSpace(os.TpmVersion), "TpmVersion", "TPM version is missing on a Windows 11 install.", HardwareAuditSeverity.Warning, os.TpmVersion);
        AddIf(component, string.IsNullOrWhiteSpace(os.BitLockerStatus), "BitLockerStatus", "BitLocker status is missing.", HardwareAuditSeverity.Info, os.BitLockerStatus);
        AddIf(component, string.IsNullOrWhiteSpace(os.DefenderStatus), "DefenderStatus", "Defender service status is missing.", HardwareAuditSeverity.Info, os.DefenderStatus);
        AddIf(component, string.IsNullOrWhiteSpace(os.FirewallStatus), "FirewallStatus", "Firewall service status is missing.", HardwareAuditSeverity.Info, os.FirewallStatus);

        return component;
    }

    private static HardwareAuditComponent AuditCpu(CpuHardwareData cpu)
    {
        var raw = cpu.Name ?? string.Empty;
        var component = CreateComponent("CPU", HardwareType.Cpu, raw, raw);

        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(cpu.Name), "Name", "CPU name is missing or generic.", HardwareAuditSeverity.Error, cpu.Name);
        AddIf(component, cpu.Cores <= 0, "Cores", "CPU core count is missing.", HardwareAuditSeverity.Error, cpu.Cores.ToString());
        AddIf(component, cpu.Threads < cpu.Cores, "Threads", "CPU logical thread count looks inconsistent.", HardwareAuditSeverity.Warning, cpu.Threads.ToString());
        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(cpu.Manufacturer), "Manufacturer", "CPU manufacturer is missing.", HardwareAuditSeverity.Warning, cpu.Manufacturer);
        AddIf(component, cpu.L2CacheKB <= 0 && cpu.L3CacheKB <= 0, "Cache", "CPU cache sizes were not detected.", HardwareAuditSeverity.Info, $"{cpu.L2CacheKB}/{cpu.L3CacheKB}");

        return component;
    }

    private static HardwareAuditComponent AuditGpu(GpuHardwareData gpu)
    {
        var raw = FirstNonEmpty(gpu.Name, gpu.VideoProcessor);
        var component = CreateComponent("GPU", HardwareType.Gpu, raw, HardwareIconService.BuildGpuLookupSeed(raw, gpu.Vendor, gpu.PnpDeviceId));

        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(gpu.Name), "Name", "GPU name is missing or generic.", HardwareAuditSeverity.Error, gpu.Name);
        AddIf(component, string.IsNullOrWhiteSpace(gpu.PnpDeviceId), "PnpDeviceId", "GPU PNP device id is missing.", HardwareAuditSeverity.Warning, gpu.PnpDeviceId);
        AddIf(component, gpu.AdapterRamBytes <= 0, "AdapterRamBytes", "GPU VRAM could not be read.", HardwareAuditSeverity.Warning, gpu.AdapterRamBytes.ToString());
        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(gpu.Vendor), "Vendor", "GPU vendor is missing.", HardwareAuditSeverity.Warning, gpu.Vendor);

        return component;
    }

    private static HardwareAuditComponent AuditMotherboard(MotherboardHardwareData board)
    {
        var raw = HardwareIconService.BuildMotherboardLookupSeed(board.Manufacturer, board.Product, board.Model, board.Version, board.BiosVendor, board.Chipset);
        var component = CreateComponent("Motherboard", HardwareType.Motherboard, FirstNonEmpty(board.Product, board.Model), raw);

        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(board.Manufacturer), "Manufacturer", "Motherboard manufacturer looks like a placeholder.", HardwareAuditSeverity.Error, board.Manufacturer);
        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(board.Product) && HardwareAuditHeuristics.IsPlaceholderValue(board.Model), "Model", "Motherboard product/model was not resolved.", HardwareAuditSeverity.Error, FirstNonEmpty(board.Product, board.Model));
        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(board.Chipset), "Chipset", "Motherboard chipset was not resolved.", HardwareAuditSeverity.Warning, board.Chipset);
        AddIf(component, string.IsNullOrWhiteSpace(board.Serial), "Serial", "Motherboard serial number is missing.", HardwareAuditSeverity.Info, board.Serial);

        return component;
    }

    private static HardwareAuditComponent AuditMemory(MemoryHardwareData memory)
    {
        var raw = $"{memory.PrimaryManufacturer} {memory.PrimaryModel} {memory.MemoryType}".Trim();
        var component = CreateComponent("Memory", HardwareType.Memory, raw, raw);

        AddIf(component, memory.TotalBytes <= 0, "TotalBytes", "Total installed memory is missing.", HardwareAuditSeverity.Error, memory.TotalBytes.ToString());
        AddIf(component, memory.ModuleCount <= 0, "ModuleCount", "No memory modules were resolved.", HardwareAuditSeverity.Warning, memory.ModuleCount.ToString());
        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(memory.MemoryType), "MemoryType", "Memory type is missing.", HardwareAuditSeverity.Warning, memory.MemoryType);
        AddIf(component, memory.Modules.Any(static module => HardwareAuditHeuristics.IsPlaceholderValue(module.PartNumber)), "PartNumber", "One or more DIMM part numbers are missing.", HardwareAuditSeverity.Info, memory.PrimaryModel);

        return component;
    }

    private static HardwareAuditComponent AuditStorage(StorageHardwareData storage)
    {
        var primaryDisk = storage.Disks.FirstOrDefault();
        var raw = primaryDisk?.Model ?? string.Empty;
        var component = CreateComponent("Storage", HardwareType.Storage, raw, raw);

        AddIf(component, storage.DeviceCount <= 0 || primaryDisk == null, "DeviceCount", "No physical storage device was resolved.", HardwareAuditSeverity.Error, storage.DeviceCount.ToString());
        AddIf(component, primaryDisk != null && HardwareAuditHeuristics.IsPlaceholderValue(primaryDisk.Model), "Model", "Primary storage model is missing.", HardwareAuditSeverity.Warning, primaryDisk?.Model);
        AddIf(component, primaryDisk != null && primaryDisk.SizeBytes <= 0, "SizeBytes", "Primary storage size is missing.", HardwareAuditSeverity.Warning, primaryDisk?.SizeBytes.ToString());
        AddIf(component, storage.Disks.Any(static disk => string.IsNullOrWhiteSpace(disk.InterfaceType)), "InterfaceType", "One or more storage interfaces are missing.", HardwareAuditSeverity.Info, primaryDisk?.InterfaceType);

        return component;
    }

    private static HardwareAuditComponent AuditDisplays(DisplayHardwareData displays)
    {
        var primaryDisplay = displays.Devices.FirstOrDefault(static device => device.IsPrimary) ?? displays.Devices.FirstOrDefault();
        var raw = primaryDisplay?.Name ?? displays.PrimaryMonitorName ?? string.Empty;
        var lookup = primaryDisplay?.IconLookupSeed ?? raw;
        var component = CreateComponent("Displays", HardwareType.Display, raw, lookup);

        AddIf(component, displays.DisplayCount <= 0 || primaryDisplay == null, "DisplayCount", "No active display was resolved.", HardwareAuditSeverity.Error, displays.DisplayCount.ToString());
        AddIf(component, primaryDisplay != null && HardwareAuditHeuristics.IsGenericDisplayName(primaryDisplay.Name), "Name", "Primary display is still using a generic monitor name.", HardwareAuditSeverity.Warning, primaryDisplay?.Name);
        AddIf(component, primaryDisplay != null && string.IsNullOrWhiteSpace(primaryDisplay.MatchMode), "MatchMode", "Primary display match mode is missing.", HardwareAuditSeverity.Warning, primaryDisplay?.MatchMode);
        AddIf(component, displays.Devices.Any(static device => string.Equals(device.MatchMode, "PrefixAmbiguous", StringComparison.OrdinalIgnoreCase)), "MatchMode", "One or more displays still rely on an ambiguous prefix match.", HardwareAuditSeverity.Warning, string.Join(", ", displays.Devices.Where(static device => string.Equals(device.MatchMode, "PrefixAmbiguous", StringComparison.OrdinalIgnoreCase)).Select(static device => device.Name)));
        AddIf(component, primaryDisplay != null && string.IsNullOrWhiteSpace(primaryDisplay.ConnectionType), "ConnectionType", "Primary display connection type is missing.", HardwareAuditSeverity.Info, primaryDisplay?.ConnectionType);

        return component;
    }

    private static HardwareAuditComponent AuditNetwork(NetworkHardwareData network)
    {
        var raw = FirstNonEmpty(network.PrimaryAdapterDescription, network.PrimaryAdapterName);
        var component = CreateComponent("Network", HardwareType.Network, raw, raw);
        var primaryAdapter = network.Adapters.FirstOrDefault(static adapter => adapter.IsPrimary)
            ?? network.Adapters.FirstOrDefault(adapter =>
                string.Equals(adapter.Name, network.PrimaryAdapterName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(adapter.Description, network.PrimaryAdapterDescription, StringComparison.OrdinalIgnoreCase))
            ?? HardwarePreloadService.ChoosePrimaryNetworkAdapter(network.Adapters);

        AddIf(component, network.AdapterCount <= 0, "AdapterCount", "No network adapters were resolved.", HardwareAuditSeverity.Error, network.AdapterCount.ToString());
        AddIf(component, network.AdapterUpCount <= 0, "AdapterUpCount", "No active network adapter is currently up.", HardwareAuditSeverity.Warning, network.AdapterUpCount.ToString());
        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(network.PrimaryAdapterDescription) && HardwareAuditHeuristics.IsPlaceholderValue(network.PrimaryAdapterName), "PrimaryAdapter", "Primary network adapter description is missing.", HardwareAuditSeverity.Warning, raw);
        AddIf(component, string.IsNullOrWhiteSpace(network.PrimaryLinkSpeed), "PrimaryLinkSpeed", "Primary network link speed is missing.", HardwareAuditSeverity.Info, network.PrimaryLinkSpeed);
        AddIf(
            component,
            primaryAdapter != null &&
            IsLikelyVirtualNetworkAdapter(primaryAdapter) &&
            network.Adapters.Any(adapter => !ReferenceEquals(adapter, primaryAdapter) && IsConnectedPhysicalNetworkAdapter(adapter)),
            "PrimaryAdapter",
            "Primary network adapter is virtual while a physical connected adapter is available.",
            HardwareAuditSeverity.Warning,
            raw);

        return component;
    }

    private static HardwareAuditComponent AuditUsb(UsbHardwareData usb)
    {
        var raw = FirstNonEmpty(usb.PrimaryControllerName, usb.PrimaryUsbDeviceName);
        var component = CreateComponent("USB", HardwareType.Usb, raw, raw);

        AddIf(component, usb.UsbControllerCount <= 0, "UsbControllerCount", "No USB controller was resolved.", HardwareAuditSeverity.Info, usb.UsbControllerCount.ToString());
        AddIf(component, usb.UsbDeviceCount <= 0, "UsbDeviceCount", "No USB devices were resolved.", HardwareAuditSeverity.Info, usb.UsbDeviceCount.ToString());
        AddIf(component, HardwareAuditHeuristics.IsPlaceholderValue(usb.PrimaryControllerName), "PrimaryControllerName", "Primary USB controller name is missing.", HardwareAuditSeverity.Info, usb.PrimaryControllerName);

        return component;
    }

    private static HardwareAuditComponent AuditAudio(AudioHardwareData audio)
    {
        var raw = audio.PrimaryDeviceName ?? string.Empty;
        var component = CreateComponent("Audio", HardwareType.Audio, raw, raw);

        AddIf(component, audio.DeviceCount <= 0, "DeviceCount", "No audio devices were resolved.", HardwareAuditSeverity.Info, audio.DeviceCount.ToString());
        AddIf(component, audio.DeviceCount > 0 && HardwareAuditHeuristics.IsPlaceholderValue(audio.PrimaryDeviceName), "PrimaryDeviceName", "Primary audio device name is missing.", HardwareAuditSeverity.Info, audio.PrimaryDeviceName);
        AddIf(component, audio.DeviceCount > 0 && string.IsNullOrWhiteSpace(audio.PrimaryStatus), "PrimaryStatus", "Primary audio device status is missing.", HardwareAuditSeverity.Info, audio.PrimaryStatus);
        AddIf(
            component,
            audio.DeviceCount > 1 &&
            AudioDetectionHelpers.IsLikelyVirtualDevice(audio.PrimaryDeviceName, audio.PrimaryManufacturer) &&
            audio.AllDevices.Any(static deviceName => !AudioDetectionHelpers.IsLikelyVirtualDevice(deviceName)),
            "PrimaryDeviceName",
            "Primary audio device resolved to a virtual endpoint while physical devices are available.",
            HardwareAuditSeverity.Warning,
            audio.PrimaryDeviceName);

        return component;
    }

    private static HardwareAuditComponent CreateComponent(string section, HardwareType type, string? rawValue, string? lookupValue)
    {
        var raw = rawValue?.Trim() ?? string.Empty;
        var lookup = string.IsNullOrWhiteSpace(lookupValue) ? raw : lookupValue!.Trim();
        var resolution = HardwareIconService.ResolveResult(type, lookup);
        var component = new HardwareAuditComponent
        {
            Section = section,
            RawValue = raw,
            LookupValue = lookup,
            IconKey = resolution.IconKey,
            IconSource = resolution.SourceLabel,
            MatchLabel = resolution.MatchLabel,
            MatchedModelName = resolution.MatchedModel?.ModelName ?? string.Empty,
            MatchedBrand = resolution.MatchedModel?.Brand ?? string.Empty,
            ConfidenceScore = GetConfidenceScore(resolution)
        };

        if (resolution.Source == HardwareIconResolutionSource.Fallback && type != HardwareType.Audio)
        {
            component.Issues.Add(new HardwareAuditIssue
            {
                Section = section,
                Field = "IconKey",
                Message = "Icon resolution fell back to the category default.",
                Severity = HardwareAuditSeverity.Warning,
                RawValue = raw
            });
        }
        return component;
    }

    private static void AddIf(HardwareAuditComponent component, bool condition, string field, string message, HardwareAuditSeverity severity, string? rawValue)
    {
        if (!condition)
        {
            return;
        }

        component.Issues.Add(new HardwareAuditIssue
        {
            Section = component.Section,
            Field = field,
            Message = message,
            Severity = severity,
            RawValue = rawValue?.Trim() ?? string.Empty
        });
    }

    private static int GetConfidenceScore(HardwareIconResolutionResult resolution)
    {
        return resolution.Source switch
        {
            HardwareIconResolutionSource.DatabaseModel when resolution.MatchKind is HardwareMatchKind.ExactName or HardwareMatchKind.ExactAlias => 96,
            HardwareIconResolutionSource.DatabaseModel => 88,
            HardwareIconResolutionSource.ExplicitKey => 90,
            HardwareIconResolutionSource.RuleMap => 72,
            _ => 35
        };
    }

    private static bool IsConnectedPhysicalNetworkAdapter(NetworkAdapterData adapter)
    {
        return !IsLikelyVirtualNetworkAdapter(adapter) &&
               (string.Equals(adapter.Status, "Up", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(adapter.Status, "Connected", StringComparison.OrdinalIgnoreCase)) &&
               (!string.IsNullOrWhiteSpace(adapter.Gateway) || !string.IsNullOrWhiteSpace(adapter.Ipv4));
    }

    private static bool IsLikelyVirtualNetworkAdapter(NetworkAdapterData adapter)
    {
        var joined = $"{adapter.Name} {adapter.Description} {adapter.AdapterType}".Trim();
        if (string.IsNullOrWhiteSpace(joined))
        {
            return false;
        }

        var value = joined.ToLowerInvariant();
        return value.Contains("wireguard") ||
               value.Contains("wintun") ||
               value.Contains("tailscale") ||
               value.Contains("zerotier") ||
               value.Contains("hamachi") ||
               value.Contains("tap-windows") ||
               value.Contains("vpn") ||
               value.Contains("virtual") ||
               value.Contains("hyper-v") ||
               value.Contains("vmware") ||
               value.Contains("tunnel") ||
               value.Contains("loopback");
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
