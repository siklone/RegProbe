using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Security;

/// <summary>
/// Cryptographically logs all tweak operations for transparency and audit
/// </summary>
public sealed class CryptographicLogger : IDisposable
{
    private readonly string _logDirectory;
    private readonly string _currentLogPath;
    private readonly SHA256 _hasher;
    private readonly SemaphoreSlim _logLock = new(1, 1);
    private readonly List<TweakLogEntry> _pendingEntries = new();

    public CryptographicLogger(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        _currentLogPath = Path.Combine(_logDirectory, $"tweaks_{timestamp}.log");

        _hasher = SHA256.Create();
    }

    /// <summary>
    /// Log a tweak operation with cryptographic proof
    /// </summary>
    public async Task LogTweakOperationAsync(TweakOperation operation, CancellationToken ct)
    {
        await _logLock.WaitAsync(ct);
        try
        {
            var entry = new TweakLogEntry
            {
                Timestamp = DateTime.UtcNow,
                TweakId = operation.TweakId,
                OperationType = operation.OperationType,
                RegistryChanges = operation.RegistryChanges,
                FileChanges = operation.FileChanges,
                ServiceChanges = operation.ServiceChanges,
                Success = operation.Success,
                ErrorMessage = operation.ErrorMessage,
                ExecutionTimeMs = operation.ExecutionTimeMs,
                UserId = Environment.UserName,
                MachineId = Environment.MachineName
            };

            // Compute hash of the entry
            entry.Hash = ComputeEntryHash(entry);

            // Chain with previous entry hash (blockchain-like)
            if (_pendingEntries.Count > 0)
            {
                entry.PreviousHash = _pendingEntries[^1].Hash;
            }

            _pendingEntries.Add(entry);

            // Write to log file
            await WriteEntryToLogAsync(entry, ct);
        }
        finally
        {
            _logLock.Release();
        }
    }

    /// <summary>
    /// Verify integrity of log chain
    /// </summary>
    public async Task<LogVerificationResult> VerifyLogIntegrityAsync(string logPath, CancellationToken ct)
    {
        if (!File.Exists(logPath))
        {
            return new LogVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Log file not found"
            };
        }

        var entries = new List<TweakLogEntry>();
        var lines = await File.ReadAllLinesAsync(logPath, ct);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize<TweakLogEntry>(line);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }
            catch
            {
                return new LogVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Log file contains invalid entries"
                };
            }
        }

        // Verify chain integrity
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            // Verify hash
            var expectedHash = ComputeEntryHash(entry);
            if (entry.Hash != expectedHash)
            {
                return new LogVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Hash mismatch at entry {i}",
                    TamperedEntryIndex = i
                };
            }

            // Verify chain
            if (i > 0 && entry.PreviousHash != entries[i - 1].Hash)
            {
                return new LogVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Chain broken at entry {i}",
                    TamperedEntryIndex = i
                };
            }
        }

        return new LogVerificationResult
        {
            IsValid = true,
            VerifiedEntryCount = entries.Count
        };
    }

    /// <summary>
    /// Get all log entries for a specific tweak
    /// </summary>
    public async Task<IReadOnlyList<TweakLogEntry>> GetTweakHistoryAsync(string tweakId, CancellationToken ct)
    {
        var entries = new List<TweakLogEntry>();

        foreach (var logFile in Directory.GetFiles(_logDirectory, "tweaks_*.log"))
        {
            var lines = await File.ReadAllLinesAsync(logFile, ct);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var entry = JsonSerializer.Deserialize<TweakLogEntry>(line);
                    if (entry != null && entry.TweakId == tweakId)
                    {
                        entries.Add(entry);
                    }
                }
                catch
                {
                    // Skip invalid entries
                }
            }
        }

        return entries;
    }

    private string ComputeEntryHash(TweakLogEntry entry)
    {
        var data = JsonSerializer.Serialize(new
        {
            entry.Timestamp,
            entry.TweakId,
            entry.OperationType,
            entry.RegistryChanges,
            entry.FileChanges,
            entry.ServiceChanges,
            entry.Success,
            entry.ErrorMessage,
            entry.ExecutionTimeMs,
            entry.UserId,
            entry.MachineId,
            entry.PreviousHash
        });

        var bytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = _hasher.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private async Task WriteEntryToLogAsync(TweakLogEntry entry, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllLinesAsync(_currentLogPath, new[] { json }, ct);
    }

    public void Dispose()
    {
        _hasher?.Dispose();
        _logLock?.Dispose();
    }
}

/// <summary>
/// Tweak operation to be logged
/// </summary>
public sealed class TweakOperation
{
    public required string TweakId { get; init; }
    public required string OperationType { get; init; }
    public required List<RegistryChange> RegistryChanges { get; init; }
    public required List<FileChange> FileChanges { get; init; }
    public required List<ServiceChange> ServiceChanges { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public required long ExecutionTimeMs { get; init; }
}

/// <summary>
/// Logged tweak entry with cryptographic proof
/// </summary>
public sealed class TweakLogEntry
{
    public required DateTime Timestamp { get; init; }
    public required string TweakId { get; init; }
    public required string OperationType { get; init; }
    public required List<RegistryChange> RegistryChanges { get; init; }
    public required List<FileChange> FileChanges { get; init; }
    public required List<ServiceChange> ServiceChanges { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public required long ExecutionTimeMs { get; init; }
    public required string UserId { get; init; }
    public required string MachineId { get; init; }
    public string Hash { get; set; } = string.Empty;
    public string? PreviousHash { get; set; }
}

public sealed class RegistryChange
{
    public required string Path { get; init; }
    public required string ValueName { get; init; }
    public required string? OldValue { get; init; }
    public required string? NewValue { get; init; }
}

public sealed class FileChange
{
    public required string Path { get; init; }
    public required string ChangeType { get; init; }
    public required long? OldSizeBytes { get; init; }
    public required long? NewSizeBytes { get; init; }
}

public sealed class ServiceChange
{
    public required string ServiceName { get; init; }
    public required string? OldStartMode { get; init; }
    public required string? NewStartMode { get; init; }
}

/// <summary>
/// Log verification result
/// </summary>
public sealed class LogVerificationResult
{
    public required bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public int? TamperedEntryIndex { get; init; }
    public int VerifiedEntryCount { get; init; }
}
