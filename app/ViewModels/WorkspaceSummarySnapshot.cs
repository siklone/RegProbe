using System.Collections.Generic;
using System.Linq;

namespace RegProbe.App.ViewModels;

internal readonly record struct WorkspaceSummaryMetric(string ValueText, string DetailText, string State);

internal readonly record struct WorkspaceSummarySnapshot(
    WorkspaceSummaryMetric Pending,
    WorkspaceSummaryMetric Rollback,
    WorkspaceSummaryMetric Elevation,
    WorkspaceSummaryMetric Verification)
{
    public static WorkspaceSummarySnapshot Create(IEnumerable<TweakItemViewModel> tweaks, bool isElevated)
    {
        var visibleTweaks = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
        if (visibleTweaks.Count == 0)
        {
            var emptyMetric = new WorkspaceSummaryMetric("0", "No visible items in this view.", "neutral");
            return new WorkspaceSummarySnapshot(
                emptyMetric,
                emptyMetric,
                emptyMetric,
                new WorkspaceSummaryMetric("0/0", "No visible items in this view.", "neutral"));
        }

        var total = visibleTweaks.Count;

        var pendingCount = visibleTweaks.Count(t => t.AppliedStatus == TweakAppliedStatus.NotApplied);
        var pendingMetric = new WorkspaceSummaryMetric(
            pendingCount.ToString(),
            pendingCount == 0
                ? "Everything visible already matches the target state."
                : $"{pendingCount} visible {Pluralize("item", pendingCount)} still differ from the target state.",
            pendingCount == 0 ? "ok" : "attention");

        var rollbackReady = visibleTweaks.Count(t => string.Equals(t.RollbackSnapshotState, "ready", System.StringComparison.OrdinalIgnoreCase));
        var rollbackPartial = visibleTweaks.Count(t => string.Equals(t.RollbackSnapshotState, "partial", System.StringComparison.OrdinalIgnoreCase));
        var rollbackMissing = total - rollbackReady - rollbackPartial;

        var rollbackDetail = rollbackReady == total
            ? "Rollback is verified for every visible item."
            : BuildRollbackDetail(rollbackReady, rollbackPartial, rollbackMissing);
        var rollbackState = rollbackReady == total
            ? "ok"
            : rollbackReady == 0 && rollbackPartial == 0
                ? "warning"
                : "attention";
        var rollbackMetric = new WorkspaceSummaryMetric(
            $"{rollbackReady}/{total}",
            rollbackDetail,
            rollbackState);

        var elevationRequired = visibleTweaks.Count(t => t.RequiresElevation);
        var elevationDetail = elevationRequired == 0
            ? "No admin prompt is expected in this view."
            : isElevated
                ? $"Admin context is already available for {elevationRequired} visible {Pluralize("item", elevationRequired)}."
                : $"{elevationRequired} visible {Pluralize("item", elevationRequired)} may prompt for elevation.";
        var elevationMetric = new WorkspaceSummaryMetric(
            elevationRequired.ToString(),
            elevationDetail,
            elevationRequired == 0 || isElevated ? "ok" : "attention");

        var checkedLive = visibleTweaks.Count(t => t.AppliedStatus != TweakAppliedStatus.Unknown);
        var adminScanNeeded = visibleTweaks.Count(t => t.WillPromptForDetect);
        var verificationDetail = checkedLive == total
            ? "Every visible item has a live state result."
            : checkedLive == 0
                ? adminScanNeeded > 0
                    ? "Run Detect with admin access to refresh live state here."
                    : "Run Detect to refresh live state for this view."
                : BuildVerificationDetail(checkedLive, total, adminScanNeeded);
        var verificationMetric = new WorkspaceSummaryMetric(
            $"{checkedLive}/{total}",
            verificationDetail,
            checkedLive == total
                ? "ok"
                : checkedLive == 0
                    ? "warning"
                    : "attention");

        return new WorkspaceSummarySnapshot(
            pendingMetric,
            rollbackMetric,
            elevationMetric,
            verificationMetric);
    }

    private static string BuildRollbackDetail(int rollbackReady, int rollbackPartial, int rollbackMissing)
    {
        var parts = new List<string>();
        if (rollbackReady > 0)
        {
            parts.Add($"{rollbackReady} ready");
        }

        if (rollbackPartial > 0)
        {
            parts.Add($"{rollbackPartial} partial");
        }

        if (rollbackMissing > 0)
        {
            parts.Add($"{rollbackMissing} missing");
        }

        return $"Rollback coverage: {string.Join(" · ", parts)}.";
    }

    private static string BuildVerificationDetail(int checkedLive, int total, int adminScanNeeded)
    {
        var detail = $"{checkedLive}/{total} visible items checked live";
        if (adminScanNeeded > 0)
        {
            detail += $" · {adminScanNeeded} still need admin";
        }

        return detail + ".";
    }

    private static string Pluralize(string noun, int count)
        => count == 1 ? noun : noun + "s";
}
