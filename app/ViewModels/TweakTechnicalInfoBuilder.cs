using System;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands;

namespace RegProbe.App.ViewModels;

internal static class TweakTechnicalInfoBuilder
{
    public static TweakTechnicalInfoSnapshot Build(ITweak tweak, string registryPath, string targetValue, string codeExample)
    {
        return tweak switch
        {
            RegistryValuePresetBatchTweak presetBatchTweak => new TweakTechnicalInfoSnapshot(
                string.IsNullOrWhiteSpace(registryPath) ? presetBatchTweak.PrimaryScopePath : null,
                null,
                string.IsNullOrWhiteSpace(codeExample)
                    ? "Choose an option from the row, then click Apply to write that preset."
                    : null),
            IChoiceTweak => new TweakTechnicalInfoSnapshot(
                null,
                null,
                string.IsNullOrWhiteSpace(codeExample)
                    ? "Choose a profile, click Apply to use it, Restore Previous to undo, or Restore Default to go back to the app's default behavior."
                    : null),
            RegistryValueTweak registryValueTweak => new TweakTechnicalInfoSnapshot(
                string.IsNullOrWhiteSpace(registryPath)
                    ? FormatRegistryValuePath(registryValueTweak.Reference)
                    : null,
                string.IsNullOrWhiteSpace(targetValue) || targetValue == "Optimized"
                    ? FormatRegistryValueForDisplay(registryValueTweak.ValueKind, registryValueTweak.TargetValue)
                    : null,
                string.IsNullOrWhiteSpace(codeExample)
                    ? BuildRegistryCommandPreview(
                        registryValueTweak.Reference,
                        registryValueTweak.ValueKind,
                        registryValueTweak.TargetValue)
                    : null),
            RegistryValueBatchTweak or RegistryValueSetTweak => new TweakTechnicalInfoSnapshot(
                null,
                string.IsNullOrWhiteSpace(targetValue) || targetValue == "Optimized" ? "Multiple values" : null,
                null),
            ServiceStartModeBatchTweak serviceStartModeBatchTweak => new TweakTechnicalInfoSnapshot(
                null,
                string.IsNullOrWhiteSpace(targetValue) || targetValue == "Optimized"
                    ? FormatServiceStartModeForDisplay(serviceStartModeBatchTweak.TargetStartModeSummary)
                    : null,
                null),
            ScheduledTaskBatchTweak => new TweakTechnicalInfoSnapshot(
                null,
                string.IsNullOrWhiteSpace(targetValue) || targetValue == "Optimized" ? "Disabled" : null,
                null),
            _ => TweakTechnicalInfoSnapshot.Empty
        };
    }

    private static string FormatServiceStartModeForDisplay(ServiceStartMode startMode)
    {
        return startMode == ServiceStartMode.Unknown
            ? "Mixed"
            : startMode.ToString();
    }

    private static string FormatRegistryValuePath(RegistryValueReference reference)
    {
        var key = FormatRegistryKey(reference);
        return $"{key}\\{reference.ValueName}";
    }

    private static string FormatRegistryKey(RegistryValueReference reference)
    {
        var keyPath = (reference.KeyPath ?? string.Empty).Trim().TrimStart('\\').TrimEnd('\\');
        if (keyPath.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCR\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKU\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCC\\", StringComparison.OrdinalIgnoreCase))
        {
            return keyPath;
        }

        var hive = reference.Hive switch
        {
            RegistryHive.LocalMachine => "HKLM",
            RegistryHive.CurrentUser => "HKCU",
            RegistryHive.ClassesRoot => "HKCR",
            RegistryHive.Users => "HKU",
            RegistryHive.CurrentConfig => "HKCC",
            _ => reference.Hive.ToString()
        };

        return string.IsNullOrEmpty(keyPath) ? hive : $"{hive}\\{keyPath}";
    }

    private static string BuildRegistryCommandPreview(
        RegistryValueReference reference,
        RegistryValueKind valueKind,
        object targetValue)
    {
        var key = FormatRegistryKey(reference);
        var regType = valueKind switch
        {
            RegistryValueKind.String => "REG_SZ",
            RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
            RegistryValueKind.MultiString => "REG_MULTI_SZ",
            RegistryValueKind.Binary => "REG_BINARY",
            RegistryValueKind.DWord => "REG_DWORD",
            RegistryValueKind.QWord => "REG_QWORD",
            _ => $"REG_{valueKind.ToString().ToUpperInvariant()}"
        };

        var viewFlag = reference.View switch
        {
            RegistryView.Registry32 => " /reg:32",
            RegistryView.Registry64 => " /reg:64",
            _ => string.Empty
        };

        var data = FormatRegistryValueForRegAdd(valueKind, targetValue);

        return string.Join(
            Environment.NewLine,
            $"reg add \"{key}\" /v \"{reference.ValueName}\" /t {regType} /d {data} /f{viewFlag}",
            $"reg query \"{key}\" /v \"{reference.ValueName}\"{viewFlag}");
    }

    private static string FormatRegistryValueForRegAdd(RegistryValueKind valueKind, object value)
    {
        switch (valueKind)
        {
            case RegistryValueKind.DWord:
            case RegistryValueKind.QWord:
                return Convert.ToInt64(value).ToString();
            case RegistryValueKind.MultiString:
                if (value is string[] strings)
                {
                    var combined = string.Join("\\0", strings);
                    return $"\"{combined}\\0\"";
                }

                return $"\"{value}\"";
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
                return $"\"{value}\"";
            case RegistryValueKind.Binary:
                if (value is byte[] bytes)
                {
                    var hex = BitConverter.ToString(bytes).Replace("-", string.Empty);
                    return hex;
                }

                return value.ToString() ?? string.Empty;
            default:
                return value.ToString() ?? string.Empty;
        }
    }

    private static string FormatRegistryValueForDisplay(RegistryValueKind valueKind, object value)
    {
        switch (valueKind)
        {
            case RegistryValueKind.DWord:
            case RegistryValueKind.QWord:
                try
                {
                    var number = Convert.ToInt64(value);
                    return $"{number} (0x{number:X})";
                }
                catch
                {
                    return value.ToString() ?? "Unknown";
                }
            case RegistryValueKind.MultiString:
                return value is string[] strings
                    ? string.Join("; ", strings)
                    : value.ToString() ?? "Unknown";
            case RegistryValueKind.Binary:
                return value is byte[] bytes
                    ? $"0x{BitConverter.ToString(bytes).Replace("-", string.Empty)}"
                    : value.ToString() ?? "Unknown";
            default:
                return value.ToString() ?? "Unknown";
        }
    }
}

internal readonly record struct TweakTechnicalInfoSnapshot(
    string? RegistryPath,
    string? TargetValue,
    string? CodeExample)
{
    public static TweakTechnicalInfoSnapshot Empty => new(null, null, null);
}
