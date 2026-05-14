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
            "needs_low_noise_rerun": 0
          },
          "records": [
            {
              "index": 1,
              "value_name": "EnableThing",
              "registry_path": "HKLM\\Software\\Example",
              "app_surface_bucket": "not_app_surface_ready",
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

        var snapshot = ContributorLabCatalog.Load(_root);

        Assert.True(snapshot.RepoRootFound);
        Assert.Equal("certified", snapshot.RunTier);
        Assert.True(snapshot.ReferenceEligible);
        Assert.Equal(0, snapshot.Operator96ReadyForAppCard);
        var observation = Assert.Single(snapshot.Observations);
        Assert.Equal("not_app_surface_ready", observation.Bucket);
        Assert.Equal("0, 1", observation.CandidateValues);
        Assert.Equal("0", observation.ValidatedValues);
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
            Operator96RecordCount: 96,
            Operator96ReadyForAppCard: 0,
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
