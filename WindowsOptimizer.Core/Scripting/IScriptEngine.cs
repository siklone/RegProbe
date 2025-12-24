using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Scripting;

/// <summary>
/// Base interface for script execution engines (LUA, Python, etc.)
/// </summary>
public interface IScriptEngine : IDisposable
{
    /// <summary>
    /// Supported script language
    /// </summary>
    ScriptLanguage Language { get; }

    /// <summary>
    /// Initialize the script engine with a security context
    /// </summary>
    Task InitializeAsync(ScriptSecurityContext securityContext, CancellationToken ct);

    /// <summary>
    /// Execute a script from source code
    /// </summary>
    Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptSource, Dictionary<string, object>? parameters = null, CancellationToken ct = default);

    /// <summary>
    /// Execute a script from a file
    /// </summary>
    Task<ScriptExecutionResult> ExecuteScriptFileAsync(string scriptPath, Dictionary<string, object>? parameters = null, CancellationToken ct = default);

    /// <summary>
    /// Validate script syntax without executing
    /// </summary>
    Task<ScriptValidationResult> ValidateScriptAsync(string scriptSource, CancellationToken ct);

    /// <summary>
    /// Get available API functions exposed to scripts
    /// </summary>
    List<ScriptApiFunction> GetAvailableApiFunctions();

    /// <summary>
    /// Set the maximum execution time for scripts
    /// </summary>
    void SetExecutionTimeout(TimeSpan timeout);
}

/// <summary>
/// Supported scripting languages
/// </summary>
public enum ScriptLanguage
{
    Lua,
    Python,
    PowerShell
}

/// <summary>
/// Result of script execution
/// </summary>
public sealed class ScriptExecutionResult
{
    public required bool Success { get; init; }
    public object? ReturnValue { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public List<string> OutputLines { get; init; } = new();
    public Dictionary<string, object> Variables { get; init; } = new();
}

/// <summary>
/// Result of script validation
/// </summary>
public sealed class ScriptValidationResult
{
    public required bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// API function exposed to scripts
/// </summary>
public sealed class ScriptApiFunction
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required List<ScriptParameter> Parameters { get; init; }
    public required string ReturnType { get; init; }
    public required string Example { get; init; }
}

/// <summary>
/// Script function parameter
/// </summary>
public sealed class ScriptParameter
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required bool IsOptional { get; init; }
    public object? DefaultValue { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Security context for script execution
/// </summary>
public sealed class ScriptSecurityContext
{
    /// <summary>
    /// Allow file system access
    /// </summary>
    public bool AllowFileSystemAccess { get; init; }

    /// <summary>
    /// Allow registry access
    /// </summary>
    public bool AllowRegistryAccess { get; init; }

    /// <summary>
    /// Allow network access
    /// </summary>
    public bool AllowNetworkAccess { get; init; }

    /// <summary>
    /// Allow process execution
    /// </summary>
    public bool AllowProcessExecution { get; init; }

    /// <summary>
    /// Allow service control
    /// </summary>
    public bool AllowServiceControl { get; init; }

    /// <summary>
    /// Maximum execution time
    /// </summary>
    public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum memory usage in MB
    /// </summary>
    public int MaxMemoryMb { get; init; } = 100;

    /// <summary>
    /// Allowed file system paths (if file system access is enabled)
    /// </summary>
    public List<string> AllowedPaths { get; init; } = new();

    /// <summary>
    /// Create a restricted security context (default)
    /// </summary>
    public static ScriptSecurityContext CreateRestricted()
    {
        return new ScriptSecurityContext
        {
            AllowFileSystemAccess = false,
            AllowRegistryAccess = false,
            AllowNetworkAccess = false,
            AllowProcessExecution = false,
            AllowServiceControl = false,
            MaxExecutionTime = TimeSpan.FromSeconds(10),
            MaxMemoryMb = 50
        };
    }

    /// <summary>
    /// Create a permissive security context (use with caution)
    /// </summary>
    public static ScriptSecurityContext CreatePermissive()
    {
        return new ScriptSecurityContext
        {
            AllowFileSystemAccess = true,
            AllowRegistryAccess = true,
            AllowNetworkAccess = true,
            AllowProcessExecution = true,
            AllowServiceControl = true,
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            MaxMemoryMb = 500
        };
    }
}
