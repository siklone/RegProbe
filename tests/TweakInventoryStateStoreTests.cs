using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTraceProject.Infrastructure;

namespace OpenTraceProject.Tests;

public sealed class TweakInventoryStateStoreTests
{
    [Fact]
    public void Load_WhenCacheFileMissing_ReturnsEmptyDictionary()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new AppPaths(root);
            var store = new TweakInventoryStateStore(paths);

            var states = store.Load();

            Assert.NotNull(states);
            Assert.Empty(states);
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsInventoryStates()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new AppPaths(root);
            var store = new TweakInventoryStateStore(paths);
            var timestamp = DateTimeOffset.UtcNow;

            var source = new List<TweakInventoryState>
            {
                new()
                {
                    Id = "system.test-1",
                    AppliedStatus = "Applied",
                    CurrentValue = "1 (0x1)",
                    TargetValue = "1 (0x1)",
                    LastDetectedAtUtc = timestamp,
                    ImpactArea = "Registry"
                },
                new()
                {
                    Id = "privacy.test-2",
                    AppliedStatus = "NotApplied",
                    CurrentValue = "0 (0x0)",
                    TargetValue = "1 (0x1)",
                    LastDetectedAtUtc = timestamp.AddMinutes(-1),
                    ImpactArea = "Registry"
                }
            };

            store.Save(source);
            var loaded = store.Load();

            Assert.Equal(2, loaded.Count);
            Assert.True(loaded.TryGetValue("system.test-1", out var first));
            Assert.NotNull(first);
            Assert.Equal("Applied", first!.AppliedStatus);
            Assert.Equal("1 (0x1)", first.CurrentValue);
            Assert.Equal("Registry", first.ImpactArea);
            Assert.Equal(timestamp, first.LastDetectedAtUtc);
        }
        finally
        {
            SafeDelete(root);
        }
    }

    [Fact]
    public void Save_DeduplicatesById_UsingLatestEntry()
    {
        var root = CreateTempRoot();
        try
        {
            var paths = new AppPaths(root);
            var store = new TweakInventoryStateStore(paths);

            store.Save(new[]
            {
                new TweakInventoryState
                {
                    Id = "system.same-id",
                    AppliedStatus = "Unknown",
                    CurrentValue = "Unknown"
                },
                new TweakInventoryState
                {
                    Id = "system.same-id",
                    AppliedStatus = "Applied",
                    CurrentValue = "1 (0x1)"
                }
            });

            var loaded = store.Load();

            Assert.Single(loaded);
            var state = loaded.Values.Single();
            Assert.Equal("Applied", state.AppliedStatus);
            Assert.Equal("1 (0x1)", state.CurrentValue);
        }
        finally
        {
            SafeDelete(root);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wo-cache-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void SafeDelete(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }
}
