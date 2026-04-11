using System;

namespace RegProbe.Infrastructure.RegistryResearch;

internal static class RegistryPathParser
{
    public static RegistryPathParts Parse(string? rawPath, bool splitLastSegmentAsValue)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return new RegistryPathParts(null, null, null);
        }

        var normalized = rawPath
            .Trim()
            .Replace('/', '\\')
            .Replace(@"HKLM:\", @"HKLM\")
            .Replace(@"HKCU:\", @"HKCU\")
            .Replace(@"HKCR:\", @"HKCR\")
            .Replace(@"HKU:\", @"HKU\")
            .Replace(@"HKEY_LOCAL_MACHINE\", @"HKLM\")
            .Replace(@"HKEY_CURRENT_USER\", @"HKCU\")
            .Replace(@"HKEY_CLASSES_ROOT\", @"HKCR\")
            .Replace(@"HKEY_USERS\", @"HKU\");

        string? hive = null;
        var remaining = normalized;
        foreach (var candidate in new[] { "HKLM", "HKCU", "HKCR", "HKU", "HKCC" })
        {
            if (!remaining.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            hive = candidate;
            remaining = remaining.Length == candidate.Length
                ? string.Empty
                : remaining[(candidate.Length + (remaining.Length > candidate.Length && remaining[candidate.Length] == '\\' ? 1 : 0))..];
            break;
        }

        if (!splitLastSegmentAsValue || string.IsNullOrWhiteSpace(remaining))
        {
            return new RegistryPathParts(hive, remaining, null);
        }

        var separator = remaining.LastIndexOf('\\');
        if (separator <= 0 || separator >= remaining.Length - 1)
        {
            return new RegistryPathParts(hive, remaining, null);
        }

        return new RegistryPathParts(
            hive,
            remaining[..separator],
            remaining[(separator + 1)..]);
    }
}
