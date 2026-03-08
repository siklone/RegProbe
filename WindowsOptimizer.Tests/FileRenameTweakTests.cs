using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Core.Files;
using Xunit;

public sealed class FileRenameTweakTests
{
    [Fact]
    public async Task ApplyRollback_RenamesAndRestoresFile()
    {
        var sourcePath = "C:\\Windows\\System32\\test.exe";
        var disabledPath = "C:\\Windows\\System32\\test.exe.disabled";
        var fileSystem = new FakeFileSystemAccessor(new[] { sourcePath });

        var tweak = new FileRenameTweak(
            "test.file",
            "Test file rename",
            "Renames a file for testing.",
            TweakRiskLevel.Advanced,
            sourcePath,
            disabledPath,
            fileSystem,
            requiresElevation: true);

        var detect = await tweak.DetectAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Detected, detect.Status);

        var apply = await tweak.ApplyAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Applied, apply.Status);

        var verify = await tweak.VerifyAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Verified, verify.Status);

        var rollback = await tweak.RollbackAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.RolledBack, rollback.Status);

        Assert.True(await fileSystem.FileExistsAsync(sourcePath, CancellationToken.None));
        Assert.False(await fileSystem.FileExistsAsync(disabledPath, CancellationToken.None));
    }

    private sealed class FakeFileSystemAccessor : IFileSystemAccessor
    {
        private readonly HashSet<string> _files = new(StringComparer.OrdinalIgnoreCase);

        public FakeFileSystemAccessor(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                _files.Add(file);
            }
        }

        public Task<bool> FileExistsAsync(string path, CancellationToken ct)
        {
            return Task.FromResult(_files.Contains(path));
        }

        public Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct)
        {
            if (!_files.Remove(sourcePath))
            {
                throw new KeyNotFoundException("Source file does not exist.");
            }

            if (!_files.Add(destinationPath))
            {
                throw new KeyNotFoundException("Destination file already exists.");
            }

            return Task.CompletedTask;
        }
    }
}
