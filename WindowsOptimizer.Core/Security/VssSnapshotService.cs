using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Security;

/// <summary>
/// Volume Shadow Copy Service integration for creating system restore points
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class VssSnapshotService
{
    /// <summary>
    /// Create a system restore point before risky operations
    /// </summary>
    public async Task<VssSnapshotResult> CreateSnapshotAsync(string description, CancellationToken ct)
    {
        try
        {
            // Check if VSS service is available
            if (!IsVssServiceAvailable())
            {
                return new VssSnapshotResult
                {
                    Success = false,
                    ErrorMessage = "Volume Shadow Copy Service is not available or disabled"
                };
            }

            // Create restore point using Windows Management Instrumentation
            var result = await CreateRestorePointAsync(description, ct);

            return result;
        }
        catch (Exception ex)
        {
            return new VssSnapshotResult
            {
                Success = false,
                ErrorMessage = $"Failed to create snapshot: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Check if VSS service is running
    /// </summary>
    private bool IsVssServiceAvailable()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "query vss",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a restore point using PowerShell
    /// </summary>
    private async Task<VssSnapshotResult> CreateRestorePointAsync(string description, CancellationToken ct)
    {
        try
        {
            var powerShellScript = $@"
                # Enable System Restore if needed
                Enable-ComputerRestore -Drive 'C:\'

                # Create restore point
                Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS'
            ";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powerShellScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas" // Request elevation
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                return new VssSnapshotResult
                {
                    Success = true,
                    SnapshotId = Guid.NewGuid().ToString(), // In real implementation, get actual snapshot ID
                    CreatedAt = DateTime.UtcNow,
                    Description = description
                };
            }
            else
            {
                return new VssSnapshotResult
                {
                    Success = false,
                    ErrorMessage = $"Restore point creation failed: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new VssSnapshotResult
            {
                Success = false,
                ErrorMessage = $"Failed to execute PowerShell command: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// List available restore points
    /// </summary>
    public async Task<VssSnapshot[]> ListSnapshotsAsync(CancellationToken ct)
    {
        try
        {
            var powerShellScript = "Get-ComputerRestorePoint | Select-Object SequenceNumber, CreationTime, Description | ConvertTo-Json";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powerShellScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(ct);

            // TODO: Parse JSON output to VssSnapshot array
            return Array.Empty<VssSnapshot>();
        }
        catch
        {
            return Array.Empty<VssSnapshot>();
        }
    }

    /// <summary>
    /// Restore system to a previous snapshot
    /// </summary>
    public async Task<bool> RestoreSnapshotAsync(string snapshotId, CancellationToken ct)
    {
        try
        {
            var powerShellScript = $@"
                # Restore to snapshot
                Restore-Computer -RestorePoint {snapshotId} -Confirm:$false
            ";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{powerShellScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas"
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// VSS snapshot result
/// </summary>
public sealed class VssSnapshotResult
{
    public required bool Success { get; init; }
    public string? SnapshotId { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? Description { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// VSS snapshot info
/// </summary>
public sealed class VssSnapshot
{
    public required string SnapshotId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string Description { get; init; }
}
