using RegProbe.Core;

namespace RegProbe.App.Services;

/// <summary>
/// Execution strategy for batch tweak operations.
/// </summary>
public enum ExecutionStrategy
{
    /// <summary>Execute tweaks one at a time (safest, slowest).</summary>
    Sequential,

    /// <summary>Execute all tweaks in parallel (fastest, may cause issues).</summary>
    Parallel,

    /// <summary>Execute with throttled parallelism (balanced).</summary>
    ParallelThrottled,

    /// <summary>Execute by risk level - Safe first, then Advanced, then Risky.</summary>
    RiskOrdered
}

/// <summary>
/// Progress information for batch execution.
/// </summary>
public sealed record BatchProgress(
    int Completed,
    int Total,
    string CurrentTweakName,
    TweakStatus? LastStatus = null);

/// <summary>
/// Result of batch tweak execution.
/// </summary>
public sealed class BatchExecutionResult
{
    public int TotalTweaks { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public TimeSpan Duration { get; init; }
    public Dictionary<string, TweakStatus> Results { get; init; } = new();
    public Dictionary<string, string> Errors { get; init; } = new();
}
