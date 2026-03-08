using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Files;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed class FileRenameTweak : ITweak
{
    private readonly IFileSystemAccessor _fileSystemAccessor;
    private readonly string _sourcePath;
    private readonly string _disabledPath;
    private FileSnapshot? _snapshot;

    public FileRenameTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        string sourcePath,
        string disabledPath,
        IFileSystemAccessor fileSystemAccessor,
        bool? requiresElevation = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Risk = risk;
        _sourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
        _disabledPath = disabledPath ?? throw new ArgumentNullException(nameof(disabledPath));
        _fileSystemAccessor = fileSystemAccessor ?? throw new ArgumentNullException(nameof(fileSystemAccessor));
        RequiresElevation = requiresElevation ?? true;

        if (string.IsNullOrWhiteSpace(_sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(_disabledPath))
        {
            throw new ArgumentException("Disabled path is required.", nameof(disabledPath));
        }
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
            var sourceExists = await _fileSystemAccessor.FileExistsAsync(_sourcePath, ct);
            var disabledExists = await _fileSystemAccessor.FileExistsAsync(_disabledPath, ct);

            _snapshot = new FileSnapshot(sourceExists, disabledExists);

            if (!sourceExists && !disabledExists)
            {
                return new TweakResult(TweakStatus.NotApplicable, "File not found.", DateTimeOffset.UtcNow);
            }

            var message = disabledExists
                ? "File is already renamed."
                : "File is present and can be renamed.";
            return new TweakResult(TweakStatus.Detected, message, DateTimeOffset.UtcNow);
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
            var sourceExists = await _fileSystemAccessor.FileExistsAsync(_sourcePath, ct);
            var disabledExists = await _fileSystemAccessor.FileExistsAsync(_disabledPath, ct);

            if (sourceExists && disabledExists)
            {
                return new TweakResult(
                    TweakStatus.Failed,
                    "Apply failed: both source and disabled files exist.",
                    DateTimeOffset.UtcNow);
            }

            if (!sourceExists && !disabledExists)
            {
                return new TweakResult(TweakStatus.NotApplicable, "File not found.", DateTimeOffset.UtcNow);
            }

            if (disabledExists)
            {
                return new TweakResult(TweakStatus.Applied, "File already renamed.", DateTimeOffset.UtcNow);
            }

            await _fileSystemAccessor.MoveFileAsync(_sourcePath, _disabledPath, ct);
            return new TweakResult(TweakStatus.Applied, "Renamed file.", DateTimeOffset.UtcNow);
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
            var sourceExists = await _fileSystemAccessor.FileExistsAsync(_sourcePath, ct);
            var disabledExists = await _fileSystemAccessor.FileExistsAsync(_disabledPath, ct);

            if (!disabledExists || sourceExists)
            {
                return new TweakResult(TweakStatus.Failed, "Verification failed. File is not renamed.", DateTimeOffset.UtcNow);
            }

            return new TweakResult(TweakStatus.Verified, "Verified renamed file.", DateTimeOffset.UtcNow);
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

        if (_snapshot is null)
        {
            return new TweakResult(
                TweakStatus.Skipped,
                "Rollback skipped because no prior detect state is available.",
                DateTimeOffset.UtcNow);
        }

        try
        {
            if (_snapshot.DisabledExists && !_snapshot.SourceExists)
            {
                return new TweakResult(
                    TweakStatus.Skipped,
                    "Rollback skipped because the file was already renamed before detect.",
                    DateTimeOffset.UtcNow);
            }

            if (!_snapshot.SourceExists && !_snapshot.DisabledExists)
            {
                return new TweakResult(TweakStatus.Skipped, "Rollback skipped because file was missing.", DateTimeOffset.UtcNow);
            }

            var sourceExists = await _fileSystemAccessor.FileExistsAsync(_sourcePath, ct);
            var disabledExists = await _fileSystemAccessor.FileExistsAsync(_disabledPath, ct);

            if (disabledExists && !sourceExists)
            {
                await _fileSystemAccessor.MoveFileAsync(_disabledPath, _sourcePath, ct);
            }

            return new TweakResult(TweakStatus.RolledBack, "Rolled back file rename.", DateTimeOffset.UtcNow);
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

    private sealed record FileSnapshot(bool SourceExists, bool DisabledExists);
}
