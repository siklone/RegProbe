using System;
using System.IO;
using System.Linq;
using RegProbe.Application.Services;

namespace RegProbe.CLI;

partial class Program
{
    private static readonly string[] SupportedRegressionPackStates =
    [
        "promoted",
        "promotion-eligible",
        "revalidation-pending"
    ];

    internal static string? ValidateOverrideOptions(bool overrideRequested, string? overrideReason)
    {
        overrideReason = NormalizeOptionalCliText(overrideReason);
        return !overrideRequested && !string.IsNullOrWhiteSpace(overrideReason)
            ? "Override reason requires --override."
            : null;
    }

    internal static string NormalizeCliText(string? value) => value?.Trim() ?? string.Empty;

    internal static string? NormalizeOptionalCliText(string? value)
    {
        var normalized = NormalizeCliText(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    internal static string? ValidateRequiredCliText(string? value, string argumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentName);
        return string.IsNullOrWhiteSpace(value)
            ? $"{argumentName} must not be empty."
            : null;
    }

    internal static string? ValidateExistingFilePath(string? value, string argumentName)
    {
        var textValidationError = ValidateRequiredCliText(value, argumentName);
        if (!string.IsNullOrWhiteSpace(textValidationError))
        {
            return textValidationError;
        }

        var normalizedPath = Path.GetFullPath(NormalizeCliText(value));
        return File.Exists(normalizedPath)
            ? null
            : $"{argumentName} was not found: {normalizedPath}";
    }

    internal static string? ValidateOutputFilePath(string? value, string argumentName)
    {
        var textValidationError = ValidateRequiredCliText(value, argumentName);
        if (!string.IsNullOrWhiteSpace(textValidationError))
        {
            return textValidationError;
        }

        var normalizedPath = Path.GetFullPath(NormalizeCliText(value));
        if (Directory.Exists(normalizedPath))
        {
            return $"{argumentName} must be a file path, not a directory: {normalizedPath}";
        }

        var parentDirectory = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrWhiteSpace(parentDirectory) && File.Exists(parentDirectory))
        {
            return $"{argumentName} parent path is not a directory: {parentDirectory}";
        }

        return null;
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

    internal static bool HasExplicitOptionToken(IEnumerable<string> tokens, string optionName)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionName);

        return tokens.Any(token =>
            string.Equals(token, optionName, StringComparison.Ordinal)
            || token.StartsWith($"{optionName}=", StringComparison.Ordinal));
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
        candidateId = NormalizeOptionalCliText(candidateId);
        var normalizedStates = states
            .Select(NormalizeCliText)
            .Where(state => !string.IsNullOrWhiteSpace(state))
            .ToArray();

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

        if (!allCandidates && normalizedStates.Length > 0)
        {
            return "--state requires --all.";
        }

        if (!allCandidates && limit.HasValue)
        {
            return "--limit requires --all.";
        }

        var invalidState = normalizedStates
            .FirstOrDefault(state => !SupportedRegressionPackStates.Contains(state, StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(invalidState))
        {
            return $"Unsupported --state value '{invalidState}'. Expected one of: {string.Join(", ", SupportedRegressionPackStates)}.";
        }

        return null;
    }

    internal static string? ValidateNormalizeRegistryTraceOptions(
        string? format,
        string? input,
        string? output,
        string? runId)
    {
        format = NormalizeCliText(format);
        input = NormalizeCliText(input);
        output = NormalizeCliText(output);
        runId = NormalizeCliText(runId);

        var normalizedFormat = format.ToLowerInvariant();
        if (normalizedFormat is not ("etl" or "procmon-csv"))
        {
            return $"Unsupported normalization format: {format}";
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            return "--run-id must not be empty.";
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return "--input must not be empty.";
        }

        var fullInputPath = Path.GetFullPath(input);
        if (!File.Exists(fullInputPath))
        {
            return $"Input trace path was not found: {fullInputPath}";
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            return "--output must not be empty.";
        }

        var fullOutputPath = Path.GetFullPath(output);
        if (string.Equals(fullInputPath, fullOutputPath, StringComparison.OrdinalIgnoreCase))
        {
            return "--output must differ from --input.";
        }

        return null;
    }
}
