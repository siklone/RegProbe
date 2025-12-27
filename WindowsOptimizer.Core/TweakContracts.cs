using System;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core;

public enum TweakRiskLevel
{
    Safe,
    Advanced,
    Risky
}

public enum TweakStatus
{
    NotApplicable,
    Detected,
    Applied,
    Verified,
    RolledBack,
    Skipped,
    Failed
}

public enum TweakAction
{
    Detect,
    Apply,
    Verify,
    Rollback
}

public sealed record TweakResult(
    TweakStatus Status,
    string Message,
    DateTimeOffset Timestamp,
    Exception? Error = null);

public interface ITweak
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    TweakRiskLevel Risk { get; }
    bool RequiresElevation { get; }
    Task<TweakResult> DetectAsync(CancellationToken ct);
    Task<TweakResult> ApplyAsync(CancellationToken ct);
    Task<TweakResult> VerifyAsync(CancellationToken ct);
    Task<TweakResult> RollbackAsync(CancellationToken ct);
}

/// <summary>
/// Optional interface for tweaks that support durable rollback state capture.
/// Implement this to enable crash recovery and persistent rollback.
/// </summary>
public interface IRollbackAwareTweak : ITweak
{
    /// <summary>
    /// Returns true if Detect has been called and original state is available.
    /// </summary>
    bool HasCapturedState { get; }

    /// <summary>
    /// Gets the captured original state after Detect, or null if not available.
    /// Call this AFTER DetectAsync but BEFORE ApplyAsync.
    /// </summary>
    TweakRollbackSnapshot? GetRollbackSnapshot();

    /// <summary>
    /// Restores state from a persisted snapshot. Used for crash recovery.
    /// </summary>
    void RestoreFromSnapshot(TweakRollbackSnapshot snapshot);
}

/// <summary>
/// Serializable snapshot of a tweak's original state for crash recovery.
/// </summary>
public sealed class TweakRollbackSnapshot
{
    public string TweakId { get; init; } = string.Empty;
    public string TweakName { get; init; } = string.Empty;
    public TweakSnapshotType SnapshotType { get; init; }

    // Registry-specific
    public string? RegistryHive { get; init; }
    public string? RegistryPath { get; init; }
    public string? RegistryValueName { get; init; }
    public string? RegistryValueKind { get; init; }
    public string? OriginalValueJson { get; init; }
    public bool ValueExisted { get; init; }

    // Service-specific (future)
    public string? ServiceName { get; init; }
    public string? OriginalStartMode { get; init; }

    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum TweakSnapshotType
{
    Registry,
    Service,
    File,
    Other
}
