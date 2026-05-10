using System;
using System.Collections.Generic;

namespace RegProbe.App.ViewModels;

internal static class TweakClaimBoundaryPresentation
{
    public static string BuildWhatWeKnowSummary(
        string friendlyDescription,
        string validatedSemanticsSummary,
        string docsSnapshotText,
        string rollbackSnapshotState,
        string rollbackStoryText)
    {
        var parts = new List<string>();
        AddPart(parts, "Claim", friendlyDescription);

        if (!string.IsNullOrWhiteSpace(validatedSemanticsSummary))
        {
            AddPart(parts, "Evidence", validatedSemanticsSummary);
        }
        else
        {
            AddPart(parts, "Evidence", docsSnapshotText);
        }

        if (string.Equals(rollbackSnapshotState, "ready", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rollbackSnapshotState, "partial", StringComparison.OrdinalIgnoreCase))
        {
            AddPart(parts, "Rollback", rollbackStoryText);
        }

        return string.Join(" ", parts);
    }

    public static string BuildWhatWeDoNotClaimSummary(
        string verdictState,
        string runtimeSnapshotState,
        string runtimeProofSummary,
        string upstreamLineageSummary,
        string publicMutationGatingReason,
        bool isMutationAllowed)
    {
        var parts = new List<string>();

        if (string.Equals(verdictState, "archived", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("Archived: retained as an audit trail, not a normal app-ready tweak.");
        }

        if (!isMutationAllowed && !string.IsNullOrWhiteSpace(publicMutationGatingReason))
        {
            AddPart(parts, "Apply gate", publicMutationGatingReason);
        }

        if (!string.Equals(runtimeSnapshotState, "ready", StringComparison.OrdinalIgnoreCase))
        {
            var runtimeSummary = string.IsNullOrWhiteSpace(runtimeProofSummary)
                ? "No benchmark, ETW/WPR trace, or current-build runtime behavior claim is implied by key/value existence alone."
                : runtimeProofSummary;
            AddPart(parts, "Runtime", runtimeSummary);
        }

        if (MentionsLineageBoundary(upstreamLineageSummary))
        {
            AddPart(parts, "Source boundary", upstreamLineageSummary);
        }

        if (parts.Count == 0)
        {
            parts.Add("No performance or benchmark result is implied beyond the proof lanes shown below.");
        }

        return string.Join(" ", parts);
    }

    private static bool MentionsLineageBoundary(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("not value semantics", StringComparison.OrdinalIgnoreCase)
            || value.Contains("naming only", StringComparison.OrdinalIgnoreCase)
            || value.Contains("discovery", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddPart(List<string> parts, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parts.Add($"{label}: {value.Trim()}");
    }
}
