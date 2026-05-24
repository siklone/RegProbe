using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Management;
using RegProbe.Application.Utilities;

namespace RegProbe.Application.Services;

public sealed record ContributorReadinessItem(
    string Label,
    string Status,
    string Detail,
    string Tone);

public sealed record ContributorCommandPack(
    string Title,
    string Purpose,
    string Command,
    string Tier,
    bool RequiresCertifiedVm,
    bool MutatesGuest);

public sealed record ContributorObservation(
    int Index,
    string ValueName,
    string RegistryPath,
    string Bucket,
    string Reason,
    string NextAction,
    string AppCardReadinessSummary,
    string MissingProofSummary,
    string RunTierAction,
    string DefaultSummary,
    string CandidateValues,
    string ValidatedValues,
    string ConfidenceSummary,
    string NoiseSummary,
    string SurfaceDestination,
    string PromotionChecklist,
    string ClaimBoundary,
    string TestedValueSummary,
    string VerdictSummary,
    string SmokeSummary,
    string ArtifactSummary,
    string AppCardBlockerSummary,
    string NoiseBadge,
    string CommandHint);

public sealed record ContributorLabSnapshot(
    string RepoRoot,
    bool RepoRootFound,
    bool IsWindows,
    bool IsElevated,
    bool PythonAvailable,
    bool GitAvailable,
    bool RequiredScriptsOk,
    bool VirtualizationFirmwareKnown,
    bool VirtualizationFirmwareEnabled,
    string VirtualizationFirmwareDetail,
    bool AppReadinessOk,
    bool AppCardsOk,
    bool CustomValueAggregateOk,
    bool CustomValueSurfaceReviewOk,
    bool VmHealthKnown,
    bool VmHealthOk,
    bool VmSnapshotKnown,
    bool VmSnapshotOk,
    string VmSnapshotName,
    bool VmDotNetKnown,
    bool VmDotNetOk,
    string VmDotNetDetail,
    int CustomValueRecordCount,
    int CustomValueReadyForAppCard,
    int CustomValueBlockedByGate,
    int CustomValueNotAppSurfaceReady,
    int CustomValueBlockedBySafety,
    bool CustomValueAggregateSurfaceBlocked,
    int CustomValueNeedsLowNoiseRerun,
    int CustomValueNoisyResultCount,
    int CustomValueNonOkCount,
    int AppCardCandidateCount,
    int AppCardPassCount,
    string RunTier,
    string VerificationBadge,
    IReadOnlyList<ContributorReadinessItem> ReadinessItems,
    IReadOnlyList<ContributorCommandPack> CommandPacks,
    IReadOnlyList<ContributorObservation> Observations)
{
    public bool ReferenceEligible =>
        string.Equals(RunTier, "certified", StringComparison.OrdinalIgnoreCase)
        && VmHealthKnown
        && VmHealthOk
        && VmSnapshotKnown
        && VmSnapshotOk
        && CustomValueNoisyResultCount == 0
        && CustomValueNonOkCount == 0
        && CustomValueNeedsLowNoiseRerun == 0;
}

public static class ContributorLabCatalog
{
    public const string CustomValueAggregatePath = "registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json";
    public const string CustomValueSurfaceReviewPath = "registry-research-framework/audit/operator96-app-surface-review-20260510.json";
    public const string CustomValueEnrichedMatrixPath = "registry-research-framework/audit/operator96-enriched-value-matrix-20260510.json";
    public const string AppReadinessPath = "registry-research-framework/audit/app-retest-readiness-latest.json";
    public const string AppCardContractsPath = "registry-research-framework/audit/app-card-evidence-contracts-latest.json";
    public const string VmHealthPath = "registry-research-framework/audit/app-retest-vm-health-latest.json";

    private static readonly string[] AllowlistedScriptPaths =
    [
        "registry-research-framework/scripts/check_single_tweak.py",
        "registry-research-framework/scripts/check_single_tweak_app_qa.py",
        "registry-research-framework/scripts/check_app_retest_readiness.py",
        "registry-research-framework/scripts/check_app_card_evidence_contracts.py",
        "registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py",
        "registry-research-framework/scripts/generate_custom_value_app_surface_review.py",
        "scripts/vm-kvm/vm-health-check.py",
    ];

    private static readonly string[] RequiredScriptPaths =
    [
        .. AllowlistedScriptPaths,
        "scripts/vm-kvm/run-guest-dotnet-toolchain-bootstrap.py",
        "scripts/vm-kvm/run-guest-registry-value-campaign.py",
        "scripts/vm-kvm/run-guest-registry-value-experiment.py",
    ];

    public static ContributorLabSnapshot Load(string? repoRootOverride = null)
    {
        var repoRoot = ResolveRepoRoot(repoRootOverride);
        var repoRootFound = IsRepoRoot(repoRoot);

        var aggregate = ReadJson(repoRoot, CustomValueAggregatePath);
        var review = ReadJson(repoRoot, CustomValueSurfaceReviewPath);
        var matrix = ReadJson(repoRoot, CustomValueEnrichedMatrixPath);
        var readiness = ReadJson(repoRoot, AppReadinessPath);
        var contracts = ReadJson(repoRoot, AppCardContractsPath);
        var vmHealth = ReadJson(repoRoot, VmHealthPath);

        var aggregateSummary = Object(aggregate, "summary");
        var reviewSummary = Object(review, "summary");
        var readinessSummary = Object(readiness, "summary");
        var contractsSummary = Object(contracts, "summary");

        var nonOk = Int(aggregateSummary, "non_ok_count");
        var noisy = Int(aggregateSummary, "noisy_result_count");
        var needsRerun = Int(reviewSummary, "needs_low_noise_rerun");
        var aggregateOk = Text(aggregate, "status") == "ok" && nonOk == 0 && noisy == 0;
        var surfaceOk = Text(review, "status") == "PASS" && needsRerun == 0;
        var appReadinessOk = Text(readinessSummary, "kvm_app_smoke_status") == "ok"
                             && Text(readinessSummary, "kvm_lane_health_status") == "ok";
        var appCardsOk = Text(contracts, "status") == "PASS" && Int(contractsSummary, "fail_count") == 0;
        var vmHealthKnown = vmHealth is not null;
        var vmHealthOk = Text(vmHealth, "status") == "ok"
                         || Text(Object(vmHealth, "summary"), "status") == "ok"
                         || Text(Object(vmHealth, "qga"), "status") == "ok";
        var vmSnapshot = Object(Object(vmHealth, "checks"), "snapshot");
        var vmSnapshotKnown = vmSnapshot.ValueKind == JsonValueKind.Object;
        var vmSnapshotOk = Text(vmSnapshot, "status") == "ok" && Bool(vmSnapshot, "exists");
        var vmSnapshotName = Text(vmSnapshot, "snapshot_name");
        var vmDotNet = Object(Object(vmHealth, "checks"), "guest_dotnet_toolchain");
        var vmDotNetKnown = vmDotNet.ValueKind == JsonValueKind.Object;
        var vmDotNetOk = Text(vmDotNet, "status") == "ok";
        var vmDotNetDetail = BuildVmDotNetDetail(vmDotNet);

        var runTier = aggregateOk && surfaceOk && appReadinessOk && appCardsOk && vmHealthKnown && vmHealthOk && vmSnapshotKnown && vmSnapshotOk
            ? "certified"
            : noisy > 0 || nonOk > 0 || needsRerun > 0
                ? "noisy"
                : "community";
        var badge = runTier switch
        {
            "certified" => "Verified",
            "noisy" => "Noisy/debug only",
            _ => "Community observed"
        };

        var observations = BuildObservations(review, matrix, aggregate);
        var commandPacks = BuildCommandPacks(repoRoot, runTier == "certified");
        var requiredScriptsOk = repoRootFound && RequiredScriptPaths.All(path =>
            File.Exists(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar))));
        var virtualization = DetectVirtualizationFirmware();

        var snapshot = new ContributorLabSnapshot(
            repoRoot,
            repoRootFound,
            OperatingSystem.IsWindows(),
            ProcessElevation.IsElevated(),
            IsExecutableOnPath("python3") || IsExecutableOnPath("python") || IsExecutableOnPath("py"),
            IsExecutableOnPath("git"),
            requiredScriptsOk,
            virtualization.Known,
            virtualization.Enabled,
            virtualization.Detail,
            appReadinessOk,
            appCardsOk,
            aggregateOk,
            surfaceOk,
            vmHealthKnown,
            vmHealthKnown && vmHealthOk,
            vmSnapshotKnown,
            vmSnapshotKnown && vmSnapshotOk,
            string.IsNullOrWhiteSpace(vmSnapshotName) ? "clean-25h2-qga" : vmSnapshotName,
            vmDotNetKnown,
            vmDotNetKnown && vmDotNetOk,
            vmDotNetDetail,
            Int(reviewSummary, "record_count"),
            Int(reviewSummary, "ready_for_bounded_app_card"),
            Int(reviewSummary, "blocked_by_gate"),
            Int(reviewSummary, "not_app_surface_ready"),
            Int(reviewSummary, "blocked_by_safety"),
            Bool(reviewSummary, "aggregate_surface_blocked"),
            needsRerun,
            noisy,
            nonOk,
            Int(contractsSummary, "candidate_count"),
            Int(contractsSummary, "pass_count"),
            runTier,
            badge,
            System.Array.Empty<ContributorReadinessItem>(),
            commandPacks,
            observations);

        return snapshot with { ReadinessItems = BuildReadinessItems(snapshot) };
    }

    public static bool IsAllowlistedCommand(string command)
    {
        var trimmed = (command ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        if (trimmed.Contains(';')
            || trimmed.Contains('&')
            || trimmed.Contains('|')
            || trimmed.Contains('<')
            || trimmed.Contains('>')
            || trimmed.Contains('\n')
            || trimmed.Contains('\r'))
        {
            return false;
        }

        var executableAllowed = trimmed.StartsWith("python3 ", StringComparison.OrdinalIgnoreCase)
                                || trimmed.StartsWith("python ", StringComparison.OrdinalIgnoreCase)
                                || trimmed.StartsWith("py -3 ", StringComparison.OrdinalIgnoreCase);
        return executableAllowed
               && AllowlistedScriptPaths.Any(script => trimmed.Contains(script, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<ContributorCommandPack> BuildCommandPacks(string repoRoot, bool certifiedReady)
    {
        return
        [
            new(
                "Single tweak lookup",
                "Find a tweak, registry value, expected value, evidence, and app mapping without touching the system.",
                "python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness --expected-value 10 --expected-value 30000 --json",
                "community-safe",
                RequiresCertifiedVm: false,
                MutatesGuest: false),
            new(
                "Custom key/value lookup template",
                "Start a user-supplied registry value investigation without mutation. Replace the value name and expected values, then inspect matching records, app mappings, and evidence.",
                "python3 registry-research-framework/scripts/check_single_tweak.py REPLACE_VALUE_NAME --expected-value 0 --expected-value 1 --json",
                "community-safe",
                RequiresCertifiedVm: false,
                MutatesGuest: false),
            new(
                "Single tweak app QA plan",
                "Create the Windows app QA launch plan for one card before VM execution.",
                "python3 registry-research-framework/scripts/check_single_tweak_app_qa.py SystemResponsiveness --expected-value 10 --json",
                "community-safe",
                RequiresCertifiedVm: false,
                MutatesGuest: false),
            new(
                "App retest readiness",
                "Check app cards, evidence drawer contracts, rollback coverage, and KVM smoke receipts.",
                "python3 registry-research-framework/scripts/check_app_retest_readiness.py --json",
                "community-safe",
                RequiresCertifiedVm: false,
                MutatesGuest: false),
            new(
                "App-card evidence contracts",
                "Verify shipped app cards keep verdict, confidence, proof lanes, and rollback copy complete.",
                "python3 registry-research-framework/scripts/check_app_card_evidence_contracts.py --json --no-write",
                "community-safe",
                RequiresCertifiedVm: false,
                MutatesGuest: false),
            new(
                "Representative promoted app QA batch",
                "Run a small category-balanced live VM batch for shipped cards; this applies, verifies, and rolls back inside the guest.",
                "python3 registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py --limit-per-category 1 --total-limit 14 --run-kvm --wait-timeout 900 --json",
                certifiedReady ? "certified-ready" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: true),
            new(
                "Custom value app-surface review",
                "Recompute why user-supplied registry value observations stay research-only or become eligible for bounded app-card review.",
                "python3 registry-research-framework/scripts/generate_custom_value_app_surface_review.py --json",
                "community-safe",
                RequiresCertifiedVm: false,
                MutatesGuest: false),
            new(
                "Certified VM health",
                "Non-mutating QGA/snapshot preflight before any certified value experiment.",
                "python3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --connect qemu:///session --snapshot-name clean-25h2-qga --json",
                certifiedReady ? "certified-ready" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: false),
            new(
                "VM .NET test toolchain",
                "Non-mutating QGA check for the guest dotnet command and Microsoft.WindowsDesktop.App runtime used by VM-side C# tests.",
                "python3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --connect qemu:///session --snapshot-name clean-25h2-qga --check-guest-dotnet --json",
                certifiedReady ? "certified-check" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: false),
            new(
                "VM .NET toolchain bootstrap",
                "Install the guest portable .NET SDK plus Microsoft.WindowsDesktop.App runtime so VM-side C# tests can run. Use only on a disposable certified VM snapshot.",
                "python3 scripts/vm-kvm/run-guest-dotnet-toolchain-bootstrap.py --domain regprobe-win11-25h2-session --connect qemu:///session --install-dir C:\\\\Tools\\\\DotNetSDK\\\\8.0.416 --sdk-version 8.0.416 --desktop-runtime-channel 8.0 --wait-timeout 1800",
                certifiedReady ? "certified-mutation" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: true),
            new(
                "Single value VM experiment",
                "Apply one value in the disposable VM, reboot-smoke it, rollback, and abort before mutation if host noise is not clean.",
                "python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --domain regprobe-win11-25h2-session --connect qemu:///session --registry-path \"HKLM\\\\SYSTEM\\\\CurrentControlSet\\\\Control\\\\Power\" --value-name MfBufferingThreshold --value-data 0 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --require-domain-snapshot --auto-revert-snapshot-on-boot-failure --revert-snapshot-name clean-25h2-qga --abort-on-noisy-host",
                certifiedReady ? "certified-ready" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: true),
            new(
                "Custom key/value VM experiment template",
                "Edit the registry path, value name, and DWORD value for one user-supplied experiment. Use one value per run so boot failure, app smoke, benchmark deltas, and rollback stay attributable.",
                "python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --domain regprobe-win11-25h2-session --connect qemu:///session --registry-path \"HKLM\\\\REPLACE\\\\KEY\\\\PATH\" --value-name REPLACE_VALUE_NAME --value-data REPLACE_DWORD_VALUE --output-name custom-value-REPLACE_VALUE_NAME-REPLACE_DWORD_VALUE --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --require-domain-snapshot --auto-revert-snapshot-on-boot-failure --revert-snapshot-name clean-25h2-qga --abort-on-noisy-host",
                certifiedReady ? "certified-ready" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: true),
            new(
                "Custom value tranche rerun",
                "Continue or rerun the value matrix in small snapshot-safe checkpoints; results remain research observations until app-surface gates pass.",
                "python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --run --limit-experiments 10 --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --snapshot-name clean-25h2-qga --abort-on-noisy-host --stop-on-failure",
                certifiedReady ? "certified-ready" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: true),
        ];
    }

    public static IReadOnlyList<ContributorReadinessItem> BuildReadinessItems(ContributorLabSnapshot snapshot)
    {
        return
        [
            Ready("Windows host", snapshot.IsWindows, "Contributor Lab v1 is Windows-first; non-Windows hosts can still inspect artifacts."),
            Ready("Administrator token", snapshot.IsElevated, "Needed for local registry apply paths. VM-only planning can run without elevation."),
            Ready("Python available", snapshot.PythonAvailable, "Python scripts are the canonical contributor API."),
            Ready("Git available", snapshot.GitAvailable, "Required for PR workflow and artifact review."),
            Ready("Repo root", snapshot.RepoRootFound, snapshot.RepoRootFound ? snapshot.RepoRoot : "Set REGPROBE_REPO_ROOT or launch from the repo checkout."),
            Ready("Required scripts", snapshot.RequiredScriptsOk, "Single-tweak lookup, app QA, VM health, VM .NET bootstrap, and value experiment scripts must exist in this checkout."),
            new(
                "BIOS virtualization",
                snapshot.VirtualizationFirmwareKnown ? snapshot.VirtualizationFirmwareEnabled ? "Ready" : "Needs attention" : "Unknown",
                snapshot.VirtualizationFirmwareDetail,
                snapshot.VirtualizationFirmwareKnown ? snapshot.VirtualizationFirmwareEnabled ? "ok" : "warning" : "neutral"),
            Ready("App readiness", snapshot.AppReadinessOk, "Cards, rollback coverage, KVM lane health, and app smoke receipts."),
            Ready("App-card contracts", snapshot.AppCardsOk, $"{snapshot.AppCardPassCount}/{snapshot.AppCardCandidateCount} shipped cards pass."),
            Ready("Custom value low-noise", snapshot.CustomValueAggregateOk, $"non_ok={snapshot.CustomValueNonOkCount}, noisy={snapshot.CustomValueNoisyResultCount}; user-supplied seed batch."),
            Ready(
                "Custom value app-surface gate",
                snapshot.CustomValueSurfaceReviewOk,
                $"ready={snapshot.CustomValueReadyForAppCard}, blocked_by_gate={snapshot.CustomValueBlockedByGate}, not_ready={snapshot.CustomValueNotAppSurfaceReady}, safety={snapshot.CustomValueBlockedBySafety}, needs_rerun={snapshot.CustomValueNeedsLowNoiseRerun}."),
            Ready(
                "Custom value aggregate gate",
                !snapshot.CustomValueAggregateSurfaceBlocked,
                snapshot.CustomValueAggregateSurfaceBlocked
                    ? "Aggregate blockers are present; custom value experiments cannot support app-card promotion."
                    : "Aggregate blockers are clear; per-record app-card gates still decide promotion."),
            new(
                "VM configured",
                snapshot.VmHealthKnown ? snapshot.VmHealthOk ? "Ready" : "Needs attention" : "Unknown",
                snapshot.VmHealthKnown
                    ? "Latest VM health artifact found for regprobe-win11-25h2-session."
                    : "Run vm-health-check so the app can see whether the contributor VM is configured.",
                snapshot.VmHealthKnown ? snapshot.VmHealthOk ? "ok" : "warning" : "neutral"),
            new(
                "VM/QGA latest receipt",
                snapshot.VmHealthKnown ? snapshot.VmHealthOk ? "Ready" : "Needs attention" : "Unknown",
                snapshot.VmHealthKnown ? "Latest VM health artifact was found." : "Run vm-health-check before certified mutation.",
                snapshot.VmHealthKnown ? snapshot.VmHealthOk ? "ok" : "warning" : "neutral"),
            new(
                "VM snapshot receipt",
                snapshot.VmSnapshotKnown ? snapshot.VmSnapshotOk ? "Ready" : "Needs attention" : "Unknown",
                snapshot.VmSnapshotKnown
                    ? $"{snapshot.VmSnapshotName} snapshot check is present in latest health artifact."
                    : "Run vm-health-check with --snapshot-name clean-25h2-qga before certified mutation.",
                snapshot.VmSnapshotKnown ? snapshot.VmSnapshotOk ? "ok" : "warning" : "neutral"),
            new(
                "VM .NET test toolchain",
                snapshot.VmDotNetKnown ? snapshot.VmDotNetOk ? "Ready" : "Needs attention" : "Unknown",
                snapshot.VmDotNetKnown
                    ? snapshot.VmDotNetDetail
                    : "Run vm-health-check with --check-guest-dotnet before relying on VM-side C# test execution.",
                snapshot.VmDotNetKnown ? snapshot.VmDotNetOk ? "ok" : "warning" : "neutral"),
            new("Run tier", snapshot.VerificationBadge, $"Current aggregate tier: {snapshot.RunTier}.", snapshot.ReferenceEligible ? "ok" : "warning"),
        ];
    }

    private static ContributorReadinessItem Ready(string label, bool ok, string detail)
        => new(label, ok ? "Ready" : "Needs attention", detail, ok ? "ok" : "warning");

    private static (bool Known, bool Enabled, string Detail) DetectVirtualizationFirmware()
    {
        if (!OperatingSystem.IsWindows())
        {
            return (false, false, "Auto-detection is Windows-only; verify BIOS/UEFI virtualization before certified VM mutation.");
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT VirtualizationFirmwareEnabled FROM Win32_Processor");
            foreach (ManagementObject processor in searcher.Get().Cast<ManagementObject>())
            {
                using (processor)
                {
                    if (processor["VirtualizationFirmwareEnabled"] is bool enabled)
                    {
                        return enabled
                            ? (true, true, "Firmware virtualization is enabled according to Win32_Processor.")
                            : (true, false, "Firmware virtualization is disabled or hidden; enable VT-x/AMD-V/SVM before certified VM runs.");
                    }
                }
            }

            return (false, false, "Win32_Processor did not expose VirtualizationFirmwareEnabled.");
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or COMException)
        {
            return (false, false, $"Could not query firmware virtualization: {ex.Message}");
        }
    }

    private static string BuildVmDotNetDetail(JsonElement toolchain)
    {
        if (toolchain.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var dotnetPath = Text(toolchain, "dotnet_path");
        var configuredPath = Text(toolchain, "configured_dotnet_path");
        var coreVersions = Strings(toolchain, "core_runtime_versions");
        var desktopVersions = Strings(toolchain, "desktop_runtime_versions");
        if (Text(toolchain, "status") == "ok")
        {
            var coreText = coreVersions.Count == 0 ? "unknown NETCore runtime" : string.Join(", ", coreVersions);
            var desktopText = desktopVersions.Count == 0 ? "unknown WindowsDesktop runtime" : string.Join(", ", desktopVersions);
            return $"Guest dotnet is available at {FirstNonEmpty(dotnetPath, configuredPath, "PATH")} with Microsoft.NETCore.App {coreText} and Microsoft.WindowsDesktop.App {desktopText}.";
        }

        var error = Text(toolchain, "error");
        if (!string.IsNullOrWhiteSpace(error))
        {
            return error;
        }

        var dotnetState = Bool(toolchain, "configured_dotnet_path_exists") || Bool(toolchain, "dotnet_on_path")
            ? $"dotnet found at {FirstNonEmpty(dotnetPath, configuredPath, "PATH")}"
            : $"dotnet missing at {FirstNonEmpty(configuredPath, "configured path")} and PATH";
        var coreState = Bool(toolchain, "core_runtime_present")
            ? "Microsoft.NETCore.App runtime present"
            : "Microsoft.NETCore.App runtime missing";
        var desktopState = Bool(toolchain, "desktop_runtime_present")
            ? "Microsoft.WindowsDesktop.App runtime present"
            : "Microsoft.WindowsDesktop.App runtime missing";
        return $"{dotnetState}; {coreState}; {desktopState}.";
    }

    private sealed record AggregateObservation(
        string Value,
        string Status,
        string Verdict,
        string Confidence,
        string HostNoise,
        string PrimaryDelta,
        bool SmokeHardOk,
        string ArtifactPath);

    private static IReadOnlyList<ContributorObservation> BuildObservations(JsonDocument? review, JsonDocument? matrix, JsonDocument? aggregate)
    {
        var matrixRecords = RecordsByIndex(matrix);
        var aggregateRecords = AggregateResultsByIndex(aggregate);
        var records = Array(review?.RootElement, "records");
        var result = new List<ContributorObservation>();

        foreach (var reviewRecord in records)
        {
            var index = Int(reviewRecord, "index");
            matrixRecords.TryGetValue(index, out var matrixRecord);
            aggregateRecords.TryGetValue(index, out var aggregateForRecord);
            aggregateForRecord ??= [];
            var candidateValues = CandidateValues(matrixRecord);
            var validatedValues = ValidatedValues(matrixRecord);
            var valueName = Text(reviewRecord, "value_name");
            var registryPath = Text(reviewRecord, "registry_path");
            var command = $"python3 registry-research-framework/scripts/check_single_tweak.py {valueName} --json";

            result.Add(new ContributorObservation(
                index,
                valueName,
                registryPath,
                Text(reviewRecord, "app_surface_bucket"),
                string.Join(", ", Strings(reviewRecord, "reasons")),
                NextAction(reviewRecord, aggregateForRecord),
                AppCardReadinessSummary(reviewRecord),
                MissingProofSummary(reviewRecord),
                RunTierAction(aggregateForRecord),
                DefaultSummary(matrixRecord, reviewRecord),
                string.IsNullOrWhiteSpace(candidateValues) ? "none listed" : candidateValues,
                string.IsNullOrWhiteSpace(validatedValues) ? "none certified yet" : validatedValues,
                Counts(reviewRecord, "proof_confidence_counts"),
                Counts(reviewRecord, "proof_host_noise_counts"),
                SurfaceDestination(reviewRecord),
                PromotionChecklist(reviewRecord),
                ClaimBoundary(reviewRecord),
                TestedValueSummary(aggregateForRecord),
                VerdictSummary(aggregateForRecord),
                SmokeSummary(aggregateForRecord),
                ArtifactSummary(aggregateForRecord),
                AppCardBlockerSummary(reviewRecord),
                NoiseBadge(aggregateForRecord),
                command));
        }

        return result.OrderBy(item => item.Index).ToList();
    }

    private static Dictionary<int, List<AggregateObservation>> AggregateResultsByIndex(JsonDocument? document)
    {
        var result = new Dictionary<int, List<AggregateObservation>>();
        foreach (var record in Array(document?.RootElement, "results"))
        {
            var index = Int(record, "index");
            if (index <= 0)
            {
                continue;
            }

            var observations = Object(record, "observations");
            var smoke = Object(observations, "smoke_hard_success");
            var smokeOk = Bool(smoke, "apply_smoke_hard_success")
                          && Bool(smoke, "post_reboot_smoke_hard_success")
                          && Bool(smoke, "post_rollback_smoke_hard_success");
            var value = Text(record, "value_data");
            if (string.IsNullOrWhiteSpace(value))
            {
                value = Text(record, "requested_data");
            }

            var item = new AggregateObservation(
                value,
                Text(record, "status"),
                Text(observations, "verdict"),
                Text(observations, "confidence"),
                Text(observations, "host_noise"),
                Text(observations, "primary_delta_pct"),
                smokeOk,
                Text(record, "artifact_json"));

            if (!result.TryGetValue(index, out var list))
            {
                list = [];
                result[index] = list;
            }

            list.Add(item);
        }

        foreach (var pair in result)
        {
            pair.Value.Sort(static (left, right) => string.Compare(left.Value, right.Value, StringComparison.OrdinalIgnoreCase));
        }

        return result;
    }

    private static Dictionary<int, JsonElement> RecordsByIndex(JsonDocument? document)
    {
        var result = new Dictionary<int, JsonElement>();
        foreach (var record in Array(document?.RootElement, "records"))
        {
            result[Int(record, "index")] = record;
        }

        return result;
    }

    private static string CandidateValues(JsonElement record)
        => string.Join(", ", Array(record, "candidates").Select(candidate => Text(candidate, "value")).Where(value => value.Length > 0).Distinct());

    private static string ValidatedValues(JsonElement record)
        => string.Join(", ", Array(record, "candidates")
            .Where(candidate => Bool(candidate, "vm_validated"))
            .Select(candidate => Text(candidate, "value"))
            .Where(value => value.Length > 0)
            .Distinct());

    private static string TestedValueSummary(IReadOnlyList<AggregateObservation> records)
    {
        if (records.Count == 0)
        {
            return "No low-noise value receipt attached yet.";
        }

        return JoinLimited(records.Select(record =>
        {
            var verdict = string.IsNullOrWhiteSpace(record.Verdict) ? record.Status : record.Verdict;
            var confidence = string.IsNullOrWhiteSpace(record.Confidence) ? "unknown" : record.Confidence;
            var noise = string.IsNullOrWhiteSpace(record.HostNoise) ? "unknown" : record.HostNoise;
            var delta = string.IsNullOrWhiteSpace(record.PrimaryDelta) ? "delta n/a" : $"delta {record.PrimaryDelta}%";
            return $"{record.Value}: {verdict}, {confidence}, noise={noise}, {delta}";
        }));
    }

    private static string VerdictSummary(IReadOnlyList<AggregateObservation> records)
        => records.Count == 0
            ? "none"
            : string.Join(", ", records
                .GroupBy(record => string.IsNullOrWhiteSpace(record.Verdict) ? record.Status : record.Verdict)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => $"{group.Key}={group.Count()}"));

    private static string SmokeSummary(IReadOnlyList<AggregateObservation> records)
        => records.Count == 0
            ? "not rerun"
            : $"{records.Count(record => record.SmokeHardOk)}/{records.Count} apply/reboot/rollback hard-smoke receipts passed";

    private static string ArtifactSummary(IReadOnlyList<AggregateObservation> records)
        => records.Count == 0
            ? "no aggregate artifact attached"
            : JoinLimited(records
                .Select(record => record.ArtifactPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(DisplayArtifactPath)
                .Distinct(StringComparer.OrdinalIgnoreCase),
                limit: 2);

    private static string DisplayArtifactPath(string path)
        => (path ?? string.Empty).Replace("operator96", "custom-value-seed", StringComparison.OrdinalIgnoreCase);

    private static string NoiseBadge(IReadOnlyList<AggregateObservation> records)
    {
        if (records.Count == 0)
        {
            return "No low-noise receipt";
        }

        return records.All(static record =>
            string.Equals(record.Status, "ok", StringComparison.OrdinalIgnoreCase)
            && string.Equals(record.HostNoise, "ok", StringComparison.OrdinalIgnoreCase))
            ? "Low-noise VM receipt"
            : "Noisy/debug only";
    }

    private static string NextAction(JsonElement record, IReadOnlyList<AggregateObservation> aggregateRecords)
    {
        var bucket = Text(record, "app_surface_bucket");
        var missing = MissingChecklist(record);

        if (bucket.Equals("needs_low_noise_rerun", StringComparison.OrdinalIgnoreCase)
            || missing.Any(static item => item.Contains("low_noise", StringComparison.OrdinalIgnoreCase))
            || HasNoisyOrNonOkReceipt(aggregateRecords))
        {
            return "Next: rerun in the certified low-noise VM lane before using this as verdict or app-card proof.";
        }

        if (bucket.Equals("blocked_by_safety", StringComparison.OrdinalIgnoreCase))
        {
            return "Next: keep research-only and open a safety/hold review before any mutating app-card path.";
        }

        if (missing.Any(static item => item.Contains("rollback", StringComparison.OrdinalIgnoreCase)))
        {
            return "Next: run one-value snapshot-safe VM experiment and capture apply, verify, and rollback proof.";
        }

        if (missing.Any(static item => item.Contains("default", StringComparison.OrdinalIgnoreCase)
                                       || item.Contains("current", StringComparison.OrdinalIgnoreCase)
                                       || item.Contains("target", StringComparison.OrdinalIgnoreCase)
                                       || item.Contains("app_write", StringComparison.OrdinalIgnoreCase)))
        {
            return "Next: fill current/default/target/app-write proof before app-card review.";
        }

        if (missing.Any(static item => item.Contains("bounded", StringComparison.OrdinalIgnoreCase)
                                       || item.Contains("positive", StringComparison.OrdinalIgnoreCase)
                                       || item.Contains("claim", StringComparison.OrdinalIgnoreCase)))
        {
            return "Next: keep as research observation until bounded evidence exists; do not claim optimization or performance gain.";
        }

        if (IsReadyForAppCardReview(record))
        {
            return "Next: ready for bounded app-card review; still needs human copy review before shipping to end users.";
        }

        if (aggregateRecords.Count == 0)
        {
            return "Next: attach a certified VM receipt or leave this as catalog-only research context.";
        }

        return "Next: keep in Contributor Lab and decide whether another value, source lane, or bounded claim closes the gap.";
    }

    private static string AppCardReadinessSummary(JsonElement record)
    {
        if (IsReadyForAppCardReview(record))
        {
            return "Ready for bounded app-card review; not auto-shipped to end users.";
        }

        return Text(record, "app_surface_bucket").ToLowerInvariant() switch
        {
            "blocked_by_gate" => "Not app-card ready: promotion gates still block user-facing card review.",
            "needs_low_noise_rerun" => "Not app-card ready: low-noise rerun is required before claims or cards.",
            "blocked_by_safety" => "Not app-card ready: safety review or hold/reject decision is required.",
            "not_app_surface_ready" => "Research-only: keep in Contributor Lab until default/current/target/rollback proof and bounded claim pass.",
            _ => "Research-only until app-card contract explicitly passes."
        };
    }

    private static string MissingProofSummary(JsonElement record)
    {
        var missing = MissingChecklist(record);
        return missing.Count == 0
            ? "No missing proof gates listed."
            : "Missing proof: " + string.Join(", ", missing);
    }

    private static string RunTierAction(IReadOnlyList<AggregateObservation> records)
    {
        if (records.Count == 0)
        {
            return "No run-tier receipt yet; export commands only until a certified VM run lands.";
        }

        return HasNoisyOrNonOkReceipt(records)
            ? "Noisy/debug receipt: rerun before any claim, verdict upgrade, or card review."
            : "Certified-low-noise receipt: usable as research evidence, not an end-user card unless gates pass.";
    }

    private static bool IsReadyForAppCardReview(JsonElement record)
        => Bool(record, "normal_app_card_allowed") || Bool(record, "app_surface_ready");

    private static IReadOnlyList<string> MissingChecklist(JsonElement record)
        => Strings(Object(record, "promotion_checklist"), "missing");

    private static bool HasNoisyOrNonOkReceipt(IReadOnlyList<AggregateObservation> records)
        => records.Any(static record =>
            !string.Equals(record.Status, "ok", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.HostNoise, "ok", StringComparison.OrdinalIgnoreCase));

    private static string AppCardBlockerSummary(JsonElement record)
    {
        var checklist = Object(record, "promotion_checklist");
        var missing = Strings(checklist, "missing");
        var reasons = Strings(record, "reasons");
        var parts = new List<string>();
        if (missing.Count > 0)
        {
            parts.Add("missing " + string.Join(", ", missing));
        }

        if (reasons.Count > 0)
        {
            parts.Add("reasons " + string.Join(", ", reasons));
        }

        return parts.Count == 0
            ? "No app-card blockers listed; review bounded claim policy before surfacing."
            : string.Join("; ", parts);
    }

    private static string JoinLimited(IEnumerable<string> values, int limit = 4)
    {
        var list = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
        if (list.Count <= limit)
        {
            return string.Join("; ", list);
        }

        return string.Join("; ", list.Take(limit)) + $"; +{list.Count - limit} more";
    }

    private static string DefaultSummary(JsonElement matrixRecord, JsonElement reviewRecord)
    {
        var status = Text(matrixRecord, "default_status");
        if (string.IsNullOrWhiteSpace(status))
        {
            status = Text(reviewRecord, "default_status");
        }

        var value = Text(matrixRecord, "default_value");
        return string.IsNullOrWhiteSpace(value) ? status : $"{status}: {value}";
    }

    private static string Counts(JsonElement record, string property)
    {
        var item = Object(record, property);
        if (item.ValueKind != JsonValueKind.Object)
        {
            return "unknown";
        }

        return string.Join(", ", item.EnumerateObject().Select(prop => $"{prop.Name}={prop.Value}"));
    }

    private static string SurfaceDestination(JsonElement record)
    {
        var destination = Text(record, "surface_destination");
        if (!string.IsNullOrWhiteSpace(destination))
        {
            return destination;
        }

        return Bool(record, "app_surface_ready") ? "normal-app-card-review" : "contributor-lab-research-only";
    }

    private static string PromotionChecklist(JsonElement record)
    {
        var checklist = Object(record, "promotion_checklist");
        var missing = Strings(checklist, "missing");
        if (missing.Count > 0)
        {
            return "missing: " + string.Join(", ", missing);
        }

        return Bool(record, "normal_app_card_allowed") || Bool(record, "app_surface_ready")
            ? "complete for bounded card review"
            : "research-only until app-card gates pass";
    }

    private static string ClaimBoundary(JsonElement record)
    {
        var boundary = Text(record, "claim_boundary");
        return string.IsNullOrWhiteSpace(boundary)
            ? "No user-facing performance claim; inspect evidence before promotion."
            : boundary;
    }

    private static JsonDocument? ReadJson(string repoRoot, string relativePath)
    {
        try
        {
            var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) ? JsonDocument.Parse(File.ReadAllText(path)) : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveRepoRoot(string? overrideRoot)
    {
        var candidates = new[]
        {
            overrideRoot,
            Environment.GetEnvironmentVariable("REGPROBE_REPO_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
        };

        foreach (var candidate in candidates.Where(static candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var current = new DirectoryInfo(Path.GetFullPath(candidate!));
            for (var depth = 0; depth < 10 && current is not null; depth++)
            {
                if (IsRepoRoot(current.FullName))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        return Path.GetFullPath(overrideRoot ?? Environment.CurrentDirectory);
    }

    private static bool IsRepoRoot(string path)
        => File.Exists(Path.Combine(path, "RegProbe.sln"))
           && Directory.Exists(Path.Combine(path, "registry-research-framework"));

    private static bool IsExecutableOnPath(string executable)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, executable + extension);
                if (File.Exists(candidate))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static JsonElement Object(JsonDocument? document, string property)
        => document is null ? default : Object(document.RootElement, property);

    private static JsonElement Object(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
            ? value
            : default;

    private static IReadOnlyList<JsonElement> Array(JsonElement? element, string property)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return System.Array.Empty<JsonElement>();
        }

        return value.EnumerateArray().ToList();
    }

    private static int Int(JsonDocument? document, string property)
        => Int(document?.RootElement, property);

    private static int Int(JsonElement? element, string property)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(property, out var value))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var number) => number,
            _ => 0
        };
    }

    private static string Text(JsonDocument? document, string property)
        => Text(document?.RootElement, property);

    private static string Text(JsonElement? element, string property)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(property, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => value.ToString()
        };
    }

    private static bool Bool(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static IReadOnlyList<string> Strings(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return System.Array.Empty<string>();
        }

        return value.EnumerateArray().Select(item => item.ToString()).Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
