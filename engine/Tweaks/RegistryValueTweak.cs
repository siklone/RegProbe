using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks;

public sealed class RegistryValueTweak : ITweak, IRollbackAwareTweak
{
    private readonly RegistryValueKind _valueKind;
    private readonly object _targetValue;
    private readonly RegistryValueData _targetValueData;
    private readonly IRegistryAccessor _registryAccessor;
    private readonly RegistryValueReference _reference;
    private bool _hasDetected;
    private bool _valueExists;
    private object? _detectedValue;
    private RegistryValueData? _detectedValueData;

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
        IRegistryAccessor registryAccessor,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        _reference = new RegistryValueReference(
            hive,
            view,
            string.IsNullOrWhiteSpace(keyPath)
                ? throw new ArgumentException("Key path is required.", nameof(keyPath))
                : keyPath,
            valueName ?? throw new ArgumentNullException(nameof(valueName)));
        if (valueKind is RegistryValueKind.None or RegistryValueKind.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, "Registry value kind must be a concrete type.");
        }

        _valueKind = valueKind;
        _targetValue = targetValue ?? throw new ArgumentNullException(nameof(targetValue));
        _targetValueData = RegistryValueData.FromObject(_valueKind, _targetValue);
        _registryAccessor = registryAccessor ?? throw new ArgumentNullException(nameof(registryAccessor));
        RequiresElevation = requiresElevation ?? hive != RegistryHive.CurrentUser;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public RegistryValueReference Reference => _reference;

    public RegistryValueKind ValueKind => _valueKind;

    public object TargetValue => _targetValue;

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var (valueExists, value, kind, data) = await ReadCurrentValueAsync(ct);
            _hasDetected = true;
            _valueExists = valueExists;
            _detectedValue = value;
            _detectedValueData = data;

            if (!valueExists)
            {
                return new TweakResult(TweakStatus.Detected, "Value not set.", DateTimeOffset.UtcNow);
            }

            if (kind == _valueKind && ValuesEqual(_valueKind, value, _targetValue))
            {
                return new TweakResult(
                    TweakStatus.Applied,
                    $"Current value is {FormatValue(value)}.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.Detected,
                $"Current value is {FormatValue(value)}.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Detect failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            await _registryAccessor.SetValueAsync(_reference, _targetValueData, ct);

            var message = $"Set value to {FormatValue(_targetValue)}.";
            return new TweakResult(TweakStatus.Applied, message, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Apply failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var (valueExists, value, kind, _) = await ReadCurrentValueAsync(ct);
            if (!valueExists)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Verification failed. Value is missing.",
                    DateTimeOffset.UtcNow);
            }

            if (kind != _valueKind)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Verification failed. Expected registry type {_valueKind}, found {kind}.",
                    DateTimeOffset.UtcNow);
            }

            if (!ValuesEqual(_valueKind, value, _targetValue))
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Verification failed. Expected {FormatValue(_targetValue)}, found {FormatValue(value)}.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.Verified,
                "Verified desired value.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Verify failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected)
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            if (!_valueExists)
            {
                await _registryAccessor.DeleteValueAsync(_reference, ct);

                return new TweakResult(
                    TweakStatus.RolledBack,
                    "Removed value to restore default.",
                    DateTimeOffset.UtcNow);
            }

            if (_detectedValueData is null)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Rollback failed. Prior value was not captured.",
                    DateTimeOffset.UtcNow);
            }

            await _registryAccessor.SetValueAsync(_reference, _detectedValueData, ct);

            var message = $"Rolled back to {FormatValue(_detectedValue)}.";
            return new TweakResult(TweakStatus.RolledBack, message, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Rollback failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    private async Task<(bool ValueExists, object? Value, RegistryValueKind Kind, RegistryValueData? Data)> ReadCurrentValueAsync(CancellationToken ct)
    {
        var result = await _registryAccessor.ReadValueAsync(_reference, ct);
        if (!result.Exists || result.Value is null)
        {
            return (false, null, RegistryValueKind.Unknown, null);
        }

        var value = result.Value.ToObject();
        return (true, value, result.Value.Kind, result.Value);
    }

    private static bool ValuesEqual(RegistryValueKind kind, object? actual, object? expected)
        => RegistryValueComparer.ValuesEqual(kind, actual, expected);

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

    #region IRollbackAwareTweak Implementation

    public bool HasCapturedState => _hasDetected;

    public TweakRollbackSnapshot? GetRollbackSnapshot()
    {
        if (!_hasDetected)
        {
            return null;
        }

        string? originalValueJson = null;
        if (_detectedValue is not null)
        {
            try
            {
                originalValueJson = JsonSerializer.Serialize(_detectedValue);
            }
            catch
            {
                // If serialization fails, use string representation
                originalValueJson = _detectedValue.ToString();
            }
        }

        return new TweakRollbackSnapshot
        {
            TweakId = Id,
            TweakName = Name,
            SnapshotType = TweakSnapshotType.Registry,
            RegistryHive = _reference.Hive.ToString(),
            RegistryPath = _reference.KeyPath,
            RegistryValueName = _reference.ValueName,
            RegistryValueKind = _valueKind.ToString(),
            OriginalValueJson = originalValueJson,
            ValueExisted = _valueExists,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    public void RestoreFromSnapshot(TweakRollbackSnapshot snapshot)
    {
        if (snapshot is null || snapshot.TweakId != Id)
        {
            return;
        }

        _hasDetected = true;
        _valueExists = snapshot.ValueExisted;

        if (!string.IsNullOrEmpty(snapshot.OriginalValueJson) && snapshot.ValueExisted)
        {
            try
            {
                // Try to deserialize based on value kind
                _detectedValue = DeserializeValue(snapshot.OriginalValueJson, snapshot.RegistryValueKind);
                if (_detectedValue is not null)
                {
                    _detectedValueData = RegistryValueData.FromObject(_valueKind, _detectedValue);
                }
            }
            catch
            {
                // If deserialization fails, we can't restore
                _hasDetected = false;
            }
        }
    }

    private static object? DeserializeValue(string json, string? valueKind)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return valueKind switch
            {
                "DWord" => JsonSerializer.Deserialize<int>(json),
                "QWord" => JsonSerializer.Deserialize<long>(json),
                "String" or "ExpandString" => JsonSerializer.Deserialize<string>(json),
                "MultiString" => JsonSerializer.Deserialize<string[]>(json),
                "Binary" => JsonSerializer.Deserialize<byte[]>(json),
                _ => JsonSerializer.Deserialize<object>(json)
            };
        }
        catch
        {
            return json; // Fall back to raw string
        }
    }

    #endregion
}
