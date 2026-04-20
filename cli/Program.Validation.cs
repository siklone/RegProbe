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

    internal static ExportOptions BuildExportOptions(bool noTweaks, bool noDns, bool noSettings)
    {
        return new ExportOptions
        {
            IncludeTweakStates = !noTweaks,
            IncludeDnsSettings = !noDns,
            IncludeAppSettings = !noSettings
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
