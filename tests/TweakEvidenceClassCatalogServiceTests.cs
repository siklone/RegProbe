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

    [Fact]
    public void Store_SuppressesExternalNohutoPseudocodeAsUserFacingSourceProof()
    {
        var store = new TweakEvidenceClassCatalogStore(_docsRoot);
        var clone = store.CloneWithResolvedLinks(new TweakEvidenceClassEntry
        {
            RecordId = "peripheral.example",
            TweakId = "peripheral.example",
            UpstreamLineage = new TweakEvidenceProofBlock
            {
                Summary = "Upstream dump / pseudocode links are attached to this record.",
                HasNohutoLineage = true,
                Links =
                {
                    new TweakEvidenceLink
                    {
                        Title = "decompiled-pseudocode / USBHUB3",
                        Url = "https://github.com/nohuto/decompiled-pseudocode/tree/main/USBHUB3",
                        Kind = "nohuto"
                    }
                }
            }
        });

        Assert.NotNull(clone.UpstreamLineage);
        Assert.Empty(clone.UpstreamLineage!.Links);
        Assert.False(clone.UpstreamLineage.HasNohutoLineage);
        Assert.DoesNotContain("nohuto", clone.UpstreamLineage.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("discovery and naming context", clone.UpstreamLineage.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Store_LabelsCatalogOnlySourceAsDiscoveryNotSemanticsProof()
    {
        var store = new TweakEvidenceClassCatalogStore(_docsRoot);
        var clone = store.CloneWithResolvedLinks(new TweakEvidenceClassEntry
        {
            RecordId = "system.catalog-only",
            TweakId = "system.catalog-only",
            UpstreamLineage = new TweakEvidenceProofBlock
            {
                Summary = "No upstream nohuto source link is attached to this record.",
                HasNohutoLineage = false
            }
        });

        Assert.NotNull(clone.UpstreamLineage);
        Assert.Contains("Catalog-only source context is not a value-semantics proof", clone.UpstreamLineage!.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Docs, Runtime, and Rollback", clone.UpstreamLineage.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Store_LabelsSuppressedPseudocodeWithoutVisibleLinks_AsNoLocalSource()
    {
        var store = new TweakEvidenceClassCatalogStore(_docsRoot);
        var clone = store.CloneWithResolvedLinks(new TweakEvidenceClassEntry
        {
            RecordId = "system.external-pseudocode-only",
            TweakId = "system.external-pseudocode-only",
            UpstreamLineage = new TweakEvidenceProofBlock
            {
                Summary = "Upstream dump / pseudocode links are attached to this record.",
                HasNohutoLineage = true,
                Links =
                {
                    new TweakEvidenceLink
                    {
                        Title = "decompiled-pseudocode / USBHUB3",
                        Url = "https://github.com/nohuto/decompiled-pseudocode/tree/main/USBHUB3",
                        Kind = "nohuto"
                    }
                }
            }
        });

        Assert.NotNull(clone.UpstreamLineage);
        Assert.Empty(clone.UpstreamLineage!.Links);
        Assert.False(clone.UpstreamLineage.HasNohutoLineage);
        Assert.Contains("No local source-code mirror", clone.UpstreamLineage.Summary, StringComparison.OrdinalIgnoreCase);
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
