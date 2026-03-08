using System;

namespace WindowsOptimizer.Infrastructure.Data;

/// <summary>
/// Defines retry behavior for data fetching operations.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts (0 = no retries).
    /// </summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>
    /// Initial delay before first retry.
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Multiplier applied to delay after each retry (exponential backoff).
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Default retry policy (2 retries, 100ms initial delay, 2x backoff).
    /// </summary>
    public static RetryPolicy Default => new();

    /// <summary>
    /// No retry policy (fails immediately on first error).
    /// </summary>
    public static RetryPolicy NoRetry => new() { MaxRetries = 0 };

    /// <summary>
    /// Aggressive retry policy (3 retries, 50ms initial delay).
    /// </summary>
    public static RetryPolicy Aggressive => new()
    {
        MaxRetries = 3,
        InitialDelay = TimeSpan.FromMilliseconds(50),
        MaxDelay = TimeSpan.FromSeconds(1)
    };

    /// <summary>
    /// Patient retry policy (5 retries, longer delays for slow services).
    /// </summary>
    public static RetryPolicy Patient => new()
    {
        MaxRetries = 5,
        InitialDelay = TimeSpan.FromMilliseconds(200),
        BackoffMultiplier = 1.5,
        MaxDelay = TimeSpan.FromSeconds(5)
    };
}
