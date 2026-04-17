using System;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

internal static class TweakSurfacePresentation
{
    public const string ElevationBadgeText = "Admin";

    public const string ActionsHelpTooltip =
        "Detect: Reads current state (no changes)\n" +
        "Preview: Dry run (no changes)\n" +
        "Apply: Detect -> Apply -> Verify (Rollback on failure)\n" +
        "Verify: Confirms current state matches desired\n" +
        "Restore Previous: Puts back the last state captured before Apply\n" +
        "Restore Default: Applies the product's default option when the tweak defines one";

    public static string BuildRiskBadgeText(TweakRiskLevel risk) => risk switch
    {
        TweakRiskLevel.Safe => "SAFE",
        TweakRiskLevel.Advanced => "ADVANCED",
        TweakRiskLevel.Risky => "RISKY",
        _ => "SAFE"
    };

    public static string BuildRepairsRiskHint(TweakRiskLevel risk) => risk switch
    {
        TweakRiskLevel.Advanced => "Advanced repair",
        TweakRiskLevel.Risky => "Risky repair",
        _ => string.Empty
    };

    public static string BuildElevationTooltip(bool isElevated)
    {
        return isElevated
            ? "Admin required. Runs via ElevatedHost."
            : "Admin required. You'll get a UAC prompt and it runs via ElevatedHost.";
    }

    public static string BuildElevationWarningText(bool willPromptForElevation)
    {
        return willPromptForElevation
            ? "Requires elevation. Approve the UAC prompt to continue."
            : string.Empty;
    }

    public static string BuildRepairsActionButtonText(string actionButtonText)
    {
        return string.Equals(actionButtonText, "Apply", StringComparison.Ordinal)
            ? "Run"
            : actionButtonText;
    }

    public static string BuildScopeFilterKey(
        string registryPath,
        bool requiresElevation)
    {
        if (registryPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
        {
            return "user";
        }

        if (registryPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase) || requiresElevation)
        {
            return "machine";
        }

        return string.IsNullOrWhiteSpace(registryPath) ? "mixed" : "mixed";
    }

    public static string BuildScopeDisplayText(string scopeFilterKey) => scopeFilterKey switch
    {
        "user" => "User",
        "machine" => "Machine",
        _ => "Mixed"
    };
}
