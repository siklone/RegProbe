using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Infrastructure.Registry;
using Xunit;

public sealed class RegistryValueSetTweakTests
{
    [Fact]
    public async Task ApplyRollback_WhenValuesMissing_RestoresMissingValues()
    {
        var keyPath = BuildKeyPath();
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);

        try
        {
            var tweak = BuildTweak(keyPath, "3");

            var detect = await tweak.DetectAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Detected, detect.Status);

            var apply = await tweak.ApplyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Applied, apply.Status);

            var verify = await tweak.VerifyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Verified, verify.Status);

            var rollback = await tweak.RollbackAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.RolledBack, rollback.Status);

            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            Assert.Null(key?.GetValue("Language Hotkey"));
            Assert.Null(key?.GetValue("Hotkey"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
        }
    }

    [Fact]
    public async Task Rollback_RestoresOriginalValues()
    {
        var keyPath = BuildKeyPath();
        using (var key = Registry.CurrentUser.CreateSubKey(keyPath, true))
        {
            key!.SetValue("Language Hotkey", "1", RegistryValueKind.String);
            key.SetValue("Hotkey", "1", RegistryValueKind.String);
        }

        try
        {
            var tweak = BuildTweak(keyPath, "3");

            await tweak.DetectAsync(CancellationToken.None);
            await tweak.ApplyAsync(CancellationToken.None);
            var rollback = await tweak.RollbackAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.RolledBack, rollback.Status);

            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            Assert.NotNull(key);
            Assert.Equal("1", key!.GetValue("Language Hotkey") as string);
            Assert.Equal("1", key.GetValue("Hotkey") as string);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
        }
    }

    private static RegistryValueSetTweak BuildTweak(string keyPath, string target)
    {
        var entries = new[]
        {
            new RegistryValueSetEntry("Language Hotkey", RegistryValueKind.String, target),
            new RegistryValueSetEntry("Hotkey", RegistryValueKind.String, target)
        };

        var accessor = new LocalRegistryAccessor();
        return new RegistryValueSetTweak(
            "test.registryset",
            "Test registry set tweak",
            "Test tweak that manipulates multiple registry values.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            keyPath,
            entries,
            accessor,
            requiresElevation: false);
    }

    private static string BuildKeyPath()
    {
        return $"Software\\WindowsOptimizer\\Tests\\RegistryValueSetTweak\\{Guid.NewGuid():N}";
    }
}
