using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed class RegistryValueTweak : ITweak
{
    private readonly RegistryHive _hive;
    private readonly RegistryView _view;
    private readonly string _keyPath;
    private readonly string _valueName;
    private readonly RegistryValueKind _valueKind;
    private readonly object _targetValue;
    private bool _hasDetected;
    private bool _valueExists;
    private object? _detectedValue;
    private RegistryValueKind _detectedKind;

    public RegistryValueTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        string valueName,
        RegistryValueKind valueKind,
        object targetValue,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        _hive = hive;
        _view = view;
        _keyPath = string.IsNullOrWhiteSpace(keyPath)
            ? throw new ArgumentException("Key path is required.", nameof(keyPath))
            : keyPath;
        _valueName = valueName ?? throw new ArgumentNullException(nameof(valueName));
        if (valueKind is RegistryValueKind.None or RegistryValueKind.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, "Registry value kind must be a concrete type.");
        }

        _valueKind = valueKind;
        _targetValue = targetValue ?? throw new ArgumentNullException(nameof(targetValue));
        RequiresElevation = requiresElevation ?? hive != RegistryHive.CurrentUser;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var (valueExists, value, kind) = ReadCurrentValue();
        _hasDetected = true;
        _valueExists = valueExists;
        _detectedValue = value;
        _detectedKind = kind;

        var message = valueExists
            ? $"Current value is {FormatValue(value)}."
            : "Value not set.";
        return Task.FromResult(new TweakResult(TweakStatus.Detected, message, DateTimeOffset.UtcNow));
    }

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var key = OpenOrCreateKey();
        key.SetValue(_valueName, _targetValue, _valueKind);

        var message = $"Set value to {FormatValue(_targetValue)}.";
        return Task.FromResult(new TweakResult(TweakStatus.Applied, message, DateTimeOffset.UtcNow));
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var (valueExists, value, kind) = ReadCurrentValue();
        if (!valueExists)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                "Verification failed. Value is missing.",
                DateTimeOffset.UtcNow));
        }

        if (kind != _valueKind)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Verification failed. Expected registry type {_valueKind}, found {kind}.",
                DateTimeOffset.UtcNow));
        }

        if (!ValuesEqual(value, _targetValue))
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Verification failed. Expected {FormatValue(_targetValue)}, found {FormatValue(value)}.",
                DateTimeOffset.UtcNow));
        }

        return Task.FromResult(new TweakResult(
            TweakStatus.Verified,
            "Verified desired value.",
            DateTimeOffset.UtcNow));
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow));
        }

        if (!_valueExists)
        {
            using var key = OpenWritableKey();
            if (key is not null && key.GetValue(_valueName) is not null)
            {
                key.DeleteValue(_valueName, false);
            }

            return Task.FromResult(new TweakResult(
                TweakStatus.RolledBack,
                "Removed value to restore default.",
                DateTimeOffset.UtcNow));
        }

        using (var key = OpenOrCreateKey())
        {
            key.SetValue(_valueName, _detectedValue!, _detectedKind);
        }

        var message = $"Rolled back to {FormatValue(_detectedValue)}.";
        return Task.FromResult(new TweakResult(TweakStatus.RolledBack, message, DateTimeOffset.UtcNow));
    }

    private RegistryKey OpenOrCreateKey()
    {
        using var baseKey = RegistryKey.OpenBaseKey(_hive, _view);
        var key = baseKey.CreateSubKey(_keyPath, true);
        if (key is null)
        {
            throw new InvalidOperationException($"Failed to open registry key {_hive}\\{_keyPath}.");
        }

        return key;
    }

    private RegistryKey? OpenWritableKey()
    {
        using var baseKey = RegistryKey.OpenBaseKey(_hive, _view);
        return baseKey.OpenSubKey(_keyPath, true);
    }

    private (bool ValueExists, object? Value, RegistryValueKind Kind) ReadCurrentValue()
    {
        using var baseKey = RegistryKey.OpenBaseKey(_hive, _view);
        using var key = baseKey.OpenSubKey(_keyPath, false);
        if (key is null)
        {
            return (false, null, RegistryValueKind.Unknown);
        }

        var value = key.GetValue(_valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is null)
        {
            return (false, null, RegistryValueKind.Unknown);
        }

        var kind = key.GetValueKind(_valueName);
        return (true, value, kind);
    }

    private static bool ValuesEqual(object? actual, object? expected)
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
            return Convert.ToInt64(actual, CultureInfo.InvariantCulture)
                == Convert.ToInt64(expected, CultureInfo.InvariantCulture);
        }

        return actual.Equals(expected);
    }

    private static bool IsNumeric(object value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong;
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        if (value is byte[] bytes)
        {
            return $"0x{BitConverter.ToString(bytes).Replace("-", string.Empty)}";
        }

        if (value is string[] strings)
        {
            return string.Join("; ", strings);
        }

        return value.ToString() ?? "<null>";
    }
}
