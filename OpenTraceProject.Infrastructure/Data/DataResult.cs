using System;
using System.Collections.Generic;

namespace OpenTraceProject.Infrastructure.Data;

/// <summary>
/// Represents the result of a data fetch operation.
/// </summary>
/// <typeparam name="T">Type of the fetched data.</typeparam>
public class DataResult<T>
{
    /// <summary>
    /// The fetched value, or default if fetch failed.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Name of the data source that successfully provided the value.
    /// </summary>
    public string Source { get; init; } = "";

    /// <summary>
    /// Whether the fetch was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Errors from failed data sources (for diagnostics).
    /// </summary>
    public List<DataSourceError> Errors { get; init; } = new();

    /// <summary>
    /// Time taken to fetch the data.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DataResult<T> Ok(T value, string source, TimeSpan duration) => new()
    {
        Value = value,
        Source = source,
        Success = true,
        Duration = duration
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DataResult<T> Fail(List<DataSourceError> errors, TimeSpan duration) => new()
    {
        Value = default,
        Success = false,
        Errors = errors,
        Duration = duration
    };
}

/// <summary>
/// Represents an error from a specific data source.
/// </summary>
/// <param name="Source">Name of the data source that failed.</param>
/// <param name="Message">Error message.</param>
/// <param name="Exception">Optional exception details.</param>
public record DataSourceError(string Source, string Message, Exception? Exception = null)
{
    /// <summary>
    /// Whether this error was due to a timeout.
    /// </summary>
    public bool IsTimeout => Message == "Timeout" || Exception is OperationCanceledException;
}

/// <summary>
/// Internal representation of a data source.
/// </summary>
internal class DataSource<T>
{
    /// <summary>
    /// Name of the data source (for logging/diagnostics).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Async function to fetch data.
    /// </summary>
    public required Func<CancellationToken, Task<T?>> Getter { get; init; }

    /// <summary>
    /// Priority (higher = tried first).
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Timeout for this source.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether this source is currently disabled (after repeated failures).
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Count of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// When this source can be re-enabled.
    /// </summary>
    public DateTime? DisabledUntil { get; set; }
}
