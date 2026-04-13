using System;
using System.IO;
using System.Linq;
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
                  "blocked": 1,
                  "promoted": 2
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
                },
                {
                  "candidate_id": "power.promoted-rollback",
                  "record_id": "power.promoted-rollback",
                  "tweak_id": "power.promoted-rollback",
                  "tweak_origin": "research-derived",
                  "promotion_state": "promoted",
                  "promotion_blockers": [],
                  "record_promotion_allowed": true,
                  "tweak_ingest_allowed": true,
                  "apply_allowed": true,
                  "app_mapping_status": "matches-research",
                  "next_missing_layer": "none",
                  "debug_override_allowed": false,
                  "schema_compatibility_mode": "native",
                  "evaluator_version": "3.6.0",
                  "rollback_status": {
                    "rollback_declared": true,
                    "rollback_executed": false,
                    "rollback_verified": false,
                    "rollback_verification_method": "state_diff",
                    "rollback_failure_reason": "rollback-state-mismatch"
                  }
                },
                {
                  "candidate_id": "power.promoted-record-only",
                  "record_id": "power.promoted-record-only",
                  "tweak_id": "power.promoted-record-only",
                  "tweak_origin": "research-derived",
                  "promotion_state": "promoted",
                  "promotion_blockers": [],
                  "record_promotion_allowed": true,
                  "tweak_ingest_allowed": false,
                  "apply_allowed": false,
                  "app_mapping_status": "not-mapped",
                  "next_missing_layer": "none",
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
              "blocked_count": 2,
              "lane_counts": {
                "ghidra": 1,
                "intentional-hold": 1
              },
              "items": [
                {
                  "candidate_id": "power.test-gate",
                  "feature_area": "Power",
                  "next_missing_layer": "ghidra",
                  "actionability": "active",
                  "priority_score": 33,
                  "blocker_count": 2,
                  "promotion_blockers": [
                    "power-test-runtime-read-unresolved",
                    "power-test-specific-caller-unresolved"
                  ],
                  "key_path": "HKLM\\\\Software\\\\Example",
                  "value_name": "Enabled",
                  "recent_audit_artifacts": [
                    "registry-research-framework/audit/power-test-runtime-audit-20260413.json"
                  ],
                  "next_action_hint": "Continue static RE."
                },
                {
                  "candidate_id": "power.intentional-hold",
                  "feature_area": "Power",
                  "next_missing_layer": "intentional-hold",
                  "actionability": "hold",
                  "priority_score": 10,
                  "blocker_count": 1,
                  "promotion_blockers": [
                    "power-hold-trigger-not-available"
                  ],
                  "key_path": "HKLM\\\\Software\\\\Hold",
                  "value_name": "Enabled",
                  "recent_audit_artifacts": [],
                  "next_action_hint": "Wait for a safer lane."
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

    [Fact]
    public void ApplyRequest_RejectsBlockedCandidateWithoutOverride_And_AllowsContributorOverride()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);

        var denied = service.EvaluateApplyRequest("power.test-gate");
        Assert.False(denied.Allowed);

        var allowed = service.EvaluateApplyRequest("power.test-gate", overrideRequested: true, overrideReason: "debug", contributorMode: true);
        Assert.True(allowed.Allowed);
        Assert.True(allowed.OverrideUsed);
    }

    [Fact]
    public void ApplyRequest_RejectsPromotedResearchRecordWhenIngestIsDisabled()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);

        var decision = service.EvaluateApplyRequest("power.promoted-record-only");

        Assert.False(decision.Allowed);
        Assert.Equal("promotion-state:promoted", decision.Message);
    }

    [Fact]
    public void RollbackRequest_WarnsWhenRollbackIsUnverified()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);

        var decision = service.EvaluateRollbackRequest("power.promoted-rollback");

        Assert.True(decision.Allowed);
        Assert.Contains("rollback-declared-but-not-executed", decision.Warnings);
        Assert.Contains("rollback-unverified", decision.Warnings);
    }

    [Fact]
    public void Apply_Uses_GenericConsumerShape()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);
        var consumer = new FakePromotionGateConsumer { Id = "power.test-gate" };

        service.Apply([consumer]);

        Assert.NotNull(consumer.LastAppliedGate);
        Assert.Equal("blocked", consumer.LastAppliedGate!.PromotionState);
    }

    [Fact]
    public void BlockedWorklist_LoadsAndFiltersActionableEntries()
    {
        var service = new TweakPromotionGateCatalogService(_docsRoot);

        var actionable = service.ListBlockedWorklist(actionableOnly: true, top: 1).ToList();

        Assert.Single(actionable);
        Assert.Equal("power.test-gate", actionable[0].CandidateId);
        Assert.Equal("active", actionable[0].Actionability);
        Assert.Single(actionable[0].RecentAuditArtifacts);
        Assert.True(service.TryResolveBlockedWorklist("power.test-gate", out var entry));
        Assert.Equal(33, entry.PriorityScore);
        Assert.Single(entry.RecentAuditArtifacts);
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

    private sealed class FakePromotionGateConsumer
    {
        public string Id { get; init; } = string.Empty;

        public TweakPromotionGateEntry? LastAppliedGate { get; private set; }

        public void ApplyResearchPromotionGate(TweakPromotionGateEntry gate)
        {
            LastAppliedGate = gate;
        }
    }
}
