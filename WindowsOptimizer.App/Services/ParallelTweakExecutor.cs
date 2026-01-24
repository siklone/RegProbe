using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.App.Services;

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

/// <summary>
/// Executes multiple tweaks with configurable parallelism and strategies.
/// </summary>
public sealed class ParallelTweakExecutor
{
    private readonly TweakExecutionPipeline _pipeline;
    private readonly int _maxConcurrency;
    
    public ParallelTweakExecutor(TweakExecutionPipeline pipeline, int maxConcurrency = 4)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _maxConcurrency = Math.Max(1, Math.Min(maxConcurrency, Environment.ProcessorCount));
    }
    
    /// <summary>
    /// Executes a batch of tweaks with the specified strategy.
    /// </summary>
    public async Task<BatchExecutionResult> ExecuteAsync(
        IReadOnlyList<ITweak> tweaks,
        TweakExecutionOptions? options = null,
        ExecutionStrategy strategy = ExecutionStrategy.ParallelThrottled,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (tweaks == null || tweaks.Count == 0)
            return new BatchExecutionResult { TotalTweaks = 0 };
        
        var startTime = DateTime.UtcNow;
        var results = new Dictionary<string, TweakStatus>();
        var errors = new Dictionary<string, string>();
        var completed = 0;
        var successful = 0;
        var failed = 0;
        var skipped = 0;
        
        var orderedTweaks = OrderTweaks(tweaks, strategy);
        
        switch (strategy)
        {
            case ExecutionStrategy.Sequential:
                foreach (var tweak in orderedTweaks)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    var result = await ExecuteSingleAsync(tweak, options, cancellationToken);
                    ProcessResult(tweak, result, ref successful, ref failed, ref skipped, results, errors);
                    
                    completed++;
                    progress?.Report(new BatchProgress(completed, tweaks.Count, tweak.Name, GetStatus(result)));
                }
                break;
                
            case ExecutionStrategy.Parallel:
                await Parallel.ForEachAsync(orderedTweaks, 
                    new ParallelOptions 
                    { 
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = cancellationToken 
                    },
                    async (tweak, ct) =>
                    {
                        var result = await ExecuteSingleAsync(tweak, options, ct);
                        lock (results)
                        {
                            ProcessResult(tweak, result, ref successful, ref failed, ref skipped, results, errors);
                            completed++;
                        }
                        progress?.Report(new BatchProgress(completed, tweaks.Count, tweak.Name, GetStatus(result)));
                    });
                break;
                
            case ExecutionStrategy.ParallelThrottled:
                using (var semaphore = new SemaphoreSlim(_maxConcurrency))
                {
                    var tasks = orderedTweaks.Select(async tweak =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var result = await ExecuteSingleAsync(tweak, options, cancellationToken);
                            lock (results)
                            {
                                ProcessResult(tweak, result, ref successful, ref failed, ref skipped, results, errors);
                                completed++;
                            }
                            progress?.Report(new BatchProgress(completed, tweaks.Count, tweak.Name, GetStatus(result)));
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    
                    await Task.WhenAll(tasks);
                }
                break;
                
            case ExecutionStrategy.RiskOrdered:
                // Execute by risk level groups sequentially, but parallelize within each group
                var riskGroups = orderedTweaks.GroupBy(t => t.Risk).OrderBy(g => g.Key);
                
                foreach (var group in riskGroups)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    using var semaphore = new SemaphoreSlim(_maxConcurrency);
                    var tasks = group.Select(async tweak =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var result = await ExecuteSingleAsync(tweak, options, cancellationToken);
                            lock (results)
                            {
                                ProcessResult(tweak, result, ref successful, ref failed, ref skipped, results, errors);
                                completed++;
                            }
                            progress?.Report(new BatchProgress(completed, tweaks.Count, tweak.Name, GetStatus(result)));
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    
                    await Task.WhenAll(tasks);
                }
                break;
        }
        
        return new BatchExecutionResult
        {
            TotalTweaks = tweaks.Count,
            Successful = successful,
            Failed = failed,
            Skipped = skipped,
            Duration = DateTime.UtcNow - startTime,
            Results = results,
            Errors = errors
        };
    }
    
    private async Task<TweakExecutionReport> ExecuteSingleAsync(
        ITweak tweak, 
        TweakExecutionOptions? options, 
        CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(tweak, options, ct: ct);
    }
    
    private static void ProcessResult(
        ITweak tweak,
        TweakExecutionReport result,
        ref int successful,
        ref int failed,
        ref int skipped,
        Dictionary<string, TweakStatus> results,
        Dictionary<string, string> errors)
    {
        // Determine status from report
        TweakStatus status;
        if (result.RolledBack) status = TweakStatus.RolledBack;
        else if (result.Verified) status = TweakStatus.Verified;
        else if (result.Applied) status = TweakStatus.Applied;
        else if (!result.Succeeded) status = TweakStatus.Failed;
        else status = TweakStatus.Skipped;
        
        results[tweak.Id] = status;
        
        if (result.Applied || result.Verified || result.RolledBack)
        {
            Interlocked.Increment(ref successful);
        }
        else if (!result.Succeeded)
        {
            Interlocked.Increment(ref failed);
            var failedStep = result.Steps.FirstOrDefault(s => s.Result.Status == TweakStatus.Failed);
            if (failedStep != null)
                errors[tweak.Id] = failedStep.Result.Message ?? "Unknown error";
        }
        else
        {
            Interlocked.Increment(ref skipped);
        }
    }
    
    private static IEnumerable<ITweak> OrderTweaks(IReadOnlyList<ITweak> tweaks, ExecutionStrategy strategy)
    {
        return strategy switch
        {
            ExecutionStrategy.RiskOrdered => tweaks.OrderBy(t => t.Risk),
            _ => tweaks
        };
    }
    
    private static TweakStatus GetStatus(TweakExecutionReport result)
    {
        if (result.RolledBack) return TweakStatus.RolledBack;
        if (result.Verified) return TweakStatus.Verified;
        if (result.Applied) return TweakStatus.Applied;
        if (!result.Succeeded) return TweakStatus.Failed;
        return TweakStatus.Skipped;
    }
}
