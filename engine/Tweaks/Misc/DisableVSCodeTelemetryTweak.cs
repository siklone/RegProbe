using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;

namespace RegProbe.Engine.Tweaks.Misc;

/// <summary>
/// Configures a small set of VS Code user settings related to telemetry and online behavior.
/// </summary>
public sealed class DisableVSCodeTelemetryTweak : IChoiceTweak, ITweakWithGuidance, IRollbackAwareTweak
{
    private const string DefaultChoice = "vscode-default";
    private const string PrivacyChoice = "privacy";
    private const string QuietChoice = "quiet";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly IReadOnlyList<ChoiceState> AvailableChoices =
    [
        new ChoiceState(
            DefaultChoice,
            "VS Code default",
            "Removes the keys this tweak manages so VS Code falls back to its own defaults.",
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)),
        new ChoiceState(
            PrivacyChoice,
            "Privacy-focused",
            "Turns telemetry and experiments off but keeps updates and most convenience features working.",
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["telemetry.telemetryLevel"] = "off",
                ["workbench.enableExperiments"] = false
            }),
        new ChoiceState(
            QuietChoice,
            "Quiet / manual",
            "Turns telemetry off and also disables auto-updates, recommendations, autofetch, and package lookups.",
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["telemetry.telemetryLevel"] = "off",
                ["workbench.enableExperiments"] = false,
                ["update.mode"] = "manual",
                ["update.showReleaseNotes"] = false,
                ["extensions.autoUpdate"] = false,
                ["extensions.autoCheckUpdates"] = false,
                ["extensions.ignoreRecommendations"] = true,
                ["git.autofetch"] = false,
                ["npm.fetchOnlinePackageInfo"] = false
            })
    ];

    private static readonly IReadOnlyDictionary<string, ChoiceState> ChoicesByKey =
        AvailableChoices.ToDictionary(static choice => choice.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly string[] ManagedKeys = AvailableChoices
        .SelectMany(static choice => choice.Settings.Keys)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private readonly string? _settingsPathOverride;
    private string _selectedChoiceKey = PrivacyChoice;
    private string? _settingsPath;
    private bool _hasDetected;
    private bool _settingsFileExisted;
    private string? _originalJson;

    public DisableVSCodeTelemetryTweak(string? settingsPath = null)
    {
        _settingsPathOverride = settingsPath;
        Id = "misc.disable-vscode-telemetry";
        Name = "VS Code Telemetry & Online Features";
        Description = "Choose how aggressively VS Code should reduce telemetry, experiments, auto-updates, and other online convenience features.";
        Risk = TweakRiskLevel.Safe;
        RequiresElevation = false;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    public IReadOnlyList<TweakChoiceDefinition> Choices =>
        AvailableChoices.Select(static choice => new TweakChoiceDefinition(choice.Key, choice.Label, choice.Description)).ToList();

    public string SelectedChoiceKey
    {
        get => _selectedChoiceKey;
        set => _selectedChoiceKey = ResolveChoice(value).Key;
    }

    public string SelectedChoiceLabel => ResolveChoice(_selectedChoiceKey).Label;

    public string SelectedChoiceDescription => ResolveChoice(_selectedChoiceKey).Description;

    public string? MatchedChoiceKey { get; private set; }

    public string? MatchedChoiceLabel => TryResolveChoice(MatchedChoiceKey)?.Label;

    public string? DefaultChoiceKey => DefaultChoice;

    public string? DefaultChoiceLabel => ResolveChoice(DefaultChoice).Label;

    public TweakGuidance Guidance => new()
    {
        CasualSummary = "This only changes your VS Code user settings file. It does not touch Windows services or block network traffic system-wide.",
        WhenHelpful = "Good if you want fewer background calls from VS Code, prefer a quieter setup, or you update tools manually on your own schedule.",
        Tradeoffs = "The stricter profile can stop extension auto-updates, Git autofetch, recommendation prompts, release notes, and npm package metadata lookups. That is good for a controlled setup, but bad if you expect VS Code to stay hands-off and up to date by itself.",
        DefaultVsPrevious = "Restore Previous puts back the exact settings.json state captured before Apply. Restore Default removes only the keys this tweak manages and lets VS Code use its built-in defaults again. Those are not always the same thing.",
        ProfessionalNotes = "Managed keys: telemetry.telemetryLevel, workbench.enableExperiments, update.mode, update.showReleaseNotes, extensions.autoUpdate, extensions.autoCheckUpdates, extensions.ignoreRecommendations, git.autofetch, npm.fetchOnlinePackageInfo."
    };

    public bool HasCapturedState => _hasDetected && !string.IsNullOrWhiteSpace(_settingsPath);

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var state = await LoadSettingsStateAsync(captureSnapshot: true, ct);
            var matchedChoice = TryFindMatchedChoice(state.Root);
            MatchedChoiceKey = matchedChoice?.Key;

            var selectedChoice = ResolveChoice(_selectedChoiceKey);
            var status = matchedChoice?.Key.Equals(selectedChoice.Key, StringComparison.OrdinalIgnoreCase) == true
                ? TweakStatus.Applied
                : TweakStatus.Detected;

            var summary = matchedChoice is null
                ? $"Current profile is custom. Selected profile is '{selectedChoice.Label}'."
                : matchedChoice.Key.Equals(selectedChoice.Key, StringComparison.OrdinalIgnoreCase)
                    ? $"Current profile is '{matchedChoice.Label}'."
                    : $"Current profile is '{matchedChoice.Label}'. Selected profile is '{selectedChoice.Label}'.";

            var managedSummary = string.Join(", ", ManagedKeys);
            return new TweakResult(
                status,
                $"{summary}\nManaged settings: {managedSummary}.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Detect error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected || string.IsNullOrWhiteSpace(_settingsPath))
        {
            return new TweakResult(
                TweakStatus.Failed,
                "Must call DetectAsync first.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            var state = await LoadSettingsStateAsync(captureSnapshot: false, ct);
            var selectedChoice = ResolveChoice(_selectedChoiceKey);
            ApplyChoice(state.Root, selectedChoice);
            await PersistSettingsAsync(_settingsPath, state.Root, ct);
            MatchedChoiceKey = selectedChoice.Key;

            return new TweakResult(
                TweakStatus.Applied,
                $"Applied profile '{selectedChoice.Label}'.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Apply error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var state = await LoadSettingsStateAsync(captureSnapshot: false, ct);
            var selectedChoice = ResolveChoice(_selectedChoiceKey);
            if (!ChoiceMatches(state.Root, selectedChoice))
            {
                var matchedChoice = TryFindMatchedChoice(state.Root);
                MatchedChoiceKey = matchedChoice?.Key;
                var currentProfile = matchedChoice?.Label ?? "Custom / Mixed";
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Verification failed. Current profile is '{currentProfile}', expected '{selectedChoice.Label}'.",
                    DateTimeOffset.UtcNow);
            }

            MatchedChoiceKey = selectedChoice.Key;
            return new TweakResult(
                TweakStatus.Verified,
                $"Verified profile '{selectedChoice.Label}'.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Verify error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public async Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected || string.IsNullOrWhiteSpace(_settingsPath))
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no previous VS Code settings snapshot is available.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            var settingsDirectory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            if (!_settingsFileExisted)
            {
                if (File.Exists(_settingsPath))
                {
                    File.Delete(_settingsPath);
                }
            }
            else
            {
                await File.WriteAllTextAsync(_settingsPath, _originalJson ?? "{}", ct);
            }

            var state = await LoadSettingsStateAsync(captureSnapshot: false, ct);
            MatchedChoiceKey = TryFindMatchedChoice(state.Root)?.Key;

            return new TweakResult(
                TweakStatus.RolledBack,
                _settingsFileExisted
                    ? "Restored the previous VS Code settings.json state."
                    : "Removed settings.json to restore the pre-tweak state.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Rollback error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public TweakRollbackSnapshot? GetRollbackSnapshot()
    {
        if (!HasCapturedState)
        {
            return null;
        }

        var payload = JsonSerializer.Serialize(new RollbackPayload(_settingsPath!, _settingsFileExisted, _originalJson));
        return new TweakRollbackSnapshot
        {
            TweakId = Id,
            TweakName = Name,
            SnapshotType = TweakSnapshotType.File,
            OriginalValueJson = payload,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    public void RestoreFromSnapshot(TweakRollbackSnapshot snapshot)
    {
        if (snapshot is null || !string.Equals(snapshot.TweakId, Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var payload = string.IsNullOrWhiteSpace(snapshot.OriginalValueJson)
                ? null
                : JsonSerializer.Deserialize<RollbackPayload>(snapshot.OriginalValueJson);

            if (payload is null || string.IsNullOrWhiteSpace(payload.SettingsPath))
            {
                return;
            }

            _settingsPath = payload.SettingsPath;
            _settingsFileExisted = payload.FileExisted;
            _originalJson = payload.OriginalJson;
            _hasDetected = true;
        }
        catch
        {
            _hasDetected = false;
        }
    }

    private async Task<SettingsState> LoadSettingsStateAsync(bool captureSnapshot, CancellationToken ct)
    {
        _settingsPath ??= ResolveSettingsPath();

        if (!File.Exists(_settingsPath))
        {
            if (captureSnapshot)
            {
                _hasDetected = true;
                _settingsFileExisted = false;
                _originalJson = null;
            }

            return new SettingsState(false, null, new JsonObject());
        }

        var json = await File.ReadAllTextAsync(_settingsPath, ct);
        var root = ParseSettings(json);
        if (captureSnapshot)
        {
            _hasDetected = true;
            _settingsFileExisted = true;
            _originalJson = json;
        }

        return new SettingsState(true, json, root);
    }

    private string ResolveSettingsPath()
    {
        if (!string.IsNullOrWhiteSpace(_settingsPathOverride))
        {
            return _settingsPathOverride;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Code", "User", "settings.json");
    }

    private static JsonObject ParseSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        var node = JsonNode.Parse(json, documentOptions: ReadOptions);
        return node as JsonObject
            ?? throw new InvalidOperationException("VS Code settings.json must contain a JSON object at the root.");
    }

    private static void ApplyChoice(JsonObject root, ChoiceState choice)
    {
        foreach (var key in ManagedKeys)
        {
            root.Remove(key);
        }

        foreach (var pair in choice.Settings)
        {
            root[pair.Key] = CreateJsonValue(pair.Value);
        }
    }

    private static async Task PersistSettingsAsync(string settingsPath, JsonObject root, CancellationToken ct)
    {
        var settingsDirectory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        if (root.Count == 0)
        {
            if (File.Exists(settingsPath))
            {
                File.Delete(settingsPath);
            }

            return;
        }

        var json = root.ToJsonString(WriteOptions);
        await File.WriteAllTextAsync(settingsPath, json, ct);
    }

    private static ChoiceState? TryFindMatchedChoice(JsonObject root)
    {
        return AvailableChoices.FirstOrDefault(choice => ChoiceMatches(root, choice));
    }

    private static bool ChoiceMatches(JsonObject root, ChoiceState choice)
    {
        foreach (var key in ManagedKeys)
        {
            var expectedExists = choice.Settings.TryGetValue(key, out var expectedValue);
            var actualExists = root.TryGetPropertyValue(key, out var actualNode) && actualNode is not null;

            if (!expectedExists)
            {
                if (actualExists)
                {
                    return false;
                }

                continue;
            }

            if (!actualExists || expectedValue is null || !ValuesEqual(actualNode!, expectedValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValuesEqual(JsonNode actualNode, object expectedValue)
    {
        if (expectedValue is bool expectedBool)
        {
            return actualNode is JsonValue jsonValue
                && jsonValue.TryGetValue<bool>(out var actualBool)
                && actualBool == expectedBool;
        }

        if (expectedValue is string expectedString)
        {
            return actualNode is JsonValue jsonValue
                && jsonValue.TryGetValue<string>(out var actualString)
                && string.Equals(actualString, expectedString, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(actualNode.ToJsonString(), JsonSerializer.Serialize(expectedValue), StringComparison.OrdinalIgnoreCase);
    }

    private static JsonNode? CreateJsonValue(object value)
    {
        return value switch
        {
            bool boolValue => JsonValue.Create(boolValue),
            string stringValue => JsonValue.Create(stringValue),
            int intValue => JsonValue.Create(intValue),
            long longValue => JsonValue.Create(longValue),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static ChoiceState ResolveChoice(string? key)
    {
        var choice = TryResolveChoice(key);
        return choice ?? throw new ArgumentException($"Choice '{key}' was not found.", nameof(key));
    }

    private static ChoiceState? TryResolveChoice(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return ChoicesByKey.TryGetValue(key, out var choice) ? choice : null;
    }

    private sealed record ChoiceState(
        string Key,
        string Label,
        string Description,
        IReadOnlyDictionary<string, object> Settings);

    private sealed record SettingsState(
        bool Exists,
        string? RawJson,
        JsonObject Root);

    private sealed record RollbackPayload(
        string SettingsPath,
        bool FileExisted,
        string? OriginalJson);
}
