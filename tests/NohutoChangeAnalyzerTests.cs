using System.Collections.Generic;
using OpenTraceProject.App.Services;

namespace OpenTraceProject.Tests;

public sealed class NohutoChangeAnalyzerTests
{
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
