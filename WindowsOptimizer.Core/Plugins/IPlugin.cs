using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Plugins;

/// <summary>
/// Base interface for all Windows Optimizer plugins
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// Plugin metadata information
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Initialize the plugin with the provided context
    /// </summary>
    Task InitializeAsync(PluginContext context, CancellationToken ct);

    /// <summary>
    /// Execute the plugin's main functionality
    /// </summary>
    Task<PluginResult> ExecuteAsync(PluginExecutionContext executionContext, CancellationToken ct);

    /// <summary>
    /// Validate plugin can run on current system
    /// </summary>
    Task<PluginValidationResult> ValidateAsync(CancellationToken ct);
}

/// <summary>
/// Plugin metadata
/// </summary>
public sealed class PluginMetadata
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Author { get; init; }
    public required string Description { get; init; }
    public required PluginCategory Category { get; init; }
    public required string[] RequiredPermissions { get; init; }
    public string? IconPath { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? DigitalSignature { get; init; }
    public DateTime PublishedDate { get; init; }
    public long DownloadCount { get; init; }
    public double AverageRating { get; init; }
}

/// <summary>
/// Plugin categories
/// </summary>
public enum PluginCategory
{
    Performance,
    Privacy,
    Security,
    Gaming,
    Networking,
    Storage,
    Cleanup,
    Monitoring,
    Automation,
    Customization
}

/// <summary>
/// Plugin initialization context
/// </summary>
public sealed class PluginContext
{
    public required string PluginDataPath { get; init; }
    public required IServiceProvider Services { get; init; }
    public required IDictionary<string, string> Configuration { get; init; }
}

/// <summary>
/// Plugin execution context
/// </summary>
public sealed class PluginExecutionContext
{
    public required IDictionary<string, object> Parameters { get; init; }
    public required IProgress<PluginProgress> Progress { get; init; }
}

/// <summary>
/// Plugin progress reporting
/// </summary>
public sealed class PluginProgress
{
    public required string Message { get; init; }
    public required double Percentage { get; init; }
    public PluginProgressType Type { get; init; }
}

public enum PluginProgressType
{
    Information,
    Warning,
    Error,
    Success
}

/// <summary>
/// Plugin execution result
/// </summary>
public sealed class PluginResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public IDictionary<string, object>? Data { get; init; }
    public Exception? Error { get; init; }
}

/// <summary>
/// Plugin validation result
/// </summary>
public sealed class PluginValidationResult
{
    public required bool IsValid { get; init; }
    public required string[] Issues { get; init; }
    public required string[] Warnings { get; init; }
}
