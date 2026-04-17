using System;
using System.Collections.Generic;
using System.Linq;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

internal static class TweakItemPresentationFormatter
{
    public static string BuildConfigurationFriendlyDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var normalized = string.Join(" ", description
            .Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (normalized.Length <= 132)
        {
            return normalized;
        }

        var sentenceEnd = normalized.IndexOfAny(['.', '!', '?']);
        if (sentenceEnd is > 0 and <= 156)
        {
            return normalized[..(sentenceEnd + 1)].Trim();
        }

        var softBreak = normalized.LastIndexOf(' ', 132);
        if (softBreak < 72)
        {
            softBreak = 132;
        }

        return $"{normalized[..softBreak].Trim()}...";
    }

    public static string BuildConfigurationImpactAreaText(string impactAreaLabel) => impactAreaLabel switch
    {
        "Registry" => "Registry setting",
        "Preset" => "Preset option",
        "Service" => "Service setting",
        "Task" => "Scheduled task",
        "Settings" => "Windows setting",
        "File" => "File cleanup",
        "Command" => "Command action",
        "Composite" => "Multi-step change",
        _ => impactAreaLabel
    };

    public static string BuildEffectSummary(
        TweakRiskLevel risk,
        string category,
        bool requiresElevation,
        bool willPromptForDetect)
    {
        var segments = new List<string>
        {
            risk switch
            {
                TweakRiskLevel.Safe => "Low risk and designed to be reversible.",
                TweakRiskLevel.Advanced => "Changes Windows behavior more noticeably, so review before applying.",
                TweakRiskLevel.Risky => "Use carefully and verify the result after applying.",
                _ => "Review before applying."
            }
        };

        var scope = category.ToLowerInvariant() switch
        {
            "privacy" => "Main impact: privacy and background data collection.",
            "performance" => "Main impact: responsiveness, latency, or background load.",
            "security" => "Main impact: security posture and protection behavior.",
            "network" => "Main impact: connectivity, DNS, or transport behavior.",
            "visibility" => "Main impact: Windows UI visibility and shell behavior.",
            "power" => "Main impact: power plans, timers, or idle behavior.",
            "system" => "Main impact: Windows platform defaults and system services.",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(scope))
        {
            segments.Add(scope);
        }

        if (requiresElevation)
        {
            segments.Add("Requires administrator approval.");
        }

        if (willPromptForDetect)
        {
            segments.Add("Status check may ask for an elevated prompt on this PC.");
        }

        return string.Join(" ", segments);
    }

    public static string BuildCompactInfoLine(bool hasCompactInfoLine, string currentValue, string targetValue)
    {
        if (!hasCompactInfoLine)
        {
            return string.Empty;
        }

        var current = string.IsNullOrWhiteSpace(currentValue) ? "Unknown" : NormalizeDisplayValue(currentValue);
        var target = string.IsNullOrWhiteSpace(targetValue) ? "Optimized" : NormalizeDisplayValue(targetValue);
        return $"{current} -> {target}";
    }

    public static string BuildConfigurationCompactInfoLine(string currentValue, string targetValue)
    {
        var target = string.IsNullOrWhiteSpace(targetValue) ? "Preferred state" : NormalizeDisplayValue(targetValue);
        var current = string.IsNullOrWhiteSpace(currentValue) ? string.Empty : NormalizeDisplayValue(currentValue);

        if (string.IsNullOrWhiteSpace(current) || current.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return $"Preferred: {target}";
        }

        if (current.Equals(target, StringComparison.OrdinalIgnoreCase))
        {
            return $"Current: {current}";
        }

        return $"Current: {current}  ·  Preferred: {target}";
    }

    public static string BuildCompactInfoTooltip(
        string impactAreaLabel,
        string compactInfoLine,
        bool hasBatchSummaryLine,
        string batchSummaryLine)
    {
        var baseText = $"{impactAreaLabel}: {compactInfoLine}";
        if (hasBatchSummaryLine)
        {
            return $"{baseText}\n{batchSummaryLine}";
        }

        return baseText;
    }

    public static string BuildHelpTooltip(string description, string implications)
    {
        if (string.IsNullOrEmpty(implications))
        {
            return description;
        }

        return $"{description}\n\n{implications}";
    }

    public static string BuildImplications(TweakRiskLevel risk, string category, bool requiresElevation)
    {
        var implications = new List<string>();

        switch (risk)
        {
            case TweakRiskLevel.Safe:
                implications.Add("SAFE: Safe to apply with minimal system impact.");
                break;
            case TweakRiskLevel.Advanced:
                implications.Add("NOTE: Advanced setting that may affect some features.");
                break;
            case TweakRiskLevel.Risky:
                implications.Add("WARN: Risky setting that could impact system stability.");
                break;
        }

        switch (category.ToLowerInvariant())
        {
            case "privacy":
                implications.Add("Affects: Privacy and data collection.");
                break;
            case "performance":
                implications.Add("Affects: System responsiveness.");
                break;
            case "security":
                implications.Add("Affects: System security posture.");
                break;
            case "network":
                implications.Add("Affects: Network connectivity.");
                break;
            case "visibility":
                implications.Add("Affects: UI elements and visual features.");
                break;
        }

        if (requiresElevation)
        {
            implications.Add("Requires administrator privileges.");
        }

        return string.Join("\n", implications);
    }

    public static string BuildRowMetaText(
        string impactAreaLabel,
        string currentValue,
        string targetValue,
        bool hasDetectedState,
        string inventoryFreshnessText,
        string lastUpdatedText)
    {
        var segments = new List<string>();

        if (!string.IsNullOrWhiteSpace(impactAreaLabel))
        {
            segments.Add(impactAreaLabel);
        }

        if (!string.IsNullOrWhiteSpace(currentValue) || !string.IsNullOrWhiteSpace(targetValue))
        {
            var current = string.IsNullOrWhiteSpace(currentValue) ? "Unknown" : currentValue;
            var target = string.IsNullOrWhiteSpace(targetValue) ? "Optimized" : targetValue;
            segments.Add($"{current} -> {target}");
        }

        segments.Add(hasDetectedState ? inventoryFreshnessText : lastUpdatedText);

        return string.Join(" / ", segments.Where(segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private static string NormalizeDisplayValue(string value)
    {
        var normalized = value.Trim();
        normalized = normalized.Replace("(0x", " (0x", StringComparison.Ordinal);
        normalized = normalized.Replace("(0X", " (0X", StringComparison.Ordinal);
        return normalized;
    }
}
