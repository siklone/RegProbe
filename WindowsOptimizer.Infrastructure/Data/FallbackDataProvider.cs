using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Data;

/// <summary>
/// Provides data fetching with multiple sources and graceful fallbacks.
/// Tries sources in priority order until one succeeds.
/// </summary>
/// <typeparam name="T">Type of data to fetch.</typeparam>
public class FallbackDataProvider<T>
{
    private readonly List<DataSource<T>> _sources = new();
    private readonly RetryPolicy _retryPolicy;
    private readonly int _circuitBreakerThreshold;
    private readonly TimeSpan _circuitBreakerCooldown;

    /// <summary>
    /// Creates a new FallbackDataProvider.
    /// </summary>
    /// <param name="retryPolicy">Retry policy for individual sources.</param>
    /// <param name="circuitBreakerThreshold">Number of failures before disabling a source.</param>
    /// <param name="circuitBreakerCooldown">How long to disable a failed source.</param>
    public FallbackDataProvider(
        RetryPolicy? retryPolicy = null,
        int circuitBreakerThreshold = 3,
        TimeSpan? circuitBreakerCooldown = null)
    {
        _retryPolicy = retryPolicy ?? RetryPolicy.Default;
        _circuitBreakerThreshold = circuitBreakerThreshold;
        _circuitBreakerCooldown = circuitBreakerCooldown ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Adds a data source to the provider.
    /// </summary>
    /// <param name="name">Name for logging/diagnostics.</param>
    /// <param name="getter">Async function to fetch data.</param>
    /// <param name="priority">Priority (higher = tried first).</param>
    /// <param name="timeout">Timeout for this source.</param>
    /// <returns>This provider for fluent chaining.</returns>
    public FallbackDataProvider<T> AddSource(
        string name,
        Func<CancellationToken, Task<T?>> getter,
        int priority = 0,
        TimeSpan? timeout = null)
    {
        _sources.Add(new DataSource<T>
        {
            Name = name,
            Getter = getter,
            Priority = priority,
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        });

        // Keep sources sorted by priority (descending)
        _sources.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return this;
    }

    /// <summary>
    /// Adds a synchronous data source.
    /// </summary>
    public FallbackDataProvider<T> AddSource(
        string name,
        Func<T?> getter,
        int priority = 0,
        TimeSpan? timeout = null)
    {
        return AddSource(
            name,
            _ => Task.FromResult(getter()),
            priority,
            timeout ?? TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Fetches data from sources in priority order until one succeeds.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the data or error information.</returns>
    public async Task<DataResult<T>> GetAsync(CancellationToken ct = default)
    {
        var errors = new List<DataSourceError>();
        var sw = Stopwatch.StartNew();

        foreach (var source in _sources)
        {
            // Check circuit breaker
            if (source.IsDisabled)
            {
                if (source.DisabledUntil.HasValue && DateTime.UtcNow < source.DisabledUntil.Value)
                {
                    Debug.WriteLine($"[FallbackDataProvider] Skipping disabled source: {source.Name}");
                    continue;
                }
                // Re-enable after cooldown
                source.IsDisabled = false;
                source.ConsecutiveFailures = 0;
            }

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(source.Timeout);

                var result = await ExecuteWithRetryAsync(
                    source.Getter,
                    timeoutCts.Token);

                if (result != null)
                {
                    // Success - reset failure count
                    source.ConsecutiveFailures = 0;
                    Debug.WriteLine($"[FallbackDataProvider] Data fetched from: {source.Name}");

                    sw.Stop();
                    return DataResult<T>.Ok(result, source.Name, sw.Elapsed);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout
                errors.Add(new DataSourceError(source.Name, "Timeout"));
                Debug.WriteLine($"[FallbackDataProvider] Source timed out: {source.Name}");
                HandleSourceFailure(source);
            }
            catch (Exception ex)
            {
                errors.Add(new DataSourceError(source.Name, ex.Message, ex));
                Debug.WriteLine($"[FallbackDataProvider] Source failed: {source.Name} - {ex.Message}");
                HandleSourceFailure(source);
            }
        }

        sw.Stop();
        return DataResult<T>.Fail(errors, sw.Elapsed);
    }

    /// <summary>
    /// Synchronously fetches data (blocks until complete).
    /// </summary>
    public DataResult<T> Get(CancellationToken ct = default)
    {
        return GetAsync(ct).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the value directly, or default if all sources fail.
    /// </summary>
    public async Task<T?> GetValueOrDefaultAsync(CancellationToken ct = default)
    {
        var result = await GetAsync(ct);
        return result.Value;
    }

    private void HandleSourceFailure(DataSource<T> source)
    {
        source.ConsecutiveFailures++;

        if (source.ConsecutiveFailures >= _circuitBreakerThreshold)
        {
            source.IsDisabled = true;
            source.DisabledUntil = DateTime.UtcNow + _circuitBreakerCooldown;
            Debug.WriteLine($"[FallbackDataProvider] Circuit breaker triggered for: {source.Name}");
        }
    }

    private async Task<T?> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<T?>> operation,
        CancellationToken ct)
    {
        var delay = _retryPolicy.InitialDelay;

        for (int attempt = 0; attempt <= _retryPolicy.MaxRetries; attempt++)
        {
            try
            {
                return await operation(ct);
            }
            catch when (attempt < _retryPolicy.MaxRetries && !ct.IsCancellationRequested)
            {
                Debug.WriteLine($"[FallbackDataProvider] Retry {attempt + 1}/{_retryPolicy.MaxRetries}");
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(
                    Math.Min(
                        delay.TotalMilliseconds * _retryPolicy.BackoffMultiplier,
                        _retryPolicy.MaxDelay.TotalMilliseconds));
            }
        }

        return default;
    }

    /// <summary>
    /// Gets the names of all registered sources.
    /// </summary>
    public IEnumerable<string> GetSourceNames()
    {
        foreach (var source in _sources)
        {
            yield return source.Name;
        }
    }

    /// <summary>
    /// Gets the number of registered sources.
    /// </summary>
    public int SourceCount => _sources.Count;

    /// <summary>
    /// Resets all circuit breakers.
    /// </summary>
    public void ResetCircuitBreakers()
    {
        foreach (var source in _sources)
        {
            source.IsDisabled = false;
            source.ConsecutiveFailures = 0;
            source.DisabledUntil = null;
        }
    }
}
