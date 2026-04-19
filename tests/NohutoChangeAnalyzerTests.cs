using System.Collections.Generic;
using RegProbe.App.Services;

namespace RegProbe.Tests;

public sealed class NohutoChangeAnalyzerTests
{
    private static void AssertEquivalent(
        NohutoChangeAnalysis expected,
        NohutoChangeAnalysis actual)
    {
        Assert.Equal(expected.TotalChangedFiles, actual.TotalChangedFiles);
        Assert.Equal(expected.DocumentationChangedFiles, actual.DocumentationChangedFiles);
        Assert.Equal(expected.ScriptChangedFiles, actual.ScriptChangedFiles);
        Assert.Equal(expected.SourceChangedFiles, actual.SourceChangedFiles);
        Assert.Equal(expected.AssetChangedFiles, actual.AssetChangedFiles);
        Assert.Equal(expected.DataChangedFiles, actual.DataChangedFiles);
        Assert.Equal(
            expected.TopCategories.Select(category => (category.Category, category.Score, category.FileCount)),
            actual.TopCategories.Select(category => (category.Category, category.Score, category.FileCount)));
    }

    [Fact]
    public void Analyze_DefaultsToWinRegistryCategories()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "records/Tcpip-Parameters.txt", Additions = 15, Deletions = 2 },
            new() { Path = "records/Power.txt", Additions = 10, Deletions = 1 },
            new() { Path = "guide/wpr-wpa.md", Additions = 8, Deletions = 0 },
            new() { Path = "README.md", Additions = 3, Deletions = 1 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze(files);

        Assert.Equal(4, analysis.TotalChangedFiles);
        Assert.Equal(2, analysis.DataChangedFiles);
        Assert.Equal(2, analysis.DocumentationChangedFiles);
        Assert.NotEmpty(analysis.TopCategories);
        Assert.Contains(analysis.TopCategories, c => c.Category == "Network");
        Assert.Contains(analysis.TopCategories, c => c.Category == "Power");
    }

    [Fact]
    public void Analyze_WinConfig_MapsTopLevelFoldersToAppDomains()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "network/desc.md", Additions = 6, Deletions = 0 },
            new() { Path = "power/assets/NV-IMOD.py", Additions = 12, Deletions = 2 },
            new() { Path = "security/desc.md", Additions = 4, Deletions = 1 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze("win-config", files);

        Assert.Equal(2, analysis.DocumentationChangedFiles);
        Assert.Equal(1, analysis.ScriptChangedFiles);
        Assert.Contains(analysis.TopCategories, c => c.Category == "Network");
        Assert.Contains(analysis.TopCategories, c => c.Category == "Power");
        Assert.Contains(analysis.TopCategories, c => c.Category == "Security");
    }

    [Fact]
    public void Analyze_DecompiledPseudocode_MapsSubsystemFolders()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "dxgkrnl/Registry.c", Additions = 25, Deletions = 3 },
            new() { Path = "stornvme/GetRegistrySettings26H1.c", Additions = 8, Deletions = 1 },
            new() { Path = "USBHUB3/HubState.c", Additions = 5, Deletions = 0 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze("decompiled-pseudocode", files);

        Assert.Equal(3, analysis.SourceChangedFiles);
        Assert.Contains(analysis.TopCategories, c => c.Category == "Graphics");
        Assert.Contains(analysis.TopCategories, c => c.Category == "Storage");
        Assert.Contains(analysis.TopCategories, c => c.Category == "Peripheral");
    }

    [Fact]
    public void Analyze_RegKit_DetectsRegistryAndInstallerWork()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "src/compare.cpp", Additions = 20, Deletions = 4 },
            new() { Path = "installer/setup.iss", Additions = 7, Deletions = 1 },
            new() { Path = "assets/icons/lucide/light/refresh.ico", Additions = 1, Deletions = 0 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze("regkit", files);

        Assert.Equal(1, analysis.SourceChangedFiles);
        Assert.Equal(1, analysis.ScriptChangedFiles);
        Assert.Equal(1, analysis.AssetChangedFiles);
        Assert.Contains(analysis.TopCategories, c => c.Category == "Registry");
        Assert.Contains(analysis.TopCategories, c => c.Category == "Installer");
    }

    [Fact]
    public void Analyze_WinConfig_CapsTopCategoriesToTopFiveScores()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "network/desc.md", Additions = 20, Deletions = 0 },
            new() { Path = "power/desc.md", Additions = 18, Deletions = 0 },
            new() { Path = "security/desc.md", Additions = 16, Deletions = 0 },
            new() { Path = "privacy/desc.md", Additions = 14, Deletions = 0 },
            new() { Path = "system/desc.md", Additions = 12, Deletions = 0 },
            new() { Path = "visibility/desc.md", Additions = 10, Deletions = 0 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze("win-config", files);

        Assert.Equal(6, analysis.DocumentationChangedFiles);
        Assert.Equal(5, analysis.TopCategories.Count);
        Assert.Equal(
            new[] { "Network", "Power", "Security", "Privacy", "System" },
            analysis.TopCategories.Select(category => category.Category).ToArray());
    }

    [Fact]
    public void Analyze_UsesMinimumWeightOfOnePerRetainedFile()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "network/zero-weight-a.md", Additions = 0, Deletions = 0 },
            new() { Path = "network/zero-weight-b.md", Additions = -3, Deletions = 0 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze("win-config", files);
        var networkCategory = Assert.Single(analysis.TopCategories);

        Assert.Equal(2, analysis.TotalChangedFiles);
        Assert.Equal(2, analysis.DocumentationChangedFiles);
        Assert.Equal("Network", networkCategory.Category);
        Assert.Equal(2, networkCategory.Score);
        Assert.Equal(2, networkCategory.FileCount);
    }

    [Fact]
    public void Analyze_BreaksScoreTiesUsingFileCount()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "network/file-a.md", Additions = 5, Deletions = 0 },
            new() { Path = "network/file-b.md", Additions = 5, Deletions = 0 },
            new() { Path = "power/desc.md", Additions = 10, Deletions = 0 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze("win-config", files);

        Assert.Equal(
            new[] { "Network", "Power" },
            analysis.TopCategories.Select(category => category.Category).ToArray());
        Assert.Equal(10, analysis.TopCategories[0].Score);
        Assert.Equal(2, analysis.TopCategories[0].FileCount);
        Assert.Equal(10, analysis.TopCategories[1].Score);
        Assert.Equal(1, analysis.TopCategories[1].FileCount);
    }

    [Fact]
    public void Analyze_DefaultOverload_MatchesExplicitWinRegistryDefinition()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "records/Tcpip-Parameters.txt", Additions = 15, Deletions = 2 },
            new() { Path = "records/Power.txt", Additions = 10, Deletions = 1 },
            new() { Path = "guide/wpr-wpa.md", Additions = 8, Deletions = 0 },
            new() { Path = "README.md", Additions = 3, Deletions = 1 }
        };

        var implicitAnalysis = NohutoChangeAnalyzer.Analyze(files);
        var explicitAnalysis = NohutoChangeAnalyzer.Analyze(
            NohutoConfigurationSourceCatalog.Get("win-registry"),
            files);

        AssertEquivalent(explicitAnalysis, implicitAnalysis);
    }

    [Fact]
    public void Analyze_StringOverload_MatchesExplicitRepositoryDefinition()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "src/compare.cpp", Additions = 20, Deletions = 4 },
            new() { Path = "installer/setup.iss", Additions = 7, Deletions = 1 },
            new() { Path = "assets/icons/lucide/light/refresh.ico", Additions = 1, Deletions = 0 }
        };

        var stringAnalysis = NohutoChangeAnalyzer.Analyze("regkit", files);
        var explicitAnalysis = NohutoChangeAnalyzer.Analyze(
            NohutoConfigurationSourceCatalog.Get("regkit"),
            files);

        AssertEquivalent(explicitAnalysis, stringAnalysis);
    }

    [Fact]
    public void Analyze_StringOverload_IsCaseInsensitive()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "src/compare.cpp", Additions = 20, Deletions = 4 },
            new() { Path = "installer/setup.iss", Additions = 7, Deletions = 1 },
            new() { Path = "assets/icons/lucide/light/refresh.ico", Additions = 1, Deletions = 0 }
        };

        var lowercaseAnalysis = NohutoChangeAnalyzer.Analyze("regkit", files);
        var mixedCaseAnalysis = NohutoChangeAnalyzer.Analyze("RegKit", files);

        AssertEquivalent(lowercaseAnalysis, mixedCaseAnalysis);
    }

    [Fact]
    public void Analyze_StringOverload_ThrowsForUnknownRepositoryId()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "README.md", Additions = 1, Deletions = 0 }
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => NohutoChangeAnalyzer.Analyze("does-not-exist", files));

        Assert.Contains("Unknown nohuto repository id", exception.Message);
    }

    [Fact]
    public void Analyze_StringOverload_ThrowsForWhitespaceRepositoryId()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "README.md", Additions = 1, Deletions = 0 }
        };

        var exception = Assert.Throws<ArgumentException>(
            () => NohutoChangeAnalyzer.Analyze("   ", files));

        Assert.Contains("Repository id is required.", exception.Message);
    }

    [Fact]
    public void Analyze_ExplicitRepositoryOverload_ThrowsForNullRepository()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "README.md", Additions = 1, Deletions = 0 }
        };

        Assert.Throws<ArgumentNullException>(
            () => NohutoChangeAnalyzer.Analyze((NohutoRepositoryDefinition)null!, files));
    }

    [Fact]
    public void Analyze_ExplicitRepositoryOverload_ThrowsForNullChangedFiles()
    {
        var repository = NohutoConfigurationSourceCatalog.Get("win-registry");

        Assert.Throws<ArgumentNullException>(
            () => NohutoChangeAnalyzer.Analyze(repository, (IEnumerable<NohutoChangedFile>)null!));
    }

    [Fact]
    public void Analyze_IgnoresNullAndWhitespacePaths()
    {
        var repository = NohutoConfigurationSourceCatalog.Get("win-registry");
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "records/Tcpip-Parameters.txt", Additions = 15, Deletions = 2 },
            new() { Path = "   ", Additions = 40, Deletions = 4 },
            new() { Path = string.Empty, Additions = 7, Deletions = 1 },
            null!
        };

        var analysis = NohutoChangeAnalyzer.Analyze(repository, files);

        Assert.Equal(1, analysis.TotalChangedFiles);
        Assert.Equal(1, analysis.DataChangedFiles);
        Assert.Contains(analysis.TopCategories, c => c.Category == "Network");
    }

    [Fact]
    public void Analyze_FilteredInputOnly_ReturnsZeroedAnalysis()
    {
        var repository = NohutoConfigurationSourceCatalog.Get("win-registry");
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "   ", Additions = 40, Deletions = 4 },
            new() { Path = string.Empty, Additions = 7, Deletions = 1 },
            null!
        };

        var analysis = NohutoChangeAnalyzer.Analyze(repository, files);

        Assert.Equal(0, analysis.TotalChangedFiles);
        Assert.Equal(0, analysis.DocumentationChangedFiles);
        Assert.Equal(0, analysis.ScriptChangedFiles);
        Assert.Equal(0, analysis.SourceChangedFiles);
        Assert.Equal(0, analysis.AssetChangedFiles);
        Assert.Equal(0, analysis.DataChangedFiles);
        Assert.Empty(analysis.TopCategories);
    }

    [Fact]
    public void Catalog_ContainsAllTrackedNohutoSources()
    {
        Assert.Collection(
            NohutoConfigurationSourceCatalog.All,
            repository => Assert.Equal("win-config", repository.Id),
            repository => Assert.Equal("win-registry", repository.Id),
            repository => Assert.Equal("decompiled-pseudocode", repository.Id),
            repository => Assert.Equal("regkit", repository.Id));
    }
}
