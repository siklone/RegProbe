using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core.Files;

namespace WindowsOptimizer.ElevatedHost;

internal sealed class LocalFileSystemAccessor : IFileSystemAccessor
{
    public Task<bool> FileExistsAsync(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("File path is required.", nameof(path));
        }

        return Task.FromResult(File.Exists(path));
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination path is required.", nameof(destinationPath));
        }

        File.Move(sourcePath, destinationPath);
        return Task.CompletedTask;
    }
}
