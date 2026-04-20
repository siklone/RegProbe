using System;
using RegProbe.Application.Services;

namespace RegProbe.CLI;

partial class Program
{
    internal static string? ValidateOverrideOptions(bool overrideRequested, string? overrideReason)
    {
        return !overrideRequested && !string.IsNullOrWhiteSpace(overrideReason)
            ? "Override reason requires --override."
            : null;
    }

    internal static string? ValidateApplyExecutionOptions(bool apply, bool noVerify, bool noRollback)
    {
        if (!apply && noVerify)
        {
            return "--no-verify requires --apply.";
        }

        if (!apply && noRollback)
        {
            return "--no-rollback requires --apply.";
        }

        return null;
    }

    internal static string? ValidateDnsSetOptions(bool apply, bool flush)
    {
        return !apply && flush
            ? "--flush requires --apply."
            : null;
    }

    internal static string? ValidateExportOptions(
        bool includeTweaks,
        bool includeTweaksSpecified,
        bool noTweaks,
        bool includeDns,
        bool includeDnsSpecified,
        bool noDns,
        bool includeSettings,
        bool includeSettingsSpecified,
        bool noSettings)
    {
        if (includeTweaksSpecified && includeTweaks && noTweaks)
        {
            return "Do not combine --include-tweaks with --no-tweaks.";
        }

        if (includeDnsSpecified && includeDns && noDns)
        {
            return "Do not combine --include-dns with --no-dns.";
        }

        if (includeSettingsSpecified && includeSettings && noSettings)
        {
            return "Do not combine --include-settings with --no-settings.";
        }

        return null;
    }

    internal static ExportOptions BuildExportOptions(
        bool includeTweaks,
        bool noTweaks,
        bool includeDns,
        bool noDns,
        bool includeSettings,
        bool noSettings)
    {
        return new ExportOptions
        {
            IncludeTweakStates = includeTweaks && !noTweaks,
            IncludeDnsSettings = includeDns && !noDns,
            IncludeAppSettings = includeSettings && !noSettings
        };
    }

    internal static string? ValidateRegressionPackArguments(
        string? candidateId,
        bool allCandidates,
        IReadOnlyCollection<string> states,
        int? limit)
    {
        if (limit.HasValue && limit.Value <= 0)
        {
            return "--limit must be a positive integer.";
        }

        if (!allCandidates && string.IsNullOrWhiteSpace(candidateId))
        {
            return "Provide <candidate-id> or use --all.";
        }

        if (allCandidates && !string.IsNullOrWhiteSpace(candidateId))
        {
            return "Provide either <candidate-id> or --all, not both.";
        }

        if (!allCandidates && states.Count > 0)
        {
            return "--state requires --all.";
        }

        if (!allCandidates && limit.HasValue)
        {
            return "--limit requires --all.";
        }

        return null;
    }
}
