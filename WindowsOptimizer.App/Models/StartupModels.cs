namespace WindowsOptimizer.App.Models;

/// <summary>
/// Represents a startup item from various sources.
/// </summary>
public record StartupItem(
    string Id,
    string Name,
    string Command,
    string Publisher,
    StartupLocation Location,
    StartupImpact Impact,
    bool IsEnabled,
    DateTime? LastModified
);

/// <summary>
/// Source location of the startup item.
/// </summary>
public enum StartupLocation
{
    RegistryCurrentUser,
    RegistryLocalMachine,
    StartupFolderUser,
    StartupFolderCommon,
    TaskScheduler
}

/// <summary>
/// Performance impact level of startup item.
/// </summary>
public enum StartupImpact
{
    Unknown,
    Low,      // < 100ms startup delay
    Medium,   // 100-500ms
    High      // > 500ms
}
