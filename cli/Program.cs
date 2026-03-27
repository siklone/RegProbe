using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure.Elevation;

namespace RegProbe.CLI;

/// <summary>
/// RegProbe Command Line Interface.
/// Provides automation capabilities for system optimization.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("RegProbe CLI - System optimization tool")
        {
            Name = "winopt"
        };

        // Add subcommands
        rootCommand.AddCommand(CreateTweakCommand());
        rootCommand.AddCommand(CreatePresetCommand());
        rootCommand.AddCommand(CreateDnsCommand());
        rootCommand.AddCommand(CreateInfoCommand());
        rootCommand.AddCommand(CreateExportCommand());

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Tweak management commands.
    /// </summary>
    static Command CreateTweakCommand()
    {
        var tweakCommand = new Command("tweak", "Manage system tweaks");

        // List tweaks
        var listCommand = new Command("list", "List all available tweaks");
        var categoryOption = new Option<string?>("--category", "Filter by category");
        var riskOption = new Option<string?>("--risk", "Filter by risk: safe, advanced, risky");
        var requiresAdminOption = new Option<bool>("--requires-admin", "Only list tweaks requiring elevation");
        var verboseOption = new Option<bool>("--verbose", "Include descriptions");
        listCommand.AddOption(categoryOption);
        listCommand.AddOption(riskOption);
        listCommand.AddOption(requiresAdminOption);
        listCommand.AddOption(verboseOption);
        listCommand.SetHandler(context =>
        {
            var category = context.ParseResult.GetValueForOption(categoryOption);
            var risk = context.ParseResult.GetValueForOption(riskOption);
            var requiresAdmin = context.ParseResult.GetValueForOption(requiresAdminOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var catalog = new TweakCatalogService();
            var entries = catalog.GetAll().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                entries = entries.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(risk))
            {
                if (!TryParseRisk(risk, out var riskLevel))
                {
                    Console.WriteLine($"Unknown risk filter: {risk}");
                    context.ExitCode = 1;
                    return;
                }

                entries = entries.Where(e => e.Tweak.Risk == riskLevel);
            }

            if (requiresAdmin)
            {
                entries = entries.Where(e => e.Tweak.RequiresElevation);
            }

            var grouped = entries
                .OrderBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Tweak.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .GroupBy(e => e.Category, StringComparer.OrdinalIgnoreCase);

            var any = false;
            foreach (var group in grouped)
            {
                any = true;
                Console.WriteLine($"Category: {group.Key}");
                foreach (var entry in group)
                {
                    var adminTag = entry.Tweak.RequiresElevation ? "admin" : "user";
                    Console.WriteLine($"  {entry.Tweak.Id} [{entry.Tweak.Risk}] ({adminTag}) - {entry.Tweak.Name}");
                    if (verbose && !string.IsNullOrWhiteSpace(entry.Tweak.Description))
                    {
                        Console.WriteLine($"    {entry.Tweak.Description}");
                    }
                }
            }

            if (!any)
            {
                Console.WriteLine("No tweaks matched the filter.");
            }

            context.ExitCode = 0;
        });
        tweakCommand.AddCommand(listCommand);

        // Apply tweak
        var applyCommand = new Command("apply", "Apply a tweak (default: dry-run)");
        var tweakIdArg = new Argument<string>("tweak-id", "ID of the tweak to apply");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        var noVerifyOption = new Option<bool>("--no-verify", "Skip verify step after apply");
        var noRollbackOption = new Option<bool>("--no-rollback", "Do not rollback on failure");
        applyCommand.AddArgument(tweakIdArg);
        applyCommand.AddOption(applyOption);
        applyCommand.AddOption(noVerifyOption);
        applyCommand.AddOption(noRollbackOption);
        applyCommand.SetHandler(async context =>
        {
            var tweakId = context.ParseResult.GetValueForArgument(tweakIdArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);
            var noVerify = context.ParseResult.GetValueForOption(noVerifyOption);
            var noRollback = context.ParseResult.GetValueForOption(noRollbackOption);

            var catalog = new TweakCatalogService();
            var tweak = catalog.FindById(tweakId);
            if (tweak == null)
            {
                Console.WriteLine($"Tweak not found: {tweakId}");
                context.ExitCode = 1;
                return;
            }

            if (!EnsureCanRunTweak(catalog, tweak, out var error))
            {
                Console.WriteLine(error);
                context.ExitCode = 2;
                return;
            }

            var options = new TweakExecutionOptions
            {
                DryRun = !apply,
                VerifyAfterApply = !noVerify,
                RollbackOnFailure = !noRollback
            };

            Console.WriteLine($"Tweak: {tweak.Id} - {tweak.Name}");
            Console.WriteLine($"Mode: {(options.DryRun ? "dry-run" : "apply")}, Verify: {options.VerifyAfterApply}, RollbackOnFailure: {options.RollbackOnFailure}");

            var report = await catalog.ExecuteAsync(tweak, options, null, CancellationToken.None);
            WriteReport(report);

            context.ExitCode = report.Succeeded ? 0 : 2;
        });
        tweakCommand.AddCommand(applyCommand);

        // Revert tweak
        var revertCommand = new Command("revert", "Rollback a tweak (default: dry-run)");
        var revertIdArg = new Argument<string>("tweak-id", "ID of the tweak to revert");
        var revertApplyOption = new Option<bool>("--apply", "Actually rollback changes (default: dry-run)");
        revertCommand.AddArgument(revertIdArg);
        revertCommand.AddOption(revertApplyOption);
        revertCommand.SetHandler(async context =>
        {
            var tweakId = context.ParseResult.GetValueForArgument(revertIdArg);
            var apply = context.ParseResult.GetValueForOption(revertApplyOption);

            var catalog = new TweakCatalogService();
            var tweak = catalog.FindById(tweakId);
            if (tweak == null)
            {
                Console.WriteLine($"Tweak not found: {tweakId}");
                context.ExitCode = 1;
                return;
            }

            if (!EnsureCanRunTweak(catalog, tweak, out var error))
            {
                Console.WriteLine(error);
                context.ExitCode = 2;
                return;
            }

            Console.WriteLine($"Tweak: {tweak.Id} - {tweak.Name}");
            Console.WriteLine($"Mode: {(apply ? "rollback" : "dry-run")}");

            var detectStep = await catalog.ExecuteStepAsync(tweak, TweakAction.Detect, null, CancellationToken.None);
            WriteStep(detectStep);

            if (detectStep.Result.Status is TweakStatus.Failed or TweakStatus.NotApplicable)
            {
                context.ExitCode = 2;
                return;
            }

            if (!apply)
            {
                Console.WriteLine("Dry-run: rollback skipped.");
                context.ExitCode = 0;
                return;
            }

            var rollbackStep = await catalog.ExecuteStepAsync(tweak, TweakAction.Rollback, null, CancellationToken.None);
            WriteStep(rollbackStep);

            context.ExitCode = rollbackStep.Result.Status == TweakStatus.Failed ? 2 : 0;
        });
        tweakCommand.AddCommand(revertCommand);

        return tweakCommand;
    }

    /// <summary>
    /// Preset management commands.
    /// </summary>
    static Command CreatePresetCommand()
    {
        var presetCommand = new Command("preset", "Manage optimization presets");

        // List presets
        var listCommand = new Command("list", "List available presets");
        listCommand.SetHandler(() =>
        {
            var service = new PresetService();
            var presets = service.GetAllPresets();
            Console.WriteLine("Available presets:");
            foreach (var preset in presets)
            {
                Console.WriteLine($"  {preset.Id} - {preset.Name} ({preset.TweakIds.Count} tweaks)");
                Console.WriteLine($"    {preset.Description}");
            }
        });
        presetCommand.AddCommand(listCommand);

        // Apply preset
        var applyCommand = new Command("apply", "Apply a preset (default: dry-run)");
        var presetArg = new Argument<string>("preset-name", "Name of the preset");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        applyCommand.AddArgument(presetArg);
        applyCommand.AddOption(applyOption);
        applyCommand.SetHandler(async context =>
        {
            var presetName = context.ParseResult.GetValueForArgument(presetArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);

            var service = new PresetService();
            Console.WriteLine($"Preset: {presetName}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            var progress = new Progress<int>(percent =>
            {
                if (percent % 10 == 0)
                {
                    Console.WriteLine($"Progress: {percent}%");
                }
            });

            var result = await service.ApplyPresetAsync(presetName, progress, dryRun: !apply);
            Console.WriteLine(result.Message);

            if (result.FailedTweaks.Count > 0)
            {
                Console.WriteLine("Failed tweaks:");
                foreach (var id in result.FailedTweaks)
                {
                    Console.WriteLine($"  {id}");
                }
            }

            context.ExitCode = result.Success ? 0 : 2;
        });
        presetCommand.AddCommand(applyCommand);

        // Revert preset
        var revertCommand = new Command("revert", "Rollback a preset (default: dry-run)");
        var revertArg = new Argument<string>("preset-name", "Name of the preset");
        var revertApplyOption = new Option<bool>("--apply", "Actually rollback changes (default: dry-run)");
        revertCommand.AddArgument(revertArg);
        revertCommand.AddOption(revertApplyOption);
        revertCommand.SetHandler(async context =>
        {
            var presetName = context.ParseResult.GetValueForArgument(revertArg);
            var apply = context.ParseResult.GetValueForOption(revertApplyOption);

            var service = new PresetService();
            Console.WriteLine($"Preset: {presetName}");
            Console.WriteLine($"Mode: {(apply ? "rollback" : "dry-run")}");

            var success = await service.RevertPresetAsync(presetName, dryRun: !apply);
            Console.WriteLine(success ? "Preset rollback completed." : "Preset rollback failed.");
            context.ExitCode = success ? 0 : 2;
        });
        presetCommand.AddCommand(revertCommand);

        return presetCommand;
    }

    /// <summary>
    /// DNS management commands.
    /// </summary>
    static Command CreateDnsCommand()
    {
        var dnsCommand = new Command("dns", "Manage DNS settings");

        // List providers
        var listCommand = new Command("list", "List DNS providers");
        listCommand.SetHandler(() =>
        {
            Console.WriteLine("Available DNS providers:");
            foreach (var provider in DnsService.GetProviders())
            {
                var secondary = string.IsNullOrWhiteSpace(provider.SecondaryDns) ? "" : $" / {provider.SecondaryDns}";
                Console.WriteLine($"  {provider.Name.ToLowerInvariant()}  {provider.PrimaryDns}{secondary}  ({provider.Description})");
            }
        });
        dnsCommand.AddCommand(listCommand);

        // Set DNS
        var setCommand = new Command("set", "Set DNS provider (default: dry-run)");
        var providerArg = new Argument<string>("provider", "DNS provider name");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        var flushOption = new Option<bool>("--flush", "Flush DNS cache after applying");
        setCommand.AddArgument(providerArg);
        setCommand.AddOption(applyOption);
        setCommand.AddOption(flushOption);
        setCommand.SetHandler(async context =>
        {
            var provider = context.ParseResult.GetValueForArgument(providerArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);
            var flush = context.ParseResult.GetValueForOption(flushOption);

            var service = new DnsService();
            var match = DnsService.GetProviders()
                .FirstOrDefault(p => string.Equals(p.Name, provider, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                Console.WriteLine($"Unknown DNS provider: {provider}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"DNS provider: {match.Name}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            if (!apply)
            {
                Console.WriteLine("Dry-run: DNS change skipped.");
                context.ExitCode = 0;
                return;
            }

            var success = await service.SetDnsAsync(match);
            if (!success)
            {
                Console.WriteLine("Failed to update DNS settings.");
                context.ExitCode = 2;
                return;
            }

            if (flush)
            {
                await service.FlushDnsCacheAsync();
            }

            Console.WriteLine("DNS updated successfully.");
            context.ExitCode = 0;
        });
        dnsCommand.AddCommand(setCommand);

        // Reset DNS
        var resetCommand = new Command("reset", "Reset DNS to automatic (default: dry-run)");
        var resetApplyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        resetCommand.AddOption(resetApplyOption);
        resetCommand.SetHandler(async context =>
        {
            var apply = context.ParseResult.GetValueForOption(resetApplyOption);

            var service = new DnsService();
            var provider = DnsService.GetProviders()
                .First(p => string.Equals(p.Name, "Automatic", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("DNS provider: Automatic");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            if (!apply)
            {
                Console.WriteLine("Dry-run: DNS reset skipped.");
                context.ExitCode = 0;
                return;
            }

            var success = await service.SetDnsAsync(provider);
            Console.WriteLine(success ? "DNS reset successfully." : "Failed to reset DNS.");
            context.ExitCode = success ? 0 : 2;
        });
        dnsCommand.AddCommand(resetCommand);

        return dnsCommand;
    }

    /// <summary>
    /// System info command.
    /// </summary>
    static Command CreateInfoCommand()
    {
        var infoCommand = new Command("info", "Display system information");

        infoCommand.SetHandler(() =>
        {
            Console.WriteLine("=======================================");
            Console.WriteLine(" RegProbe - System Information");
            Console.WriteLine("=======================================");
            Console.WriteLine($"  OS:         {Environment.OSVersion}");
            Console.WriteLine($"  Machine:    {Environment.MachineName}");
            Console.WriteLine($"  User:       {Environment.UserName}");
            Console.WriteLine($"  Processors: {Environment.ProcessorCount}");
            Console.WriteLine($"  64-bit:     {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"  CLR:        {Environment.Version}");
            Console.WriteLine("=======================================");
        });

        return infoCommand;
    }

    /// <summary>
    /// Export/import commands.
    /// </summary>
    static Command CreateExportCommand()
    {
        var exportCommand = new Command("config", "Configuration export/import");

        // Export
        var exportSubCommand = new Command("export", "Export configuration to file");
        var fileOption = new Option<string>("--file", () => "config.json", "Output file path");
        var includeTweaks = new Option<bool>("--include-tweaks", () => true, "Include applied tweak states");
        var includeStartup = new Option<bool>("--include-startup", () => true, "Include disabled startup items");
        var includeDns = new Option<bool>("--include-dns", () => true, "Include DNS settings");
        var includeSettings = new Option<bool>("--include-settings", () => true, "Include app settings");
        exportSubCommand.AddOption(fileOption);
        exportSubCommand.AddOption(includeTweaks);
        exportSubCommand.AddOption(includeStartup);
        exportSubCommand.AddOption(includeDns);
        exportSubCommand.AddOption(includeSettings);
        exportSubCommand.SetHandler(async context =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption) ?? "config.json";
            var includeTweaksValue = context.ParseResult.GetValueForOption(includeTweaks);
            var includeStartupValue = context.ParseResult.GetValueForOption(includeStartup);
            var includeDnsValue = context.ParseResult.GetValueForOption(includeDns);
            var includeSettingsValue = context.ParseResult.GetValueForOption(includeSettings);

            var service = new ConfigExportService();
            var options = new ExportOptions
            {
                IncludeTweakStates = includeTweaksValue,
                IncludeStartupItems = includeStartupValue,
                IncludeDnsSettings = includeDnsValue,
                IncludeAppSettings = includeSettingsValue
            };

            Console.WriteLine($"Exporting configuration to: {file}");
            var success = await service.ExportAsync(file, options);
            Console.WriteLine(success ? "Export completed." : "Export failed.");
            context.ExitCode = success ? 0 : 2;
        });
        exportCommand.AddCommand(exportSubCommand);

        // Import
        var importSubCommand = new Command("import", "Import configuration from file (default: dry-run)");
        var importFileArg = new Argument<string>("file", "Configuration file path");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        importSubCommand.AddArgument(importFileArg);
        importSubCommand.AddOption(applyOption);
        importSubCommand.SetHandler(async context =>
        {
            var file = context.ParseResult.GetValueForArgument(importFileArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);

            var service = new ConfigExportService();
            Console.WriteLine($"Importing configuration from: {file}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            var result = await service.ImportAsync(file, dryRun: !apply);
            Console.WriteLine(result.Message);
            Console.WriteLine($"Tweaks: {result.TweaksToApply}, Startup: {result.StartupItemsToRestore}, DNS: {(result.DnsToSet ? "yes" : "no")}, Settings: {result.SettingsToApply}");
            Console.WriteLine($"Total changes: {result.TotalChanges}");

            context.ExitCode = result.Success ? 0 : 2;
        });
        exportCommand.AddCommand(importSubCommand);

        return exportCommand;
    }

    private static bool TryParseRisk(string? value, out TweakRiskLevel risk)
    {
        risk = TweakRiskLevel.Safe;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "safe" => (risk = TweakRiskLevel.Safe) == TweakRiskLevel.Safe,
            "advanced" => (risk = TweakRiskLevel.Advanced) == TweakRiskLevel.Advanced,
            "risky" => (risk = TweakRiskLevel.Risky) == TweakRiskLevel.Risky,
            _ => false
        };
    }

    private static void WriteReport(TweakExecutionReport report)
    {
        foreach (var step in report.Steps)
        {
            WriteStep(step);
        }

        Console.WriteLine(report.Succeeded ? "Result: success" : "Result: failed");
    }

    private static void WriteStep(TweakExecutionStep step)
    {
        Console.WriteLine($"  {step.Action}: {step.Result.Status} - {step.Result.Message}");
    }

    private static bool EnsureCanRunTweak(ITweakCatalog catalog, ITweak tweak, out string error)
    {
        error = string.Empty;
        if (!tweak.RequiresElevation)
        {
            return true;
        }

        if (catalog.IsElevated)
        {
            return true;
        }

        if (catalog.IsElevatedHostAvailable)
        {
            return true;
        }

        error = $"Tweak requires elevation, but ElevatedHost was not found at: {catalog.ElevatedHostPath}. " +
                $"Build RegProbe.ElevatedHost or set {ElevatedHostDefaults.OverridePathEnvVar}.";
        return false;
    }
}
