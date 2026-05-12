using System;
using System.Collections.Generic;

namespace RegProbe.App.ViewModels;

public sealed class TweakExecutionPlanSnapshot
{
    private TweakExecutionPlanSnapshot(
        IReadOnlyList<string> lines,
        string collapsedSummary,
        string exportText)
    {
        Lines = lines;
        CollapsedSummary = collapsedSummary;
        ExportText = exportText;
    }

    public static TweakExecutionPlanSnapshot Empty { get; } = new(
        Array.Empty<string>(),
        "Plan unavailable",
        string.Empty);

    public IReadOnlyList<string> Lines { get; }

    public string CollapsedSummary { get; }

    public string ExportText { get; }

    public static TweakExecutionPlanSnapshot Create(TweakItemViewModel? tweak)
    {
        if (tweak is null)
        {
            return Empty;
        }

        var lines = new List<string>();
        var targetValue = string.IsNullOrWhiteSpace(tweak.TargetValue) ? "preferred state" : tweak.TargetValue;
        var currentValue = string.IsNullOrWhiteSpace(tweak.CurrentValue) ? "Unknown" : tweak.CurrentValue;
        var defaultValue = BuildDefaultValueLine(tweak);

        lines.Add($"Current system value: {currentValue}");
        lines.Add($"Known/default value: {defaultValue}");
        lines.Add($"Target value RegProbe will apply: {targetValue}");
        lines.Add(tweak.HasRegistryPath
            ? $"Write target: {tweak.RegistryPath} => {targetValue}"
            : $"Apply target state: {tweak.Name} => {targetValue}");

        lines.Add(tweak.RequiresElevation
            ? "Validate elevated host isolation and request administrator approval."
            : "Validate current shell context before applying the change.");

        lines.Add($"Verify result after apply: current value should become {targetValue}.");

        var rollbackLine = tweak.RollbackSnapshotState switch
        {
            "ready" => BuildRollbackLine(tweak, "verified and ready"),
            "partial" => BuildRollbackLine(tweak, "declared, but full verification is still pending"),
            _ => BuildRollbackLine(tweak, "missing or still needs stronger proof")
        };
        lines.Add($"Rollback behavior: {rollbackLine}");

        var rollbackSummary = tweak.RollbackSnapshotState switch
        {
            "ready" => "Rollback ready",
            "partial" => "Rollback partial",
            _ => "Rollback missing"
        };

        var numberedLines = new List<string>(lines.Count);
        for (var index = 0; index < lines.Count; index++)
        {
            numberedLines.Add($"[{index + 1}] {lines[index]}");
        }

        var exportText = string.Join(Environment.NewLine, numberedLines);
        return new TweakExecutionPlanSnapshot(
            numberedLines,
            $"Apply review ({numberedLines.Count} checks) • {rollbackSummary}",
            exportText);
    }

    private static string BuildDefaultValueLine(TweakItemViewModel tweak)
    {
        if (tweak.HasDefaultChoice)
        {
            return string.IsNullOrWhiteSpace(tweak.DefaultChoiceLabel)
                ? "Built-in default option is available."
                : tweak.DefaultChoiceLabel;
        }

        return "No known/default value is published on this card; restore uses the captured previous state.";
    }

    private static string BuildRollbackLine(TweakItemViewModel tweak, string state)
    {
        if (!string.IsNullOrWhiteSpace(tweak.RollbackStoryText))
        {
            return $"{state}. {tweak.RollbackStoryText}";
        }

        return state;
    }
}
