using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;
using RegProbe.Engine.Tweaks.Developer;

namespace RegProbe.Tests;

public sealed class SetWsl2MemoryLimitTweakTests
{
    [Fact]
    public async Task Apply_WritesWslConfigMemoryLimit_AndPreservesOtherSections()
    {
        var root = CreateTempDirectory();
        try
        {
            var settingsPath = Path.Combine(root, ".wslconfig");
            await File.WriteAllTextAsync(settingsPath, """
            [wsl2]
            processors=4

            [interop]
            enabled=true
            """);

            var tweak = new SetWsl2MemoryLimitTweak(settingsPath);

            var detect = await tweak.DetectAsync(CancellationToken.None);
            var apply = await tweak.ApplyAsync(CancellationToken.None);
            var verify = await tweak.VerifyAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.Detected, detect.Status);
            Assert.Equal(TweakStatus.Applied, apply.Status);
            Assert.Equal(TweakStatus.Verified, verify.Status);

            var text = await File.ReadAllTextAsync(settingsPath);
            Assert.Contains("[wsl2]", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("processors=4", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("memory=4GB", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[interop]", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("enabled=true", text, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task Rollback_RestoresOriginalText()
    {
        var root = CreateTempDirectory();
        try
        {
            var settingsPath = Path.Combine(root, ".wslconfig");
            var originalText = """
            ; keep my comments
            [wsl2]
            processors=2
            """;

            await File.WriteAllTextAsync(settingsPath, originalText);

            var tweak = new SetWsl2MemoryLimitTweak(settingsPath);

            await tweak.DetectAsync(CancellationToken.None);
            await tweak.ApplyAsync(CancellationToken.None);
            var rollback = await tweak.RollbackAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.RolledBack, rollback.Status);
            var restoredText = await File.ReadAllTextAsync(settingsPath);
            Assert.Equal(originalText, restoredText);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "RegProbe.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
