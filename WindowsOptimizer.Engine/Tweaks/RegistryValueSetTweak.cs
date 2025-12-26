using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed class RegistryValueSetTweak : ITweak
{
    private readonly IReadOnlyList<EntryState> _entries;
    private readonly Dictionary<string, RegistryValueSnapshot> _snapshots;
    private readonly IRegistryAccessor _registryAccessor;
    private bool _hasDetected;

    public RegistryValueSetTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        IReadOnlyList<RegistryValueSetEntry> entries,
        IRegistryAccessor registryAccessor,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        RequiresElevation = requiresElevation ?? hive != RegistryHive.CurrentUser;
        _registryAccessor = registryAccessor ?? throw new ArgumentNullException(nameof(registryAccessor));

        var resolvedKeyPath = string.IsNullOrWhiteSpace(keyPath)
            ? throw new ArgumentException("Key path is required.", nameof(keyPath))
            : keyPath;

        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        if (entries.Count == 0)
        {
            throw new ArgumentException("At least one registry value entry is required.", nameof(entries));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entryStates = new List<EntryState>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                throw new ArgumentException("Entries cannot contain null values.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                throw new ArgumentException("Entry names must be provided.", nameof(entries));
            }

            if (entry.Kind is RegistryValueKind.None or RegistryValueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(entries), entry.Kind, "Registry value kind must be a concrete type.");
            }

            if (entry.TargetValue is null)
            {
                throw new ArgumentException("Entry target values must be provided.", nameof(entries));
            }

            if (!seen.Add(entry.Name))
            {
                throw new ArgumentException($"Duplicate registry value name '{entry.Name}'.", nameof(entries));
            }

            var reference = new RegistryValueReference(hive, view, resolvedKeyPath, entry.Name);
            var targetData = RegistryValueData.FromObject(entry.Kind, entry.TargetValue);
            entryStates.Add(new EntryState(reference, targetData, entry.TargetValue));
        }

        _entries = entryStates;
        _snapshots = new Dictionary<string, RegistryValueSnapshot>(StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            _snapshots.Clear();
            var matchingTargets = 0;
            foreach (var entry in _entries)
            {
                var result = await _registryAccessor.ReadValueAsync(entry.Reference, ct);
                _snapshots[entry.Reference.ValueName] = new RegistryValueSnapshot(result.Exists, result.Value);

                if (result.Exists
                    && result.Value is not null
                    && result.Value.Kind == entry.TargetData.Kind
                    && ValuesEqual(result.Value.ToObject(), entry.TargetValue))
                {
                    matchingTargets++;
                }
            }

            _hasDetected = true;
            var detectedCount = _snapshots.Values.Count(snapshot => snapshot.Exists);
            var status = matchingTargets == _entries.Count ? TweakStatus.Applied : TweakStatus.Detected;
            var message = status == TweakStatus.Applied
                ? $"All {_entries.Count} values already match the desired configuration."
                : $"Detected {detectedCount} of {_entries.Count} values (matches: {matchingTargets}).";
            return new TweakResult(status, message, DateTimeOffset.UtcNow);
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
            foreach (var entry in _entries)
            {
                await _registryAccessor.SetValueAsync(entry.Reference, entry.TargetData, ct);
            }

            return new TweakResult(
                TweakStatus.Applied,
                $"Updated {_entries.Count} values.",
                DateTimeOffset.UtcNow);
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
            foreach (var entry in _entries)
            {
                var result = await _registryAccessor.ReadValueAsync(entry.Reference, ct);
                if (!result.Exists || result.Value is null)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Verification failed. '{entry.Reference.ValueName}' is missing.",
                        DateTimeOffset.UtcNow);
                }

                if (result.Value.Kind != entry.TargetData.Kind)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Verification failed. '{entry.Reference.ValueName}' expected {entry.TargetData.Kind}, found {result.Value.Kind}.",
                        DateTimeOffset.UtcNow);
                }

                var currentValue = result.Value.ToObject();
                if (!ValuesEqual(currentValue, entry.TargetValue))
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Verification failed. '{entry.Reference.ValueName}' expected {FormatValue(entry.TargetValue)}, found {FormatValue(currentValue)}.",
                        DateTimeOffset.UtcNow);
                }
            }

            return new TweakResult(TweakStatus.Verified, "Verified desired values.", DateTimeOffset.UtcNow);
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
            foreach (var entry in _entries)
            {
                if (!_snapshots.TryGetValue(entry.Reference.ValueName, out var snapshot)
                    || !snapshot.Exists
                    || snapshot.Value is null)
                {
                    await _registryAccessor.DeleteValueAsync(entry.Reference, ct);
                    continue;
                }

                await _registryAccessor.SetValueAsync(entry.Reference, snapshot.Value, ct);
            }

            return new TweakResult(TweakStatus.RolledBack, "Rolled back registry values.", DateTimeOffset.UtcNow);
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

    private sealed record EntryState(
        RegistryValueReference Reference,
        RegistryValueData TargetData,
        object TargetValue);

    private sealed record RegistryValueSnapshot(bool Exists, RegistryValueData? Value);
}

public sealed record RegistryValueSetEntry(string Name, RegistryValueKind Kind, object TargetValue);
