using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core;

namespace OpenTraceProject.Engine.Tweaks.Developer;

public sealed class EnableDockerWsl2BackendTweak : ITweak
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly string? _settingsPathOverride;
    private string? _settingsPath;
    private bool _hasDetected;
    private bool _settingsFileExisted;
    private string? _originalJson;

    public EnableDockerWsl2BackendTweak(string? settingsPath = null)
    {
        _settingsPathOverride = settingsPath;
        Id = "developer.docker-performance";
        Name = "Enable Docker Desktop WSL 2 Backend";
        Description = "Writes Docker Desktop's WSL engine setting in settings-store.json so Docker uses the WSL 2 backend on Windows.";
        Risk = TweakRiskLevel.Advanced;
        RequiresElevation = false;
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
            var state = await LoadStateAsync(captureSnapshot: true, ct);
            var enabled = IsWslEngineEnabled(state.Root);
            var message = enabled
                ? "Docker Desktop WSL 2 backend is enabled."
                : "Docker Desktop WSL 2 backend is disabled or not configured.";

            return new TweakResult(
                enabled ? TweakStatus.Applied : TweakStatus.Detected,
                message,
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
            var state = await LoadStateAsync(captureSnapshot: false, ct);
            SetWslEngineEnabled(state.Root, true);
            await PersistSettingsAsync(_settingsPath, state.Root, ct);

            return new TweakResult(
                TweakStatus.Applied,
                "Enabled Docker Desktop WSL 2 backend.",
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
            var state = await LoadStateAsync(captureSnapshot: false, ct);
            if (IsWslEngineEnabled(state.Root))
            {
                return new TweakResult(
                    TweakStatus.Verified,
                    "Verified Docker Desktop WSL 2 backend is enabled.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.Failed,
                "Verification failed. Docker Desktop WSL 2 backend is not enabled.",
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
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            if (!_settingsFileExisted)
            {
                if (File.Exists(_settingsPath))
                {
                    File.Delete(_settingsPath);
                }

                return new TweakResult(
                    TweakStatus.RolledBack,
                    "Removed Docker settings file created by the tweak.",
                    DateTimeOffset.UtcNow);
            }

            await File.WriteAllTextAsync(_settingsPath, _originalJson ?? string.Empty, ct);
            return new TweakResult(
                TweakStatus.RolledBack,
                "Restored original Docker settings file.",
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

    private async Task<DockerSettingsState> LoadStateAsync(bool captureSnapshot, CancellationToken ct)
    {
        var path = ResolveSettingsPath();
        _settingsPath = path;

        var fileExists = File.Exists(path);
        var json = fileExists ? await File.ReadAllTextAsync(path, ct) : null;

        if (captureSnapshot)
        {
            _settingsFileExisted = fileExists;
            _originalJson = json;
            _hasDetected = true;
        }

        return new DockerSettingsState(fileExists, json, ParseSettings(json));
    }

    private string ResolveSettingsPath()
    {
        if (!string.IsNullOrWhiteSpace(_settingsPathOverride))
        {
            return _settingsPathOverride;
        }

        var dockerRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Docker");
        var settingsStorePath = Path.Combine(dockerRoot, "settings-store.json");
        var legacySettingsPath = Path.Combine(dockerRoot, "settings.json");

        if (File.Exists(settingsStorePath))
        {
            return settingsStorePath;
        }

        if (File.Exists(legacySettingsPath))
        {
            return legacySettingsPath;
        }

        return settingsStorePath;
    }

    private static JsonObject ParseSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        var node = JsonNode.Parse(json, documentOptions: ReadOptions);
        return node as JsonObject
            ?? throw new InvalidOperationException("Docker settings file must contain a JSON object at the root.");
    }

    private static bool IsWslEngineEnabled(JsonObject root)
    {
        if (root.TryGetPropertyValue("wslEngineEnabled", out var flatNode) && TryGetBooleanValue(flatNode, out var flatValue))
        {
            return flatValue;
        }

        if (root.TryGetPropertyValue("linuxVM", out var linuxVmNode) && linuxVmNode is JsonObject linuxVm)
        {
            if (linuxVm.TryGetPropertyValue("wslEngineEnabled", out var wslNode))
            {
                if (wslNode is JsonObject nested && nested.TryGetPropertyValue("value", out var valueNode) && TryGetBooleanValue(valueNode, out var nestedValue))
                {
                    return nestedValue;
                }

                if (TryGetBooleanValue(wslNode, out var directNestedValue))
                {
                    return directNestedValue;
                }
            }
        }

        return false;
    }

    private static void SetWslEngineEnabled(JsonObject root, bool enabled)
    {
        var linuxVm = GetOrCreateObject(root, "linuxVM");
        var wslEngine = GetOrCreateObject(linuxVm, "wslEngineEnabled");
        wslEngine["locked"] = false;
        wslEngine["value"] = enabled;
    }

    private static JsonObject GetOrCreateObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        parent[propertyName] = created;
        return created;
    }

    private static bool TryGetBooleanValue(JsonNode? node, out bool value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value))
        {
            return true;
        }

        value = false;
        return false;
    }

    private static async Task PersistSettingsAsync(string settingsPath, JsonObject root, CancellationToken ct)
    {
        var settingsDirectory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        await File.WriteAllTextAsync(settingsPath, root.ToJsonString(WriteOptions), ct);
    }

    private sealed record DockerSettingsState(bool FileExisted, string? OriginalJson, JsonObject Root);
}
