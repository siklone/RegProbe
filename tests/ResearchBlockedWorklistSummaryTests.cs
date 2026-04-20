using System;
using System.IO;
using System.Linq;
using RegProbe.CLI;

namespace RegProbe.Tests;

public sealed class ResearchBlockedWorklistSummaryTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _docsRoot;

    public ResearchBlockedWorklistSummaryTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "RegProbe-BlockedSummaryTests", Guid.NewGuid().ToString("N"));
        _docsRoot = Path.Combine(_rootDirectory, "Docs");
        Directory.CreateDirectory(Path.Combine(_docsRoot, "research"));
        Directory.CreateDirectory(Path.Combine(_rootDirectory, "registry-research-framework", "audit"));

        File.WriteAllText(
            Path.Combine(_docsRoot, "research", "promotion-gates.json"),
            """
            {
              "schema_version": "1.0",
              "evaluator_version": "3.6.0",
              "generated_utc": "2026-04-09T12:00:00Z",
              "summary": {
                "total_records": 3,
                "promotion_state_counts": {
                  "blocked": 3
                }
              },
              "entries": [
                {
                  "candidate_id": "power.alpha",
                  "record_id": "power.alpha",
                  "tweak_id": "power.alpha",
                  "tweak_origin": "research-derived",
                  "promotion_state": "blocked",
                  "promotion_blockers": ["ghidra"],
                  "record_promotion_allowed": false,
                  "tweak_ingest_allowed": false,
                  "apply_allowed": false,
                  "app_mapping_status": "not-mapped",
                  "next_missing_layer": "ghidra",
                  "debug_override_allowed": true,
                  "schema_compatibility_mode": "native",
                  "evaluator_version": "3.6.0"
                },
                {
                  "candidate_id": "power.beta",
                  "record_id": "power.beta",
                  "tweak_id": "power.beta",
                  "tweak_origin": "research-derived",
                  "promotion_state": "blocked",
                  "promotion_blockers": ["intentional-hold"],
                  "record_promotion_allowed": false,
                  "tweak_ingest_allowed": false,
                  "apply_allowed": false,
                  "app_mapping_status": "not-mapped",
                  "next_missing_layer": "intentional-hold",
                  "debug_override_allowed": true,
                  "schema_compatibility_mode": "native",
                  "evaluator_version": "3.6.0"
                },
                {
                  "candidate_id": "power.gamma",
                  "record_id": "power.gamma",
                  "tweak_id": "power.gamma",
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
                  "evaluator_version": "3.6.0"
                }
              ]
            }
            """);

        File.WriteAllText(
            Path.Combine(_rootDirectory, "registry-research-framework", "audit", "blocked-worklist.json"),
            """
            {
              "generated_at": "2026-04-13T04:00:00Z",
              "blocked_count": 3,
              "actionability_counts": {
                "active": 2,
                "hold": 1
              },
              "lane_counts": {
                "ghidra": 1,
                "intentional-hold": 1,
                "runtime-trace": 1
              },
              "ordered_lanes": [
                "ghidra",
                "intentional-hold"
              ],
              "lane_focus": {},
              "top_actionable_candidates": [],
              "top_hold_candidates": [],
              "items": [
                {
                  "candidate_id": "power.alpha",
                  "feature_area": "Power",
                  "next_missing_layer": "ghidra",
                  "actionability": "active",
                  "priority_score": 100,
                  "blocker_count": 2,
                  "promotion_blockers": ["ghidra"],
                  "suggested_command": "show alpha",
                  "next_action_hint": "Continue static RE."
                },
                {
                  "candidate_id": "power.beta",
                  "feature_area": "Power",
                  "next_missing_layer": "intentional-hold",
                  "actionability": "hold",
                  "priority_score": 40,
                  "blocker_count": 1,
                  "promotion_blockers": ["intentional-hold"],
                  "suggested_command": "show beta",
                  "next_action_hint": "Wait."
                },
                {
                  "candidate_id": "power.gamma",
                  "feature_area": "Power",
                  "next_missing_layer": "runtime-trace",
                  "actionability": "active",
                  "priority_score": 70,
                  "blocker_count": 1,
                  "promotion_blockers": ["runtime-trace"],
                  "suggested_command": "show gamma",
                  "next_action_hint": "Capture runtime trace."
                }
              ]
            }
            """);
    }

    [Fact]
    public void BuildBlockedWorklistSummary_RespectsTopLimitedEntries()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);
        var entries = service.ListBlockedWorklist(top: 1).ToList();

        var summary = Program.BuildBlockedWorklistSummary(service, entries);

        Assert.Equal(1, summary.BlockedCount);
        Assert.Equal(1, summary.ActionabilityCounts["active"]);
        Assert.DoesNotContain("hold", summary.ActionabilityCounts.Keys);
        Assert.Equal(["ghidra"], summary.OrderedLanes);
        Assert.Equal("power.alpha", summary.TopActionableCandidates.Single());
        Assert.Empty(summary.TopHoldCandidates);
        Assert.Equal("power.alpha", summary.LaneFocus["ghidra"].CandidateId);
    }

    [Fact]
    public void BuildBlockedWorklistSummary_UsesOrderedLanesBeforeFallbackLanes()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);
        var entries = service.ListBlockedWorklist().ToList();

        var summary = Program.BuildBlockedWorklistSummary(service, entries);

        Assert.Equal(["ghidra", "intentional-hold", "runtime-trace"], summary.OrderedLanes);
        Assert.Equal("power.gamma", summary.LaneFocus["runtime-trace"].CandidateId);
        Assert.Equal("power.alpha", summary.TopActionableCandidates.First());
        Assert.Equal("power.beta", summary.TopHoldCandidates.Single());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(5)]
    public void ValidateBlockedWorklistTop_AllowsNullAndPositiveValues(int? top)
    {
        var error = Program.ValidateBlockedWorklistTop(top);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void ValidateBlockedWorklistTop_RejectsNonPositiveValues(int top)
    {
        var error = Program.ValidateBlockedWorklistTop(top);

        Assert.Equal("Blocked worklist --top must be a positive integer.", error);
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
