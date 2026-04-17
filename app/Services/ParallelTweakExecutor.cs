using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.App.Services;

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
        var tracker = new BatchExecutionResultTracker();

        var orderedTweaks = OrderTweaks(tweaks, strategy);

        switch (strategy)
        {
            case ExecutionStrategy.Sequential:
                foreach (var tweak in orderedTweaks)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var result = await ExecuteSingleAsync(tweak, options, cancellationToken);
                    var progressState = tracker.Record(tweak, result);
                    progress?.Report(new BatchProgress(progressState.Completed, tweaks.Count, tweak.Name, progressState.Status));
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
                        var progressState = tracker.Record(tweak, result);
                        progress?.Report(new BatchProgress(progressState.Completed, tweaks.Count, tweak.Name, progressState.Status));
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
                            var progressState = tracker.Record(tweak, result);
                            progress?.Report(new BatchProgress(progressState.Completed, tweaks.Count, tweak.Name, progressState.Status));
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
                            var progressState = tracker.Record(tweak, result);
                            progress?.Report(new BatchProgress(progressState.Completed, tweaks.Count, tweak.Name, progressState.Status));
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

        return tracker.ToResult(tweaks.Count, DateTime.UtcNow - startTime);
    }
    
    private async Task<TweakExecutionReport> ExecuteSingleAsync(
        ITweak tweak, 
        TweakExecutionOptions? options, 
        CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(tweak, options, ct: ct);
    }

    private static IEnumerable<ITweak> OrderTweaks(IReadOnlyList<ITweak> tweaks, ExecutionStrategy strategy)
    {
        return strategy switch
        {
            ExecutionStrategy.RiskOrdered => tweaks.OrderBy(t => t.Risk),
            _ => tweaks
        };
    }
}
