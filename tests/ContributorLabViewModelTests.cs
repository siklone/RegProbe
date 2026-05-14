using System;
using System.IO;
using RegProbe.Application.Services;
using RegProbe.App.ViewModels;

namespace RegProbe.Tests;

public sealed class ContributorLabViewModelTests : IDisposable
{
    private readonly string _root;

    public ContributorLabViewModelTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "RegProbe-ContributorLabTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "registry-research-framework", "audit"));
        File.WriteAllText(Path.Combine(_root, "RegProbe.sln"), string.Empty);
    }

    [Fact]
    public void Gate_RequiresAcknowledgementBeforeUnlockingTools()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot());

        Assert.True(viewModel.IsGateVisible);
        Assert.False(viewModel.IsToolsVisible);
        Assert.False(viewModel.EnableContributorToolsCommand.CanExecute(null));

        viewModel.RiskAcknowledged = true;
        Assert.True(viewModel.EnableContributorToolsCommand.CanExecute(null));
        viewModel.EnableContributorToolsCommand.Execute(null);

        Assert.False(viewModel.IsGateVisible);
        Assert.True(viewModel.IsToolsVisible);
    }

    [Fact]
    public void Allowlist_AcceptsKnownPythonScriptsAndRejectsArbitraryCommands()
    {
        Assert.True(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness --json"));
        Assert.True(ContributorLabCatalog.IsAllowlistedCommand(
            "py -3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --json"));

        Assert.False(ContributorLabCatalog.IsAllowlistedCommand("cmd.exe /c del C:\\important"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/check_single_tweak.py Foo --json; del C:\\important"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/check_single_tweak.py Foo --json\ncmd.exe /c del C:\\important"));
    }

    [Fact]
    public void BuildCommandPacks_IncludesResearchReviewAndCertifiedMutationPacks()
    {
        var packs = ContributorLabCatalog.BuildCommandPacks(_root, certifiedReady: true);

        Assert.Contains(packs, pack => pack.Title == "Representative promoted app QA batch" && pack.MutatesGuest && pack.RequiresCertifiedVm);
        Assert.Contains(packs, pack => pack.Title == "Operator96 app-surface review" && !pack.MutatesGuest && !pack.RequiresCertifiedVm);
        Assert.Contains(packs, pack => pack.Title == "Operator96 tranche rerun" && pack.MutatesGuest && pack.RequiresCertifiedVm);
        Assert.Contains(packs, pack => pack.Title == "Certified VM health"
                                      && pack.Command.Contains("--snapshot-name clean-25h2-qga", StringComparison.Ordinal));
        Assert.Contains(packs, pack => pack.Title == "Single value VM experiment"
                                      && pack.Command.Contains("--value-name MfBufferingThreshold", StringComparison.Ordinal)
                                      && !pack.Command.Contains("--value-name SystemResponsiveness", StringComparison.Ordinal));
    }

    [Fact]
    public void AppSurfacePolicySummary_BlocksCardsWhenOperator96HasNoisyOrNonOkCounts()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot() with
        {
            Operator96ReadyForAppCard = 3,
            Operator96NoisyResultCount = 1
        });

        Assert.Contains("blocked from app cards", viewModel.AppSurfacePolicySummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Operator96ResearchOnlyCount_SubtractsReadyCardsFromResearchRecords()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot() with
        {
            Operator96RecordCount = 96,
            Operator96ReadyForAppCard = 4,
            Operator96BlockedByGate = 17,
            Operator96NotAppSurfaceReady = 75
        });

        Assert.Equal(92, viewModel.Operator96ResearchOnlyCount);
        Assert.Contains("blocked_by_gate=17", viewModel.Operator96GateBreakdown, StringComparison.Ordinal);
        Assert.Contains("not_app_surface_ready=75", viewModel.Operator96GateBreakdown, StringComparison.Ordinal);
        Assert.Contains("Review only ready_for_bounded_app_card", viewModel.Operator96NextActionSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MapsOperator96ObservationsWithoutPromotingThemToAppCards()
    {
        WriteArtifact(ContributorLabCatalog.Operator96AggregatePath, """
        {
          "status": "ok",
          "summary": {
            "non_ok_count": 0,
            "noisy_result_count": 0
          }
        }
        """);
        WriteArtifact(ContributorLabCatalog.Operator96SurfaceReviewPath, """
        {
          "status": "PASS",
          "summary": {
            "record_count": 1,
            "ready_for_bounded_app_card": 0,
            "blocked_by_gate": 1,
            "not_app_surface_ready": 0,
            "blocked_by_safety": 0,
            "aggregate_surface_blocked": false,
            "needs_low_noise_rerun": 0
          },
            "records": [
            {
              "index": 1,
              "value_name": "EnableThing",
              "registry_path": "HKLM\\Software\\Example",
              "app_surface_bucket": "not_app_surface_ready",
              "normal_app_card_allowed": false,
              "surface_destination": "contributor-lab-research-only",
              "claim_boundary": "VM smoke only; no performance claim",
              "promotion_checklist": {
                "missing": ["rollback_tested", "bounded_claims"]
              },
              "reasons": ["insufficient-positive-bounded-evidence-for-app-card"],
              "proof_confidence_counts": {"low": 1},
              "proof_host_noise_counts": {"ok": 1}
            }
          ]
        }
        """);
        WriteArtifact(ContributorLabCatalog.Operator96EnrichedMatrixPath, """
        {
          "records": [
            {
              "index": 1,
              "value_name": "EnableThing",
              "registry_path": "HKLM\\Software\\Example",
              "default_status": "known-absent",
              "candidates": [
                {"value": 0, "vm_validated": true},
                {"value": 1, "vm_validated": false}
              ]
            }
          ]
        }
        """);
        WriteArtifact(ContributorLabCatalog.AppReadinessPath, """
        {
          "summary": {
            "kvm_app_smoke_status": "ok",
            "kvm_lane_health_status": "ok"
          }
        }
        """);
        WriteArtifact(ContributorLabCatalog.AppCardContractsPath, """
        {
          "status": "PASS",
          "summary": {
            "candidate_count": 258,
            "pass_count": 258,
            "fail_count": 0
          }
        }
        """);
        WriteArtifact(ContributorLabCatalog.VmHealthPath, """
        {
          "status": "ok",
          "checks": {
            "snapshot": {
              "status": "ok",
              "exists": true,
              "snapshot_name": "clean-25h2-qga"
            }
          }
        }
        """);

        var snapshot = ContributorLabCatalog.Load(_root);

        Assert.True(snapshot.RepoRootFound);
        Assert.Equal("certified", snapshot.RunTier);
        Assert.True(snapshot.ReferenceEligible);
        Assert.True(snapshot.VmSnapshotKnown);
        Assert.True(snapshot.VmSnapshotOk);
        Assert.Equal("clean-25h2-qga", snapshot.VmSnapshotName);
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "VM snapshot receipt" && item.Status == "Ready");
        Assert.Equal(0, snapshot.Operator96ReadyForAppCard);
        Assert.Equal(1, snapshot.Operator96BlockedByGate);
        Assert.Equal(0, snapshot.Operator96NotAppSurfaceReady);
        Assert.False(snapshot.Operator96AggregateSurfaceBlocked);
        var viewModel = new ContributorLabViewModel(snapshot);
        Assert.Equal(1, viewModel.Operator96ResearchOnlyCount);
        Assert.Contains("Do not create end-user cards yet", viewModel.Operator96NextActionSummary, StringComparison.Ordinal);
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "Operator96 aggregate gate" && item.Status == "Ready");
        var observation = Assert.Single(snapshot.Observations);
        Assert.Equal("not_app_surface_ready", observation.Bucket);
        Assert.Equal("0, 1", observation.CandidateValues);
        Assert.Equal("0", observation.ValidatedValues);
        Assert.Equal("contributor-lab-research-only", observation.SurfaceDestination);
        Assert.Contains("rollback_tested", observation.PromotionChecklist, StringComparison.Ordinal);
        Assert.Contains("VM smoke only", observation.ClaimBoundary, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
        }
    }

    private void WriteArtifact(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static ContributorLabSnapshot CreateSnapshot()
        => new(
            RepoRoot: "/repo",
            RepoRootFound: true,
            IsWindows: true,
            IsElevated: true,
            PythonAvailable: true,
            GitAvailable: true,
            AppReadinessOk: true,
            AppCardsOk: true,
            Operator96AggregateOk: true,
            Operator96SurfaceReviewOk: true,
            VmHealthKnown: true,
            VmHealthOk: true,
            VmSnapshotKnown: true,
            VmSnapshotOk: true,
            VmSnapshotName: "clean-25h2-qga",
            Operator96RecordCount: 96,
            Operator96ReadyForAppCard: 0,
            Operator96BlockedByGate: 17,
            Operator96NotAppSurfaceReady: 79,
            Operator96BlockedBySafety: 0,
            Operator96AggregateSurfaceBlocked: false,
            Operator96NeedsLowNoiseRerun: 0,
            Operator96NoisyResultCount: 0,
            Operator96NonOkCount: 0,
            AppCardCandidateCount: 258,
            AppCardPassCount: 258,
            RunTier: "certified",
            VerificationBadge: "Verified",
            ReadinessItems: Array.Empty<ContributorReadinessItem>(),
            CommandPacks: Array.Empty<ContributorCommandPack>(),
            Observations: Array.Empty<ContributorObservation>());
}
