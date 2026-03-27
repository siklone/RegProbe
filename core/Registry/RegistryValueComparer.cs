using System;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;

namespace RegProbe.Core.Registry;

public static class RegistryValueComparer
{
    public static bool ValuesEqual(RegistryValueKind kind, object? actual, object? expected)
    {
        if (actual is null || expected is null)
        {
            return actual is null && expected is null;
        }

        if (actual is byte[] actualBytes && expected is byte[] expectedBytes)
        {
            return actualBytes.SequenceEqual(expectedBytes);
        }

        if (actual is string[] actualStrings && expected is string[] expectedStrings)
        {
            return actualStrings.SequenceEqual(expectedStrings, StringComparer.Ordinal);
        }

        if (IsNumeric(actual) && IsNumeric(expected))
        {
            return kind switch
            {
                RegistryValueKind.DWord => ToUInt32(actual) == ToUInt32(expected),
                RegistryValueKind.QWord => ToUInt64(actual) == ToUInt64(expected),
                _ => Convert.ToInt64(actual, CultureInfo.InvariantCulture)
                    == Convert.ToInt64(expected, CultureInfo.InvariantCulture)
            };
        }

        return actual.Equals(expected);
    }

    public static string FormatNumericValueForRegExe(RegistryValueKind kind, long? value)
    {
        if (!value.HasValue)
        {
            throw new InvalidOperationException("Numeric registry value is missing.");
        }

        return kind switch
        {
            RegistryValueKind.DWord => ToUInt32(value.Value).ToString(CultureInfo.InvariantCulture),
            RegistryValueKind.QWord => ToUInt64(value.Value).ToString(CultureInfo.InvariantCulture),
            _ => value.Value.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static bool IsNumeric(object value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong;
    }

    private static uint ToUInt32(object value)
    {
        return value switch
        {
            byte typed => typed,
            sbyte typed => unchecked((uint)typed),
            short typed => unchecked((uint)typed),
            ushort typed => typed,
            int typed => unchecked((uint)typed),
            uint typed => typed,
            long typed when typed >= 0 && typed <= uint.MaxValue => (uint)typed,
            long typed when typed >= int.MinValue => unchecked((uint)typed),
            ulong typed when typed <= uint.MaxValue => (uint)typed,
            _ => throw new OverflowException("Value is out of range for DWORD comparison.")
        };
    }

    private static ulong ToUInt64(object value)
    {
        return value switch
        {
            byte typed => typed,
            sbyte typed => unchecked((ulong)typed),
            short typed => unchecked((ulong)typed),
            ushort typed => typed,
            int typed => unchecked((ulong)typed),
            uint typed => typed,
            long typed => unchecked((ulong)typed),
            ulong typed => typed,
            _ => throw new OverflowException("Value is out of range for QWORD comparison.")
        };
    }
}
