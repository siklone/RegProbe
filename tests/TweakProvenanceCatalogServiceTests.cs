using System;
using System.IO;
using System.Linq;
using OpenTraceProject.App.Services;

namespace OpenTraceProject.Tests;

public sealed class TweakProvenanceCatalogServiceTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _docsRoot;

    public TweakProvenanceCatalogServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "OpenTraceProject-ProvenanceTests", Guid.NewGuid().ToString("N"));
        _docsRoot = Path.Combine(_rootDirectory, "Docs");
        Directory.CreateDirectory(Path.Combine(_docsRoot, "tweaks"));

        File.WriteAllText(Path.Combine(_docsRoot, "tweaks", "tweaks.md"), "# marker\n");
        File.WriteAllText(Path.Combine(_docsRoot, "tweaks", "tweak-provenance.md"), "# provenance\n");
        File.WriteAllText(
            Path.Combine(_docsRoot, "tweaks", "tweak-provenance.json"),
            """
            {
              "GeneratedAtUtc": "2026-03-08T20:30:00Z",
              "Summary": "1/1 tweaks have nohuto evidence.",
              "TotalTweaks": 1,
              "RepoBackedTweaks": 1,
              "InternalsBackedTweaks": 1,
              "ReviewNeededTweaks": 0,
              "Entries": [
                {
                  "Id": "cleanup.eventlog-{logName.ToLowerInvariant()}",
                  "Name": "Clear Event Log",
                  "Category": "Cleanup",
                  "Risk": "Advanced",
                  "Source": "engine\\\\Tweaks\\\\Commands\\\\Cleanup\\\\ClearEventLogsTweak.cs#L15",
                  "HasNohutoEvidence": true,
                  "HasWindowsInternalsContext": true,
                  "NeedsReview": false,
                  "CoverageState": "repo-backed",
                  "Summary": "Matched upstream nohuto documentation.",
                  "SourceRepositories": [ "win-config", "win-registry" ],
                  "MatchedTokens": [ "event log", "eventvwr" ],
                  "References": [
                    {
                      "Title": "win-config / cleanup/desc.md",
                      "Url": "https://github.com/nohuto/win-config/blob/main/cleanup/desc.md",
                      "Kind": "nohuto",
                      "Summary": "Category documentation."
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public void Catalog_LoadsSummaryAndEntries()
    {
        var service = new TweakProvenanceCatalogService(_docsRoot);

        Assert.Equal(1, service.Catalog.TotalTweaks);
        Assert.Equal("1/1 tweaks have nohuto evidence.", service.Catalog.Summary);
        Assert.Single(service.Catalog.Entries);
        Assert.True(service.Catalog.Entries[0].HasNohutoEvidence);
        Assert.True(service.Catalog.Entries[0].HasWindowsInternalsContext);
    }

    [Fact]
    public void ResolveMarkdownReportPath_ReturnsLocalPath()
    {
        var service = new TweakProvenanceCatalogService(_docsRoot);

        var path = service.ResolveMarkdownReportPath();

        Assert.Equal(Path.Combine(_docsRoot, "tweaks", "tweak-provenance.md"), path);
    }

    [Fact]
    public void Catalog_RealGeneratedProvenance_UsesOverridesAndKeepsWinsockInReview()
    {
        var docsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Docs"));
        Assert.True(Directory.Exists(docsRoot));

        var service = new TweakProvenanceCatalogService(docsRoot);

        var componentStore = service.Catalog.Entries.Single(entry =>
            string.Equals(entry.Id, "cleanup.component-store", StringComparison.OrdinalIgnoreCase));
        Assert.False(componentStore.NeedsReview);
        Assert.Equal("repo-backed", componentStore.CoverageState);
        Assert.Contains(componentStore.References, reference =>
            reference.Url.Contains("manage-the-component-store", StringComparison.OrdinalIgnoreCase));

        var winsock = service.Catalog.Entries.Single(entry =>
            string.Equals(entry.Id, "network.reset-winsock", StringComparison.OrdinalIgnoreCase));
        Assert.True(winsock.NeedsReview);
        Assert.Equal("category-fallback", winsock.CoverageState);
        Assert.Contains(winsock.References, reference =>
            reference.Url.Contains("netsh-winsock", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup for temp fixtures.
        }
    }
}
