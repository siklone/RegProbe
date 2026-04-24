using System;
using System.IO;
using System.Linq;
using RegProbe.Application.Models;
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
        if (Directory.Exists(normalizedPath))
        {
            return $"{argumentName} must be a file path, not a directory: {normalizedPath}";
        }

        return File.Exists(normalizedPath)
            ? null
            : $"{argumentName} was not found: {normalizedPath}";
    }

    internal static string? ValidateExistingDirectoryPath(string? value, string argumentName)
    {
        var textValidationError = ValidateRequiredCliText(value, argumentName);
        if (!string.IsNullOrWhiteSpace(textValidationError))
        {
            return textValidationError;
        }

        var normalizedPath = Path.GetFullPath(NormalizeCliText(value));
        if (File.Exists(normalizedPath))
        {
            return $"{argumentName} must be a directory path, not a file: {normalizedPath}";
        }

        return Directory.Exists(normalizedPath)
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

    internal static string? ValidateOutputDirectoryPath(string? value, string argumentName)
    {
        var textValidationError = ValidateRequiredCliText(value, argumentName);
        if (!string.IsNullOrWhiteSpace(textValidationError))
        {
            return textValidationError;
        }

        var normalizedPath = Path.GetFullPath(NormalizeCliText(value));
        if (File.Exists(normalizedPath))
        {
            return $"{argumentName} must be a directory path, not a file: {normalizedPath}";
        }

        var parentDirectory = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrWhiteSpace(parentDirectory) && File.Exists(parentDirectory))
        {
            return $"{argumentName} parent path is not a directory: {parentDirectory}";
        }

        return null;
    }

    internal static string? ValidateKnownCategory(string? category, IEnumerable<string> knownCategories)
    {
        ArgumentNullException.ThrowIfNull(knownCategories);

        var normalizedCategory = NormalizeOptionalCliText(category);
        if (normalizedCategory is null)
        {
            return null;
        }

        var orderedCategories = knownCategories
            .Select(NormalizeOptionalCliText)
            .Where(currentCategory => currentCategory is not null)
            .Select(currentCategory => currentCategory!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(currentCategory => currentCategory, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return orderedCategories.Any(currentCategory =>
            string.Equals(currentCategory, normalizedCategory, StringComparison.OrdinalIgnoreCase))
            ? null
            : $"Unknown category filter: {normalizedCategory}. Expected one of: {string.Join(", ", orderedCategories)}.";
    }

    internal static DnsProvider? FindDnsProviderByName(IEnumerable<DnsProvider> providers, string? providerName)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var normalizedProviderName = NormalizeOptionalCliText(providerName);
        if (normalizedProviderName is null)
        {
            return null;
        }

        return providers.FirstOrDefault(provider =>
            string.Equals(provider.Name, normalizedProviderName, StringComparison.OrdinalIgnoreCase));
    }

    internal static string? ValidateKnownDnsProvider(string? providerName, IEnumerable<DnsProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var normalizedProviderName = NormalizeOptionalCliText(providerName);
        if (normalizedProviderName is null)
        {
            return null;
        }

        var orderedProviders = providers
            .Select(provider => NormalizeOptionalCliText(provider.Name))
            .Where(currentProvider => currentProvider is not null)
            .Select(currentProvider => currentProvider!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(currentProvider => currentProvider, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return FindDnsProviderByName(providers, normalizedProviderName) is not null
            ? null
            : $"Unknown DNS provider: {normalizedProviderName}. Expected one of: {string.Join(", ", orderedProviders)}.";
    }

    internal static PresetModel? FindPresetByIdentifier(IEnumerable<PresetModel> presets, string? presetIdentifier)
    {
        ArgumentNullException.ThrowIfNull(presets);
        var normalizedPresetIdentifier = NormalizeOptionalCliText(presetIdentifier);
        if (normalizedPresetIdentifier is null)
        {
            return null;
        }

        return presets.FirstOrDefault(preset =>
            string.Equals(preset.Id, normalizedPresetIdentifier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(preset.Name, normalizedPresetIdentifier, StringComparison.OrdinalIgnoreCase));
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

        var inputValidationError = ValidateExistingFilePath(input, "input");
        if (!string.IsNullOrWhiteSpace(inputValidationError))
        {
            return inputValidationError;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            return "--output must not be empty.";
        }

        var fullInputPath = Path.GetFullPath(input);
        var fullOutputPath = Path.GetFullPath(output);
        if (string.Equals(fullInputPath, fullOutputPath, StringComparison.OrdinalIgnoreCase))
        {
            return "--output must differ from --input.";
        }

        return null;
    }
}
