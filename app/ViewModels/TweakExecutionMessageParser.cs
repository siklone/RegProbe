using System;
using System.Collections.Generic;
using System.IO;

namespace RegProbe.App.ViewModels;

internal static class TweakExecutionMessageParser
{
    private static readonly (string Marker, string Title)[] BatchMarkers =
    {
        ("Services:", "Services"),
        ("Tasks:", "Tasks"),
        ("Entries:", "Registry Values"),
        ("Values:", "Registry Values")
    };

    private static readonly string[] SummaryMarkers =
    {
        "\nEntries:",
        "\nValues:",
        "\nServices:",
        "\nTasks:"
    };

    private static readonly string[] CurrentStateCutoffs =
    {
        "Details",
        "Services",
        "Tasks",
        "Entries",
        "Values"
    };

    public static string CondenseForDisplay(string message, int maxLength)
    {
        var trimmed = message.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        foreach (var marker in SummaryMarkers)
        {
            var markerIndex = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex > 0)
            {
                var headline = trimmed[..markerIndex].Trim();
                if (headline.Length > 0)
                {
                    return headline;
                }
            }
        }

        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return $"{trimmed[..maxLength].TrimEnd()}...";
    }

    public static bool TryExtractCurrentValue(string message, out string value)
    {
        value = string.Empty;

        if (message.Contains("Value not set", StringComparison.OrdinalIgnoreCase))
        {
            value = "Not set";
            return true;
        }

        if (TryExtractAfterPrefix(message, "Current value is ", out var directValue))
        {
            value = directValue.TrimEnd('.');
            return true;
        }

        if (!TryExtractAfterPrefix(message, "Current state:", out var state))
        {
            return false;
        }

        var trimmed = state.Trim();
        var newlineIndex = trimmed.IndexOfAny(new[] { '\r', '\n' });
        if (newlineIndex >= 0)
        {
            trimmed = trimmed[..newlineIndex];
        }

        foreach (var cutoff in CurrentStateCutoffs)
        {
            var cutoffIndex = trimmed.IndexOf(cutoff, StringComparison.OrdinalIgnoreCase);
            if (cutoffIndex >= 0)
            {
                trimmed = trimmed[..cutoffIndex];
            }
        }

        var periodIndex = trimmed.IndexOf('.');
        if (periodIndex >= 0)
        {
            trimmed = trimmed[..periodIndex];
        }

        value = trimmed.Trim();
        return value.Length > 0;
    }

    public static bool TryParseBatchDetails(string message, int maxLines, out TweakBatchDetailsSnapshot snapshot)
    {
        foreach (var (marker, markerTitle) in BatchMarkers)
        {
            var index = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var start = index + marker.Length;
            if (start >= message.Length)
            {
                continue;
            }

            var detailText = message[start..];
            var parsedLines = ExtractBatchLines(detailText, maxLines, out var totalLineCount);

            if (parsedLines.Count == 0)
            {
                continue;
            }

            var omittedLineCount = Math.Max(0, totalLineCount - parsedLines.Count);
            snapshot = new TweakBatchDetailsSnapshot(
                markerTitle,
                parsedLines,
                omittedLineCount,
                BuildBatchSummary(markerTitle, parsedLines, omittedLineCount));
            return true;
        }

        snapshot = TweakBatchDetailsSnapshot.Empty;
        return false;
    }

    private static bool TryExtractAfterPrefix(string message, string prefix, out string value)
    {
        value = string.Empty;
        var index = message.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        value = message[(index + prefix.Length)..];
        return true;
    }

    private static List<string> ExtractBatchLines(string detailText, int maxLines, out int totalLineCount)
    {
        totalLineCount = 0;
        var result = new List<string>(Math.Min(maxLines, 16));

        using var reader = new StringReader(detailText);
        while (reader.ReadLine() is { } rawLine)
        {
            var trimmed = rawLine.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            totalLineCount++;
            if (result.Count >= maxLines)
            {
                continue;
            }

            result.Add(trimmed.StartsWith("-", StringComparison.Ordinal) ? trimmed : $"- {trimmed}");
        }

        return result;
    }

    private static string BuildBatchSummary(string title, IReadOnlyList<string> lines, int omittedLineCount)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var matched = 0;
        var missing = 0;
        var mismatched = 0;
        var errors = 0;
        var unknown = 0;

        foreach (var line in lines)
        {
            var lower = line.ToLowerInvariant();
            if (lower.Contains("missing"))
            {
                missing++;
                continue;
            }

            if (lower.Contains("error"))
            {
                errors++;
                continue;
            }

            if (lower.Contains("unknown"))
            {
                unknown++;
                continue;
            }

            if (title.Equals("Tasks", StringComparison.OrdinalIgnoreCase))
            {
                if (lower.Contains("disabled"))
                {
                    matched++;
                }
                else if (lower.Contains("enabled"))
                {
                    mismatched++;
                }
                else
                {
                    unknown++;
                }

                continue;
            }

            if (TryEvaluateArrowMatch(line, out var isMatch))
            {
                if (isMatch)
                {
                    matched++;
                }
                else
                {
                    mismatched++;
                }
            }
            else
            {
                unknown++;
            }
        }

        var parts = new List<string>
        {
            $"{matched} matched",
            $"{missing} missing"
        };

        if (mismatched > 0)
        {
            parts.Add($"{mismatched} mismatched");
        }

        if (errors > 0)
        {
            parts.Add($"{errors} error{(errors == 1 ? string.Empty : "s")}");
        }

        if (unknown > 0)
        {
            parts.Add($"{unknown} unknown");
        }

        if (omittedLineCount > 0)
        {
            parts.Add($"{omittedLineCount} more hidden");
        }

        return string.Join(" / ", parts);
    }

    private static bool TryEvaluateArrowMatch(string line, out bool isMatch)
    {
        isMatch = false;

        var arrowIndex = line.IndexOf("->", StringComparison.Ordinal);
        if (arrowIndex < 0)
        {
            return false;
        }

        var colonIndex = line.IndexOf(':');
        var currentStart = colonIndex >= 0 ? colonIndex + 1 : 0;
        if (currentStart >= arrowIndex)
        {
            return false;
        }

        var current = line[currentStart..arrowIndex].Trim();
        var target = line[(arrowIndex + 2)..].Trim();
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        var currentValue = current.Split('(')[0].Trim();
        var targetValue = target.Split('(')[0].Trim();
        if (string.IsNullOrWhiteSpace(currentValue) || string.IsNullOrWhiteSpace(targetValue))
        {
            return false;
        }

        isMatch = currentValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase)
            || currentValue.Contains(targetValue, StringComparison.OrdinalIgnoreCase);
        return true;
    }
}

internal readonly record struct TweakBatchDetailsSnapshot(
    string Title,
    IReadOnlyList<string> Lines,
    int OmittedLineCount,
    string Summary)
{
    public static TweakBatchDetailsSnapshot Empty => new(string.Empty, Array.Empty<string>(), 0, string.Empty);
}
