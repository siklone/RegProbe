using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenTraceProject.Core;

namespace OpenTraceProject.Engine.Tweaks;

public sealed class CompositeTweak : ITweak
{
    private readonly IReadOnlyList<ITweak> _tweaks;

    public CompositeTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<ITweak> tweaks)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;

        if (tweaks is null)
        {
            throw new ArgumentNullException(nameof(tweaks));
        }

        if (tweaks.Count == 0)
        {
            throw new ArgumentException("At least one sub-tweak is required.", nameof(tweaks));
        }

        if (tweaks.Any(tweak => tweak is null))
        {
            throw new ArgumentException("Sub-tweaks cannot contain null values.", nameof(tweaks));
        }

        _tweaks = tweaks;
        RequiresElevation = _tweaks.Any(tweak => tweak.RequiresElevation);
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
            var results = new List<TweakResult>();
            foreach (var tweak in _tweaks)
            {
                var result = await tweak.DetectAsync(ct);
                results.Add(result);
                if (result.Status == TweakStatus.Failed)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Detect failed: {result.Message}",
                        DateTimeOffset.UtcNow,
                        result.Error);
                }
            }

            if (results.All(result => result.Status == TweakStatus.NotApplicable))
            {
                return new TweakResult(TweakStatus.NotApplicable, "No sub-tweaks applicable.", DateTimeOffset.UtcNow);
            }

            var applicableResults = results.Where(result => result.Status != TweakStatus.NotApplicable).ToList();
            if (applicableResults.Count > 0
                && applicableResults.All(result => result.Status is TweakStatus.Applied or TweakStatus.Verified))
            {
                return new TweakResult(
                    TweakStatus.Applied,
                    $"All {applicableResults.Count} applicable sub-tweaks already match the desired configuration.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(
                TweakStatus.Detected,
                $"Detected {applicableResults.Count} applicable sub-tweaks.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Detect failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            foreach (var tweak in _tweaks)
            {
                var result = await tweak.ApplyAsync(ct);
                if (result.Status == TweakStatus.Failed)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Apply failed: {result.Message}",
                        DateTimeOffset.UtcNow,
                        result.Error);
                }
            }

            return new TweakResult(TweakStatus.Applied, "Applied composite tweak.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Apply failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            foreach (var tweak in _tweaks)
            {
                var result = await tweak.VerifyAsync(ct);
                if (result.Status == TweakStatus.Failed)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Verify failed: {result.Message}",
                        DateTimeOffset.UtcNow,
                        result.Error);
                }
            }

            return new TweakResult(TweakStatus.Verified, "Verified composite tweak.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Verify failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var failures = 0;
            foreach (var tweak in _tweaks)
            {
                var result = await tweak.RollbackAsync(ct);
                if (result.Status == TweakStatus.Failed)
                {
                    failures++;
                }
            }

            if (failures > 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Rollback failed for {failures} sub-tweaks.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(TweakStatus.RolledBack, "Rolled back composite tweak.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Rollback failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }
}
