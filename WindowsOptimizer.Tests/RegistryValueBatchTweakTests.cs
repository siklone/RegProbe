using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Infrastructure.Registry;
using Xunit;

public sealed class RegistryValueBatchTweakTests
{
    [Fact]
    public async Task Detect_WhenAllValuesMatchTarget_ReturnsApplied()
    {
        var keyPathA = BuildKeyPath("A");
        var keyPathB = BuildKeyPath("B");

        using (var keyA = Registry.CurrentUser.CreateSubKey(keyPathA, true))
        {
            keyA!.SetValue("ValueA", "1", RegistryValueKind.String);
        }

        using (var keyB = Registry.CurrentUser.CreateSubKey(keyPathB, true))
        {
            keyB!.SetValue("ValueB", "1", RegistryValueKind.String);
        }

        try
        {
            var tweak = BuildTweak(keyPathA, keyPathB, "1");

            var detect = await tweak.DetectAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Applied, detect.Status);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPathA, false);
            Registry.CurrentUser.DeleteSubKeyTree(keyPathB, false);
        }
    }

    [Fact]
    public async Task ApplyRollback_WhenValuesMissing_RestoresMissingValues()
    {
        var keyPathA = BuildKeyPath("A");
        var keyPathB = BuildKeyPath("B");
        Registry.CurrentUser.DeleteSubKeyTree(keyPathA, false);
        Registry.CurrentUser.DeleteSubKeyTree(keyPathB, false);

        try
        {
            var tweak = BuildTweak(keyPathA, keyPathB, "1");

            var detect = await tweak.DetectAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Detected, detect.Status);

            var apply = await tweak.ApplyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Applied, apply.Status);

            var verify = await tweak.VerifyAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.Verified, verify.Status);

            var rollback = await tweak.RollbackAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.RolledBack, rollback.Status);

            using var keyA = Registry.CurrentUser.OpenSubKey(keyPathA);
            using var keyB = Registry.CurrentUser.OpenSubKey(keyPathB);
            Assert.Null(keyA?.GetValue("ValueA"));
            Assert.Null(keyB?.GetValue("ValueB"));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPathA, false);
            Registry.CurrentUser.DeleteSubKeyTree(keyPathB, false);
        }
    }

    [Fact]
    public async Task Rollback_RestoresOriginalValues()
    {
        var keyPathA = BuildKeyPath("A");
        var keyPathB = BuildKeyPath("B");
        using (var keyA = Registry.CurrentUser.CreateSubKey(keyPathA, true))
        {
            keyA!.SetValue("ValueA", "OriginalA", RegistryValueKind.String);
        }

        using (var keyB = Registry.CurrentUser.CreateSubKey(keyPathB, true))
        {
            keyB!.SetValue("ValueB", "OriginalB", RegistryValueKind.String);
        }

        try
        {
            var tweak = BuildTweak(keyPathA, keyPathB, "2");

            await tweak.DetectAsync(CancellationToken.None);
            await tweak.ApplyAsync(CancellationToken.None);
            var rollback = await tweak.RollbackAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.RolledBack, rollback.Status);

            using var keyA = Registry.CurrentUser.OpenSubKey(keyPathA);
            using var keyB = Registry.CurrentUser.OpenSubKey(keyPathB);
            Assert.Equal("OriginalA", keyA?.GetValue("ValueA") as string);
            Assert.Equal("OriginalB", keyB?.GetValue("ValueB") as string);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPathA, false);
            Registry.CurrentUser.DeleteSubKeyTree(keyPathB, false);
        }
    }

    [Fact]
    public async Task Verify_SupportsBinaryAndMultiStringValues()
    {
        var keyPathBinary = BuildKeyPath("Binary");
        var keyPathMulti = BuildKeyPath("Multi");
        var originalBinary = new byte[] { 0x10, 0x20 };
        var originalMulti = new[] { "one", "two" };

        using (var key = Registry.CurrentUser.CreateSubKey(keyPathBinary, true))
        {
            key!.SetValue("BinaryValue", originalBinary, RegistryValueKind.Binary);
        }

        using (var key = Registry.CurrentUser.CreateSubKey(keyPathMulti, true))
        {
            key!.SetValue("MultiValue", originalMulti, RegistryValueKind.MultiString);
        }

        try
        {
            var tweak = BuildBinaryMultiStringTweak(keyPathBinary, keyPathMulti);

            await tweak.DetectAsync(CancellationToken.None);
            await tweak.ApplyAsync(CancellationToken.None);
            var verify = await tweak.VerifyAsync(CancellationToken.None);

            Assert.Equal(TweakStatus.Verified, verify.Status);

            var rollback = await tweak.RollbackAsync(CancellationToken.None);
            Assert.Equal(TweakStatus.RolledBack, rollback.Status);

            using var binaryKey = Registry.CurrentUser.OpenSubKey(keyPathBinary);
            using var multiKey = Registry.CurrentUser.OpenSubKey(keyPathMulti);
            Assert.Equal(originalBinary, binaryKey?.GetValue("BinaryValue") as byte[]);
            Assert.Equal(originalMulti, multiKey?.GetValue("MultiValue") as string[]);
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPathBinary, false);
            Registry.CurrentUser.DeleteSubKeyTree(keyPathMulti, false);
        }
    }

    private static RegistryValueBatchTweak BuildTweak(string keyPathA, string keyPathB, string target)
    {
        var entries = new[]
        {
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, keyPathA, "ValueA", RegistryValueKind.String, target),
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, keyPathB, "ValueB", RegistryValueKind.String, target)
        };

        var accessor = new LocalRegistryAccessor();
        return new RegistryValueBatchTweak(
            "test.registrybatch",
            "Test registry batch tweak",
            "Test tweak that manipulates multiple registry keys.",
            TweakRiskLevel.Safe,
            entries,
            accessor,
            requiresElevation: false);
    }

    private static RegistryValueBatchTweak BuildBinaryMultiStringTweak(string keyPathBinary, string keyPathMulti)
    {
        var entries = new[]
        {
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, keyPathBinary, "BinaryValue", RegistryValueKind.Binary, new byte[] { 0x00, 0xFF }),
            new RegistryValueBatchEntry(RegistryHive.CurrentUser, keyPathMulti, "MultiValue", RegistryValueKind.MultiString, new[] { "alpha", "beta" })
        };

        var accessor = new LocalRegistryAccessor();
        return new RegistryValueBatchTweak(
            "test.registrybatch.multitype",
            "Test registry batch tweak (multi-type)",
            "Test tweak that uses binary and multi-string registry values.",
            TweakRiskLevel.Safe,
            entries,
            accessor,
            requiresElevation: false);
    }

    private static string BuildKeyPath(string suffix)
    {
        return $"Software\\WindowsOptimizer\\Tests\\RegistryValueBatchTweak\\{suffix}\\{Guid.NewGuid():N}";
    }
}
