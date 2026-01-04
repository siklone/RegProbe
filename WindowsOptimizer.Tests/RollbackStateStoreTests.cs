using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Infrastructure;
using Xunit;

namespace WindowsOptimizer.Tests;

/// <summary>
/// Unit tests for RollbackStateStore.
/// Tests JSON serialization, state persistence, and recovery scenarios.
/// Source: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices
/// </summary>
public sealed class RollbackStateStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly AppPaths _paths;
    private readonly RollbackStateStore _store;

    public RollbackStateStoreTests()
    {
        // Create isolated test directory
        _testDir = Path.Combine(Path.GetTempPath(), $"WindowsOptimizerTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        
        _paths = new AppPaths(_testDir);
        _store = new RollbackStateStore(_paths);
    }

    public void Dispose()
    {
        // Cleanup test directory
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public async Task SaveOriginalState_CreatesEntry()
    {
        // Arrange
        var entry = CreateTestEntry("test.tweak.1");

        // Act
        await _store.SaveOriginalStateAsync(entry, CancellationToken.None);
        var pending = await _store.GetPendingRollbacksAsync(CancellationToken.None);

        // Assert
        Assert.Single(pending);
        Assert.Equal("test.tweak.1", pending[0].TweakId);
        Assert.Equal(RollbackStatus.Pending, pending[0].Status);
    }

    [Fact]
    public async Task SaveOriginalState_UpdatesExistingEntry()
    {
        // Arrange
        var entry1 = CreateTestEntry("test.tweak.1", "OriginalValue_1");
        var entry2 = CreateTestEntry("test.tweak.1", "OriginalValue_2");

        // Act
        await _store.SaveOriginalStateAsync(entry1, CancellationToken.None);
        await _store.SaveOriginalStateAsync(entry2, CancellationToken.None);
        var pending = await _store.GetPendingRollbacksAsync(CancellationToken.None);

        // Assert - should only have one entry with updated value
        Assert.Single(pending);
        Assert.Equal("OriginalValue_2", pending[0].OriginalValue?.ToString());
    }

    [Fact]
    public async Task MarkApplied_ChangesStatus()
    {
        // Arrange
        var entry = CreateTestEntry("test.tweak.2");
        await _store.SaveOriginalStateAsync(entry, CancellationToken.None);

        // Act
        await _store.MarkAppliedAsync("test.tweak.2", CancellationToken.None);
        var pending = await _store.GetPendingRollbacksAsync(CancellationToken.None);

        // Assert - Applied entries should not be in pending list
        Assert.Empty(pending);
    }

    [Fact]
    public async Task MarkRolledBack_RemovesEntry()
    {
        // Arrange
        var entry = CreateTestEntry("test.tweak.3");
        await _store.SaveOriginalStateAsync(entry, CancellationToken.None);

        // Act
        await _store.MarkRolledBackAsync("test.tweak.3", CancellationToken.None);
        var pending = await _store.GetPendingRollbacksAsync(CancellationToken.None);

        // Assert
        Assert.Empty(pending);
    }

    [Fact]
    public async Task GetOriginalState_ReturnsCorrectEntry()
    {
        // Arrange
        var entry1 = CreateTestEntry("test.tweak.a");
        var entry2 = CreateTestEntry("test.tweak.b");
        await _store.SaveOriginalStateAsync(entry1, CancellationToken.None);
        await _store.SaveOriginalStateAsync(entry2, CancellationToken.None);

        // Act
        var result = await _store.GetOriginalStateAsync("test.tweak.b", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.tweak.b", result.TweakId);
    }

    [Fact]
    public async Task GetOriginalState_ReturnsNullForMissing()
    {
        // Arrange - no entries saved

        // Act
        var result = await _store.GetOriginalStateAsync("nonexistent.tweak", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAll_RemovesAllEntries()
    {
        // Arrange
        await _store.SaveOriginalStateAsync(CreateTestEntry("test.1"), CancellationToken.None);
        await _store.SaveOriginalStateAsync(CreateTestEntry("test.2"), CancellationToken.None);
        await _store.SaveOriginalStateAsync(CreateTestEntry("test.3"), CancellationToken.None);

        // Act
        await _store.ClearAllAsync(CancellationToken.None);
        var pending = await _store.GetPendingRollbacksAsync(CancellationToken.None);

        // Assert
        Assert.Empty(pending);
    }

    [Fact]
    public async Task SaveOriginalState_PersistsToFile()
    {
        // Arrange
        var entry = CreateTestEntry("test.persist");
        await _store.SaveOriginalStateAsync(entry, CancellationToken.None);

        // Act - create new store instance to verify file persistence
        var newStore = new RollbackStateStore(_paths);
        var pending = await newStore.GetPendingRollbacksAsync(CancellationToken.None);

        // Assert
        Assert.Single(pending);
        Assert.Equal("test.persist", pending[0].TweakId);
    }

    [Fact]
    public async Task GetPendingRollbacks_CaseInsensitive()
    {
        // Arrange
        var entry = CreateTestEntry("Test.Tweak.CaseTest");
        await _store.SaveOriginalStateAsync(entry, CancellationToken.None);

        // Act
        var result = await _store.GetOriginalStateAsync("test.tweak.casetest", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task MarkApplied_IgnoresNullOrEmpty()
    {
        // Arrange - no entries

        // Act & Assert - should not throw
        await _store.MarkAppliedAsync(null!, CancellationToken.None);
        await _store.MarkAppliedAsync(string.Empty, CancellationToken.None);
        await _store.MarkAppliedAsync("   ", CancellationToken.None);
    }

    [Fact]
    public async Task MarkRolledBack_IgnoresNullOrEmpty()
    {
        // Arrange - no entries

        // Act & Assert - should not throw
        await _store.MarkRolledBackAsync(null!, CancellationToken.None);
        await _store.MarkRolledBackAsync(string.Empty, CancellationToken.None);
    }

    [Fact]
    public async Task MultipleTweaks_TrackedIndependently()
    {
        // Arrange
        await _store.SaveOriginalStateAsync(CreateTestEntry("tweak.1"), CancellationToken.None);
        await _store.SaveOriginalStateAsync(CreateTestEntry("tweak.2"), CancellationToken.None);
        await _store.SaveOriginalStateAsync(CreateTestEntry("tweak.3"), CancellationToken.None);

        // Act
        await _store.MarkRolledBackAsync("tweak.2", CancellationToken.None);
        var pending = await _store.GetPendingRollbacksAsync(CancellationToken.None);

        // Assert - should have 2 remaining
        Assert.Equal(2, pending.Count);
        Assert.DoesNotContain(pending, e => e.TweakId == "tweak.2");
    }

    private static RollbackEntry CreateTestEntry(string tweakId, string? originalValue = null)
    {
        return new RollbackEntry
        {
            TweakId = tweakId,
            TweakName = $"Test Tweak {tweakId}",
            Category = "Test",
            RegistryHive = "HKEY_CURRENT_USER",
            RegistryPath = @"SOFTWARE\Test",
            RegistryValueName = "TestValue",
            OriginalValue = originalValue ?? "OriginalTestValue",
            OriginalValueKind = "String",
            ValueExisted = true,
            CapturedAt = DateTimeOffset.UtcNow,
            Status = RollbackStatus.Pending
        };
    }
}
