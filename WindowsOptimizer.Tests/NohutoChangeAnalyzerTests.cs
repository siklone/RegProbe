using System.Collections.Generic;
using WindowsOptimizer.App.Services;

namespace WindowsOptimizer.Tests;

public sealed class NohutoChangeAnalyzerTests
{
    [Fact]
    public void Analyze_ProducesTopCategories_FromRecordsAndGuides()
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
        Assert.Equal(2, analysis.RecordsChangedFiles);
        Assert.Equal(1, analysis.GuidesChangedFiles);
        Assert.Equal(1, analysis.DocsChangedFiles);
        Assert.NotEmpty(analysis.TopCategories);
        Assert.Contains(analysis.TopCategories, c => c.Category == "Network");
        Assert.Contains(analysis.TopCategories, c => c.Category == "Power");
    }

    [Fact]
    public void Analyze_ReturnsMisc_WhenNoPatternMatches()
    {
        var files = new List<NohutoChangedFile>
        {
            new() { Path = "records/UnknownThing.txt", Additions = 1, Deletions = 0 }
        };

        var analysis = NohutoChangeAnalyzer.Analyze(files);

        Assert.Single(analysis.TopCategories);
        Assert.Equal("Misc", analysis.TopCategories[0].Category);
    }
}
