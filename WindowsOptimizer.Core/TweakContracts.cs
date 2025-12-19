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
    Task<TweakResult> DetectAsync(CancellationToken ct);
    Task<TweakResult> ApplyAsync(CancellationToken ct);
    Task<TweakResult> VerifyAsync(CancellationToken ct);
    Task<TweakResult> RollbackAsync(CancellationToken ct);
}
