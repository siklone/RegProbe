using System;
using System.IO;
using RegProbe.App.Services;

namespace RegProbe.Tests;

public sealed class TweakPromotionGateCatalogServiceTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _docsRoot;

    public TweakPromotionGateCatalogServiceTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "RegProbe-PromotionGateTests", Guid.NewGuid().ToString("N"));
        _docsRoot = Path.Combine(_rootDirectory, "Docs");
        Directory.CreateDirectory(Path.Combine(_docsRoot, "research"));

        File.WriteAllText(
            Path.Combine(_docsRoot, "research", "promotion-gates.json"),
            """
            {
              "schema_version": "1.0",
              "evaluator_version": "3.6.0",
              "generated_utc": "2026-04-09T12:00:00Z",
              "summary": {
                "total_records": 1,
                "promotion_state_counts": {
                  "blocked": 1
                }
              },
              "entries": [
                {
                  "candidate_id": "power.test-gate",
                  "record_id": "power.test-gate",
                  "tweak_id": "power.test-gate",
                  "tweak_origin": "research-derived",
                  "promotion_state": "blocked",
                  "promotion_blockers": ["runtime-trace"],
                  "record_promotion_allowed": false,
                  "tweak_ingest_allowed": false,
                  "apply_allowed": false,
                  "app_mapping_status": "not-mapped",
                  "next_missing_layer": "runtime-trace",
                  "debug_override_allowed": true,
                  "schema_compatibility_mode": "native",
                  "evaluator_version": "3.6.0",
                  "score_breakdown": {
                    "overall_score": 3.33
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void Catalog_LoadsBlockedEntryAndFallbacksLegacyTweaks()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);

        Assert.True(service.TryResolve("power.test-gate", out var blocked));
        Assert.Equal("blocked", blocked.PromotionState);
        Assert.Equal("research-derived", blocked.TweakOrigin);
        Assert.Contains("runtime-trace", blocked.GatingReason.ToLowerInvariant());

        var fallback = service.ResolveOrFallback("legacy.example");
        Assert.Equal("legacy-curated", fallback.TweakOrigin);
        Assert.Equal("promoted", fallback.PromotionState);
        Assert.True(fallback.TweakIngestAllowed);
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
