using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using RegProbe.Application.Services;

namespace RegProbe.App.ViewModels;

public sealed class ContributorLabViewModel : ViewModelBase
{
    private bool _riskAcknowledged;
    private bool _areToolsUnlocked;
    private string _customRegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\Power";
    private string _customValueName = "SystemResponsiveness";
    private string _customExpectedValues = "10, 30000";

    public ContributorLabViewModel()
        : this(ContributorLabCatalog.Load())
    {
    }

    public ContributorLabViewModel(ContributorLabSnapshot snapshot)
    {
        Snapshot = snapshot;
        ReadinessItems = new ObservableCollection<ContributorReadinessItem>(snapshot.ReadinessItems);
        CommandPacks = new ObservableCollection<ContributorCommandPackViewModel>(
            snapshot.CommandPacks.Select(pack => new ContributorCommandPackViewModel(pack)));
        Observations = new ObservableCollection<ContributorObservation>(snapshot.Observations);

        EnableContributorToolsCommand = new RelayCommand(
            _ => AreToolsUnlocked = true,
            _ => RiskAcknowledged);
        ResetAcknowledgmentCommand = new RelayCommand(_ =>
        {
            AreToolsUnlocked = false;
            RiskAcknowledged = false;
        });
    }

    public ContributorLabSnapshot Snapshot { get; }

    public string Title => "Contributor Lab";

    public string Subtitle => "Windows-first research workspace for contributors and agentic AI. Normal users should stay in Tweaks.";

    public string VerificationBadge => Snapshot.VerificationBadge;

    public string RunTier => Snapshot.RunTier;

    public bool ReferenceEligible => Snapshot.ReferenceEligible;

    public string RepoRoot => Snapshot.RepoRootFound ? Snapshot.RepoRoot : "Repo root not found";

    public int Operator96RecordCount => Snapshot.Operator96RecordCount;

    public int CustomValueRecordCount => Snapshot.Operator96RecordCount;

    public int Operator96ReadyForAppCard => Snapshot.Operator96ReadyForAppCard;

    public int CustomValueReadyForAppCard => Snapshot.Operator96ReadyForAppCard;

    public int Operator96ResearchOnlyCount => System.Math.Max(0, Operator96RecordCount - Operator96ReadyForAppCard);

    public int CustomValueResearchOnlyCount => System.Math.Max(0, CustomValueRecordCount - CustomValueReadyForAppCard);

    public int Operator96BlockedByGate => Snapshot.Operator96BlockedByGate;

    public int CustomValueBlockedByGate => Snapshot.Operator96BlockedByGate;

    public int Operator96NotAppSurfaceReady => Snapshot.Operator96NotAppSurfaceReady;

    public int CustomValueNotAppSurfaceReady => Snapshot.Operator96NotAppSurfaceReady;

    public int Operator96BlockedBySafety => Snapshot.Operator96BlockedBySafety;

    public int CustomValueBlockedBySafety => Snapshot.Operator96BlockedBySafety;

    public bool Operator96AggregateSurfaceBlocked => Snapshot.Operator96AggregateSurfaceBlocked;

    public bool CustomValueAggregateSurfaceBlocked => Snapshot.Operator96AggregateSurfaceBlocked;

    public int Operator96NeedsLowNoiseRerun => Snapshot.Operator96NeedsLowNoiseRerun;

    public int CustomValueNeedsLowNoiseRerun => Snapshot.Operator96NeedsLowNoiseRerun;

    public int Operator96NoisyResultCount => Snapshot.Operator96NoisyResultCount;

    public int CustomValueNoisyResultCount => Snapshot.Operator96NoisyResultCount;

    public int Operator96NonOkCount => Snapshot.Operator96NonOkCount;

    public int CustomValueNonOkCount => Snapshot.Operator96NonOkCount;

    public int AppCardCandidateCount => Snapshot.AppCardCandidateCount;

    public int AppCardPassCount => Snapshot.AppCardPassCount;

    public string AppSurfacePolicySummary =>
        CustomValueNonOkCount > 0 || CustomValueNoisyResultCount > 0 || CustomValueNeedsLowNoiseRerun > 0
            ? "Custom registry value experiments are blocked from app cards until non_ok, noisy, and low-noise rerun counts are all zero."
            : CustomValueReadyForAppCard == 0
                ? "Custom registry value experiments are clean Contributor Lab research observations. They are not normal optimization cards until each record has a known default/current value story, tested rollback, explicit app-write, clean low-noise proof, and bounded claims."
                : "Only ready_for_bounded_app_card records with known defaults, rollback proof, explicit app writes, and bounded claims may move into normal app cards.";

    public string CustomValueWorkflowSummary =>
        "For a user-supplied key/value, start with repository lookup from inside the app, then run one value at a time in a certified disposable VM. Record current/default/target, boot result, app smoke, benchmark deltas as observations only, and rollback proof before any app-card review.";

    public string CustomRegistryPath
    {
        get => _customRegistryPath;
        set
        {
            if (SetProperty(ref _customRegistryPath, value))
            {
                RaiseCustomValueCommandProperties();
            }
        }
    }

    public string CustomValueName
    {
        get => _customValueName;
        set
        {
            if (SetProperty(ref _customValueName, value))
            {
                RaiseCustomValueCommandProperties();
            }
        }
    }

    public string CustomExpectedValues
    {
        get => _customExpectedValues;
        set
        {
            if (SetProperty(ref _customExpectedValues, value))
            {
                RaiseCustomValueCommandProperties();
            }
        }
    }

    public bool HasCustomValueInput => !string.IsNullOrWhiteSpace(CustomValueName);

    public string CustomEvidenceLookupCommand =>
        $"python3 registry-research-framework/scripts/check_single_tweak.py {QuoteArg(CustomValueName)}{BuildExpectedValueArgs(CustomExpectedValues)} --json";

    public string CustomAppQaCommand =>
        $"python3 registry-research-framework/scripts/check_single_tweak_app_qa.py {QuoteArg(CustomValueName)}{BuildExpectedValueArgs(CustomExpectedValues)} --json";

    public string CustomVmHealthCommand =>
        $"python3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --connect qemu:///session --snapshot-name {QuoteArg(Snapshot.VmSnapshotName)} --json";

    public string CustomVmExperimentCommand =>
        $"python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --domain regprobe-win11-25h2-session --connect qemu:///session --registry-path {QuoteArg(CustomRegistryPath)} --value-name {QuoteArg(CustomValueName)} --value-data {FirstExpectedValueOrDefault(CustomExpectedValues)} --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --require-domain-snapshot --auto-revert-snapshot-on-boot-failure --revert-snapshot-name clean-25h2-qga --abort-on-noisy-host";

    public string CustomValueWorkflowChecklist =>
        "App checks: repo artifact hit, current/default value story, VM/QGA/snapshot readiness, one-value run command, boot/app-smoke result, benchmark observation, rollback proof, then app-card gate.";

    public string CertifiedMutationGuardSummary =>
        ReferenceEligible
            ? "Certified mutation templates are available, but still require per-run confirmation and a snapshot rollback plan."
            : "Mutation templates are copy-only until VM/QGA/snapshot and noise gates are certified; non-certified results are community/debug observations, never reference proof.";

    public IReadOnlyList<ContributorCommandPackViewModel> CustomValueDiscoverySteps =>
    [
        new(new ContributorCommandPack(
            "1. Repo/evidence lookup",
            "Non-mutating search across research records, tweak catalog, app surface entries, runtime evidence, and app/source mappings.",
            CustomEvidenceLookupCommand,
            "community-safe",
            RequiresCertifiedVm: false,
            MutatesGuest: false)),
        new(new ContributorCommandPack(
            "2. Existing app-card QA map",
            "If the value already maps to a shipped card, produce the card QA plan and current/default/target/rollback expectations.",
            CustomAppQaCommand,
            "community-safe",
            RequiresCertifiedVm: false,
            MutatesGuest: false)),
        new(new ContributorCommandPack(
            "3. App surface readiness",
            "Check whether shipped cards and evidence contracts are healthy before adding or changing any card surface.",
            "python3 registry-research-framework/scripts/check_app_retest_readiness.py --json",
            "community-safe",
            RequiresCertifiedVm: false,
            MutatesGuest: false)),
        new(new ContributorCommandPack(
            "4. Certified VM health",
            "Non-mutating QGA/snapshot/readiness receipt. Required before the app should treat a mutation run as certified reference evidence.",
            CustomVmHealthCommand,
            ReferenceEligible ? "certified-ready" : "certified-required",
            RequiresCertifiedVm: true,
            MutatesGuest: false)),
        new(new ContributorCommandPack(
            "5. One-value VM experiment",
            "Apply exactly one value in the disposable VM, run boot/app smoke and microbench observations, then rollback from snapshot if needed.",
            CustomVmExperimentCommand,
            ReferenceEligible ? "certified-ready" : "certified-required",
            RequiresCertifiedVm: true,
            MutatesGuest: true)),
    ];

    public string CustomValueGateBreakdown =>
        $"ready={CustomValueReadyForAppCard}; blocked_by_gate={CustomValueBlockedByGate}; not_app_surface_ready={CustomValueNotAppSurfaceReady}; safety={CustomValueBlockedBySafety}; aggregate_blocked={CustomValueAggregateSurfaceBlocked.ToString().ToLowerInvariant()}; seed_batch=custom-value";

    public string CustomValueNextActionSummary =>
        CustomValueAggregateSurfaceBlocked
            ? "Stop promotion: aggregate blockers must be cleared before any custom value experiment app-card review."
            : CustomValueReadyForAppCard > 0
                ? "Review only ready_for_bounded_app_card records; keep all others in Contributor Lab."
                : "Do not create end-user cards yet. Continue per-record rollback/default/app-write proof, then rerun the app-surface review.";

    public string Operator96GateBreakdown =>
        CustomValueGateBreakdown;

    public string Operator96NextActionSummary =>
        CustomValueNextActionSummary;

    public bool RiskAcknowledged
    {
        get => _riskAcknowledged;
        set
        {
            if (SetProperty(ref _riskAcknowledged, value))
            {
                EnableContributorToolsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool AreToolsUnlocked
    {
        get => _areToolsUnlocked;
        private set
        {
            if (SetProperty(ref _areToolsUnlocked, value))
            {
                OnPropertyChanged(nameof(IsGateVisible));
                OnPropertyChanged(nameof(IsToolsVisible));
            }
        }
    }

    public bool IsGateVisible => !AreToolsUnlocked;

    public bool IsToolsVisible => AreToolsUnlocked;

    public RelayCommand EnableContributorToolsCommand { get; }

    public ICommand ResetAcknowledgmentCommand { get; }

    public ObservableCollection<ContributorReadinessItem> ReadinessItems { get; }

    public ObservableCollection<ContributorCommandPackViewModel> CommandPacks { get; }

    public ObservableCollection<ContributorObservation> Observations { get; }

    private void RaiseCustomValueCommandProperties()
    {
        OnPropertyChanged(nameof(HasCustomValueInput));
        OnPropertyChanged(nameof(CustomEvidenceLookupCommand));
        OnPropertyChanged(nameof(CustomAppQaCommand));
        OnPropertyChanged(nameof(CustomVmHealthCommand));
        OnPropertyChanged(nameof(CustomVmExperimentCommand));
        OnPropertyChanged(nameof(CustomValueDiscoverySteps));
    }

    private static string BuildExpectedValueArgs(string values)
    {
        var tokens = (values ?? string.Empty)
            .Split([',', ';', ' ', '\t', '\r', '\n'], System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .Select(value => $" --expected-value {QuoteArg(value)}");
        return string.Concat(tokens);
    }

    private static string FirstExpectedValueOrDefault(string values)
        => (values ?? string.Empty)
            .Split([',', ';', ' ', '\t', '\r', '\n'], System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))
           ?? "0";

    private static string QuoteArg(string value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\\\"", System.StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(safe) ? "REPLACE_VALUE_NAME" : $"\"{safe}\"";
    }
}
