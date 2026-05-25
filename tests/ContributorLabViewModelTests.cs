using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public void GateCopy_ExplainsContributorRiskAndRunTierBoundary()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot());

        Assert.Contains("user-supplied registry keys", viewModel.RiskGateSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("QGA", viewModel.RiskGateSummary, StringComparison.Ordinal);
        Assert.Contains("noisy benchmark", viewModel.RiskGateSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Normal optimization users", viewModel.RiskGateBoundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("certified, community, and noisy/debug", viewModel.RiskGateBoundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BIOS/UEFI virtualization", viewModel.RiskAcknowledgementText, StringComparison.Ordinal);
        Assert.Contains("healthy QGA", viewModel.RiskAcknowledgementText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Allowlist_AcceptsKnownPythonScriptsAndRejectsArbitraryCommands()
    {
        Assert.True(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness --json"));
        Assert.True(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/check_single_tweak.py REPLACE_VALUE_NAME --expected-value 0 --expected-value 1 --json"));
        Assert.True(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/generate_custom_value_app_surface_review.py --json"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/generate_operator96_app_surface_review.py --json"));
        Assert.True(ContributorLabCatalog.IsAllowlistedCommand(
            "py -3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --json"));
        Assert.True(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --snapshot-name clean-25h2-qga --check-guest-dotnet --json"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 scripts/vm-kvm/run-guest-dotnet-toolchain-bootstrap.py --domain regprobe-win11-25h2-session"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --registry-path \"HKLM\\REPLACE\\KEY\\PATH\" --value-name REPLACE_VALUE_NAME --value-data REPLACE_DWORD_VALUE"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --run --limit-experiments 10"));

        Assert.False(ContributorLabCatalog.IsAllowlistedCommand("cmd.exe /c del C:\\important"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/check_single_tweak.py Foo --json; del C:\\important"));
        Assert.False(ContributorLabCatalog.IsAllowlistedCommand(
            "python3 registry-research-framework/scripts/check_single_tweak.py Foo --json\ncmd.exe /c del C:\\important"));
    }

    [Fact]
    public void CommandRunner_SplitsQuotedRegistryPathsWithoutDroppingBackslashes()
    {
        var tokens = ContributorLabCommandRunner.SplitCommandLine(
            "python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --registry-path \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\" --value-name \"TimerCheckFlags\"");

        Assert.Contains("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power", tokens);
        Assert.Contains("TimerCheckFlags", tokens);
    }

    [Fact]
    public void BuildCommandPacks_IncludesResearchReviewAndCertifiedMutationPacks()
    {
        var packs = ContributorLabCatalog.BuildCommandPacks(_root, certifiedReady: true);

        Assert.Contains(packs, pack => pack.Title == "Representative promoted app QA batch" && pack.MutatesGuest && pack.RequiresCertifiedVm);
        Assert.Contains(packs, pack => pack.Title == "Custom key/value lookup template" && !pack.MutatesGuest && !pack.RequiresCertifiedVm);
        Assert.Contains(packs, pack => pack.Title == "Custom value app-surface review"
                                      && !pack.MutatesGuest
                                      && !pack.RequiresCertifiedVm
                                      && pack.Command.Contains("generate_custom_value_app_surface_review.py", StringComparison.Ordinal)
                                      && !pack.Command.Contains("generate_operator96_app_surface_review.py", StringComparison.Ordinal));
        Assert.Contains(packs, pack => pack.Title == "Custom value tranche rerun" && pack.MutatesGuest && pack.RequiresCertifiedVm);
        Assert.Contains(packs, pack => pack.Title == "Custom value tranche rerun"
                                      && pack.Command.Contains("--snapshot-name clean-25h2-qga", StringComparison.Ordinal)
                                      && pack.Command.Contains("--abort-on-noisy-host", StringComparison.Ordinal)
                                      && pack.Command.Contains("--stop-on-failure", StringComparison.Ordinal));
        Assert.Contains(packs, pack => pack.Title == "Custom key/value VM experiment template"
                                      && pack.MutatesGuest
                                      && pack.RequiresCertifiedVm
                                      && pack.Command.Contains("REPLACE_VALUE_NAME", StringComparison.Ordinal)
                                      && pack.Command.Contains("--abort-on-noisy-host", StringComparison.Ordinal));
        Assert.Contains(packs, pack => pack.Title == "Certified VM health"
                                      && pack.Command.Contains("--snapshot-name clean-25h2-qga", StringComparison.Ordinal));
        Assert.Contains(packs, pack => pack.Title == "VM .NET test toolchain"
                                      && !pack.MutatesGuest
                                      && pack.RequiresCertifiedVm
                                      && pack.Command.Contains("--check-guest-dotnet", StringComparison.Ordinal)
                                      && pack.Command.Contains("vm-health-check.py", StringComparison.Ordinal));
        Assert.Contains(packs, pack => pack.Title == "VM .NET toolchain bootstrap"
                                      && pack.MutatesGuest
                                      && pack.RequiresCertifiedVm
                                      && pack.Command.Contains("run-guest-dotnet-toolchain-bootstrap.py", StringComparison.Ordinal)
                                      && pack.Command.Contains("--desktop-runtime-channel 8.0", StringComparison.Ordinal));
        Assert.Contains(packs, pack => pack.Title == "Single value VM experiment"
                                      && pack.Command.Contains("--value-name MfBufferingThreshold", StringComparison.Ordinal)
                                      && !pack.Command.Contains("--value-name SystemResponsiveness", StringComparison.Ordinal));
    }

    [Fact]
    public void AppSurfacePolicySummary_BlocksCardsWhenCustomValuesHaveNoisyOrNonOkCounts()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot() with
        {
            CustomValueReadyForAppCard = 3,
            CustomValueNoisyResultCount = 1
        });

        Assert.Contains("blocked from app cards", viewModel.AppSurfacePolicySummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContributorReadinessDecisionSummary_GivesOneNextSafeAction()
    {
        var certified = new ContributorLabViewModel(CreateSnapshot());
        Assert.Equal("Certified reference lane", certified.ContributorLabOperatingMode);
        Assert.Contains("reference-eligible", certified.ContributorLabOperatingModeDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shipped cards", certified.EndUserSurfaceSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user-supplied custom value observations", certified.ContributorResearchSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("copy-only in v1", certified.ContributorActionBoundarySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("certified reference lane is ready", certified.ContributorReadinessDecisionSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("one value at a time", certified.ContributorReadinessDecisionSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ready_for_bounded_app_card", certified.ContributorReadinessDecisionSummary, StringComparison.Ordinal);

        var noisy = new ContributorLabViewModel(CreateSnapshot() with
        {
            CustomValueNoisyResultCount = 1,
            CustomValueNeedsLowNoiseRerun = 1
        });
        Assert.Equal("Noisy/debug lane", noisy.ContributorLabOperatingMode);
        Assert.Contains("debug-only", noisy.ContributorLabOperatingModeDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rerun noisy", noisy.ContributorReadinessDecisionSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("debug-only", noisy.ContributorReadinessDecisionSummary, StringComparison.OrdinalIgnoreCase);

        var community = new ContributorLabViewModel(CreateSnapshot() with
        {
            RunTier = "community",
            VmHealthOk = false,
            VmSnapshotOk = false
        });
        Assert.Equal("Community observation lane", community.ContributorLabOperatingMode);
        Assert.Contains("community/debug", community.ContributorLabOperatingModeDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("copy-only/community-debug", community.ContributorReadinessDecisionSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("QGA", community.ContributorReadinessDecisionSummary, StringComparison.Ordinal);

        var missingScripts = new ContributorLabViewModel(CreateSnapshot() with
        {
            RequiredScriptsOk = false
        });
        Assert.Contains("repair the contributor script checkout", missingScripts.ContributorReadinessDecisionSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CustomValueResearchOnlyCount_SubtractsReadyCardsFromResearchRecords()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot() with
        {
            CustomValueRecordCount = 96,
            CustomValueReadyForAppCard = 4,
            CustomValueBlockedByGate = 17,
            CustomValueNotAppSurfaceReady = 75
        });

        Assert.Equal(92, viewModel.CustomValueResearchOnlyCount);
        Assert.Contains("blocked_by_gate=17", viewModel.CustomValueGateBreakdown, StringComparison.Ordinal);
        Assert.Contains("not_app_surface_ready=75", viewModel.CustomValueGateBreakdown, StringComparison.Ordinal);
        Assert.Contains("surface=contributor-lab-only", viewModel.CustomValueGateBreakdown, StringComparison.Ordinal);
        Assert.Contains("app-card review-ready, not shipped cards", viewModel.CustomValueSurfaceBoundarySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("App-card review-ready (not shipped): 4", viewModel.CustomValueReviewReadySummary, StringComparison.Ordinal);
        Assert.Contains("contributor-only observations: 92", viewModel.CustomValueReviewReadySummary, StringComparison.Ordinal);
        Assert.Contains("Review only ready_for_bounded_app_card", viewModel.CustomValueNextActionSummary, StringComparison.Ordinal);
        Assert.Contains("user-supplied key/value", viewModel.CustomValueWorkflowSummary, StringComparison.Ordinal);
        Assert.Contains("per-run confirmation", viewModel.CertifiedMutationGuardSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void CustomValueInput_BuildsLookupAndCertifiedVmCommands()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot())
        {
            CustomRegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel",
            CustomValueName = "TimerCheckFlags",
            CustomExpectedValues = "0, 1"
        };

        Assert.True(viewModel.HasCustomValueInput);
        Assert.Contains("check_single_tweak.py \"TimerCheckFlags\"", viewModel.CustomEvidenceLookupCommand, StringComparison.Ordinal);
        Assert.Contains("--registry-path \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel\"", viewModel.CustomEvidenceLookupCommand, StringComparison.Ordinal);
        Assert.Contains("--expected-value \"0\"", viewModel.CustomEvidenceLookupCommand, StringComparison.Ordinal);
        Assert.Contains("--expected-value \"1\"", viewModel.CustomEvidenceLookupCommand, StringComparison.Ordinal);
        Assert.Contains("--registry-path \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel\"", viewModel.CustomVmExperimentCommand, StringComparison.Ordinal);
        Assert.Contains("--value-name \"TimerCheckFlags\"", viewModel.CustomVmExperimentCommand, StringComparison.Ordinal);
        Assert.Contains("--value-data \"0\"", viewModel.CustomVmExperimentCommand, StringComparison.Ordinal);
        Assert.Equal("custom-value-timercheckflags-0", viewModel.CustomVmExperimentOutputName);
        Assert.Contains("--output-name \"custom-value-timercheckflags-0\"", viewModel.CustomVmExperimentCommand, StringComparison.Ordinal);
        Assert.Contains("--abort-on-noisy-host", viewModel.CustomVmExperimentCommand, StringComparison.Ordinal);
        Assert.DoesNotContain("operator96", viewModel.CustomVmExperimentCommand, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("non-mutating lookup/readiness/VM-health", viewModel.ContributorExecutionPolicySummary, StringComparison.Ordinal);
        Assert.Contains("checks both the value name and path", viewModel.CustomValueInputHelp, StringComparison.Ordinal);
        Assert.Contains("default/current/target", viewModel.CustomValueAppCardEntryCriteria, StringComparison.Ordinal);
        Assert.Contains("one-off seed batch", viewModel.CustomValueObservationBoundarySummary, StringComparison.Ordinal);
        Assert.Contains("TimerCheckFlags", viewModel.CustomValueInvestigationContract, StringComparison.Ordinal);
        Assert.Contains("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Kernel", viewModel.CustomValueInvestigationContract, StringComparison.Ordinal);
        Assert.Contains("target value(s) 0, 1", viewModel.CustomValueStorySummary, StringComparison.Ordinal);
        Assert.Contains("First VM target: 0", viewModel.CustomValueStorySummary, StringComparison.Ordinal);
        Assert.Contains("copy-only", viewModel.CustomValueMutationBoundarySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(viewModel.CustomValueEvidenceChecklist, item => item.Contains("Current value", StringComparison.Ordinal)
                                                                       && item.Contains("absent", StringComparison.Ordinal));
        Assert.Contains(viewModel.CustomValueEvidenceChecklist, item => item.Contains("Rollback story", StringComparison.Ordinal));

        Assert.Contains("check_single_tweak_app_qa.py \"TimerCheckFlags\"", viewModel.CustomAppQaCommand, StringComparison.Ordinal);
        Assert.Contains("vm-health-check.py", viewModel.CustomVmHealthCommand, StringComparison.Ordinal);
        Assert.Contains("--check-guest-dotnet", viewModel.CustomVmHealthCommand, StringComparison.Ordinal);
        Assert.Contains(viewModel.CustomValueDiscoverySteps, step => step.Title == "1. Repo/evidence lookup"
                                                                  && !step.MutatesGuest
                                                                  && step.Command.Contains("check_single_tweak.py", StringComparison.Ordinal));
        Assert.Contains(viewModel.CustomValueDiscoverySteps, step => step.Title == "4. Certified VM health"
                                                                  && step.Command.Contains("--check-guest-dotnet", StringComparison.Ordinal));
        Assert.Contains(viewModel.CustomValueDiscoverySteps, step => step.Title == "5. One-value VM experiment"
                                                                  && step.MutatesGuest
                                                                  && step.RequiresCertifiedVm
                                                                  && step.Command.Contains("--output-name \"custom-value-timercheckflags-0\"", StringComparison.Ordinal)
                                                                  && step.Command.Contains("--value-name \"TimerCheckFlags\"", StringComparison.Ordinal));
    }

    [Fact]
    public void CustomValueInput_SanitizesVmExperimentOutputName()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot())
        {
            CustomValueName = "Disable CFG Export Suppression",
            CustomExpectedValues = "0x00000001"
        };

        Assert.Equal("custom-value-disable-cfg-export-suppression-0x00000001", viewModel.CustomVmExperimentOutputName);
        Assert.Contains("--output-name \"custom-value-disable-cfg-export-suppression-0x00000001\"", viewModel.CustomVmExperimentCommand, StringComparison.Ordinal);
        Assert.DoesNotContain(" ", viewModel.CustomVmExperimentOutputName, StringComparison.Ordinal);
        Assert.DoesNotContain("operator96", viewModel.CustomVmExperimentOutputName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CustomValueInvestigationCopy_MarksNonCertifiedMutationAsCommunityDebug()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot() with
        {
            RunTier = "community",
            VmHealthOk = false,
            VmSnapshotOk = false,
            CustomValueNoisyResultCount = 0,
            CustomValueNonOkCount = 0
        })
        {
            CustomValueName = "EnableThing",
            CustomExpectedValues = string.Empty
        };

        Assert.Contains("EnableThing", viewModel.CustomValueInvestigationContract, StringComparison.Ordinal);
        Assert.Contains("target value(s) not listed yet", viewModel.CustomValueStorySummary, StringComparison.Ordinal);
        Assert.Contains("First VM target: 0", viewModel.CustomValueStorySummary, StringComparison.Ordinal);
        Assert.Contains("community/debug", viewModel.CustomValueMutationBoundarySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(viewModel.CustomValueEvidenceChecklist, item => item.Contains("Default story", StringComparison.Ordinal));
        Assert.Contains(viewModel.CustomValueEvidenceChecklist, item => item.Contains("Evidence boundary", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadinessAndVmHealthCommands_DoNotRequireCustomValueInput()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot())
        {
            CustomValueName = string.Empty
        };

        Assert.False(viewModel.HasCustomValueInput);
        viewModel.RiskAcknowledged = true;
        viewModel.EnableContributorToolsCommand.Execute(null);

        Assert.False(viewModel.RunCustomLookupCommand.CanExecute(null));
        Assert.False(viewModel.RunCustomAppQaCommand.CanExecute(null));
        Assert.True(viewModel.RunAppReadinessCommand.CanExecute(null));
        Assert.True(viewModel.RunVmHealthCommand.CanExecute(null));
    }

    [Fact]
    public void CommandPackExecutionPolicy_MarksMutatingPacksAsCopyOnly()
    {
        var mutatingPack = new ContributorCommandPackViewModel(new ContributorCommandPack(
            "Single value VM experiment",
            "Mutates the guest.",
            "python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --value-name Example",
            "certified-ready",
            RequiresCertifiedVm: true,
            MutatesGuest: true));
        var readOnlyPack = new ContributorCommandPackViewModel(new ContributorCommandPack(
            "Single tweak lookup",
            "Reads repo evidence.",
            "python3 registry-research-framework/scripts/check_single_tweak.py Example --json",
            "community-safe",
            RequiresCertifiedVm: false,
            MutatesGuest: false));

        Assert.Contains("Copy-only", mutatingPack.ExecutionPolicyLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("per-run confirmation", mutatingPack.ExecutionPolicyLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Read-only", readOnlyPack.ExecutionPolicyLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadOnlyRunner_RunsAllowlistedCustomLookupAfterGateUnlock()
    {
        var runner = new FakeContributorCommandRunner(new ContributorCommandRunResult(
            ExitCode: 0,
            StandardOutput: "{\"status\":\"ok\",\"matched\":true}",
            StandardError: string.Empty,
            TimedOut: false));
        var viewModel = new ContributorLabViewModel(CreateSnapshot(), runner)
        {
            CustomValueName = "TimerCheckFlags",
            CustomExpectedValues = "0, 1"
        };

        Assert.False(viewModel.RunCustomLookupCommand.CanExecute(null));
        viewModel.RiskAcknowledged = true;
        viewModel.EnableContributorToolsCommand.Execute(null);
        Assert.True(viewModel.RunCustomLookupCommand.CanExecute(null));

        await ((IAsyncCommand)viewModel.RunCustomLookupCommand).ExecuteAsync();

        Assert.Equal("Repo/evidence lookup", viewModel.CommandRunTitle);
        Assert.Equal("Success", viewModel.CommandRunStatus);
        Assert.Contains("\"matched\":true", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("check_single_tweak.py \"TimerCheckFlags\"", runner.LastCommand, StringComparison.Ordinal);
        Assert.Contains("--registry-path \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\"", runner.LastCommand, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomLookupResult_PrependsContributorDecisionSummaryBeforeRawJson()
    {
        var runner = new FakeContributorCommandRunner(new ContributorCommandRunResult(
            ExitCode: 0,
            StandardOutput: """
            {
              "status": "ok",
              "match_count": 1,
              "matches": [
                {
                  "candidate_id": "power.example",
                  "promotion_state": "promoted",
                  "apply_allowed": true,
                  "restore_previous_supported": true,
                  "restore_default_supported": true,
                  "app_mapping_status": "matches-research",
                  "catalog_entry": {"name": "Example Power Card"},
                  "app_write_targets": [
                    {"value_name": "SystemResponsiveness", "value": 10}
                  ],
                  "registry_path_query_check": {
                    "query": "HKLM\\\\SOFTWARE\\\\Microsoft\\\\Windows NT\\\\CurrentVersion\\\\Multimedia\\\\SystemProfile",
                    "matched": true,
                    "hits": [
                      {
                        "path": "HKLM\\\\SOFTWARE\\\\Microsoft\\\\Windows NT\\\\CurrentVersion\\\\Multimedia\\\\SystemProfile",
                        "value_name": "SystemResponsiveness",
                        "source": "app_current_implementation.writes"
                      }
                    ]
                  },
                  "windows_and_recommended_profiles": [
                    {
                      "label": "Windows default",
                      "states": [
                        {"target_id": "system-responsiveness", "state_kind": "value", "value": 10}
                      ]
                    }
                  ],
                  "evidence": [
                    {"kind": "official-doc"},
                    {"kind": "etw-trace"}
                  ],
                  "expected_value_checks": [
                    {"expected_value": "10", "found_any": true},
                    {"expected_value": "30000", "found_any": false}
                  ]
                }
              ]
            }
            """,
            StandardError: string.Empty,
            TimedOut: false));
        var viewModel = new ContributorLabViewModel(CreateSnapshot(), runner)
        {
            CustomValueName = "SystemResponsiveness",
            CustomExpectedValues = "10, 30000"
        };
        viewModel.RiskAcknowledged = true;
        viewModel.EnableContributorToolsCommand.Execute(null);

        await ((IAsyncCommand)viewModel.RunCustomLookupCommand).ExecuteAsync();

        Assert.StartsWith("Contributor summary: ok", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Matched records: 1", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Best match: Example Power Card (promoted, apply_allowed=true)", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Value story inputs:", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Registry path check: matched (1 hit(s))", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("App writes: SystemResponsiveness=10", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Default/profile story: Windows default", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Evidence lanes: etw-trace=1, official-doc=1", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Expected values: 10: found, 30000: missing", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("App-card gate: missing expected value(s) 30000", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Next action: investigate expected value(s) 30000", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("--- Raw JSON ---", viewModel.CommandRunOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomLookupResult_ExplainsNoMatchBeforeAnyVmMutation()
    {
        var runner = new FakeContributorCommandRunner(new ContributorCommandRunResult(
            ExitCode: 0,
            StandardOutput: """
            {
              "status": "no-match",
              "query": "MysteryValue",
              "match_count": 0,
              "matches": []
            }
            """,
            StandardError: string.Empty,
            TimedOut: false));
        var viewModel = new ContributorLabViewModel(CreateSnapshot(), runner)
        {
            CustomValueName = "MysteryValue",
            CustomExpectedValues = "0, 1"
        };
        viewModel.RiskAcknowledged = true;
        viewModel.EnableContributorToolsCommand.Execute(null);

        await ((IAsyncCommand)viewModel.RunCustomLookupCommand).ExecuteAsync();

        Assert.Contains("Matched records: 0", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("App-card gate: no matching research/app record", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("do not mutate a VM", viewModel.CommandRunOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CustomAppQaResult_PrependsCardRollbackAndEvidenceSummary()
    {
        var runner = new FakeContributorCommandRunner(new ContributorCommandRunResult(
            ExitCode: 0,
            StandardOutput: """
            {
              "status": "ok",
              "inspect_match_count": 2,
              "qa_candidate_count": 1,
              "candidates": [
                {
                  "candidate_id": "power.example",
                  "apply_allowed": true,
                  "restore_previous_supported": true,
                  "restore_default_supported": true,
                  "card_expectations": {
                    "name": "Example Power Card",
                    "category": "Power"
                  },
                  "value_expectations": ["10 -> matched"],
                  "evidence_expectations": {
                    "linked_evidence_count": 4,
                    "runtime_read_signal_count": 1
                  },
                  "qa_report_path": "C:\\\\Tools\\\\ValidationController\\\\smoke\\\\power.example.qa.json"
                }
              ]
            }
            """,
            StandardError: string.Empty,
            TimedOut: false));
        var viewModel = new ContributorLabViewModel(CreateSnapshot(), runner)
        {
            CustomValueName = "SystemResponsiveness",
            CustomExpectedValues = "10"
        };
        viewModel.RiskAcknowledged = true;
        viewModel.EnableContributorToolsCommand.Execute(null);

        await ((IAsyncCommand)viewModel.RunCustomAppQaCommand).ExecuteAsync();

        Assert.Contains("QA candidates: 1; inspect matches=2", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Card: Example Power Card (Power)", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Apply allowed: true; rollback previous=true; rollback default=true", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Value checks: 10 -> matched", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("Evidence counts: linked=4, runtime=1", viewModel.CommandRunOutput, StringComparison.Ordinal);
        Assert.Contains("power.example.qa.json", viewModel.CommandRunOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadOnlyRunner_BlocksWhenGeneratedCommandIsNotAllowlisted()
    {
        var viewModel = new ContributorLabViewModel(CreateSnapshot(), new FakeContributorCommandRunner(
            new ContributorCommandRunResult(0, "should-not-run", string.Empty, TimedOut: false)))
        {
            CustomValueName = "Bad;Command"
        };
        viewModel.RiskAcknowledged = true;
        viewModel.EnableContributorToolsCommand.Execute(null);

        await ((IAsyncCommand)viewModel.RunCustomLookupCommand).ExecuteAsync();

        Assert.Equal("Blocked", viewModel.CommandRunStatus);
        Assert.Contains("not allowlisted", viewModel.CommandRunOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReferenceEligible_RequiresVmHealthAndSnapshotReceipt()
    {
        var snapshot = CreateSnapshot() with
        {
            RunTier = "certified",
            VmHealthKnown = false,
            VmHealthOk = false,
            VmSnapshotKnown = false,
            VmSnapshotOk = false
        };
        var viewModel = new ContributorLabViewModel(snapshot);

        Assert.False(snapshot.ReferenceEligible);
        Assert.Contains("copy-only", viewModel.CertifiedMutationGuardSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(viewModel.CustomValueDiscoverySteps, step => step.Title == "4. Certified VM health"
                                                                  && step.Tier == "certified-required");
    }

    [Fact]
    public void ReadinessItems_SurfaceScriptsVirtualizationAndVmSetup()
    {
        var snapshot = CreateSnapshot() with
        {
            ReadinessItems = ContributorLabCatalog.BuildReadinessItems(CreateSnapshot())
        };

        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "Required scripts" && item.Status == "Ready");
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "BIOS virtualization" && item.Status == "Ready");
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "VM configured" && item.Status == "Ready");
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "VM .NET test toolchain" && item.Status == "Ready");
    }

    [Fact]
    public void Load_MapsCustomValueObservationsWithoutPromotingThemToAppCards()
    {
        WriteArtifact(ContributorLabCatalog.CustomValueAggregatePath, """
        {
          "status": "ok",
          "summary": {
            "non_ok_count": 0,
            "noisy_result_count": 0
          },
          "results": [
            {
              "index": 1,
              "status": "ok",
              "value_data": 0,
              "artifact_json": "registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablething-0.json",
              "observations": {
                "verdict": "low_confidence",
                "confidence": "low",
                "host_noise": "ok",
                "primary_delta_pct": 0.5,
                "smoke_hard_success": {
                  "apply_smoke_hard_success": true,
                  "post_reboot_smoke_hard_success": true,
                  "post_rollback_smoke_hard_success": true
                }
              }
            }
          ]
        }
        """);
        WriteArtifact(ContributorLabCatalog.CustomValueSurfaceReviewPath, """
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
        WriteArtifact(ContributorLabCatalog.CustomValueEnrichedMatrixPath, """
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
              },
              "guest_dotnet_toolchain": {
                "status": "ok",
                "configured_dotnet_path": "C:\\Tools\\DotNetSDK\\8.0.416\\dotnet.exe",
                "configured_dotnet_path_exists": true,
                "dotnet_on_path": false,
                "dotnet_path": "C:\\Tools\\DotNetSDK\\8.0.416\\dotnet.exe",
                "core_runtime_present": true,
                "core_runtime_versions": ["8.0.27"],
                "desktop_runtime_present": true,
                "desktop_runtime_versions": ["8.0.27"]
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
        Assert.True(snapshot.VmDotNetKnown);
        Assert.True(snapshot.VmDotNetOk);
        Assert.Contains("Microsoft.NETCore.App 8.0.27", snapshot.VmDotNetDetail, StringComparison.Ordinal);
        Assert.Contains("Microsoft.WindowsDesktop.App 8.0.27", snapshot.VmDotNetDetail, StringComparison.Ordinal);
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "VM snapshot receipt" && item.Status == "Ready");
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "VM .NET test toolchain" && item.Status == "Ready");
        Assert.Equal(0, snapshot.CustomValueReadyForAppCard);
        Assert.Equal(1, snapshot.CustomValueBlockedByGate);
        Assert.Equal(0, snapshot.CustomValueNotAppSurfaceReady);
        Assert.False(snapshot.CustomValueAggregateSurfaceBlocked);
        var viewModel = new ContributorLabViewModel(snapshot);
        Assert.Equal(1, viewModel.CustomValueResearchOnlyCount);
        Assert.Contains("Do not create end-user cards yet", viewModel.CustomValueNextActionSummary, StringComparison.Ordinal);
        Assert.Contains("All 1 custom value records stay in Contributor Lab", viewModel.CustomValueSurfaceBoundarySummary, StringComparison.Ordinal);
        Assert.Contains("not optimization recommendations or shipped cards", viewModel.ObservationBrowserSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(snapshot.ReadinessItems, item => item.Label == "Custom value aggregate gate" && item.Status == "Ready");
        var observation = Assert.Single(snapshot.Observations);
        Assert.Equal("not_app_surface_ready", observation.Bucket);
        Assert.Equal("0, 1", observation.CandidateValues);
        Assert.Equal("0", observation.ValidatedValues);
        Assert.Equal("contributor-lab-research-only", observation.SurfaceDestination);
        Assert.Contains("one-value snapshot-safe VM experiment", observation.NextAction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Research-only", observation.AppCardReadinessSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rollback_tested", observation.MissingProofSummary, StringComparison.Ordinal);
        Assert.Contains("Certified-low-noise receipt", observation.RunTierAction, StringComparison.Ordinal);
        Assert.Contains("rollback_tested", observation.PromotionChecklist, StringComparison.Ordinal);
        Assert.Contains("VM smoke only", observation.ClaimBoundary, StringComparison.Ordinal);
        Assert.Contains("0: low_confidence", observation.TestedValueSummary, StringComparison.Ordinal);
        Assert.Equal("low_confidence=1", observation.VerdictSummary);
        Assert.Equal("1/1 apply/reboot/rollback hard-smoke receipts passed", observation.SmokeSummary);
        Assert.Equal("Low-noise VM receipt", observation.NoiseBadge);
        Assert.Contains("rollback_tested", observation.AppCardBlockerSummary, StringComparison.Ordinal);
        Assert.Contains("registry-value-experiments-low-noise", observation.ArtifactSummary, StringComparison.Ordinal);
        Assert.Contains("custom-value-seed-001-enablething-0.json", observation.ArtifactSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("operator96", observation.ArtifactSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DerivesActionGuidanceForNoisyAndReadyCustomValueRecords()
    {
        WriteArtifact(ContributorLabCatalog.CustomValueAggregatePath, """
        {
          "status": "ok",
          "summary": {
            "non_ok_count": 0,
            "noisy_result_count": 1
          },
          "results": [
            {
              "index": 1,
              "status": "ok",
              "value_data": 1,
              "observations": {
                "verdict": "low_confidence",
                "confidence": "low",
                "host_noise": "noisy",
                "primary_delta_pct": 1.2,
                "smoke_hard_success": {
                  "apply_smoke_hard_success": true,
                  "post_reboot_smoke_hard_success": true,
                  "post_rollback_smoke_hard_success": true
                }
              }
            },
            {
              "index": 2,
              "status": "ok",
              "value_data": 0,
              "observations": {
                "verdict": "neutral",
                "confidence": "medium",
                "host_noise": "ok",
                "primary_delta_pct": 0,
                "smoke_hard_success": {
                  "apply_smoke_hard_success": true,
                  "post_reboot_smoke_hard_success": true,
                  "post_rollback_smoke_hard_success": true
                }
              }
            }
          ]
        }
        """);
        WriteArtifact(ContributorLabCatalog.CustomValueSurfaceReviewPath, """
        {
          "status": "PASS",
          "summary": {
            "record_count": 2,
            "ready_for_bounded_app_card": 1,
            "blocked_by_gate": 0,
            "not_app_surface_ready": 0,
            "blocked_by_safety": 0,
            "aggregate_surface_blocked": true,
            "needs_low_noise_rerun": 1
          },
          "records": [
            {
              "index": 1,
              "value_name": "NoisyThing",
              "registry_path": "HKLM\\Software\\Example",
              "app_surface_bucket": "needs_low_noise_rerun",
              "normal_app_card_allowed": false,
              "surface_destination": "contributor-lab-research-only",
              "promotion_checklist": {
                "missing": ["clean_low_noise_vm_proofs"]
              },
              "reasons": ["needs-low-noise-rerun"],
              "proof_confidence_counts": {"low": 1},
              "proof_host_noise_counts": {"noisy": 1}
            },
            {
              "index": 2,
              "value_name": "ReadyThing",
              "registry_path": "HKLM\\Software\\Example",
              "app_surface_bucket": "ready_for_bounded_app_card",
              "normal_app_card_allowed": true,
              "app_surface_ready": true,
              "surface_destination": "normal-app-card-review",
              "claim_boundary": "Bounded smoke-only claim",
              "promotion_checklist": {
                "missing": []
              },
              "reasons": [],
              "proof_confidence_counts": {"medium": 1},
              "proof_host_noise_counts": {"ok": 1}
            }
          ]
        }
        """);
        WriteArtifact(ContributorLabCatalog.CustomValueEnrichedMatrixPath, """
        {
          "records": [
            {
              "index": 1,
              "value_name": "NoisyThing",
              "registry_path": "HKLM\\Software\\Example",
              "default_status": "known",
              "default_value": 0,
              "candidates": [{"value": 1, "vm_validated": false}]
            },
            {
              "index": 2,
              "value_name": "ReadyThing",
              "registry_path": "HKLM\\Software\\Example",
              "default_status": "known",
              "default_value": 0,
              "candidates": [{"value": 0, "vm_validated": true}]
            }
          ]
        }
        """);

        var observations = ContributorLabCatalog.Load(_root).Observations;

        var noisy = observations.Single(item => item.ValueName == "NoisyThing");
        Assert.Contains("certified low-noise VM lane", noisy.NextAction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("low-noise rerun", noisy.AppCardReadinessSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("clean_low_noise_vm_proofs", noisy.MissingProofSummary, StringComparison.Ordinal);
        Assert.Contains("Noisy/debug receipt", noisy.RunTierAction, StringComparison.Ordinal);

        var ready = observations.Single(item => item.ValueName == "ReadyThing");
        Assert.Contains("ready for bounded app-card review", ready.NextAction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ready for bounded app-card review", ready.AppCardReadinessSummary, StringComparison.Ordinal);
        Assert.Equal("No missing proof gates listed.", ready.MissingProofSummary);
        Assert.Contains("Certified-low-noise receipt", ready.RunTierAction, StringComparison.Ordinal);
    }

    [Fact]
    public void ObservationBrowser_FiltersByBucketAndSearchText()
    {
        var snapshot = CreateSnapshot() with
        {
            Observations =
            [
                Observation(1, "TimerCheckFlags", "blocked_by_gate", "missing rollback", "harmful=1"),
                Observation(2, "MfBufferingThreshold", "not_app_surface_ready", "needs bounded claim", "low_confidence=1")
            ]
        };
        var viewModel = new ContributorLabViewModel(snapshot);

        Assert.Equal(2, viewModel.FilteredObservationCount);
        Assert.Contains("Showing 2/2", viewModel.ObservationBrowserSummary, StringComparison.Ordinal);

        viewModel.ObservationBucketFilter = "blocked_by_gate";
        Assert.Equal(1, viewModel.FilteredObservationCount);
        Assert.Equal("TimerCheckFlags", viewModel.FilteredObservations.Single().ValueName);

        viewModel.ObservationSearchText = "harmful";
        Assert.Equal(1, viewModel.FilteredObservationCount);

        viewModel.ObservationSearchText = "buffer";
        Assert.Equal(0, viewModel.FilteredObservationCount);
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
            RequiredScriptsOk: true,
            VirtualizationFirmwareKnown: true,
            VirtualizationFirmwareEnabled: true,
            VirtualizationFirmwareDetail: "Firmware virtualization is enabled according to Win32_Processor.",
            AppReadinessOk: true,
            AppCardsOk: true,
            CustomValueAggregateOk: true,
            CustomValueSurfaceReviewOk: true,
            VmHealthKnown: true,
            VmHealthOk: true,
            VmSnapshotKnown: true,
            VmSnapshotOk: true,
            VmSnapshotName: "clean-25h2-qga",
            VmDotNetKnown: true,
            VmDotNetOk: true,
            VmDotNetDetail: "Guest dotnet is available at C:\\Tools\\DotNetSDK\\8.0.416\\dotnet.exe with Microsoft.NETCore.App 8.0.27 and Microsoft.WindowsDesktop.App 8.0.27.",
            CustomValueRecordCount: 96,
            CustomValueReadyForAppCard: 0,
            CustomValueBlockedByGate: 17,
            CustomValueNotAppSurfaceReady: 79,
            CustomValueBlockedBySafety: 0,
            CustomValueAggregateSurfaceBlocked: false,
            CustomValueNeedsLowNoiseRerun: 0,
            CustomValueNoisyResultCount: 0,
            CustomValueNonOkCount: 0,
            AppCardCandidateCount: 258,
            AppCardPassCount: 258,
            RunTier: "certified",
            VerificationBadge: "Verified",
            ReadinessItems: Array.Empty<ContributorReadinessItem>(),
            CommandPacks: Array.Empty<ContributorCommandPack>(),
            Observations: Array.Empty<ContributorObservation>());

    private static ContributorObservation Observation(
        int index,
        string valueName,
        string bucket,
        string blockers,
        string verdicts)
        => new(
            index,
            valueName,
            @"HKLM\Software\Example",
            bucket,
            "reason",
            blockers.Contains("rollback", StringComparison.OrdinalIgnoreCase)
                ? "Next: run one-value snapshot-safe VM experiment and capture apply, verify, and rollback proof."
                : "Next: keep in Contributor Lab and decide whether another value, source lane, or bounded claim closes the gap.",
            bucket == "ready_for_bounded_app_card"
                ? "Ready for bounded app-card review; not auto-shipped to end users."
                : "Research-only until app-card contract explicitly passes.",
            string.IsNullOrWhiteSpace(blockers) ? "No missing proof gates listed." : $"Missing proof: {blockers}",
            "Certified-low-noise receipt: usable as research evidence, not an end-user card unless gates pass.",
            "known-absent",
            "0, 1",
            "0",
            "low=1",
            "ok=1",
            "contributor-lab-research-only",
            blockers,
            "VM smoke only; no performance claim",
            $"{valueName}: tested",
            verdicts,
            "1/1 apply/reboot/rollback hard-smoke receipts passed",
            "artifact.json",
            blockers,
            "Low-noise VM receipt",
            "python3 registry-research-framework/scripts/check_single_tweak.py Example --json");

    private sealed class FakeContributorCommandRunner : IContributorLabCommandRunner
    {
        private readonly ContributorCommandRunResult _result;

        public FakeContributorCommandRunner(ContributorCommandRunResult result)
        {
            _result = result;
        }

        public string LastCommand { get; private set; } = string.Empty;

        public Task<ContributorCommandRunResult> RunAsync(
            string repoRoot,
            string command,
            CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(_result);
        }
    }
}
