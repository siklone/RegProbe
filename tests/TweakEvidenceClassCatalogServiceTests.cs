using System;
using System.IO;
using RegProbe.App.Services;

namespace RegProbe.Tests;

public sealed class TweakEvidenceClassCatalogServiceTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _docsRoot;

    public TweakEvidenceClassCatalogServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "RegProbe-EvidenceClassTests", Guid.NewGuid().ToString("N"));
        _docsRoot = Path.Combine(_rootDirectory, "Docs");
        Directory.CreateDirectory(Path.Combine(_docsRoot, "research"));

        File.WriteAllText(
            Path.Combine(_docsRoot, "research", "evidence-classes.json"),
            """
            {
              "generated_utc": "2026-04-09T12:00:00Z",
              "summary": {
                "total_records": 1,
                "class_counts": {
                  "A": 1
                },
                "action_state_counts": {
                  "actionable": 1
                }
              },
              "classes": {},
              "entries": [
                {
                  "record_id": "system.example",
                  "tweak_id": "system.example",
                  "record_status": "validated",
                  "evidence_class": "A",
                  "class_label": "Class A",
                  "class_title": "Ready",
                  "class_description": "Ready for app surface",
                  "show_in_app": true,
                  "is_actionable": true,
                  "is_archived": false,
                  "action_state": "actionable",
                  "gating_reason": "",
                  "confidence": "high",
                  "app_mapping_status": "matches-research",
                  "restore_story_known": true
                }
              ]
            }
            """);
    }

    [Fact]
    public void Catalog_LoadsSnakeCaseEntries()
    {
        var service = new TweakEvidenceClassCatalogService(_docsRoot);

        Assert.Single(service.Catalog.Entries);
        Assert.Equal("system.example", service.Catalog.Entries[0].TweakId);
        Assert.True(service.Catalog.Entries[0].IsActionable);
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
        }
    }
}
