using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OpenTraceProject.App.ViewModels;

internal static class DashboardInfoHelpers
{
    internal readonly record struct EdidInfo(
        string ManufacturerId,
        string ProductCodeHex,
        string SerialNumber,
        string MonitorName,
        uint SerialNumberValue,
        int? HorizontalSizeCm,
        int? VerticalSizeCm,
        int? MinVerticalHz,
        int? MaxVerticalHz,
        int? MinHorizontalKHz,
        int? MaxHorizontalKHz,
        int? MaxPixelClockMHz);

    internal static long SumPageFileBytes(IEnumerable<long> sizes)
    {
        if (sizes == null)
        {
            return 0;
        }

        long total = 0;
        foreach (var size in sizes)
        {
            if (size <= 0)
            {
                continue;
            }

            try
            {
                checked
                {
                    total += size;
                }
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }

        return total;
    }

    internal static int? SumPositiveInts(IEnumerable<int?> values)
    {
        if (values == null)
        {
            return null;
        }

        var total = 0;
        var hasValue = false;
        foreach (var value in values)
        {
            if (!value.HasValue || value.Value <= 0)
            {
                continue;
            }

            total += value.Value;
            hasValue = true;
        }

        return hasValue ? total : null;
    }

    internal static string ResolveDiskType(
        IReadOnlyDictionary<uint, (int mediaType, int busType)> msftDisks,
        uint? diskIndex,
        string model,
        string interfaceType)
    {
        if (!string.IsNullOrEmpty(interfaceType) &&
            interfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
        {
            return "NVMe SSD";
        }

        if (!string.IsNullOrEmpty(model) &&
            model.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
        {
            return "NVMe SSD";
        }

        if (diskIndex.HasValue &&
            msftDisks != null &&
            msftDisks.TryGetValue(diskIndex.Value, out var info))
        {
            return info.mediaType switch
            {
                3 => "HDD",
                4 when info.busType == 17 => "NVMe SSD",
                4 => "SATA SSD",
                _ => "Unknown"
            };
        }

        if (!string.IsNullOrEmpty(model) &&
            model.Contains("SSD", StringComparison.OrdinalIgnoreCase))
        {
            return "SSD";
        }

        return "HDD";
    }

    internal static string NormalizeMonitorInstanceId(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return string.Empty;
        }

        var trimmed = deviceId.Trim();
        if (trimmed.StartsWith("DISPLAY\\", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("MONITOR\\", StringComparison.OrdinalIgnoreCase))
        {
            return "DISPLAY\\" + trimmed.Substring("MONITOR\\".Length);
        }

        return trimmed;
    }

    internal static EdidInfo ParseEdid(byte[]? edid)
    {
        if (edid == null || edid.Length < 128)
        {
            return default;
        }

        var manufacturer = DecodeEdidManufacturerId(edid[8], edid[9]);
        var productCode = DecodeEdidProductCode(edid[10], edid[11]);
        var serialValue = DecodeEdidSerialNumberValue(edid[12], edid[13], edid[14], edid[15]);
        var serialText = GetEdidDescriptorString(edid, 0xFF);
        var monitorName = GetEdidDescriptorString(edid, 0xFC);
        var horizontalSizeCm = edid[21] > 0 ? (int?)edid[21] : null;
        var verticalSizeCm = edid[22] > 0 ? (int?)edid[22] : null;
        var range = GetEdidRangeLimits(edid);

        return new EdidInfo(
            manufacturer,
            productCode,
            serialText ?? string.Empty,
            monitorName ?? string.Empty,
            serialValue,
            horizontalSizeCm,
            verticalSizeCm,
            range.MinVerticalHz,
            range.MaxVerticalHz,
            range.MinHorizontalKHz,
            range.MaxHorizontalKHz,
            range.MaxPixelClockMHz);
    }

    internal static string DecodeWmiString(ushort[]? data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        var chars = data.TakeWhile(c => c != 0).Select(c => (char)c).ToArray();
        var text = new string(chars).Trim();
        return text;
    }

    internal static string DecodeWmiProductCode(ushort[]? data)
    {
        if (data == null || data.Length < 2)
        {
            return string.Empty;
        }

        var low = (byte)(data[0] & 0xFF);
        var high = (byte)(data[1] & 0xFF);
        var code = (ushort)(low | (high << 8));
        return code == 0 ? string.Empty : code.ToString("X4", CultureInfo.InvariantCulture);
    }

    internal static IReadOnlyList<(string Key, string Mode)> GetEdidMatchCandidates(
        string manufacturerId,
        string productCodeHex,
        string serialNumberText,
        uint serialNumberValue)
    {
        var info = new EdidInfo(
            manufacturerId ?? string.Empty,
            productCodeHex ?? string.Empty,
            serialNumberText ?? string.Empty,
            string.Empty,
            serialNumberValue,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        return GetEdidMatchCandidates(info);
    }

    internal static IReadOnlyList<(string Key, string Mode)> GetEdidMatchCandidates(EdidInfo info)
    {
        var candidates = new List<(string Key, string Mode)>();
        var mfg = info.ManufacturerId?.Trim();
        var prod = info.ProductCodeHex?.Trim();
        if (string.IsNullOrWhiteSpace(mfg) || string.IsNullOrWhiteSpace(prod))
        {
            return candidates;
        }

        if (!string.IsNullOrWhiteSpace(info.SerialNumber))
        {
            AddCandidate(candidates, BuildEdidKey(mfg, prod, info.SerialNumber.Trim()), "EdidSerialText");
        }
        else if (info.SerialNumberValue > 0)
        {
            AddCandidate(candidates, BuildEdidKey(mfg, prod, info.SerialNumberValue.ToString(CultureInfo.InvariantCulture)), "EdidSerialValue");
        }

        if (!string.IsNullOrWhiteSpace(info.MonitorName))
        {
            AddCandidate(candidates, BuildEdidKey(mfg, prod, NormalizeEdidToken(info.MonitorName)), "EdidName");
        }

        if (info.HorizontalSizeCm.HasValue && info.VerticalSizeCm.HasValue)
        {
            var sizeToken = $"{info.HorizontalSizeCm.Value}x{info.VerticalSizeCm.Value}";
            AddCandidate(candidates, BuildEdidKey(mfg, prod, $"SIZE:{sizeToken}"), "EdidSize");
        }

        if (info.MinVerticalHz.HasValue && info.MaxVerticalHz.HasValue &&
            info.MinHorizontalKHz.HasValue && info.MaxHorizontalKHz.HasValue &&
            info.MaxPixelClockMHz.HasValue)
        {
            var rangeToken =
                $"RANGE:V{info.MinVerticalHz.Value}-{info.MaxVerticalHz.Value}" +
                $"H{info.MinHorizontalKHz.Value}-{info.MaxHorizontalKHz.Value}" +
                $"P{info.MaxPixelClockMHz.Value}";
            AddCandidate(candidates, BuildEdidKey(mfg, prod, rangeToken), "EdidRange");
        }

        AddCandidate(candidates, BuildEdidKey(mfg, prod, null), "EdidMfgProduct");
        return candidates;
    }

    internal static string TrimMonitorInstanceSuffix(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return string.Empty;
        }

        var trimmed = instanceId.Trim();
        if (trimmed.EndsWith("_0", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[..^2];
        }

        return trimmed;
    }

    internal static IReadOnlyList<(string Key, string Mode)> GetMonitorMatchCandidates(string? instanceId)
    {
        var normalized = NormalizeMonitorInstanceId(instanceId);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<(string, string)>();
        }

        var candidates = new List<(string Key, string Mode)>();
        AddCandidate(candidates, normalized, "Exact");

        var trimmed = TrimMonitorInstanceSuffix(normalized);
        if (!string.Equals(trimmed, normalized, StringComparison.OrdinalIgnoreCase))
        {
            AddCandidate(candidates, trimmed, "TrimSuffix");
        }

        if (!normalized.EndsWith("_0", StringComparison.OrdinalIgnoreCase))
        {
            AddCandidate(candidates, normalized + "_0", "AppendSuffix");
        }

        if (!trimmed.EndsWith("_0", StringComparison.OrdinalIgnoreCase))
        {
            AddCandidate(candidates, trimmed + "_0", "TrimSuffixAppend");
        }

        return candidates;
    }

    internal static string? GetMonitorInstancePrefix(string? instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return null;
        }

        var trimmed = instanceId.Trim();
        var parts = trimmed.Split('\\');
        if (parts.Length >= 2)
        {
            return $"{parts[0]}\\{parts[1]}";
        }

        return trimmed;
    }

    internal static string DecodeEdidManufacturerId(byte high, byte low)
    {
        var raw = (ushort)((high << 8) | low);
        var c1 = (char)('A' + ((raw >> 10) & 0x1F) - 1);
        var c2 = (char)('A' + ((raw >> 5) & 0x1F) - 1);
        var c3 = (char)('A' + (raw & 0x1F) - 1);

        if (!IsEdidLetter(c1) || !IsEdidLetter(c2) || !IsEdidLetter(c3))
        {
            return string.Empty;
        }

        return new string(new[] { c1, c2, c3 });
    }

    internal static string DecodeEdidProductCode(byte low, byte high)
    {
        var code = (ushort)(low | (high << 8));
        return code == 0 ? string.Empty : code.ToString("X4", CultureInfo.InvariantCulture);
    }

    private static (int? MinVerticalHz, int? MaxVerticalHz, int? MinHorizontalKHz, int? MaxHorizontalKHz, int? MaxPixelClockMHz)
        GetEdidRangeLimits(byte[] edid)
    {
        if (edid.Length < 128)
        {
            return default;
        }

        const int descriptorStart = 54;
        const int descriptorLength = 18;
        const int descriptorCount = 4;

        for (var i = 0; i < descriptorCount; i++)
        {
            var offset = descriptorStart + (i * descriptorLength);
            if (edid[offset] != 0x00 || edid[offset + 1] != 0x00 || edid[offset + 2] != 0x00)
            {
                continue;
            }

            if (edid[offset + 3] != 0xFD)
            {
                continue;
            }

            var minV = edid[offset + 5];
            var maxV = edid[offset + 6];
            var minH = edid[offset + 7];
            var maxH = edid[offset + 8];
            var maxPixelClock = edid[offset + 9];

            return (
                minV > 0 ? minV : null,
                maxV > 0 ? maxV : null,
                minH > 0 ? minH : null,
                maxH > 0 ? maxH : null,
                maxPixelClock > 0 ? maxPixelClock * 10 : null);
        }

        return default;
    }

    private static uint DecodeEdidSerialNumberValue(byte b0, byte b1, byte b2, byte b3)
    {
        return (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
    }

    private static string? GetEdidDescriptorString(byte[] edid, byte descriptorType)
    {
        if (edid.Length < 128)
        {
            return null;
        }

        const int descriptorStart = 54;
        const int descriptorLength = 18;
        const int descriptorCount = 4;

        for (var i = 0; i < descriptorCount; i++)
        {
            var offset = descriptorStart + (i * descriptorLength);
            if (edid[offset] != 0x00 || edid[offset + 1] != 0x00 || edid[offset + 2] != 0x00)
            {
                continue;
            }

            if (edid[offset + 3] != descriptorType)
            {
                continue;
            }

            var textBytes = edid.Skip(offset + 5).Take(13).TakeWhile(b => b != 0x0A).ToArray();
            var text = Encoding.ASCII.GetString(textBytes).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string BuildEdidKey(string manufacturerId, string productCodeHex, string? serial)
    {
        if (string.IsNullOrWhiteSpace(manufacturerId) || string.IsNullOrWhiteSpace(productCodeHex))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(serial))
        {
            return $"{NormalizeEdidToken(manufacturerId)}|{NormalizeEdidToken(productCodeHex)}";
        }

        return $"{NormalizeEdidToken(manufacturerId)}|{NormalizeEdidToken(productCodeHex)}|{NormalizeEdidToken(serial)}";
    }

    private static bool IsEdidLetter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    private static string NormalizeEdidToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return normalized;
    }

    private static void AddCandidate(List<(string Key, string Mode)> candidates, string key, string mode)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        foreach (var existing in candidates)
        {
            if (string.Equals(existing.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        candidates.Add((key, mode));
    }

    internal static string MapVideoOutputTechnology(int videoType)
    {
        return videoType switch
        {
            0 => "VGA",
            1 => "S-Video",
            2 => "Composite",
            3 => "Component",
            4 => "DVI",
            5 => "HDMI",
            6 => "LVDS",
            8 => "D-JPeg",
            9 => "SDI",
            10 => "DisplayPort External",
            11 => "DisplayPort Embedded",
            12 => "UDI External",
            13 => "UDI Embedded",
            14 => "SDTV Dongle",
            15 => "Miracast",
            16 => "Internal",
            _ => "Unknown"
        };
    }
}
