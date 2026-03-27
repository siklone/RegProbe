using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks;

public sealed class RegistryValuePresetBatchTweak : ITweak, IChoiceTweak
{
    private readonly IReadOnlyList<PresetState> _presets;
    private readonly IReadOnlyList<RegistryValueReference> _allReferences;
    private readonly Dictionary<RegistryValueReference, RegistryValueSnapshot> _snapshots;
    private readonly IRegistryAccessor _registryAccessor;
    private bool _hasDetected;
    private string _selectedPresetKey;

    public RegistryValuePresetBatchTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<RegistryValuePresetBatchOption> presets,
        string defaultPresetKey,
        IRegistryAccessor registryAccessor,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        _registryAccessor = registryAccessor ?? throw new ArgumentNullException(nameof(registryAccessor));

        if (presets is null)
        {
            throw new ArgumentNullException(nameof(presets));
        }

        if (presets.Count == 0)
        {
            throw new ArgumentException("At least one preset option is required.", nameof(presets));
        }

        var seenPresetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allReferences = new HashSet<RegistryValueReference>();
        var presetStates = new List<PresetState>(presets.Count);
        foreach (var preset in presets)
        {
            if (preset is null)
            {
                throw new ArgumentException("Preset entries cannot contain null values.", nameof(presets));
            }

            if (string.IsNullOrWhiteSpace(preset.Key))
            {
                throw new ArgumentException("Preset keys must be provided.", nameof(presets));
            }

            if (string.IsNullOrWhiteSpace(preset.Label))
            {
                throw new ArgumentException("Preset labels must be provided.", nameof(presets));
            }

            if (!seenPresetKeys.Add(preset.Key))
            {
                throw new ArgumentException($"Duplicate preset key '{preset.Key}'.", nameof(presets));
            }

            if (preset.Entries is null || preset.Entries.Count == 0)
            {
                throw new ArgumentException($"Preset '{preset.Key}' must define at least one registry entry.", nameof(presets));
            }

            var seenEntries = new HashSet<RegistryValueReference>();
            var entryStates = new List<EntryState>(preset.Entries.Count);
            foreach (var entry in preset.Entries)
            {
                if (entry is null)
                {
                    throw new ArgumentException("Preset entries cannot contain null values.", nameof(presets));
                }

                if (string.IsNullOrWhiteSpace(entry.KeyPath))
                {
                    throw new ArgumentException($"Preset '{preset.Key}' contains an empty key path.", nameof(presets));
                }

                if (entry.ValueName is null)
                {
                    throw new ArgumentException($"Preset '{preset.Key}' contains an empty value name.", nameof(presets));
                }

                if (entry.Kind is RegistryValueKind.None or RegistryValueKind.Unknown)
                {
                    throw new ArgumentOutOfRangeException(nameof(presets), entry.Kind, "Registry value kind must be a concrete type.");
                }

                if (entry.TargetValue is null)
                {
                    throw new ArgumentException($"Preset '{preset.Key}' contains a null target value.", nameof(presets));
                }

                var reference = new RegistryValueReference(entry.Hive, entry.View, entry.KeyPath, entry.ValueName);
                if (!seenEntries.Add(reference))
                {
                    throw new ArgumentException(
                        $"Preset '{preset.Key}' contains duplicate entry '{entry.Hive}\\{entry.KeyPath}\\{entry.ValueName}'.",
                        nameof(presets));
                }

                allReferences.Add(reference);
                entryStates.Add(new EntryState(reference, RegistryValueData.FromObject(entry.Kind, entry.TargetValue), entry.TargetValue));
            }

            presetStates.Add(new PresetState(preset.Key, preset.Label, preset.Description, entryStates));
        }

        _presets = presetStates;
        _allReferences = allReferences.ToList();
        _snapshots = new Dictionary<RegistryValueReference, RegistryValueSnapshot>();
        _selectedPresetKey = ResolvePreset(defaultPresetKey).Key;
        RequiresElevation = requiresElevation ?? _presets.SelectMany(preset => preset.Entries).Any(entry => entry.Reference.Hive != RegistryHive.CurrentUser);
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public IReadOnlyList<RegistryValuePresetBatchOptionInfo> Presets =>
        _presets.Select(static preset => new RegistryValuePresetBatchOptionInfo(preset.Key, preset.Label, preset.Description)).ToList();

    public IReadOnlyList<TweakChoiceDefinition> Choices =>
        _presets.Select(static preset => new TweakChoiceDefinition(preset.Key, preset.Label, preset.Description)).ToList();

    public string SelectedPresetKey
    {
        get => _selectedPresetKey;
        set => _selectedPresetKey = ResolvePreset(value).Key;
    }

    public string SelectedChoiceKey
    {
        get => SelectedPresetKey;
        set => SelectedPresetKey = value;
    }

    public string SelectedPresetLabel => ResolvePreset(_selectedPresetKey).Label;

    public string SelectedChoiceLabel => SelectedPresetLabel;

    public string SelectedPresetDescription => ResolvePreset(_selectedPresetKey).Description;

    public string SelectedChoiceDescription => SelectedPresetDescription;

    public string? MatchedPresetKey { get; private set; }

    public string? MatchedChoiceKey => MatchedPresetKey;

    public string? MatchedPresetLabel => TryResolvePreset(MatchedPresetKey)?.Label;

    public string? MatchedChoiceLabel => MatchedPresetLabel;

    public string? DefaultChoiceKey => null;

    public string? DefaultChoiceLabel => null;

    public string PrimaryScopePath => FormatReference(ResolvePreset(_selectedPresetKey).Entries[0].Reference);

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            _snapshots.Clear();
            foreach (var reference in _allReferences)
            {
                var result = await _registryAccessor.ReadValueAsync(reference, ct);
                _snapshots[reference] = new RegistryValueSnapshot(result.Exists, result.Value);
            }

            _hasDetected = true;
            var matchedPreset = TryFindMatchedPreset();
            MatchedPresetKey = matchedPreset?.Key;

            var selectedPreset = ResolvePreset(_selectedPresetKey);
            var status = matchedPreset?.Key.Equals(selectedPreset.Key, StringComparison.OrdinalIgnoreCase) == true
                ? TweakStatus.Applied
                : TweakStatus.Detected;

            var summary = matchedPreset is null
                ? $"Current values do not match a named option. Selected option is '{selectedPreset.Label}'."
                : matchedPreset.Key.Equals(selectedPreset.Key, StringComparison.OrdinalIgnoreCase)
                    ? $"Current option is '{matchedPreset.Label}'."
                    : $"Current option is '{matchedPreset.Label}'. Selected option is '{selectedPreset.Label}'.";

            var details = BuildEntryDetails(selectedPreset.Entries, _snapshots);
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
            var selectedPreset = ResolvePreset(_selectedPresetKey);
            foreach (var entry in selectedPreset.Entries)
            {
                await _registryAccessor.SetValueAsync(entry.Reference, entry.TargetData, ct);
            }

            MatchedPresetKey = selectedPreset.Key;
            return new TweakResult(
                TweakStatus.Applied,
                $"Applied preset '{selectedPreset.Label}'.",
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
            var selectedPreset = ResolvePreset(_selectedPresetKey);
            foreach (var entry in selectedPreset.Entries)
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

                if (!ValuesEqual(entry.TargetData.Kind, result.Value.ToObject(), entry.TargetValue))
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Verification failed. '{entry.Reference.ValueName}' expected {FormatValue(entry.TargetValue)}, found {FormatValue(result.Value.ToObject())}.",
                        DateTimeOffset.UtcNow);
                }
            }

            MatchedPresetKey = selectedPreset.Key;
            return new TweakResult(TweakStatus.Verified, $"Verified preset '{selectedPreset.Label}'.", DateTimeOffset.UtcNow);
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
            foreach (var reference in _allReferences)
            {
                if (!_snapshots.TryGetValue(reference, out var snapshot)
                    || !snapshot.Exists
                    || snapshot.Value is null)
                {
                    await _registryAccessor.DeleteValueAsync(reference, ct);
                    continue;
                }

                await _registryAccessor.SetValueAsync(reference, snapshot.Value, ct);
            }

            MatchedPresetKey = TryFindMatchedPreset()?.Key;
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

    private PresetState ResolvePreset(string? key)
    {
        var preset = TryResolvePreset(key);
        return preset ?? throw new ArgumentException($"Preset '{key}' was not found.", nameof(key));
    }

    private PresetState? TryResolvePreset(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return _presets.FirstOrDefault(preset => preset.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private PresetState? TryFindMatchedPreset()
    {
        foreach (var preset in _presets)
        {
            if (preset.Entries.All(entry =>
                    _snapshots.TryGetValue(entry.Reference, out var snapshot)
                    && snapshot.Exists
                    && snapshot.Value is not null
                    && snapshot.Value.Kind == entry.TargetData.Kind
                    && ValuesEqual(entry.TargetData.Kind, snapshot.Value.ToObject(), entry.TargetValue)))
            {
                return preset;
            }
        }

        return null;
    }

    private static bool ValuesEqual(RegistryValueKind kind, object? actual, object? expected)
    {
        if (RegistryValueComparer.ValuesEqual(kind, actual, expected))
        {
            return true;
        }

        return string.Equals(
            Convert.ToString(actual, CultureInfo.InvariantCulture),
            Convert.ToString(expected, CultureInfo.InvariantCulture),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "(null)";
        }

        return value switch
        {
            byte[] bytes => $"0x{BitConverter.ToString(bytes).Replace("-", string.Empty)}",
            string[] strings => string.Join("; ", strings),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty
        };
    }

    private static string BuildEntryDetails(
        IReadOnlyList<EntryState> entries,
        IReadOnlyDictionary<RegistryValueReference, RegistryValueSnapshot> snapshots)
    {
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            if (!snapshots.TryGetValue(entry.Reference, out var snapshot) || !snapshot.Exists || snapshot.Value is null)
            {
                lines.Add($"- {FormatReference(entry.Reference)}: missing â†’ {FormatValue(entry.TargetValue)}");
                continue;
            }

            lines.Add($"- {FormatReference(entry.Reference)}: {FormatValue(snapshot.Value.ToObject())} â†’ {FormatValue(entry.TargetValue)}");
        }

        return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
    }

    private static string FormatReference(RegistryValueReference reference)
    {
        var keyPath = (reference.KeyPath ?? string.Empty).Trim().TrimStart('\\').TrimEnd('\\');
        var valueName = string.IsNullOrWhiteSpace(reference.ValueName) ? "(Default)" : reference.ValueName;

        if (keyPath.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCR\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKU\\", StringComparison.OrdinalIgnoreCase)
            || keyPath.StartsWith("HKCC\\", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(keyPath) ? valueName : $"{keyPath}\\{valueName}";
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

        var fullPath = string.IsNullOrEmpty(keyPath) ? hive : $"{hive}\\{keyPath}";
        return $"{fullPath}\\{valueName}";
    }

    private sealed record PresetState(
        string Key,
        string Label,
        string Description,
        IReadOnlyList<EntryState> Entries);

    private sealed record EntryState(
        RegistryValueReference Reference,
        RegistryValueData TargetData,
        object TargetValue);

    private sealed record RegistryValueSnapshot(bool Exists, RegistryValueData? Value);
}

public sealed record RegistryValuePresetBatchOption(
    string Key,
    string Label,
    string Description,
    IReadOnlyList<RegistryValueBatchEntry> Entries);

public sealed record RegistryValuePresetBatchOptionInfo(
    string Key,
    string Label,
    string Description);
