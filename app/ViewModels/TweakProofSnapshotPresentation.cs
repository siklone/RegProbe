using RegProbe.Core;

namespace RegProbe.App.ViewModels;

internal static class TweakProofSnapshotPresentation
{
    public static string BuildDocsSnapshotState(
        bool hasDocumentationReference,
        bool hasSemanticsEvidenceFlag,
        bool hasValidatedSemantics,
        string validatedSemanticsSource)
    {
        if (hasDocumentationReference)
        {
            return "ready";
        }

        if (hasSemanticsEvidenceFlag || hasValidatedSemantics || !string.IsNullOrWhiteSpace(validatedSemanticsSource))
        {
            return "partial";
        }

        return "missing";
    }

    public static string BuildRuntimeSnapshotState(
        bool hasRuntimeEvidenceFlag,
        bool hasRuntimeProof,
        bool needsVmValidationFlag,
        bool hasSemanticsEvidenceFlag,
        bool hasValidatedSemantics)
    {
        if (hasRuntimeEvidenceFlag || hasRuntimeProof)
        {
            return "ready";
        }

        if (needsVmValidationFlag || hasSemanticsEvidenceFlag || hasValidatedSemantics)
        {
            return "partial";
        }

        return "missing";
    }

    public static bool HasRuntimeProofSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return false;
        }

        return !summary.Contains("No runtime proof", StringComparison.OrdinalIgnoreCase)
               && !summary.Contains("needs VM validation", StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildSourceSnapshotState(
        bool hasLineageEvidenceFlag,
        bool hasUpstreamLineage,
        bool hasNohutoEvidence,
        bool hasWindowsInternalsContext,
        bool needsSourceReview,
        string provenanceSummary)
    {
        if (hasLineageEvidenceFlag || hasUpstreamLineage || hasNohutoEvidence)
        {
            return "ready";
        }

        if (hasWindowsInternalsContext || needsSourceReview || !string.IsNullOrWhiteSpace(provenanceSummary))
        {
            return "partial";
        }

        return "missing";
    }

    public static string BuildRollbackSnapshotState(
        bool rollbackVerified,
        bool rollbackDeclared,
        bool restoreStoryKnown,
        bool hasDefaultChoice,
        string rollbackFailureReason)
    {
        if (rollbackVerified)
        {
            return "ready";
        }

        if (rollbackDeclared || restoreStoryKnown || hasDefaultChoice || !string.IsNullOrWhiteSpace(rollbackFailureReason))
        {
            return "partial";
        }

        return "missing";
    }

    public static string BuildSnapshotText(string label, string state) => state switch
    {
        "ready" => $"{label} ready",
        "partial" => $"{label} partial",
        _ => $"{label} pending"
    };

    public static string BuildRiskSnapshotText(TweakRiskLevel risk, bool isMutationAllowed)
    {
        var summary = risk switch
        {
            TweakRiskLevel.Safe => "Risk: Low-risk surface with the standard preview and verify flow.",
            TweakRiskLevel.Advanced => "Risk: Higher-impact change, so preview first and verify after applying.",
            TweakRiskLevel.Risky => "Risk: High-impact change. Treat it carefully and keep recovery in view.",
            _ => "Risk: Review carefully before you apply."
        };

        if (!isMutationAllowed)
        {
            return $"{summary} It stays evidence-first until the remaining proof lands.";
        }

        return summary;
    }
}
