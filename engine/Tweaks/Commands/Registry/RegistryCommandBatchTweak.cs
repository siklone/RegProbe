using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Commands.RegistryOps;

public sealed class RegistryCommandBatchTweak : ITweak
{
    private readonly IReadOnlyList<EntryState> _entries;
    private readonly Dictionary<RegistryValueReference, RegistryValueSnapshot> _snapshots;
    private readonly IRegistryAccessor _readRegistryAccessor;
    private readonly IRegistryAccessor _writeRegistryAccessor;
    private bool _hasDetected;

    public RegistryCommandBatchTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<RegistryValueBatchEntry> entries,
        IRegistryAccessor readRegistryAccessor,
        IRegistryAccessor writeRegistryAccessor,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        _readRegistryAccessor = readRegistryAccessor ?? throw new ArgumentNullException(nameof(readRegistryAccessor));
        _writeRegistryAccessor = writeRegistryAccessor ?? throw new ArgumentNullException(nameof(writeRegistryAccessor));

        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        if (entries.Count == 0)
        {
            throw new ArgumentException("At least one registry value entry is required.", nameof(entries));
        }

        var seen = new HashSet<RegistryValueReference>();
        var entryStates = new List<EntryState>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                throw new ArgumentException("Entries cannot contain null values.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.KeyPath))
            {
                throw new ArgumentException("Entry key paths must be provided.", nameof(entries));
            }

            if (entry.ValueName is null)
            {
                throw new ArgumentException("Entry value names must be provided.", nameof(entries));
            }

            if (entry.Kind is RegistryValueKind.None or RegistryValueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(entries), entry.Kind, "Registry value kind must be a concrete type.");
            }

            if (entry.TargetValue is null)
            {
                throw new ArgumentException("Entry target values must be provided.", nameof(entries));
            }

            var reference = new RegistryValueReference(entry.Hive, entry.View, entry.KeyPath, entry.ValueName);
            if (!seen.Add(reference))
            {
                throw new ArgumentException(
                    $"Duplicate registry entry '{entry.Hive}\\{entry.KeyPath}\\{entry.ValueName}'.",
                    nameof(entries));
            }

            var targetData = RegistryValueData.FromObject(entry.Kind, entry.TargetValue);
            entryStates.Add(new EntryState(reference, targetData, entry.TargetValue));
        }

        _entries = entryStates;
        Definitions = new ReadOnlyCollection<RegistryValueBatchEntry>(entries.ToList());
        _snapshots = new Dictionary<RegistryValueReference, RegistryValueSnapshot>();
        RequiresElevation = requiresElevation ?? true;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }
    public IReadOnlyList<RegistryValueBatchEntry> Definitions { get; }

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            _snapshots.Clear();
            var matchingTargets = 0;
            foreach (var entry in _entries)
            {
                var result = await _readRegistryAccessor.ReadValueAsync(entry.Reference, ct);
                _snapshots[entry.Reference] = new RegistryValueSnapshot(result.Exists, result.Value);

                if (result.Exists
                    && result.Value is not null
                    && result.Value.Kind == entry.TargetData.Kind
                    && ValuesEqual(entry.TargetData.Kind, result.Value.ToObject(), entry.TargetValue))
                {
                    matchingTargets++;
                }
            }

            _hasDetected = true;
            var detectedCount = _snapshots.Values.Count(snapshot => snapshot.Exists);
            var missingCount = _entries.Count - detectedCount;
            var status = matchingTargets == _entries.Count ? TweakStatus.Applied : TweakStatus.Detected;
            var summary = status == TweakStatus.Applied
                ? $"All {_entries.Count} values already match the desired configuration."
                : $"Detected {detectedCount} of {_entries.Count} values (matches: {matchingTargets}, missing: {missingCount}).";
            var details = BuildEntryDetails(_entries, _snapshots);
            var message = string.IsNullOrWhiteSpace(details)
                ? summary
                : $"{summary}\nEntries:\n{details}";
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
            var pendingUpdates = new List<EntryState>(_entries.Count);
            foreach (var entry in _entries)
            {
                RegistryValueSnapshot snapshot;
                if (_hasDetected && _snapshots.TryGetValue(entry.Reference, out var detectedSnapshot))
                {
                    snapshot = detectedSnapshot;
                }
                else
                {
                    var result = await _readRegistryAccessor.ReadValueAsync(entry.Reference, ct);
                    snapshot = new RegistryValueSnapshot(result.Exists, result.Value);
                    _snapshots[entry.Reference] = snapshot;
                }

                var alreadyMatches = snapshot.Exists
                    && snapshot.Value is not null
                    && snapshot.Value.Kind == entry.TargetData.Kind
                    && ValuesEqual(entry.TargetData.Kind, snapshot.Value.ToObject(), entry.TargetValue);

                if (!alreadyMatches)
                {
                    pendingUpdates.Add(entry);
                }
            }

            _hasDetected = true;

            if (pendingUpdates.Count == 0)
            {
                return new TweakResult(
                    TweakStatus.Applied,
                    $"All {_entries.Count} values already match the desired configuration.",
                    DateTimeOffset.UtcNow);
            }

            foreach (var entry in pendingUpdates)
            {
                await _writeRegistryAccessor.SetValueAsync(entry.Reference, entry.TargetData, ct);
            }

            return new TweakResult(
                TweakStatus.Applied,
                $"Updated {pendingUpdates.Count} of {_entries.Count} values.",
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
                var result = await _readRegistryAccessor.ReadValueAsync(entry.Reference, ct);
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
                if (!ValuesEqual(entry.TargetData.Kind, currentValue, entry.TargetValue))
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
                if (!_snapshots.TryGetValue(entry.Reference, out var snapshot))
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Rollback refused: missing snapshot for '{entry.Reference.ValueName}'. Run Detect first.",
                        DateTimeOffset.UtcNow);
                }

                if (!snapshot.Exists)
                {
                    await _writeRegistryAccessor.DeleteValueAsync(entry.Reference, ct);
                    continue;
                }

                if (snapshot.Value is null)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Rollback refused: snapshot value missing for '{entry.Reference.ValueName}'.",
                        DateTimeOffset.UtcNow);
                }

                await _writeRegistryAccessor.SetValueAsync(entry.Reference, snapshot.Value, ct);
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
            return $"0x{BitConverter.ToString(bytes).Replace("-", string.Empty, StringComparison.Ordinal)}";
        }

        if (value is string[] strings)
        {
            return string.Join("; ", strings);
        }

        return value.ToString() ?? "<null>";
    }

    private static string BuildEntryDetails(
        IReadOnlyList<EntryState> entries,
        IReadOnlyDictionary<RegistryValueReference, RegistryValueSnapshot> snapshots)
    {
        var lines = new List<string>();

        foreach (var entry in entries
                     .OrderBy(e => e.Reference.KeyPath, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(e => e.Reference.ValueName, StringComparer.OrdinalIgnoreCase))
        {
            var referenceLabel = FormatReference(entry.Reference);
            if (!snapshots.TryGetValue(entry.Reference, out var snapshot))
            {
                lines.Add($"- {referenceLabel}: unknown");
                continue;
            }

            if (!snapshot.Exists || snapshot.Value is null)
            {
                lines.Add($"- {referenceLabel}: missing");
                continue;
            }

            var currentValue = snapshot.Value.ToObject();
            lines.Add($"- {referenceLabel}: {FormatValue(currentValue)} -> {FormatValue(entry.TargetValue)}");
        }

        return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
    }

    private static string FormatReference(RegistryValueReference reference)
    {
        var keyPath = (reference.KeyPath ?? string.Empty).Trim().TrimStart('\\').TrimEnd('\\');
        var valueName = string.IsNullOrWhiteSpace(reference.ValueName) ? "(Default)" : reference.ValueName;
        var hive = reference.Hive switch
        {
            RegistryHive.LocalMachine => "HKLM",
            RegistryHive.CurrentUser => "HKCU",
            RegistryHive.ClassesRoot => "HKCR",
            RegistryHive.Users => "HKU",
            RegistryHive.CurrentConfig => "HKCC",
            _ => reference.Hive.ToString()
        };

        var fullPath = string.IsNullOrEmpty(keyPath) ? hive : $"{hive}\\{keyPath}";
        return $"{fullPath}\\{valueName}";
    }

    private sealed record EntryState(
        RegistryValueReference Reference,
        RegistryValueData TargetData,
        object TargetValue);

    private sealed record RegistryValueSnapshot(bool Exists, RegistryValueData? Value);
}
