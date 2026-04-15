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

        if (tweak.HasRegistryPath)
        {
            lines.Add($"Set registry target: {tweak.RegistryPath} => {targetValue}");
        }
        else
        {
            lines.Add($"Apply target state: {tweak.Name} => {targetValue}");
        }

        lines.Add(tweak.RequiresElevation
            ? "Validate elevated host isolation and request administrator approval."
            : "Validate current shell context before applying the change.");

        lines.Add($"Verify result: {currentValue} -> {targetValue}");

        var rollbackLine = tweak.RollbackSnapshotState switch
        {
            "ready" => "Rollback story: verified and ready to restore.",
            "partial" => "Rollback story: declared, but full verification is still pending.",
            _ => "Rollback story: missing or still needs stronger proof."
        };
        lines.Add(rollbackLine);

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
            $"Plan ({numberedLines.Count} steps) • {rollbackSummary}",
            exportText);
    }
}
