using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Infrastructure.Registry;
using Xunit;

public sealed class RegistryValueTweakTests
{
    [Fact]
    public async Task Detect_WhenValueMatchesTarget_ReturnsApplied()
    {
        var keyPath = BuildKeyPath();
        var valueName = "TestValue";

        using (var key = Registry.CurrentUser.CreateSubKey(keyPath, true))
        {
            key!.SetValue(valueName, 1, RegistryValueKind.DWord);
        }

        try
        {
            var tweak = BuildTweak(keyPath, valueName);
            var detect = await tweak.DetectAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Applied, detect.Status);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
        }
    }

    [Fact]
    public async Task ApplyRollback_WhenValueMissing_RestoresMissingValue()
    {
        var keyPath = BuildKeyPath();
        var valueName = "TestValue";
        Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);

        try
        {
            var tweak = BuildTweak(keyPath, valueName);
            var detect = await tweak.DetectAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Detected, detect.Status);

            var apply = await tweak.ApplyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Applied, apply.Status);

            var verify = await tweak.VerifyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Verified, verify.Status);

            var rollback = await tweak.RollbackAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.RolledBack, rollback.Status);

            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            Assert.Null(key?.GetValue(valueName));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
        }
    }

    [Fact]
    public async Task Rollback_RestoresOriginalValue()
    {
        var keyPath = BuildKeyPath();
        var valueName = "TestValue";

        using (var key = Registry.CurrentUser.CreateSubKey(keyPath, true))
        {
            key!.SetValue(valueName, 0, RegistryValueKind.DWord);
        }

        try
        {
            var tweak = BuildTweak(keyPath, valueName);

            await tweak.DetectAsync(CancellationToken.None);
            await tweak.ApplyAsync(CancellationToken.None);
            var rollback = await tweak.RollbackAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.RolledBack, rollback.Status);

            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            Assert.NotNull(key);
            Assert.Equal(0, (int)key!.GetValue(valueName)!);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
        }
    }

    private static RegistryValueTweak BuildTweak(string keyPath, string valueName)
    {
        var registryAccessor = new LocalRegistryAccessor();
        return new RegistryValueTweak(
            "test.registry",
            "Test registry tweak",
            "Test tweak that manipulates a registry value.",
            TweakRiskLevel.Safe,
            RegistryHive.CurrentUser,
            keyPath,
            valueName,
            RegistryValueKind.DWord,
            1,
            registryAccessor,
            requiresElevation: false);
    }

    private static string BuildKeyPath()
    {
        return $"Software\\WindowsOptimizer\\Tests\\RegistryValueTweak\\{Guid.NewGuid():N}";
    }
}
