using System;
using System.Collections.Generic;
using System.Linq;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

internal sealed class TweakChoiceStateSnapshot
{
    public TweakChoiceOption? SelectedOption { get; set; }

    public string? TargetValue { get; set; }

    public string? CurrentValue { get; set; }

    public TweakAppliedStatus? AppliedStatus { get; set; }

    public string? StatusMessage { get; set; }
}

internal static class TweakChoiceStateCoordinator
{
    public static IReadOnlyList<TweakChoiceOption> BuildOptions(IChoiceTweak choiceTweak)
    {
        return choiceTweak.Choices
            .Select(choice => new TweakChoiceOption(choice.Key, choice.Label, choice.Description))
            .ToArray();
    }

    public static TweakChoiceOption? ResolveSelectedOption(
        IEnumerable<TweakChoiceOption> choiceOptions,
        string? selectedChoiceKey)
    {
        if (string.IsNullOrWhiteSpace(selectedChoiceKey))
        {
            return null;
        }

        return choiceOptions.FirstOrDefault(
            option => option.Key.Equals(selectedChoiceKey, StringComparison.OrdinalIgnoreCase));
    }

    public static TweakChoiceOption? ResolveOptionForTargetValue(
        IEnumerable<TweakChoiceOption> choiceOptions,
        string? targetValue)
    {
        if (string.IsNullOrWhiteSpace(targetValue))
        {
            return null;
        }

        return choiceOptions.FirstOrDefault(
            option => option.Label.Equals(targetValue, StringComparison.OrdinalIgnoreCase));
    }

    public static TweakChoiceOption? ResolveDefaultOption(
        IChoiceTweak choiceTweak,
        IEnumerable<TweakChoiceOption> choiceOptions)
    {
        if (string.IsNullOrWhiteSpace(choiceTweak.DefaultChoiceKey))
        {
            return null;
        }

        return ResolveSelectedOption(choiceOptions, choiceTweak.DefaultChoiceKey);
    }

    public static TweakChoiceStateSnapshot BuildSelectionSnapshot(
        TweakChoiceOption selectedOption,
        string? matchedChoiceKey,
        string? matchedChoiceLabel)
    {
        return new TweakChoiceStateSnapshot
        {
            SelectedOption = selectedOption,
            TargetValue = selectedOption.Label,
            CurrentValue = string.IsNullOrWhiteSpace(matchedChoiceLabel) ? null : matchedChoiceLabel,
            AppliedStatus = string.IsNullOrWhiteSpace(matchedChoiceKey)
                ? null
                : string.Equals(matchedChoiceKey, selectedOption.Key, StringComparison.OrdinalIgnoreCase)
                    ? TweakAppliedStatus.Applied
                    : TweakAppliedStatus.NotApplied,
            StatusMessage = $"Selected '{selectedOption.Label}'. Click Apply to use it."
        };
    }

    public static TweakChoiceStateSnapshot BuildSyncSnapshot(
        IChoiceTweak choiceTweak,
        TweakChoiceOption? selectedOption,
        bool hasDetectedState,
        bool updateAppliedStatus)
    {
        var snapshot = new TweakChoiceStateSnapshot
        {
            SelectedOption = selectedOption,
            TargetValue = selectedOption?.Label
        };

        if (!string.IsNullOrWhiteSpace(choiceTweak.MatchedChoiceLabel))
        {
            snapshot.CurrentValue = choiceTweak.MatchedChoiceLabel;
        }
        else if (hasDetectedState)
        {
            snapshot.CurrentValue = "Custom / Mixed";
        }

        if (!updateAppliedStatus)
        {
            return snapshot;
        }

        snapshot.AppliedStatus = string.IsNullOrWhiteSpace(choiceTweak.MatchedChoiceKey)
            ? TweakAppliedStatus.NotApplied
            : string.Equals(choiceTweak.MatchedChoiceKey, choiceTweak.SelectedChoiceKey, StringComparison.OrdinalIgnoreCase)
                ? TweakAppliedStatus.Applied
                : TweakAppliedStatus.NotApplied;

        return snapshot;
    }
}
