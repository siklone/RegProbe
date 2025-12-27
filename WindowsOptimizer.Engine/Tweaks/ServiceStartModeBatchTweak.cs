using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Services;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed class ServiceStartModeBatchTweak : ITweak
{
    private readonly IReadOnlyList<ServiceStartModeEntry> _entries;
    private readonly IServiceManager _serviceManager;
    private readonly Dictionary<string, ServiceSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _stopRunning;
    private IReadOnlyList<ServiceTarget>? _resolvedTargets;
    private bool _hasDetected;

    public ServiceStartModeBatchTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<ServiceStartModeEntry> entries,
        IServiceManager serviceManager,
        bool stopRunning = true,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
        _stopRunning = stopRunning;

        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        if (entries.Count == 0)
        {
            throw new ArgumentException("At least one service entry is required.", nameof(entries));
        }

        foreach (var entry in entries)
        {
            if (entry is null)
            {
                throw new ArgumentException("Entries cannot contain null values.", nameof(entries));
            }

            if (string.IsNullOrWhiteSpace(entry.ServiceName))
            {
                throw new ArgumentException("Service names must be provided.", nameof(entries));
            }

            if (entry.TargetStartMode == ServiceStartMode.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(entries), entry.TargetStartMode, "Target start mode must be a concrete value.");
            }
        }

        _entries = entries;
        TargetStartModeSummary = entries.Select(entry => entry.TargetStartMode).Distinct().Count() == 1
            ? entries[0].TargetStartMode
            : ServiceStartMode.Unknown;
        RequiresElevation = requiresElevation ?? true;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }
    public ServiceStartMode TargetStartModeSummary { get; }

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            _snapshots.Clear();
            var targets = await ResolveTargetsAsync(ct);
            foreach (var target in targets)
            {
                var info = await _serviceManager.QueryAsync(target.ServiceName, ct);
                _snapshots[target.ServiceName] = new ServiceSnapshot(info.Exists, info.StartMode, info.Status);
            }

            _hasDetected = true;
            var detectedCount = _snapshots.Values.Count(snapshot => snapshot.Exists);
            var missingCount = targets.Count - detectedCount;
            var matchingTargets = targets.Count(target =>
                _snapshots.TryGetValue(target.ServiceName, out var snapshot)
                && snapshot.Exists
                && snapshot.StartMode == target.TargetStartMode);

            var status = detectedCount == 0
                ? TweakStatus.NotApplicable
                : matchingTargets == detectedCount
                    ? TweakStatus.Applied
                    : TweakStatus.Detected;

            var summary = missingCount > 0
                ? $"Detected {detectedCount} of {targets.Count} services ({missingCount} missing)."
                : $"Detected {detectedCount} of {targets.Count} services.";

            var currentState = detectedCount == 0
                ? "Not present"
                : GetStartModeSummary(_snapshots.Values.Where(snapshot => snapshot.Exists).Select(snapshot => snapshot.StartMode));

            var message = $"{summary} Current state: {currentState}.";
            return new TweakResult(status, message, DateTimeOffset.UtcNow);
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
            var targets = await ResolveTargetsAsync(ct);
            var updatedCount = 0;
            var stopFailures = 0;

            foreach (var target in targets)
            {
                var snapshot = _snapshots.TryGetValue(target.ServiceName, out var cached)
                    ? cached
                    : await GetSnapshotAsync(target.ServiceName, ct);

                if (!snapshot.Exists)
                {
                    continue;
                }

                await _serviceManager.SetStartModeAsync(target.ServiceName, target.TargetStartMode, ct);
                updatedCount++;

                if (_stopRunning && snapshot.Status == ServiceStatus.Running)
                {
                    try
                    {
                        await _serviceManager.StopAsync(target.ServiceName, ct);
                    }
                    catch
                    {
                        stopFailures++;
                    }
                }
            }

            var message = stopFailures > 0
                ? $"Updated {updatedCount} services (failed to stop {stopFailures})."
                : $"Updated {updatedCount} services.";
            return new TweakResult(TweakStatus.Applied, message, DateTimeOffset.UtcNow);
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
            var targets = await ResolveTargetsAsync(ct);
            foreach (var target in targets)
            {
                var info = await _serviceManager.QueryAsync(target.ServiceName, ct);
                if (!info.Exists)
                {
                    continue;
                }

                if (info.StartMode != target.TargetStartMode)
                {
                    return new TweakResult(
                        TweakStatus.Failed,
                        $"Verification failed. '{target.ServiceName}' expected {target.TargetStartMode}, found {info.StartMode}.",
                        DateTimeOffset.UtcNow);
                }
            }

            return new TweakResult(TweakStatus.Verified, "Verified service start modes.", DateTimeOffset.UtcNow);
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

        if (!_hasDetected)
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            var failures = new List<string>();

            foreach (var (serviceName, snapshot) in _snapshots)
            {
                if (!snapshot.Exists || snapshot.StartMode == ServiceStartMode.Unknown)
                {
                    continue;
                }

                try
                {
                    await _serviceManager.SetStartModeAsync(serviceName, snapshot.StartMode, ct);
                }
                catch (Exception ex)
                {
                    failures.Add($"{serviceName}: {ex.Message}");
                    continue;
                }

                if (snapshot.Status == ServiceStatus.Running)
                {
                    try
                    {
                        await _serviceManager.StartAsync(serviceName, ct);
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{serviceName}: {ex.Message}");
                    }
                }
            }

            if (failures.Count > 0)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    $"Rollback failed for {failures.Count} services.",
                    DateTimeOffset.UtcNow);
            }

            return new TweakResult(TweakStatus.RolledBack, "Rolled back service start modes.", DateTimeOffset.UtcNow);
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

    private async Task<IReadOnlyList<ServiceTarget>> ResolveTargetsAsync(CancellationToken ct)
    {
        if (_resolvedTargets is not null)
        {
            return _resolvedTargets;
        }

        var serviceNames = await _serviceManager.ListServiceNamesAsync(ct);
        var targets = new Dictionary<string, ServiceStartMode>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _entries)
        {
            var name = entry.ServiceName.Trim();
            if (name.Contains('*'))
            {
                var prefix = name.Replace("*", string.Empty, StringComparison.Ordinal);
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                foreach (var candidate in serviceNames.Where(candidate => candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    targets[candidate] = entry.TargetStartMode;
                }

                continue;
            }

            targets[name] = entry.TargetStartMode;
            var instancePrefix = name + "_";
            foreach (var candidate in serviceNames.Where(candidate => candidate.StartsWith(instancePrefix, StringComparison.OrdinalIgnoreCase)))
            {
                targets[candidate] = entry.TargetStartMode;
            }
        }

        _resolvedTargets = targets.Select(pair => new ServiceTarget(pair.Key, pair.Value)).ToList();
        return _resolvedTargets;
    }

    private async Task<ServiceSnapshot> GetSnapshotAsync(string serviceName, CancellationToken ct)
    {
        var info = await _serviceManager.QueryAsync(serviceName, ct);
        return new ServiceSnapshot(info.Exists, info.StartMode, info.Status);
    }

    private sealed record ServiceTarget(string ServiceName, ServiceStartMode TargetStartMode);

    private sealed record ServiceSnapshot(bool Exists, ServiceStartMode StartMode, ServiceStatus Status);

    private static string GetStartModeSummary(IEnumerable<ServiceStartMode> startModes)
    {
        var distinct = startModes
            .Where(mode => mode != ServiceStartMode.Unknown)
            .Distinct()
            .ToArray();

        if (distinct.Length == 0)
        {
            return "Unknown";
        }

        if (distinct.Length == 1)
        {
            return distinct[0].ToString();
        }

        return "Mixed";
    }
}
