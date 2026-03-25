using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core;

namespace OpenTraceProject.Infrastructure;

/// <summary>
/// Persists original tweak values before Apply for crash recovery.
/// Stores data in JSON format at %AppData%\OpenTraceProject\rollback-state.json
/// </summary>
public sealed class RollbackStateStore : IRollbackStateStore
{
    private readonly object _sync = new();
    private readonly AppPaths _paths;
    private readonly string _stateFilePath;
    private RollbackStateData _cachedData;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public RollbackStateStore(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _stateFilePath = Path.Combine(_paths.AppDataRoot, "rollback-state.json");
        _cachedData = new RollbackStateData();
    }

    /// <summary>
    /// Saves the original value of a tweak before applying changes.
    /// Call this BEFORE ApplyAsync to enable crash recovery.
    /// </summary>
    public async Task SaveOriginalStateAsync(RollbackEntry entry, CancellationToken ct)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));

        ct.ThrowIfCancellationRequested();
        _paths.EnsureDirectories();

        lock (_sync)
        {
            LoadIfNeeded();

            // Remove any existing entry for this tweak (update scenario)
            _cachedData.PendingRollbacks.RemoveAll(e =>
                string.Equals(e.TweakId, entry.TweakId, StringComparison.OrdinalIgnoreCase));

            // Add the new entry
            _cachedData.PendingRollbacks.Add(entry);
            _cachedData.LastUpdated = DateTimeOffset.UtcNow;

            SaveToFile();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Marks a tweak as successfully applied and verified.
    /// Removes the pending rollback entry since the apply was successful.
    /// </summary>
    public async Task MarkAppliedAsync(string tweakId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tweakId)) return;

        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            LoadIfNeeded();

            var entry = _cachedData.PendingRollbacks.FirstOrDefault(e =>
                string.Equals(e.TweakId, tweakId, StringComparison.OrdinalIgnoreCase));

            if (entry != null)
            {
                entry.Status = RollbackStatus.Applied;
                entry.AppliedAt = DateTimeOffset.UtcNow;
                _cachedData.LastUpdated = DateTimeOffset.UtcNow;
                SaveToFile();
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Marks a tweak as rolled back (either manually or after recovery).
    /// Removes the entry from the pending list.
    /// </summary>
    public async Task MarkRolledBackAsync(string tweakId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tweakId)) return;

        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            LoadIfNeeded();

            _cachedData.PendingRollbacks.RemoveAll(e =>
                string.Equals(e.TweakId, tweakId, StringComparison.OrdinalIgnoreCase));

            _cachedData.LastUpdated = DateTimeOffset.UtcNow;
            SaveToFile();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets all pending rollback entries (tweaks that were applied but not verified/rolled back).
    /// Call this on app startup to check for crash recovery scenarios.
    /// </summary>
    public Task<IReadOnlyList<RollbackEntry>> GetPendingRollbacksAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            LoadIfNeeded();

            // Return entries that are still pending (not yet applied/verified)
            IReadOnlyList<RollbackEntry> pending = _cachedData.PendingRollbacks
                .Where(e => e.Status == RollbackStatus.Pending)
                .ToList()
                .AsReadOnly();
            return Task.FromResult(pending);
        }
    }

    /// <summary>
    /// Gets the original value for a specific tweak if available.
    /// </summary>
    public Task<RollbackEntry?> GetOriginalStateAsync(string tweakId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tweakId)) return Task.FromResult<RollbackEntry?>(null);

        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            LoadIfNeeded();

            var entry = _cachedData.PendingRollbacks.FirstOrDefault(e =>
                string.Equals(e.TweakId, tweakId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(entry);
        }
    }

    /// <summary>
    /// Saves a TweakRollbackSnapshot from an IRollbackAwareTweak.
    /// Converts it to RollbackEntry internally.
    /// </summary>
    public async Task SaveSnapshotAsync(TweakRollbackSnapshot snapshot, CancellationToken ct)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var entry = new RollbackEntry
        {
            TweakId = snapshot.TweakId,
            TweakName = snapshot.TweakName,
            Category = snapshot.SnapshotType.ToString(),
            RegistryHive = snapshot.RegistryHive,
            RegistryPath = snapshot.RegistryPath,
            RegistryValueName = snapshot.RegistryValueName,
            OriginalValueKind = snapshot.RegistryValueKind,
            OriginalValue = snapshot.OriginalValueJson,
            ValueExisted = snapshot.ValueExisted,
            ServiceName = snapshot.ServiceName,
            OriginalStartMode = snapshot.OriginalStartMode,
            CapturedAt = snapshot.CapturedAt,
            Status = RollbackStatus.Pending
        };

        await SaveOriginalStateAsync(entry, ct);
    }

    /// <summary>
    /// Gets a TweakRollbackSnapshot for a specific tweak if available.
    /// </summary>
    public async Task<TweakRollbackSnapshot?> GetSnapshotAsync(string tweakId, CancellationToken ct)
    {
        var entry = await GetOriginalStateAsync(tweakId, ct);
        if (entry is null)
        {
            return null;
        }

        return new TweakRollbackSnapshot
        {
            TweakId = entry.TweakId,
            TweakName = entry.TweakName,
            SnapshotType = Enum.TryParse<TweakSnapshotType>(entry.Category, out var type) ? type : TweakSnapshotType.Other,
            RegistryHive = entry.RegistryHive,
            RegistryPath = entry.RegistryPath,
            RegistryValueName = entry.RegistryValueName,
            RegistryValueKind = entry.OriginalValueKind,
            OriginalValueJson = entry.OriginalValue?.ToString(),
            ValueExisted = entry.ValueExisted,
            ServiceName = entry.ServiceName,
            OriginalStartMode = entry.OriginalStartMode,
            CapturedAt = entry.CapturedAt
        };
    }

    /// <summary>
    /// Clears all pending rollback entries.
    /// Use with caution - typically after user confirms they don't want to recover.
    /// </summary>
    public async Task ClearAllAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_sync)
        {
            _cachedData = new RollbackStateData { LastUpdated = DateTimeOffset.UtcNow };
            SaveToFile();
        }

        await Task.CompletedTask;
    }

    private void LoadIfNeeded()
    {
        if (_cachedData.PendingRollbacks.Count > 0 || !File.Exists(_stateFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath);
            var data = JsonSerializer.Deserialize<RollbackStateData>(json, JsonOptions);
            if (data != null)
            {
                _cachedData = data;
            }
        }
        catch (Exception ex)
        {
            // Log error but continue with empty state
            LogToFile($"RollbackStateStore: Failed to load state file: {ex.Message}");
            _cachedData = new RollbackStateData();
        }
    }

    private void SaveToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cachedData, JsonOptions);
            File.WriteAllText(_stateFilePath, json);
        }
        catch (Exception ex)
        {
            LogToFile($"RollbackStateStore: Failed to save state file: {ex.Message}");
        }
    }

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(Path.GetTempPath(), "OpenTraceProject_Diagnostics.log");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}

/// <summary>
/// Interface for rollback state persistence.
/// </summary>
public interface IRollbackStateStore
{
    Task SaveOriginalStateAsync(RollbackEntry entry, CancellationToken ct);
    Task SaveSnapshotAsync(Core.TweakRollbackSnapshot snapshot, CancellationToken ct);
    Task MarkAppliedAsync(string tweakId, CancellationToken ct);
    Task MarkRolledBackAsync(string tweakId, CancellationToken ct);
    Task<IReadOnlyList<RollbackEntry>> GetPendingRollbacksAsync(CancellationToken ct);
    Task<RollbackEntry?> GetOriginalStateAsync(string tweakId, CancellationToken ct);
    Task<Core.TweakRollbackSnapshot?> GetSnapshotAsync(string tweakId, CancellationToken ct);
    Task ClearAllAsync(CancellationToken ct);
}

/// <summary>
/// Represents a single rollback entry with original state information.
/// </summary>
public sealed class RollbackEntry
{
    public string TweakId { get; set; } = string.Empty;
    public string TweakName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // Registry-specific fields
    public string? RegistryHive { get; set; }
    public string? RegistryPath { get; set; }
    public string? RegistryValueName { get; set; }
    public string? OriginalValueKind { get; set; }
    public object? OriginalValue { get; set; }
    public bool ValueExisted { get; set; }

    // Service-specific fields
    public string? ServiceName { get; set; }
    public string? OriginalStartMode { get; set; }

    // Timestamps
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AppliedAt { get; set; }

    // Status
    public RollbackStatus Status { get; set; } = RollbackStatus.Pending;
}

/// <summary>
/// Status of a rollback entry.
/// </summary>
public enum RollbackStatus
{
    /// <summary>Original state captured, apply in progress or not yet verified.</summary>
    Pending,

    /// <summary>Apply succeeded and was verified.</summary>
    Applied,

    /// <summary>Entry was rolled back (manually or via recovery).</summary>
    RolledBack
}

/// <summary>
/// Container for the rollback state file.
/// </summary>
internal sealed class RollbackStateData
{
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public List<RollbackEntry> PendingRollbacks { get; set; } = new();
}
