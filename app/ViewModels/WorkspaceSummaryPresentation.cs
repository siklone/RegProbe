using System.Collections.Generic;
using System.Linq;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

internal sealed class WorkspaceSummaryPresentation
{
    private WorkspaceSummaryPresentation(
        string pendingLabel,
        WorkspaceSummaryMetric pending,
        WorkspaceSummaryMetric rollback,
        WorkspaceSummaryMetric elevation,
        WorkspaceSummaryMetric verification,
        string verificationStripText,
        string riskStripText,
        string pendingStripText,
        string rollbackStripText)
    {
        PendingLabel = pendingLabel;
        Pending = pending;
        Rollback = rollback;
        Elevation = elevation;
        Verification = verification;
        VerificationStripText = verificationStripText;
        RiskStripText = riskStripText;
        PendingStripText = pendingStripText;
        RollbackStripText = rollbackStripText;
    }

    public string PendingLabel { get; }

    public WorkspaceSummaryMetric Pending { get; }

    public WorkspaceSummaryMetric Rollback { get; }

    public WorkspaceSummaryMetric Elevation { get; }

    public WorkspaceSummaryMetric Verification { get; }

    public string VerificationStripText { get; }

    public string RiskStripText { get; }

    public string PendingStripText { get; }

    public string RollbackStripText { get; }

    public static WorkspaceSummaryPresentation Create(
        IEnumerable<TweakItemViewModel> visibleWorkspaceTweaks,
        bool isElevated,
        bool isMaintenanceWorkspaceSelected)
    {
        var visibleTweaks = (visibleWorkspaceTweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
        var snapshot = WorkspaceSummarySnapshot.Create(visibleTweaks, isElevated);
        var pendingLabel = isMaintenanceWorkspaceSelected ? "Pending recovery" : "Pending changes";

        return new WorkspaceSummaryPresentation(
            pendingLabel,
            snapshot.Pending,
            snapshot.Rollback,
            snapshot.Elevation,
            snapshot.Verification,
            snapshot.Verification.State == "ok" ? "Verified" : "Needs review",
            BuildRiskStripText(visibleTweaks),
            $"{snapshot.Pending.ValueText} pending",
            snapshot.Rollback.State switch
            {
                "ok" => "Rollback ready",
                "attention" => "Rollback partial",
                _ => "Rollback missing"
            });
    }

    private static string BuildRiskStripText(IReadOnlyCollection<TweakItemViewModel> visibleTweaks)
    {
        if (visibleTweaks.Any(t => t.Risk == TweakRiskLevel.Risky))
        {
            return "Mixed risk";
        }

        if (visibleTweaks.Any(t => t.Risk == TweakRiskLevel.Advanced))
        {
            return "Managed risk";
        }

        return "Low risk";
    }
}
