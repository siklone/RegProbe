namespace RegProbe.App.ViewModels;

internal static class TweakOutcomePresentation
{
    public static bool HasOutcome(TweakRunOutcome outcome)
    {
        return outcome != TweakRunOutcome.None;
    }

    public static string BuildOutcomeText(TweakRunOutcome outcome) => outcome switch
    {
        TweakRunOutcome.InProgress => "Running",
        TweakRunOutcome.RolledBack => "Rolled Back",
        TweakRunOutcome.Success => "Success",
        TweakRunOutcome.Failed => "Failed",
        TweakRunOutcome.Cancelled => "Cancelled",
        TweakRunOutcome.Skipped => "Skipped",
        _ => "Idle"
    };

    public static string BuildOutcomeSummary(
        TweakRunOutcome outcome,
        string lastActionText)
    {
        return HasOutcome(outcome)
            ? $"{lastActionText} - {BuildOutcomeText(outcome)}"
            : "No runs yet";
    }
}
