using System;
using System.IO;
using WindowsOptimizer.Infrastructure;
using Xunit;

namespace WindowsOptimizer.Tests;

/// <summary>
/// Unit tests for AppPaths and AppSettings infrastructure classes.
/// Source: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices
/// </summary>
public sealed class AppPathsTests : IDisposable
{
    private readonly string _testDir;

    public AppPathsTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"AppPathsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void Constructor_WithValidPath_SetsBaseAppDataPath()
    {
        // Arrange & Act
        var paths = new AppPaths(_testDir);

        // Assert
        Assert.Equal(_testDir, paths.BaseAppDataPath);
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AppPaths(null!));
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AppPaths(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AppPaths("   "));
    }

    [Fact]
    public void AppDataRoot_ReturnsCorrectPath()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act
        var root = paths.AppDataRoot;

        // Assert
        Assert.Equal(Path.Combine(_testDir, AppPaths.AppFolderName), root);
    }

    [Fact]
    public void SettingsFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act
        var settingsPath = paths.SettingsFilePath;

        // Assert
        Assert.EndsWith("settings.json", settingsPath);
        Assert.Contains(AppPaths.AppFolderName, settingsPath);
    }

    [Fact]
    public void LogDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act
        var logDir = paths.LogDirectory;

        // Assert
        Assert.EndsWith("logs", logDir);
    }

    [Fact]
    public void TweakLogFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act
        var logPath = paths.TweakLogFilePath;

        // Assert
        Assert.EndsWith("tweak-log.csv", logPath);
    }

    [Fact]
    public void ProfilesDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act
        var profilesDir = paths.ProfilesDirectory;

        // Assert
        Assert.EndsWith("profiles", profilesDir);
    }

    [Fact]
    public void PresetsDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act
        var presetsDir = paths.PresetsDirectory;

        // Assert
        Assert.EndsWith("presets", presetsDir);
    }

    [Fact]
    public void EnsureDirectories_CreatesAllDirectories()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act
        paths.EnsureDirectories();

        // Assert
        Assert.True(Directory.Exists(paths.AppDataRoot), "AppDataRoot should exist");
        Assert.True(Directory.Exists(paths.LogDirectory), "LogDirectory should exist");
        Assert.True(Directory.Exists(paths.ProfilesDirectory), "ProfilesDirectory should exist");
        Assert.True(Directory.Exists(paths.PresetsDirectory), "PresetsDirectory should exist");
    }

    [Fact]
    public void EnsureDirectories_IdempotentWhenCalledTwice()
    {
        // Arrange
        var paths = new AppPaths(_testDir);

        // Act - call twice
        paths.EnsureDirectories();
        paths.EnsureDirectories();

        // Assert - should not throw
        Assert.True(Directory.Exists(paths.AppDataRoot));
    }

    [Fact]
    public void FromEnvironment_ReturnsValidInstance()
    {
        // Act
        var paths = AppPaths.FromEnvironment();

        // Assert
        Assert.NotNull(paths);
        Assert.False(string.IsNullOrWhiteSpace(paths.BaseAppDataPath));
    }

    [Fact]
    public void AppFolderName_IsWindowsOptimizerSuite()
    {
        // Assert
        Assert.Equal("WindowsOptimizerSuite", AppPaths.AppFolderName);
    }
}

/// <summary>
/// Unit tests for AppSettings.
/// </summary>
public sealed class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(1, settings.SchemaVersion);
        Assert.False(settings.DemoTweakAlphaEnabled);
        Assert.False(settings.DemoTweakBetaEnabled);
        Assert.Equal("Dark", settings.Theme);
        Assert.False(settings.EnableCardShadows);
        Assert.True(settings.RunStartupScanOnLaunch);
        Assert.True(settings.ShowPreviewHint);
        Assert.NotNull(settings.MonitorSections);
        Assert.Empty(settings.MonitorSections);
    }

    [Fact]
    public void Theme_CanBeSetToLight()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.Theme = "Light";

        // Assert
        Assert.Equal("Light", settings.Theme);
    }

    [Fact]
    public void MonitorSections_CanAddItems()
    {
        // Arrange
        var settings = new AppSettings();
        var section = new MonitorSectionState
        {
            Key = "cpu",
            Order = 1,
            IsVisible = true
        };

        // Act
        settings.MonitorSections.Add(section);

        // Assert
        Assert.Single(settings.MonitorSections);
        Assert.Equal("cpu", settings.MonitorSections[0].Key);
    }
}

/// <summary>
/// Unit tests for MonitorSectionState.
/// </summary>
public sealed class MonitorSectionStateTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var state = new MonitorSectionState();

        // Assert
        Assert.Equal(string.Empty, state.Key);
        Assert.Equal(0, state.Order);
        Assert.True(state.IsVisible);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var state = new MonitorSectionState
        {
            Key = "memory",
            Order = 2,
            IsVisible = false
        };

        // Assert
        Assert.Equal("memory", state.Key);
        Assert.Equal(2, state.Order);
        Assert.False(state.IsVisible);
    }
}
