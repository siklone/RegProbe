namespace RegProbe.Application.Services;

internal static class TweakPromotionGateMutationEvaluator
{
    public static TweakMutationDecision EvaluateApply(
        TweakPromotionGateEntry entry,
        bool overrideRequested,
        string? overrideReason,
        bool contributorModeEnabled)
    {
        var isHold = IsHoldState(entry.PromotionState);
        var allowedWithoutOverride =
            !isHold
            && (string.Equals(entry.TweakOrigin, "legacy-curated", StringComparison.OrdinalIgnoreCase)
                || entry.TweakIngestAllowed);
        var overrideUsed =
            !isHold
            && !allowedWithoutOverride
            && overrideRequested
            && contributorModeEnabled
            && entry.DebugOverrideAllowed;

        return new TweakMutationDecision
        {
            Allowed = allowedWithoutOverride || overrideUsed,
            OverrideRequested = overrideRequested,
            OverrideUsed = overrideUsed,
            OverrideReason = overrideReason?.Trim() ?? string.Empty,
            Message = allowedWithoutOverride
                ? "apply-allowed"
                : overrideUsed
                    ? "apply-override-allowed"
                    : $"promotion-state:{entry.PromotionState}",
            Entry = entry,
        };
    }

    public static TweakMutationDecision EvaluateRollback(
        TweakPromotionGateEntry entry,
        bool overrideRequested,
        string? overrideReason,
        bool contributorModeEnabled)
    {
        var decision = EvaluateApply(entry, overrideRequested, overrideReason, contributorModeEnabled);
        decision.Message = decision.Allowed ? "rollback-allowed" : decision.Message;

        if (!decision.Allowed)
        {
            return decision;
        }

        var rollback = decision.Entry.RollbackStatus;
        if (rollback is null && !IsLegacyCurated(decision.Entry))
        {
            decision.Allowed = false;
            decision.Message = "rollback-not-declared";
            return decision;
        }

        if (rollback is null)
        {
            return decision;
        }

        if (!rollback.RollbackDeclared && !rollback.RollbackExecuted && !IsLegacyCurated(decision.Entry))
        {
            decision.Allowed = false;
            decision.Message = "rollback-not-declared";
            return decision;
        }

        if (rollback.RollbackDeclared && !rollback.RollbackExecuted)
        {
            decision.Warnings.Add("rollback-declared-but-not-executed");
        }

        if (!rollback.RollbackVerified)
        {
            decision.Warnings.Add("rollback-unverified");
        }

        return decision;
    }

    private static bool IsLegacyCurated(TweakPromotionGateEntry entry)
        => string.Equals(entry.TweakOrigin, "legacy-curated", StringComparison.OrdinalIgnoreCase);

    private static bool IsHoldState(string? promotionState)
        => promotionState?.Contains("hold", StringComparison.OrdinalIgnoreCase) == true;
}
