using System;
using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.App.ViewModels;

internal static class TweakExecutionLogFormatter
{
    public static string FormatTerminalLine(DateTime timestamp, string message)
    {
        return $"[{timestamp:HH:mm:ss}] {message}\n";
    }

    public static string FormatStatusMessage(TweakAction action, TweakStatus status)
    {
        if (action == TweakAction.Detect && status == TweakStatus.Detected)
        {
            return "Current state captured.";
        }

        return status.ToString();
    }

    public static string CoalesceMessage(TweakAction action, TweakStatus status, string message, int maxDisplayMessageLength)
    {
        return string.IsNullOrWhiteSpace(message)
            ? FormatStatusMessage(action, status)
            : TweakExecutionMessageParser.CondenseForDisplay(message, maxDisplayMessageLength);
    }

    public static string FormatStepLogLine(TweakAction action, TweakStatus status, string message, int maxDisplayMessageLength)
    {
        var details = CoalesceMessage(action, status, message, maxDisplayMessageLength);
        if (action == TweakAction.Detect &&
            details.StartsWith("Detected ", StringComparison.OrdinalIgnoreCase))
        {
            details = $"Found {details["Detected ".Length..]}";
        }

        return $"> {action}: {details}";
    }
}
