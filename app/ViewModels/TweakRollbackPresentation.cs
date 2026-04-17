namespace RegProbe.App.ViewModels;

internal static class TweakRollbackPresentation
{
    public static string BuildRestoreDefaultTooltip(
        bool isMutationAllowed,
        string publicMutationGatingReason,
        string defaultChoiceLabel)
    {
        if (!isMutationAllowed)
        {
            return publicMutationGatingReason;
        }

        if (string.IsNullOrWhiteSpace(defaultChoiceLabel))
        {
            return "Restore the product's default option.";
        }

        return $"Apply '{defaultChoiceLabel}' instead of restoring your previously captured value.";
    }

    public static string BuildRollbackStoryText(
        bool rollbackVerified,
        string rollbackVerificationMethod,
        string rollbackFailureReason,
        bool rollbackDeclared,
        bool rollbackExecuted,
        bool restoreStoryKnown,
        bool hasDefaultChoice)
    {
        if (rollbackVerified)
        {
            var method = string.IsNullOrWhiteSpace(rollbackVerificationMethod)
                ? string.Empty
                : $" via {rollbackVerificationMethod}";
            return $"Rollback: Verified{method}.";
        }

        if (!string.IsNullOrWhiteSpace(rollbackFailureReason))
        {
            return $"Rollback: Restore path exists, but the last verification failed ({rollbackFailureReason}).";
        }

        if (rollbackDeclared && rollbackExecuted)
        {
            return "Rollback: Restore path executed, but verification is still pending.";
        }

        if (rollbackDeclared)
        {
            return "Rollback: Restore path is declared, but full verification is still pending.";
        }

        if (restoreStoryKnown || hasDefaultChoice)
        {
            return "Rollback: Restore story is defined, but it still needs stronger gate proof.";
        }

        return "Rollback: Restore story still needs stronger proof.";
    }

    public static string BuildConfigurationPrimaryActionTooltip(
        bool isMutationAllowed,
        string publicMutationGatingReason)
    {
        return isMutationAllowed ? "Apply this setting." : publicMutationGatingReason;
    }

    public static string BuildConfigurationRollbackActionTooltip(
        bool isMutationAllowed,
        string publicMutationGatingReason)
    {
        return isMutationAllowed
            ? "Restore the value from before you changed this setting."
            : publicMutationGatingReason;
    }

    public static string BuildPrimaryActionTooltip(
        bool isMutationAllowed,
        string publicMutationGatingReason)
    {
        return isMutationAllowed ? "Run this action." : publicMutationGatingReason;
    }

    public static string BuildRollbackActionTooltip(
        bool isMutationAllowed,
        string publicMutationGatingReason)
    {
        return isMutationAllowed
            ? "Restore the previous value captured before you ran this action."
            : publicMutationGatingReason;
    }
}
