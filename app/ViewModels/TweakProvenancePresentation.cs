namespace RegProbe.App.ViewModels;

internal static class TweakProvenancePresentation
{
    public static bool HasProvenance(
        bool hasNohutoEvidence,
        bool hasWindowsInternalsContext,
        bool needsSourceReview,
        string provenanceSummary)
    {
        return hasNohutoEvidence ||
            hasWindowsInternalsContext ||
            needsSourceReview ||
            !string.IsNullOrWhiteSpace(provenanceSummary);
    }

    public static string BuildStatusText(
        bool hasNohutoEvidence,
        bool hasWindowsInternalsContext,
        bool needsSourceReview)
    {
        if (hasNohutoEvidence && hasWindowsInternalsContext)
        {
            return "Dump source + Internals";
        }

        if (hasNohutoEvidence)
        {
            return "Dump source";
        }

        if (hasWindowsInternalsContext)
        {
            return "Internals";
        }

        return needsSourceReview ? "Needs review" : "No source links";
    }
}
