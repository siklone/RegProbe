using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

/// <summary>
/// Base class for cleanup tweaks that delete files or folders.
/// </summary>
public abstract class FileCleanupTweak : ITweak
{
    private long _detectedSizeBytes;
    private int _detectedFileCount;
    private bool _hasDetected;

    protected FileCleanupTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        bool requiresElevation)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        RequiresElevation = requiresElevation;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk { get; }
    public bool RequiresElevation { get; }

    /// <summary>
    /// Returns the list of paths to clean (files or directories).
    /// </summary>
    protected abstract IEnumerable<string> GetPathsToClean();

    /// <summary>
    /// Optionally stop services before cleanup (e.g., DoSvc before cleaning delivery optimization).
    /// </summary>
    protected virtual Task<TweakResult?> StopServicesAsync(CancellationToken ct)
    {
        return Task.FromResult<TweakResult?>(null);
    }

    /// <summary>
    /// Optionally start services after cleanup.
    /// </summary>
    protected virtual Task<TweakResult?> StartServicesAsync(CancellationToken ct)
    {
        return Task.FromResult<TweakResult?>(null);
    }

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            _detectedSizeBytes = 0;
            _detectedFileCount = 0;

            foreach (var path in GetPathsToClean())
            {
                ct.ThrowIfCancellationRequested();

                if (Directory.Exists(path))
                {
                    var (size, count) = await CalculateDirectorySizeAsync(path, ct);
                    _detectedSizeBytes += size;
                    _detectedFileCount += count;
                }
                else if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    _detectedSizeBytes += fileInfo.Length;
                    _detectedFileCount++;
                }
            }

            _hasDetected = true;

            var sizeMB = _detectedSizeBytes / (1024.0 * 1024.0);
            return new TweakResult(
                TweakStatus.Detected,
                $"Found {_detectedFileCount} files ({sizeMB:F2} MB)",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Detect error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public virtual async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!_hasDetected)
        {
            return new TweakResult(
                TweakStatus.Failed,
                "Must call DetectAsync before ApplyAsync.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            // Stop services if needed
            var stopResult = await StopServicesAsync(ct);
            if (stopResult != null && stopResult.Status == TweakStatus.Failed)
            {
                return stopResult;
            }

            var deletedFiles = 0;
            var errors = new List<string>();

            foreach (var path in GetPathsToClean())
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (Directory.Exists(path))
                    {
                        deletedFiles += DeleteDirectoryContents(path, errors);
                    }
                    else if (File.Exists(path))
                    {
                        File.Delete(path);
                        deletedFiles++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
                }
            }

            // Start services if needed
            var startResult = await StartServicesAsync(ct);
            if (startResult != null && startResult.Status == TweakStatus.Failed)
            {
                errors.Add($"Service start warning: {startResult.Message}");
            }

            var message = $"Deleted {deletedFiles} files";
            if (errors.Count > 0)
            {
                message += $" ({errors.Count} errors)";
            }

            return new TweakResult(
                errors.Count == 0 ? TweakStatus.Applied : TweakStatus.Failed,
                message,
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(
                TweakStatus.Failed,
                $"Apply error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex);
        }
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var remainingFiles = 0;

            foreach (var path in GetPathsToClean())
            {
                if (Directory.Exists(path))
                {
                    remainingFiles += Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
                }
                else if (File.Exists(path))
                {
                    remainingFiles++;
                }
            }

            var status = remainingFiles == 0 ? TweakStatus.Verified : TweakStatus.Failed;
            var message = remainingFiles == 0
                ? "All files successfully deleted"
                : $"{remainingFiles} files still remain";

            return Task.FromResult(new TweakResult(status, message, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TweakResult(
                TweakStatus.Failed,
                $"Verify error: {ex.Message}",
                DateTimeOffset.UtcNow,
                ex));
        }
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        // File cleanup cannot be rolled back
        return Task.FromResult(new TweakResult(
            TweakStatus.NotApplicable,
            "Deleted files cannot be restored.",
            DateTimeOffset.UtcNow));
    }

    private async Task<(long size, int count)> CalculateDirectorySizeAsync(string path, CancellationToken ct)
    {
        long totalSize = 0;
        int fileCount = 0;

        try
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                    fileCount++;
                }
                catch
                {
                    // Skip files we can't access
                }
            }
        }
        catch
        {
            // Skip directories we can't access
        }

        return await Task.FromResult((totalSize, fileCount));
    }

    private int DeleteDirectoryContents(string path, List<string> errors)
    {
        int deleted = 0;

        try
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                }
            }

            // Try to delete empty directories
            try
            {
                var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                    .OrderByDescending(d => d.Length); // Delete deepest first

                foreach (var dir in directories)
                {
                    try
                    {
                        if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                        {
                            Directory.Delete(dir);
                        }
                    }
                    catch
                    {
                        // Skip directories we can't delete
                    }
                }
            }
            catch
            {
                // Continue even if directory cleanup fails
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Directory error: {ex.Message}");
        }

        return deleted;
    }
}
