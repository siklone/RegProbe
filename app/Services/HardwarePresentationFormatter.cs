using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenTraceProject.App.Services;

public static class HardwarePresentationFormatter
{
    public static string BuildNormalizedOsName(int buildNumber, string? edition, string? displayVersion, string? releaseId)
    {
        var osBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var normalizedEdition = NormalizeEditionLabel(edition);
        var normalized = string.IsNullOrWhiteSpace(normalizedEdition)
            ? osBase
            : $"{osBase} {normalizedEdition}";
        var version = !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion.Trim() : releaseId?.Trim();
        return string.IsNullOrWhiteSpace(version) ? normalized : $"{normalized} ({version})";
    }

    public static string BuildDisplayProductName(string? productName, int buildNumber, string? edition)
    {
        var normalizedEdition = NormalizeEditionLabel(edition);
        var normalizedBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var fallbackName = string.IsNullOrWhiteSpace(normalizedEdition)
            ? normalizedBase
            : $"{normalizedBase} {normalizedEdition}";

        var canonicalName = CanonicalizeWindowsProductName(productName, buildNumber, normalizedEdition);
        return string.IsNullOrWhiteSpace(canonicalName) ? fallbackName : canonicalName;
    }

    public static bool ShouldShowRegistryProductName(string? productName, string? displayProductName, int buildNumber, string? edition)
    {
        if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(displayProductName))
        {
            return false;
        }

        var canonicalRegistryName = CanonicalizeWindowsProductName(productName, buildNumber, NormalizeEditionLabel(edition));
        return !string.Equals(canonicalRegistryName, displayProductName.Trim(), StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(productName.Trim(), displayProductName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeEditionLabel(string? edition)
    {
        if (string.IsNullOrWhiteSpace(edition))
        {
            return string.Empty;
        }

        return edition.Trim() switch
        {
            "Professional" => "Pro",
            "Core" => "Home",
            "CoreSingleLanguage" => "Home Single Language",
            "EnterpriseS" => "Enterprise LTSC",
            _ => edition.Trim()
        };
    }

    public static string? InferVramType(string? gpuName, string? vendorId)
    {
        if (string.IsNullOrWhiteSpace(gpuName))
        {
            return null;
        }

        var name = gpuName.ToUpperInvariant();

        if (name.Contains("RTX 50") ||
            name.Contains("RTX 5090") ||
            name.Contains("RTX 5080") ||
            name.Contains("RTX 5070") ||
            name.Contains("RTX 5060"))
        {
            return "GDDR7";
        }

        if (name.Contains("RTX 40") ||
            name.Contains("RTX 4090") ||
            name.Contains("RTX 4080") ||
            name.Contains("RTX 4070 TI") ||
            name.Contains("RTX 3090") ||
            name.Contains("RTX 3080"))
        {
            return "GDDR6X";
        }

        if (name.Contains("RTX") ||
            name.Contains("GTX 16") ||
            name.Contains("GTX 1660") ||
            name.Contains("RX 7") ||
            name.Contains("RX 6") ||
            name.Contains("RX 5") ||
            name.Contains("ARC"))
        {
            return "GDDR6";
        }

        if (name.Contains("GTX 10") || name.Contains("GTX 1080") || name.Contains("GTX 1070"))
        {
            return "GDDR5X";
        }

        if (name.Contains("VEGA") || name.Contains("RX 590") || name.Contains("RX 580"))
        {
            return "HBM2";
        }

        if (name.Contains("IRIS") ||
            name.Contains("UHD") ||
            name.Contains("HD GRAPHICS") ||
            string.Equals(vendorId, "8086", StringComparison.OrdinalIgnoreCase) && !name.Contains("ARC"))
        {
            return "Shared (DDR)";
        }

        return null;
    }

    public static string BuildCompactCpuTitle(string? processorName)
    {
        if (string.IsNullOrWhiteSpace(processorName))
        {
            return string.Empty;
        }

        var compact = Regex.Replace(processorName.Trim(), @"\s+", " ");
        compact = Regex.Replace(compact, @"\s+\d+\-Core Processor\b", string.Empty, RegexOptions.IgnoreCase);
        compact = Regex.Replace(compact, @"\s+Processor\b", string.Empty, RegexOptions.IgnoreCase);
        compact = Regex.Replace(compact, @"\s+CPU\b", string.Empty, RegexOptions.IgnoreCase);
        compact = compact.Trim();
        return string.IsNullOrWhiteSpace(compact) ? processorName.Trim() : compact;
    }

    public static string BuildCompactOsTitle(string? osName)
    {
        if (string.IsNullOrWhiteSpace(osName))
        {
            return string.Empty;
        }

        var compact = Regex.Replace(osName.Trim(), @"\s+", " ");
        compact = Regex.Replace(compact, @"^Microsoft\s+", string.Empty, RegexOptions.IgnoreCase);
        compact = Regex.Replace(compact, @"\s*\([^)]*\)", string.Empty);
        compact = compact.Trim();
        return string.IsNullOrWhiteSpace(compact) ? osName.Trim() : compact;
    }

    public static string BuildCompactGpuTitle(string? gpuName)
    {
        if (string.IsNullOrWhiteSpace(gpuName))
        {
            return string.Empty;
        }

        var compact = Regex.Replace(gpuName.Trim(), @"\s+", " ");
        compact = Regex.Replace(compact, @"\bNVIDIA\s+GeForce\s+", string.Empty, RegexOptions.IgnoreCase);
        compact = Regex.Replace(compact, @"\bNVIDIA\s+", string.Empty, RegexOptions.IgnoreCase);
        compact = Regex.Replace(compact, @"\bAMD\s+Radeon\s+", string.Empty, RegexOptions.IgnoreCase);
        compact = Regex.Replace(compact, @"\bAMD\s+", string.Empty, RegexOptions.IgnoreCase);
        compact = Regex.Replace(compact, @"\bIntel\s+Arc\s+", "Arc ", RegexOptions.IgnoreCase);

        var nvidiaMatch = Regex.Match(compact, @"\b(RTX|GTX)\s+\d{3,4}[A-Z]*\b", RegexOptions.IgnoreCase);
        if (nvidiaMatch.Success)
        {
            return nvidiaMatch.Value.ToUpperInvariant();
        }

        var amdMatch = Regex.Match(compact, @"\bRX\s+\d{3,4}\s*[A-Z]*\b", RegexOptions.IgnoreCase);
        if (amdMatch.Success)
        {
            return Regex.Replace(amdMatch.Value.ToUpperInvariant(), @"\s+", " ").Trim();
        }

        return compact;
    }

    public static string BuildStorageInterfaceSummary(string? interfaceType, string? mediaType, bool isExternal = false)
    {
        if (isExternal ||
            (!string.IsNullOrWhiteSpace(interfaceType) &&
             interfaceType.Contains("USB", StringComparison.OrdinalIgnoreCase)))
        {
            return "USB";
        }

        if (!string.IsNullOrWhiteSpace(mediaType) &&
            mediaType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
        {
            return "NVMe";
        }

        if (!string.IsNullOrWhiteSpace(interfaceType))
        {
            if (interfaceType.Contains("SATA", StringComparison.OrdinalIgnoreCase) ||
                interfaceType.Contains("IDE", StringComparison.OrdinalIgnoreCase))
            {
                return "SATA";
            }

            if (interfaceType.Contains("SCSI", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(mediaType) &&
                mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase))
            {
                return "NVMe";
            }

            return interfaceType.Trim();
        }

        return string.IsNullOrWhiteSpace(mediaType) ? string.Empty : mediaType.Trim();
    }

    public static string ClassifyUsbDeviceCategory(
        string? name,
        string? className,
        string? service,
        bool isController,
        bool isHub)
    {
        if (isController)
        {
            return "Controller";
        }

        if (isHub)
        {
            return "Hub";
        }

        var value = $"{name} {className} {service}".Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Peripheral";
        }

        if (value.Contains("AUDIO", StringComparison.Ordinal) ||
            value.Contains("SOUND", StringComparison.Ordinal) ||
            value.Contains("HEADSET", StringComparison.Ordinal) ||
            value.Contains("MIC", StringComparison.Ordinal) ||
            value.Contains("SPEAKER", StringComparison.Ordinal) ||
            value.Contains("DAC", StringComparison.Ordinal))
        {
            return "Audio";
        }

        if (value.Contains("KEYBOARD", StringComparison.Ordinal) ||
            value.Contains("MOUSE", StringComparison.Ordinal) ||
            value.Contains("HID", StringComparison.Ordinal) ||
            value.Contains("RECEIVER", StringComparison.Ordinal) ||
            value.Contains("GAMEPAD", StringComparison.Ordinal) ||
            value.Contains("CONTROLLER", StringComparison.Ordinal))
        {
            return "Input";
        }

        if (value.Contains("STORAGE", StringComparison.Ordinal) ||
            value.Contains("DISK", StringComparison.Ordinal) ||
            value.Contains("FLASH", StringComparison.Ordinal) ||
            value.Contains("CARD READER", StringComparison.Ordinal) ||
            value.Contains("MASS STORAGE", StringComparison.Ordinal))
        {
            return "Storage";
        }

        if (value.Contains("NETWORK", StringComparison.Ordinal) ||
            value.Contains("LAN", StringComparison.Ordinal) ||
            value.Contains("WIFI", StringComparison.Ordinal) ||
            value.Contains("WIRELESS", StringComparison.Ordinal) ||
            value.Contains("BLUETOOTH", StringComparison.Ordinal))
        {
            return "Connectivity";
        }

        return "Peripheral";
    }

    public static string? BuildAudioDriverSummary(string? provider, string? version)
    {
        var normalizedProvider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        var normalizedVersion = string.IsNullOrWhiteSpace(version) ? null : version.Trim();

        if (normalizedProvider == null && normalizedVersion == null)
        {
            return null;
        }

        if (normalizedProvider != null && normalizedVersion != null)
        {
            return $"{normalizedProvider} {normalizedVersion}";
        }

        return normalizedProvider ?? normalizedVersion;
    }

    public static string? BuildMemoryPopulationSummary(int moduleCount, int totalSlotCount)
    {
        if (moduleCount <= 0 && totalSlotCount <= 0)
        {
            return null;
        }

        if (moduleCount > 0 && totalSlotCount > 0)
        {
            return $"{moduleCount} / {totalSlotCount} slots occupied";
        }

        if (totalSlotCount > 0)
        {
            return $"{totalSlotCount} slots";
        }

        return moduleCount == 1 ? "1 module" : $"{moduleCount} modules";
    }

    public static string? BuildDisplayOccupancySummary(int activeDisplayCount, int primaryDisplayCount)
    {
        if (activeDisplayCount <= 0 && primaryDisplayCount <= 0)
        {
            return null;
        }

        var segments = new List<string>();
        if (activeDisplayCount > 0)
        {
            segments.Add($"{activeDisplayCount} active");
        }

        if (primaryDisplayCount > 0)
        {
            segments.Add($"{primaryDisplayCount} primary");
        }

        return segments.Count == 0 ? null : string.Join(" Â· ", segments);
    }

    public static string? BuildStorageOccupancySummary(int totalDriveCount, int systemDriveCount, int externalDriveCount)
    {
        if (totalDriveCount <= 0 && systemDriveCount <= 0 && externalDriveCount <= 0)
        {
            return null;
        }

        var segments = new List<string>();
        if (systemDriveCount > 0)
        {
            segments.Add($"{systemDriveCount} system");
        }

        var secondaryDriveCount = Math.Max(0, totalDriveCount - Math.Max(systemDriveCount, 0));
        if (secondaryDriveCount > 0)
        {
            segments.Add($"{secondaryDriveCount} secondary");
        }

        if (externalDriveCount > 0)
        {
            segments.Add($"{externalDriveCount} external");
        }

        if (segments.Count == 0 && totalDriveCount > 0)
        {
            segments.Add(totalDriveCount == 1 ? "1 drive" : $"{totalDriveCount} drives");
        }

        return segments.Count == 0 ? null : string.Join(" Â· ", segments);
    }

    public static string? BuildUsbOccupancySummary(int controllerCount, int hubCount, int deviceCount)
    {
        if (controllerCount <= 0 && hubCount <= 0 && deviceCount <= 0)
        {
            return null;
        }

        var segments = new List<string>();
        if (controllerCount > 0)
        {
            segments.Add(controllerCount == 1 ? "1 controller" : $"{controllerCount} controllers");
        }

        if (hubCount > 0)
        {
            segments.Add(hubCount == 1 ? "1 hub" : $"{hubCount} hubs");
        }

        if (deviceCount > 0)
        {
            segments.Add(deviceCount == 1 ? "1 device" : $"{deviceCount} devices");
        }

        return segments.Count == 0 ? null : string.Join(" Â· ", segments);
    }

    public static string BuildMemoryModuleHeader(MemoryModuleData module, IReadOnlyList<MemoryModuleData> modules, int index)
    {
        var slotLabel = NormalizeMemorySlotLabel(module.Slot);
        var bankLabel = FormatMemoryBankLabel(module.BankLabel);
        var hasDuplicateSlot = !string.IsNullOrWhiteSpace(slotLabel) &&
                               modules.Count(other => string.Equals(
                                   NormalizeMemorySlotLabel(other.Slot),
                                   slotLabel,
                                   StringComparison.OrdinalIgnoreCase)) > 1;

        if (hasDuplicateSlot && !string.IsNullOrWhiteSpace(bankLabel))
        {
            return $"{bankLabel} / {slotLabel}";
        }

        if (!string.IsNullOrWhiteSpace(slotLabel))
        {
            return slotLabel;
        }

        if (!string.IsNullOrWhiteSpace(bankLabel))
        {
            return bankLabel;
        }

        return $"Module {index + 1}";
    }

    public static string? FormatMemoryBankLabel(string? bankLabel)
    {
        if (string.IsNullOrWhiteSpace(bankLabel))
        {
            return null;
        }

        var compact = Regex.Replace(bankLabel.Trim(), @"\s+", " ");
        var channelMatch = Regex.Match(compact, @"CHANNEL\s+([A-Z0-9]+)", RegexOptions.IgnoreCase);
        if (channelMatch.Success)
        {
            return $"Channel {channelMatch.Groups[1].Value.ToUpperInvariant()}";
        }

        return compact;
    }

    private static string CanonicalizeWindowsProductName(string? productName, int buildNumber, string normalizedEdition)
    {
        var normalizedBase = buildNumber >= 22000 ? "Windows 11" : "Windows 10";
        var candidate = Regex.Replace(productName?.Trim() ?? string.Empty, @"\s+", " ");
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return string.Empty;
        }

        candidate = buildNumber >= 22000
            ? Regex.Replace(candidate, @"Windows\s+10", "Windows 11", RegexOptions.IgnoreCase)
            : buildNumber > 0 && buildNumber < 22000
                ? Regex.Replace(candidate, @"Windows\s+11", "Windows 10", RegexOptions.IgnoreCase)
                : candidate;

        candidate = Regex.Replace(candidate, @"\bProfessional\b", "Pro", RegexOptions.IgnoreCase);
        candidate = Regex.Replace(candidate, @"\bCoreSingleLanguage\b", "Home Single Language", RegexOptions.IgnoreCase);
        candidate = Regex.Replace(candidate, @"\bCore\b", "Home", RegexOptions.IgnoreCase);
        candidate = Regex.Replace(candidate, @"\bEnterpriseS\b", "Enterprise LTSC", RegexOptions.IgnoreCase);
        candidate = Regex.Replace(candidate, @"\s+", " ").Trim();

        if (!string.IsNullOrWhiteSpace(normalizedEdition) &&
            candidate.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase) &&
            !candidate.Contains(normalizedEdition, StringComparison.OrdinalIgnoreCase))
        {
            candidate = $"{normalizedBase} {normalizedEdition}";
        }

        return candidate;
    }

    private static string NormalizeMemorySlotLabel(string? slot)
    {
        if (string.IsNullOrWhiteSpace(slot))
        {
            return string.Empty;
        }

        var compact = Regex.Replace(slot.Trim(), @"\s+", " ");
        compact = Regex.Replace(compact, @"\b(DIMM|SLOT|BANK)(\d+)\b", "$1 $2", RegexOptions.IgnoreCase);
        return compact;
    }
}
