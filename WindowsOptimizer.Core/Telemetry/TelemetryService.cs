using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Telemetry;

/// <summary>
/// Anonymous telemetry service for crowdsourced optimization intelligence
/// </summary>
public sealed class TelemetryService
{
    private readonly Queue<TelemetryEvent> _eventQueue = new();
    private readonly object _queueLock = new();
    private bool _enabled;
    private string? _anonymousUserId;

    public TelemetryService()
    {
        _enabled = false; // Opt-in by default
    }

    /// <summary>
    /// Enable or disable telemetry
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (enabled && _anonymousUserId == null)
        {
            _anonymousUserId = Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>
    /// Track a tweak application event
    /// </summary>
    public void TrackTweakApplied(TweakTelemetry tweak)
    {
        if (!_enabled) return;

        var telemetryEvent = new TelemetryEvent
        {
            EventType = "TweakApplied",
            Timestamp = DateTime.UtcNow,
            UserId = _anonymousUserId!,
            Data = new Dictionary<string, object>
            {
                ["TweakId"] = tweak.TweakId,
                ["Category"] = tweak.Category,
                ["Success"] = tweak.Success,
                ["ExecutionTimeMs"] = tweak.ExecutionTimeMs
            }
        };

        EnqueueEvent(telemetryEvent);
    }

    /// <summary>
    /// Track system performance impact
    /// </summary>
    public void TrackPerformanceImpact(PerformanceImpact impact)
    {
        if (!_enabled) return;

        var telemetryEvent = new TelemetryEvent
        {
            EventType = "PerformanceImpact",
            Timestamp = DateTime.UtcNow,
            UserId = _anonymousUserId!,
            Data = new Dictionary<string, object>
            {
                ["TweakId"] = impact.TweakId,
                ["BeforeFps"] = impact.BeforeFps,
                ["AfterFps"] = impact.AfterFps,
                ["BeforeLatency"] = impact.BeforeLatencyMs,
                ["AfterLatency"] = impact.AfterLatencyMs,
                ["TestDurationSeconds"] = impact.TestDurationSeconds
            }
        };

        EnqueueEvent(telemetryEvent);
    }

    /// <summary>
    /// Track system hardware configuration
    /// </summary>
    public void TrackHardwareConfiguration(HardwareConfig hardware)
    {
        if (!_enabled) return;

        var telemetryEvent = new TelemetryEvent
        {
            EventType = "HardwareConfig",
            Timestamp = DateTime.UtcNow,
            UserId = _anonymousUserId!,
            Data = new Dictionary<string, object>
            {
                ["CpuModel"] = hardware.CpuModel,
                ["CpuCores"] = hardware.CpuCores,
                ["RamGb"] = hardware.RamGb,
                ["GpuModel"] = hardware.GpuModel,
                ["OsVersion"] = hardware.OsVersion
            }
        };

        EnqueueEvent(telemetryEvent);
    }

    /// <summary>
    /// Flush telemetry events to backend API
    /// </summary>
    public async Task FlushAsync(CancellationToken ct)
    {
        if (!_enabled) return;

        List<TelemetryEvent> events;
        lock (_queueLock)
        {
            events = new List<TelemetryEvent>(_eventQueue);
            _eventQueue.Clear();
        }

        if (events.Count == 0) return;

        try
        {
            // TODO: Send to telemetry API endpoint
            // await SendToApiAsync(events, ct);
            await Task.CompletedTask;

            System.Diagnostics.Debug.WriteLine($"Flushed {events.Count} telemetry events");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to flush telemetry: {ex.Message}");
            // Re-queue events on failure
            lock (_queueLock)
            {
                foreach (var evt in events)
                {
                    _eventQueue.Enqueue(evt);
                }
            }
        }
    }

    private void EnqueueEvent(TelemetryEvent telemetryEvent)
    {
        lock (_queueLock)
        {
            _eventQueue.Enqueue(telemetryEvent);

            // Limit queue size to prevent memory issues
            while (_eventQueue.Count > 1000)
            {
                _eventQueue.Dequeue();
            }
        }
    }
}

/// <summary>
/// Base telemetry event
/// </summary>
public sealed class TelemetryEvent
{
    public required string EventType { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string UserId { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}

/// <summary>
/// Tweak application telemetry
/// </summary>
public sealed class TweakTelemetry
{
    public required string TweakId { get; init; }
    public required string Category { get; init; }
    public required bool Success { get; init; }
    public required long ExecutionTimeMs { get; init; }
}

/// <summary>
/// Performance impact telemetry
/// </summary>
public sealed class PerformanceImpact
{
    public required string TweakId { get; init; }
    public required double BeforeFps { get; init; }
    public required double AfterFps { get; init; }
    public required double BeforeLatencyMs { get; init; }
    public required double AfterLatencyMs { get; init; }
    public required int TestDurationSeconds { get; init; }
}

/// <summary>
/// Hardware configuration telemetry
/// </summary>
public sealed class HardwareConfig
{
    public required string CpuModel { get; init; }
    public required int CpuCores { get; init; }
    public required double RamGb { get; init; }
    public required string GpuModel { get; init; }
    public required string OsVersion { get; init; }
}
