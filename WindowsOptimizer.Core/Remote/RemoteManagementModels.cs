using System;
using System.Collections.Generic;

namespace WindowsOptimizer.Core.Remote;

/// <summary>
/// Remote command sent from management server to agents
/// </summary>
public sealed class RemoteCommand
{
    public required string CommandId { get; init; }
    public required RemoteCommandType Type { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public required DateTime IssuedAt { get; init; }
    public required string IssuedBy { get; init; }
    public TimeSpan? Timeout { get; init; }
    public bool RequireConfirmation { get; init; }
}

/// <summary>
/// Types of remote commands
/// </summary>
public enum RemoteCommandType
{
    // System Information
    GetSystemInfo,
    GetMetrics,
    GetInstalledTweaks,

    // Tweak Operations
    ApplyTweak,
    RevertTweak,
    ApplyPreset,

    // Configuration
    UpdateSettings,
    InstallPlugin,
    UninstallPlugin,

    // Diagnostics
    RunDiagnostics,
    CollectLogs,
    CreateRestorePoint,

    // System Control
    Restart,
    Shutdown,
    ExecuteScript
}

/// <summary>
/// Response from agent to management server
/// </summary>
public sealed class RemoteCommandResponse
{
    public required string CommandId { get; init; }
    public required bool Success { get; init; }
    public required DateTime CompletedAt { get; init; }
    public required TimeSpan ExecutionTime { get; init; }
    public object? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Agent status report sent periodically to management server
/// </summary>
public sealed class AgentStatusReport
{
    public required string AgentId { get; init; }
    public required string MachineName { get; init; }
    public required string AgentVersion { get; init; }
    public required DateTime Timestamp { get; init; }
    public required AgentHealth Health { get; init; }
    public required SystemMetricsSnapshot Metrics { get; init; }
    public required List<string> ActiveTweaks { get; init; }
    public required List<string> InstalledPlugins { get; init; }
    public required DateTime LastRebootTime { get; init; }
    public required TimeSpan Uptime { get; init; }
}

/// <summary>
/// Agent health status
/// </summary>
public enum AgentHealth
{
    Healthy,
    Warning,
    Critical,
    Offline
}

/// <summary>
/// Snapshot of system metrics
/// </summary>
public sealed class SystemMetricsSnapshot
{
    public required double CpuUsagePercent { get; init; }
    public required double RamUsagePercent { get; init; }
    public required double DiskUsagePercent { get; init; }
    public required double NetworkMbps { get; init; }
    public required double CpuTemperature { get; init; }
    public required double GpuTemperature { get; init; }
    public required int ProcessCount { get; init; }
    public required int ThreadCount { get; init; }
}

/// <summary>
/// Fleet-wide policy that can be deployed to multiple agents
/// </summary>
public sealed class FleetPolicy
{
    public required string PolicyId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required List<string> TargetAgentIds { get; init; }
    public required List<PolicyAction> Actions { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public required PolicySchedule? Schedule { get; init; }
    public bool IsEnabled { get; init; }
}

/// <summary>
/// Action to be performed as part of a policy
/// </summary>
public sealed class PolicyAction
{
    public required RemoteCommandType CommandType { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public required int Order { get; init; }
    public bool ContinueOnError { get; init; }
}

/// <summary>
/// Schedule for policy execution
/// </summary>
public sealed class PolicySchedule
{
    public required ScheduleType Type { get; init; }
    public DateTime? ExecuteAt { get; init; }
    public string? CronExpression { get; init; }
    public TimeSpan? Interval { get; init; }
}

/// <summary>
/// Schedule types
/// </summary>
public enum ScheduleType
{
    Once,
    Recurring,
    Cron
}

/// <summary>
/// Agent registration request
/// </summary>
public sealed class AgentRegistration
{
    public required string AgentId { get; init; }
    public required string MachineName { get; init; }
    public required string AgentVersion { get; init; }
    public required string OsVersion { get; init; }
    public required string HardwareProfile { get; init; }
    public required DateTime RegisteredAt { get; init; }
    public required string ApiKey { get; init; }
}

/// <summary>
/// Agent registration response
/// </summary>
public sealed class AgentRegistrationResponse
{
    public required bool Success { get; init; }
    public required string? SessionToken { get; init; }
    public required string? ErrorMessage { get; init; }
    public required int HeartbeatIntervalSeconds { get; init; }
    public required List<FleetPolicy> AssignedPolicies { get; init; }
}

/// <summary>
/// Real-time event from agent to server
/// </summary>
public sealed class AgentEvent
{
    public required string AgentId { get; init; }
    public required AgentEventType EventType { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Message { get; init; }
    public required Dictionary<string, object> Data { get; init; }
    public AgentEventSeverity Severity { get; init; }
}

/// <summary>
/// Agent event types
/// </summary>
public enum AgentEventType
{
    TweakApplied,
    TweakReverted,
    PluginInstalled,
    PluginUninstalled,
    ErrorOccurred,
    WarningIssued,
    MetricThresholdExceeded,
    SystemRestarted,
    ConfigurationChanged
}

/// <summary>
/// Event severity levels
/// </summary>
public enum AgentEventSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
