using System;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

internal static class TweakVerdictPresentation
{
    public const string PublicResearchGateExplanation = "Evidence pending";

    public static string BuildPublicEvidenceClassGatingReason(string evidenceClassGatingReason)
    {
        return ContributorMode.IsEnabled
            ? evidenceClassGatingReason
            : PublicResearchGateExplanation;
    }

    public static string BuildPublicMutationGatingReason(
        bool isEvidenceClassActionable,
        string publicEvidenceClassGatingReason,
        bool isMutationAllowed,
        string promotionGatingReason)
    {
        if (!isEvidenceClassActionable)
        {
            return publicEvidenceClassGatingReason;
        }

        if (isMutationAllowed)
        {
            return string.Empty;
        }

        return ContributorMode.IsEnabled
            ? promotionGatingReason
            : PublicResearchGateExplanation;
    }

    public static bool IsResearchGated(bool showInApp, bool isMutationAllowed)
    {
        return showInApp && !isMutationAllowed;
    }

    public static string BuildVerdictState(
        bool isEvidenceArchived,
        string evidenceClassActionState,
        bool showInApp,
        bool isMutationAllowed,
        bool isResearchGated,
        bool isPromotionActionable,
        bool isEvidenceClassActionable)
    {
        if (isEvidenceArchived || string.Equals(evidenceClassActionState, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return "archived";
        }

        if (!showInApp)
        {
            return "research";
        }

        if (isMutationAllowed)
        {
            return "allowed";
        }

        if (isResearchGated || !isPromotionActionable || !isEvidenceClassActionable)
        {
            return "blocked";
        }

        return "research";
    }

    public static string BuildVerdictText(string verdictState) => verdictState switch
    {
        "allowed" => "Apply allowed",
        "blocked" => "Blocked",
        "archived" => "Archived",
        _ => "Research-only"
    };

    public static string BuildCompactStateText(string verdictState) => verdictState switch
    {
        "allowed" => "Verified",
        "blocked" => "Needs review",
        "archived" => "Archived",
        _ => "Research"
    };

    public static string BuildCompactStateTone(string verdictState) => verdictState switch
    {
        "allowed" => "ok",
        "blocked" => "warning",
        "archived" => "muted",
        _ => "info"
    };

    public static string BuildVerdictSummary(
        string verdictState,
        bool rollbackVerified,
        bool isEvidenceClassActionable) => verdictState switch
    {
        "allowed" => rollbackVerified
            ? "Proof and rollback signals are strong enough for the normal apply flow."
            : "Apply is available, but the safest path is still preview, verify, and keep rollback close.",
        "blocked" => !isEvidenceClassActionable
            ? "The control surface is still being validated, so this stays visible for review instead of normal apply."
            : "This setting is visible for review, but stronger proof is still required before apply opens.",
        "archived" => "This record stays in the evidence trail so we do not rediscover the same dead end later.",
        _ => "This record is useful for research and interpretation, but it stays outside the normal apply flow."
    };

    public static string BuildResearchGateMessage(bool isMutationAllowed)
    {
        return isMutationAllowed ? string.Empty : PublicResearchGateExplanation;
    }
}
