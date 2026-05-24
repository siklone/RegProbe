using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using RegProbe.Application.Services;

namespace RegProbe.App.ViewModels;

public sealed class ContributorLabViewModel : ViewModelBase
{
    private bool _riskAcknowledged;
    private bool _areToolsUnlocked;
    private bool _isContributorCommandRunning;
    private string _commandRunTitle = "No command run yet";
    private string _commandRunStatus = "Idle";
    private string _commandRunOutput = "Use the read-only buttons below to run lookup/readiness commands from the app.";
    private string _customRegistryPath = @"HKLM\SYSTEM\CurrentControlSet\Control\Power";
    private string _customValueName = "SystemResponsiveness";
    private string _customExpectedValues = "10, 30000";
    private string _observationSearchText = string.Empty;
    private string _observationBucketFilter = "all";
    private readonly IContributorLabCommandRunner _commandRunner;
    private readonly AsyncRelayCommand _runCustomLookupCommand;
    private readonly AsyncRelayCommand _runCustomAppQaCommand;
    private readonly AsyncRelayCommand _runAppReadinessCommand;
    private readonly AsyncRelayCommand _runVmHealthCommand;

    public ContributorLabViewModel()
        : this(ContributorLabCatalog.Load(), new ContributorLabCommandRunner())
    {
    }

    public ContributorLabViewModel(ContributorLabSnapshot snapshot)
        : this(snapshot, new ContributorLabCommandRunner())
    {
    }

    public ContributorLabViewModel(ContributorLabSnapshot snapshot, IContributorLabCommandRunner commandRunner)
    {
        Snapshot = snapshot;
        _commandRunner = commandRunner;
        ReadinessItems = new ObservableCollection<ContributorReadinessItem>(snapshot.ReadinessItems);
        CommandPacks = new ObservableCollection<ContributorCommandPackViewModel>(
            snapshot.CommandPacks.Select(pack => new ContributorCommandPackViewModel(pack)));
        Observations = new ObservableCollection<ContributorObservation>(snapshot.Observations);
        ObservationBucketOptions = new ObservableCollection<string>(
            new[] { "all" }.Concat(Observations
                .Select(static observation => observation.Bucket)
                .Where(static bucket => !string.IsNullOrWhiteSpace(bucket))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static bucket => bucket, StringComparer.OrdinalIgnoreCase)));
        _runCustomLookupCommand = new AsyncRelayCommand(
            () => RunContributorCommandAsync("Repo/evidence lookup", CustomEvidenceLookupCommand),
            CanRunCustomValueContributorCommand,
            ex => SetCommandRunResult("Repo/evidence lookup", "Failed", ex.Message));
        _runCustomAppQaCommand = new AsyncRelayCommand(
            () => RunContributorCommandAsync("Existing app-card QA map", CustomAppQaCommand),
            CanRunCustomValueContributorCommand,
            ex => SetCommandRunResult("Existing app-card QA map", "Failed", ex.Message));
        _runAppReadinessCommand = new AsyncRelayCommand(
            () => RunContributorCommandAsync("App readiness/contracts", "python3 registry-research-framework/scripts/check_app_retest_readiness.py --json"),
            CanRunReadOnlyContributorCommand,
            ex => SetCommandRunResult("App readiness/contracts", "Failed", ex.Message));
        _runVmHealthCommand = new AsyncRelayCommand(
            () => RunContributorCommandAsync("Certified VM health", CustomVmHealthCommand),
            CanRunReadOnlyContributorCommand,
            ex => SetCommandRunResult("Certified VM health", "Failed", ex.Message));

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

    public string Subtitle => "Windows-first single key/value evidence workspace for contributors and agentic AI. Normal users should stay in Tweaks.";

    public string RiskGateSummary =>
        "This area validates user-supplied registry keys and values with disposable Windows VMs, clean snapshots, QGA health checks, and reproducible Python scripts. It can mutate the guest registry, reboot the VM, break boot, or produce noisy benchmark data if used casually.";

    public string RiskGateBoundary =>
        "Normal optimization users should stay in Tweaks. Contributor Lab separates certified, community, and noisy/debug observations; only clean reference-eligible evidence can ever support app-card review.";

    public string RiskAcknowledgementText =>
        "I read this and understand that certified mutation requires BIOS/UEFI virtualization, a disposable Windows 11 VM, a clean snapshot, healthy QGA, rollback proof, and tight noise gates.";

    public string VerificationBadge => Snapshot.VerificationBadge;

    public string RunTier => Snapshot.RunTier;

    public bool ReferenceEligible => Snapshot.ReferenceEligible;

    public bool HasOpenCustomValueNoiseGate =>
        CustomValueNonOkCount > 0
        || CustomValueNoisyResultCount > 0
        || CustomValueNeedsLowNoiseRerun > 0;

    public string ContributorLabOperatingMode => RunTier switch
    {
        _ when HasOpenCustomValueNoiseGate => "Noisy/debug lane",
        "certified" => "Certified reference lane",
        "noisy" => "Noisy/debug lane",
        _ => "Community observation lane"
    };

    public string ContributorLabOperatingModeDetail =>
        HasOpenCustomValueNoiseGate
            ? "One or more noise/non-ok/rerun gates is open. Keep results debug-only and rerun before using them for verdicts or app-card review."
            : ReferenceEligible
                ? "VM health, snapshot receipt, app contracts, and low-noise gates are clean enough for reference-eligible contributor evidence."
                : RunTier.Equals("noisy", StringComparison.OrdinalIgnoreCase)
                ? "One or more noise/non-ok/rerun gates is open. Keep results debug-only and rerun before using them for verdicts or app-card review."
                : "Read-only app checks are useful, but mutation results remain community/debug until VM health, snapshot, and noise gates are certified.";

    public string EndUserSurfaceSummary =>
        $"{AppCardPassCount}/{AppCardCandidateCount} shipped cards pass the app-card contract. Normal users see only app-ready cards with bounded claims, visible current/default/target/rollback copy, and no VM pipeline details.";

    public string ContributorResearchSummary =>
        $"{CustomValueRecordCount} user-supplied custom value observations are available for contributor review; {CustomValueReadyForAppCard} are app-card review-ready, {CustomValueResearchOnlyCount} stay research-only, noisy={CustomValueNoisyResultCount}, non_ok={CustomValueNonOkCount}, needs_low_noise_rerun={CustomValueNeedsLowNoiseRerun}.";

    public string ContributorActionBoundarySummary =>
        "WPF can run allowlisted read-only lookup/readiness/VM-health commands. Mutating VM experiments stay copy-only in v1 and require a disposable snapshot plus per-run confirmation.";

    public string RepoRoot => Snapshot.RepoRootFound ? Snapshot.RepoRoot : "Repo root not found";

    public int CustomValueRecordCount => Snapshot.CustomValueRecordCount;

    public int CustomValueReadyForAppCard => Snapshot.CustomValueReadyForAppCard;

    public int CustomValueResearchOnlyCount => System.Math.Max(0, CustomValueRecordCount - CustomValueReadyForAppCard);

    public int CustomValueBlockedByGate => Snapshot.CustomValueBlockedByGate;

    public int CustomValueNotAppSurfaceReady => Snapshot.CustomValueNotAppSurfaceReady;

    public int CustomValueBlockedBySafety => Snapshot.CustomValueBlockedBySafety;

    public bool CustomValueAggregateSurfaceBlocked => Snapshot.CustomValueAggregateSurfaceBlocked;

    public int CustomValueNeedsLowNoiseRerun => Snapshot.CustomValueNeedsLowNoiseRerun;

    public int CustomValueNoisyResultCount => Snapshot.CustomValueNoisyResultCount;

    public int CustomValueNonOkCount => Snapshot.CustomValueNonOkCount;

    public int AppCardCandidateCount => Snapshot.AppCardCandidateCount;

    public int AppCardPassCount => Snapshot.AppCardPassCount;

    public string AppSurfacePolicySummary =>
        HasOpenCustomValueNoiseGate
            ? "Custom registry value experiments are blocked from app cards until non_ok, noisy, and low-noise rerun counts are all zero."
            : CustomValueReadyForAppCard == 0
                ? "Custom registry value experiments are clean Contributor Lab research observations. They are not normal optimization cards until each record has a known default/current value story, tested rollback, explicit app-write, clean low-noise proof, and bounded claims."
                : "Only ready_for_bounded_app_card records with known defaults, rollback proof, explicit app writes, and bounded claims may move into normal app cards.";

    public string CustomValueSurfaceBoundarySummary =>
        CustomValueReadyForAppCard == 0
            ? $"All {CustomValueRecordCount} custom value records stay in Contributor Lab. End users still only see the {AppCardPassCount} shipped cards that pass app-card contracts."
            : $"{CustomValueReadyForAppCard} custom value records are app-card review-ready, not shipped cards. Keep the remaining {CustomValueResearchOnlyCount} records research-only until per-record default/current/target/rollback proof is complete.";

    public string CustomValueReviewReadySummary =>
        $"App-card review-ready (not shipped): {CustomValueReadyForAppCard}; contributor-only observations: {CustomValueResearchOnlyCount}.";

    public string CustomValueWorkflowSummary =>
        "For a user-supplied key/value, start with repository lookup from inside the app, then run one value at a time in a certified disposable VM. Record current/default/target, boot result, app smoke, benchmark deltas as observations only, and rollback proof before any app-card review.";

    public string ContributorReadinessDecisionSummary
    {
        get
        {
            if (!Snapshot.RepoRootFound)
            {
                return "Next safe action: open RegProbe from the repository root or set REGPROBE_REPO_ROOT. Contributor commands stay disabled until the repo is found.";
            }

            if (!Snapshot.RequiredScriptsOk)
            {
                return "Next safe action: repair the contributor script checkout before running lookups. The app only runs allowlisted scripts, so missing scripts block reliable evidence work.";
            }

            if (HasOpenCustomValueNoiseGate)
            {
                return "Next safe action: rerun noisy, non-ok, or needs-low-noise custom value records before any app-card review or certified claim. These observations remain debug-only.";
            }

            if (!Snapshot.AppReadinessOk || !Snapshot.AppCardsOk)
            {
                return "Next safe action: fix existing app readiness and app-card contract checks before adding or promoting new card surfaces.";
            }

            if (!ReferenceEligible)
            {
                return "Next safe action: use in-app lookup/app-QA/readiness checks, but keep VM mutation commands copy-only/community-debug until VM health, QGA, clean snapshot, and run tier are certified.";
            }

            return "Next safe action: certified reference lane is ready. Run exactly one value at a time, confirm each mutation, capture current/default/target/rollback proof, and keep custom values in Contributor Lab until ready_for_bounded_app_card passes.";
        }
    }

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

    public bool IsContributorCommandRunning
    {
        get => _isContributorCommandRunning;
        private set
        {
            if (SetProperty(ref _isContributorCommandRunning, value))
            {
                RaiseContributorCommandCanExecuteChanged();
            }
        }
    }

    public string CommandRunTitle
    {
        get => _commandRunTitle;
        private set => SetProperty(ref _commandRunTitle, value);
    }

    public string CommandRunStatus
    {
        get => _commandRunStatus;
        private set => SetProperty(ref _commandRunStatus, value);
    }

    public string CommandRunOutput
    {
        get => _commandRunOutput;
        private set => SetProperty(ref _commandRunOutput, value);
    }

    public string CustomEvidenceLookupCommand =>
        $"python3 registry-research-framework/scripts/check_single_tweak.py {QuoteArg(CustomValueName)}{BuildExpectedValueArgs(CustomExpectedValues)} --json";

    public string CustomAppQaCommand =>
        $"python3 registry-research-framework/scripts/check_single_tweak_app_qa.py {QuoteArg(CustomValueName)}{BuildExpectedValueArgs(CustomExpectedValues)} --json";

    public string CustomVmHealthCommand =>
        $"python3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --connect qemu:///session --snapshot-name {QuoteArg(Snapshot.VmSnapshotName)} --check-guest-dotnet --json";

    public string CustomVmExperimentOutputName =>
        $"custom-value-{SlugForArtifact(CustomValueName, "replace-value-name")}-{SlugForArtifact(FirstExpectedValueOrDefault(CustomExpectedValues), "0")}";

    public string CustomVmExperimentCommand =>
        $"python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --domain regprobe-win11-25h2-session --connect qemu:///session --registry-path {QuoteArg(CustomRegistryPath)} --value-name {QuoteArg(CustomValueName)} --value-data {QuoteArg(FirstExpectedValueOrDefault(CustomExpectedValues))} --output-name {QuoteArg(CustomVmExperimentOutputName)} --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --require-domain-snapshot --auto-revert-snapshot-on-boot-failure --revert-snapshot-name clean-25h2-qga --abort-on-noisy-host";

    public string CustomValueWorkflowChecklist =>
        "App checks: repo artifact hit, current/default value story, VM/QGA/snapshot readiness, one-value run command, boot/app-smoke result, benchmark observation, rollback proof, then app-card gate.";

    public string CustomValueInvestigationContract =>
        $"Question: does {FirstNonEmpty(CustomValueName, "REPLACE_VALUE_NAME")} exist under {FirstNonEmpty(CustomRegistryPath, "REPLACE_REGISTRY_PATH")}, which values are known, and is there enough evidence for a bounded app card?";

    public string CustomValueStorySummary =>
        $"Value story to capture: current system value, known Windows/default profile, target value(s) {ExpectedValuesForDisplay(CustomExpectedValues)}, and rollback action (restore previous, restore default, or delete if originally absent). First VM target: {FirstExpectedValueOrDefault(CustomExpectedValues)}.";

    public string CustomValueMutationBoundarySummary =>
        ReferenceEligible
            ? "Read-only lookup/readiness commands may run in WPF. VM value experiments still stay copy-only in v1: confirm the clean snapshot/QGA receipt, run one value at a time, and keep rollback ready."
            : "Read-only lookup/readiness commands may run in WPF. VM mutation commands stay copy-only because this environment is not reference-eligible; results are community/debug until certified VM and noise gates pass.";

    public IReadOnlyList<string> CustomValueEvidenceChecklist =>
    [
        "Current value: capture the live registry value, or explicitly record that the value/key is absent.",
        "Default story: show the known Windows/default profile when available; otherwise mark default unknown instead of guessing.",
        "Target story: list each tested value and the source/reason for trying it.",
        "Rollback story: state whether rollback restores previous, restores default, or deletes an originally absent value.",
        "Runtime safety: certified VM health, boot result, Windows shell/app smoke, and rollback smoke must be recorded before app-card review.",
        "Evidence boundary: ETW/Procmon/Ghidra/noise/tranche details stay technical; normal cards only show bounded user-facing claims.",
    ];

    public string CertifiedMutationGuardSummary =>
        ReferenceEligible
            ? "Certified mutation templates are available, but still require per-run confirmation and a snapshot rollback plan."
            : "Mutation templates are copy-only until VM/QGA/snapshot and noise gates are certified; non-certified results are community/debug observations, never reference proof.";

    public string ContributorExecutionPolicySummary =>
        "In-app execution is limited to allowlisted, non-mutating lookup/readiness/VM-health checks. VM value experiments and campaign reruns stay copy-only in WPF v1; run them only after certified VM/snapshot health passes and confirm each mutation outside the app.";

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
        $"ready={CustomValueReadyForAppCard}; blocked_by_gate={CustomValueBlockedByGate}; not_app_surface_ready={CustomValueNotAppSurfaceReady}; safety={CustomValueBlockedBySafety}; aggregate_blocked={CustomValueAggregateSurfaceBlocked.ToString().ToLowerInvariant()}; surface=contributor-lab-only";

    public string CustomValueNextActionSummary =>
        CustomValueAggregateSurfaceBlocked
            ? "Stop promotion: aggregate blockers must be cleared before any custom value experiment app-card review."
            : CustomValueReadyForAppCard > 0
                ? "Review only ready_for_bounded_app_card records; keep all others in Contributor Lab."
                : "Do not create end-user cards yet. Continue per-record rollback/default/app-write proof, then rerun the app-surface review.";

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
                RaiseContributorCommandCanExecuteChanged();
            }
        }
    }

    public bool IsGateVisible => !AreToolsUnlocked;

    public bool IsToolsVisible => AreToolsUnlocked;

    public RelayCommand EnableContributorToolsCommand { get; }

    public ICommand ResetAcknowledgmentCommand { get; }

    public ICommand RunCustomLookupCommand => _runCustomLookupCommand;

    public ICommand RunCustomAppQaCommand => _runCustomAppQaCommand;

    public ICommand RunAppReadinessCommand => _runAppReadinessCommand;

    public ICommand RunVmHealthCommand => _runVmHealthCommand;

    public ObservableCollection<ContributorReadinessItem> ReadinessItems { get; }

    public ObservableCollection<ContributorCommandPackViewModel> CommandPacks { get; }

    public ObservableCollection<ContributorObservation> Observations { get; }

    public ObservableCollection<string> ObservationBucketOptions { get; }

    public string ObservationSearchText
    {
        get => _observationSearchText;
        set
        {
            if (SetProperty(ref _observationSearchText, value))
            {
                RaiseObservationFilterProperties();
            }
        }
    }

    public string ObservationBucketFilter
    {
        get => _observationBucketFilter;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? "all" : value;
            if (SetProperty(ref _observationBucketFilter, next))
            {
                RaiseObservationFilterProperties();
            }
        }
    }

    public IEnumerable<ContributorObservation> FilteredObservations =>
        Observations.Where(MatchesObservationFilter);

    public int FilteredObservationCount => FilteredObservations.Count();

    public string ObservationBrowserSummary =>
        $"Showing {FilteredObservationCount}/{Observations.Count} contributor research observations. These are not optimization recommendations or shipped cards; app-card promotion still requires known default/current/target, tested rollback, explicit app write, clean low-noise proof, and bounded claims.";

    private void RaiseCustomValueCommandProperties()
    {
        OnPropertyChanged(nameof(HasCustomValueInput));
        OnPropertyChanged(nameof(CustomEvidenceLookupCommand));
        OnPropertyChanged(nameof(CustomAppQaCommand));
        OnPropertyChanged(nameof(CustomVmHealthCommand));
        OnPropertyChanged(nameof(CustomVmExperimentOutputName));
        OnPropertyChanged(nameof(CustomVmExperimentCommand));
        OnPropertyChanged(nameof(CustomValueInvestigationContract));
        OnPropertyChanged(nameof(CustomValueStorySummary));
        OnPropertyChanged(nameof(CustomValueMutationBoundarySummary));
        OnPropertyChanged(nameof(CustomValueEvidenceChecklist));
        OnPropertyChanged(nameof(CustomValueDiscoverySteps));
        RaiseContributorCommandCanExecuteChanged();
    }

    private bool MatchesObservationFilter(ContributorObservation observation)
    {
        var bucketMatches = string.Equals(ObservationBucketFilter, "all", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(observation.Bucket, ObservationBucketFilter, StringComparison.OrdinalIgnoreCase);
        if (!bucketMatches)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(ObservationSearchText))
        {
            return true;
        }

        var needle = ObservationSearchText.Trim();
        return Contains(observation.ValueName, needle)
               || Contains(observation.RegistryPath, needle)
               || Contains(observation.Reason, needle)
               || Contains(observation.TestedValueSummary, needle)
               || Contains(observation.VerdictSummary, needle)
               || Contains(observation.AppCardBlockerSummary, needle);
    }

    private void RaiseObservationFilterProperties()
    {
        OnPropertyChanged(nameof(FilteredObservations));
        OnPropertyChanged(nameof(FilteredObservationCount));
        OnPropertyChanged(nameof(ObservationBrowserSummary));
    }

    private static bool Contains(string source, string needle)
        => (source ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase);

    private bool CanRunReadOnlyContributorCommand()
        => AreToolsUnlocked
           && !IsContributorCommandRunning
           && Snapshot.RepoRootFound;

    private bool CanRunCustomValueContributorCommand()
        => CanRunReadOnlyContributorCommand()
           && HasCustomValueInput;

    private async Task RunContributorCommandAsync(string title, string command)
    {
        if (!ContributorLabCatalog.IsAllowlistedCommand(command))
        {
            SetCommandRunResult(title, "Blocked", "Command is not allowlisted for Contributor Lab.");
            return;
        }

        try
        {
            IsContributorCommandRunning = true;
            SetCommandRunResult(title, "Running", command);
            var result = await _commandRunner.RunAsync(Snapshot.RepoRoot, command).ConfigureAwait(false);
            var preview = ContributorLabResultPreviewBuilder.Build(result.StandardOutput);
            var output = string.Join(
                "\n\n",
                new[]
                {
                    preview,
                    result.StandardOutput,
                    string.IsNullOrWhiteSpace(result.StandardError) ? string.Empty : "STDERR:\n" + result.StandardError
                }.Where(static part => !string.IsNullOrWhiteSpace(part)));
            SetCommandRunResult(
                title,
                result.IsSuccess ? "Success" : result.TimedOut ? "Timed out" : $"Exit {result.ExitCode}",
                TruncateOutput(string.IsNullOrWhiteSpace(output) ? "(no output)" : output));
        }
        finally
        {
            IsContributorCommandRunning = false;
        }
    }

    private void SetCommandRunResult(string title, string status, string output)
    {
        CommandRunTitle = title;
        CommandRunStatus = status;
        CommandRunOutput = output;
    }

    private void RaiseContributorCommandCanExecuteChanged()
    {
        _runCustomLookupCommand.RaiseCanExecuteChanged();
        _runCustomAppQaCommand.RaiseCanExecuteChanged();
        _runAppReadinessCommand.RaiseCanExecuteChanged();
        _runVmHealthCommand.RaiseCanExecuteChanged();
    }

    private static string TruncateOutput(string output)
    {
        const int maxLength = 20000;
        return output.Length <= maxLength
            ? output
            : output[..maxLength] + "\n\n... output truncated in Contributor Lab preview ...";
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

    private static string ExpectedValuesForDisplay(string values)
    {
        var tokens = (values ?? string.Empty)
            .Split([',', ';', ' ', '\t', '\r', '\n'], System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return tokens.Length == 0
            ? "not listed yet"
            : string.Join(", ", tokens);
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string QuoteArg(string value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\\\"", System.StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(safe) ? "REPLACE_VALUE_NAME" : $"\"{safe}\"";
    }

    private static string SlugForArtifact(string value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var chars = source
            .ToLowerInvariant()
            .Select(static ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = string.Join(
            "-",
            new string(chars)
                .Split('-', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries));
        return string.IsNullOrWhiteSpace(slug) ? fallback : slug;
    }
}
