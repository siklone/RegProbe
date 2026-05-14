using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
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
    string DefaultSummary,
    string CandidateValues,
    string ValidatedValues,
    string ConfidenceSummary,
    string NoiseSummary,
    string SurfaceDestination,
    string PromotionChecklist,
    string ClaimBoundary,
    string CommandHint);

public sealed record ContributorLabSnapshot(
    string RepoRoot,
    bool RepoRootFound,
    bool IsWindows,
    bool IsElevated,
    bool PythonAvailable,
    bool GitAvailable,
    bool AppReadinessOk,
    bool AppCardsOk,
    bool Operator96AggregateOk,
    bool Operator96SurfaceReviewOk,
    bool VmHealthKnown,
    bool VmHealthOk,
    bool VmSnapshotKnown,
    bool VmSnapshotOk,
    string VmSnapshotName,
    int Operator96RecordCount,
    int Operator96ReadyForAppCard,
    int Operator96BlockedByGate,
    int Operator96NotAppSurfaceReady,
    int Operator96BlockedBySafety,
    bool Operator96AggregateSurfaceBlocked,
    int Operator96NeedsLowNoiseRerun,
    int Operator96NoisyResultCount,
    int Operator96NonOkCount,
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
        && Operator96NoisyResultCount == 0
        && Operator96NonOkCount == 0
        && Operator96NeedsLowNoiseRerun == 0;
}

public static class ContributorLabCatalog
{
    public const string Operator96AggregatePath = "registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json";
    public const string Operator96SurfaceReviewPath = "registry-research-framework/audit/operator96-app-surface-review-20260510.json";
    public const string Operator96EnrichedMatrixPath = "registry-research-framework/audit/operator96-enriched-value-matrix-20260510.json";
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
        "registry-research-framework/scripts/generate_operator96_app_surface_review.py",
        "scripts/vm-kvm/vm-health-check.py",
        "scripts/vm-kvm/run-guest-registry-value-experiment.py",
        "scripts/vm-kvm/run-guest-registry-value-campaign.py",
    ];

    public static ContributorLabSnapshot Load(string? repoRootOverride = null)
    {
        var repoRoot = ResolveRepoRoot(repoRootOverride);
        var repoRootFound = IsRepoRoot(repoRoot);

        var aggregate = ReadJson(repoRoot, Operator96AggregatePath);
        var review = ReadJson(repoRoot, Operator96SurfaceReviewPath);
        var matrix = ReadJson(repoRoot, Operator96EnrichedMatrixPath);
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

        var runTier = aggregateOk && surfaceOk && appReadinessOk && appCardsOk
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

        var observations = BuildObservations(review, matrix);
        var commandPacks = BuildCommandPacks(repoRoot, runTier == "certified");

        var snapshot = new ContributorLabSnapshot(
            repoRoot,
            repoRootFound,
            OperatingSystem.IsWindows(),
            ProcessElevation.IsElevated(),
            IsExecutableOnPath("python3") || IsExecutableOnPath("python") || IsExecutableOnPath("py"),
            IsExecutableOnPath("git"),
            appReadinessOk,
            appCardsOk,
            aggregateOk,
            surfaceOk,
            vmHealthKnown,
            vmHealthKnown && vmHealthOk,
            vmSnapshotKnown,
            vmSnapshotKnown && vmSnapshotOk,
            string.IsNullOrWhiteSpace(vmSnapshotName) ? "clean-25h2-qga" : vmSnapshotName,
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
                "Recompute why user-supplied registry value observations stay research-only or become eligible for bounded app-card review. Uses the legacy operator96 artifact set as the current seed fixture.",
                "python3 registry-research-framework/scripts/generate_operator96_app_surface_review.py --json",
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
                "Single value VM experiment",
                "Apply one value in the disposable VM, reboot-smoke it, rollback, and abort before mutation if host noise is not clean.",
                "python3 scripts/vm-kvm/run-guest-registry-value-experiment.py --domain regprobe-win11-25h2-session --connect qemu:///session --registry-path \"HKLM\\\\SYSTEM\\\\CurrentControlSet\\\\Control\\\\Power\" --value-name MfBufferingThreshold --value-data 0 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90 --require-domain-snapshot --auto-revert-snapshot-on-boot-failure --revert-snapshot-name clean-25h2-qga --abort-on-noisy-host",
                certifiedReady ? "certified-ready" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: true),
            new(
                "Custom value tranche rerun",
                "Continue or rerun the value matrix in small snapshot-safe checkpoints; results remain research observations until app-surface gates pass.",
                "python3 scripts/vm-kvm/run-guest-registry-value-campaign.py --run --limit-experiments 10 --max-values-per-record 2 --smoke-profile gui --stage-wait-timeout 420 --reboot-wait-timeout 420 --post-reboot-delay-seconds 90",
                certifiedReady ? "certified-ready" : "certified-required",
                RequiresCertifiedVm: true,
                MutatesGuest: true),
        ];
    }

    private static IReadOnlyList<ContributorReadinessItem> BuildReadinessItems(ContributorLabSnapshot snapshot)
    {
        return
        [
            Ready("Windows host", snapshot.IsWindows, "Contributor Lab v1 is Windows-first; non-Windows hosts can still inspect artifacts."),
            Ready("Administrator token", snapshot.IsElevated, "Needed for local registry apply paths. VM-only planning can run without elevation."),
            Ready("Python available", snapshot.PythonAvailable, "Python scripts are the canonical contributor API."),
            Ready("Git available", snapshot.GitAvailable, "Required for PR workflow and artifact review."),
            Ready("Repo root", snapshot.RepoRootFound, snapshot.RepoRootFound ? snapshot.RepoRoot : "Set REGPROBE_REPO_ROOT or launch from the repo checkout."),
            Ready("App readiness", snapshot.AppReadinessOk, "Cards, rollback coverage, KVM lane health, and app smoke receipts."),
            Ready("App-card contracts", snapshot.AppCardsOk, $"{snapshot.AppCardPassCount}/{snapshot.AppCardCandidateCount} shipped cards pass."),
            Ready("Custom value low-noise", snapshot.Operator96AggregateOk, $"non_ok={snapshot.Operator96NonOkCount}, noisy={snapshot.Operator96NoisyResultCount}; legacy campaign id: operator96."),
            Ready(
                "Custom value app-surface gate",
                snapshot.Operator96SurfaceReviewOk,
                $"ready={snapshot.Operator96ReadyForAppCard}, blocked_by_gate={snapshot.Operator96BlockedByGate}, not_ready={snapshot.Operator96NotAppSurfaceReady}, safety={snapshot.Operator96BlockedBySafety}, needs_rerun={snapshot.Operator96NeedsLowNoiseRerun}."),
            Ready(
                "Custom value aggregate gate",
                !snapshot.Operator96AggregateSurfaceBlocked,
                snapshot.Operator96AggregateSurfaceBlocked
                    ? "Aggregate blockers are present; custom value experiments cannot support app-card promotion."
                    : "Aggregate blockers are clear; per-record app-card gates still decide promotion."),
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
            new("Run tier", snapshot.VerificationBadge, $"Current aggregate tier: {snapshot.RunTier}.", snapshot.ReferenceEligible ? "ok" : "warning"),
        ];
    }

    private static ContributorReadinessItem Ready(string label, bool ok, string detail)
        => new(label, ok ? "Ready" : "Needs attention", detail, ok ? "ok" : "warning");

    private static IReadOnlyList<ContributorObservation> BuildObservations(JsonDocument? review, JsonDocument? matrix)
    {
        var matrixRecords = RecordsByIndex(matrix);
        var records = Array(review?.RootElement, "records");
        var result = new List<ContributorObservation>();

        foreach (var reviewRecord in records)
        {
            var index = Int(reviewRecord, "index");
            matrixRecords.TryGetValue(index, out var matrixRecord);
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
                DefaultSummary(matrixRecord, reviewRecord),
                string.IsNullOrWhiteSpace(candidateValues) ? "none listed" : candidateValues,
                string.IsNullOrWhiteSpace(validatedValues) ? "none certified yet" : validatedValues,
                Counts(reviewRecord, "proof_confidence_counts"),
                Counts(reviewRecord, "proof_host_noise_counts"),
                SurfaceDestination(reviewRecord),
                PromotionChecklist(reviewRecord),
                ClaimBoundary(reviewRecord),
                command));
        }

        return result.OrderBy(item => item.Index).ToList();
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
}
