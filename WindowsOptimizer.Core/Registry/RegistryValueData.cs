using System;
using System.Globalization;
using Microsoft.Win32;

namespace WindowsOptimizer.Core.Registry;

public sealed record RegistryValueData(
    RegistryValueKind Kind,
    long? NumericValue = null,
    string? StringValue = null,
    string[]? MultiStringValue = null,
    byte[]? BinaryValue = null)
{
    public static RegistryValueData FromObject(RegistryValueKind kind, object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return kind switch
        {
            RegistryValueKind.DWord or RegistryValueKind.QWord => new RegistryValueData(
                kind,
                NumericValue: Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            RegistryValueKind.String or RegistryValueKind.ExpandString => new RegistryValueData(
                kind,
                StringValue: value.ToString()),
            RegistryValueKind.MultiString => value is string[] multi
                ? new RegistryValueData(kind, MultiStringValue: multi)
                : throw new ArgumentException("Expected string array for multi-string registry value.", nameof(value)),
            RegistryValueKind.Binary => value is byte[] bytes
                ? new RegistryValueData(kind, BinaryValue: bytes)
                : throw new ArgumentException("Expected byte array for binary registry value.", nameof(value)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Registry value kind must be a concrete type.")
        };
    }

    public object ToObject()
    {
        return Kind switch
        {
            RegistryValueKind.DWord => ToInt32(NumericValue),
            RegistryValueKind.QWord => ToInt64(NumericValue),
            RegistryValueKind.String or RegistryValueKind.ExpandString => StringValue ?? string.Empty,
            RegistryValueKind.MultiString => MultiStringValue ?? Array.Empty<string>(),
            RegistryValueKind.Binary => BinaryValue ?? Array.Empty<byte>(),
            _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Registry value kind must be a concrete type.")
        };
    }

    private static int ToInt32(long? value)
    {
        if (!value.HasValue)
        {
            throw new ArgumentException("Numeric value is required for DWORD registry value.", nameof(value));
        }

        if (value.Value is < int.MinValue or > int.MaxValue)
        {
            throw new OverflowException("DWORD registry value is out of range.");
        }

        return (int)value.Value;
    }

    private static long ToInt64(long? value)
    {
        if (!value.HasValue)
        {
            throw new ArgumentException("Numeric value is required for QWORD registry value.", nameof(value));
        }

        return value.Value;
    }
}
