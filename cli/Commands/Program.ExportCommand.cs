using System;
using System.CommandLine;
using System.Linq;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateExportCommand()
    {
        var exportCommand = new Command("config", "Configuration export/import");

        var exportSubCommand = new Command("export", "Export configuration to file");
        var fileOption = CreateOption<string>("--file", () => "config.json", "Output file path");
        var includeTweaks = CreateOption<bool>("--include-tweaks", () => true, "Include applied tweak states");
        var includeDns = CreateOption<bool>("--include-dns", () => true, "Include DNS settings");
        var includeSettings = CreateOption<bool>("--include-settings", () => true, "Include app settings");
        var noTweaks = CreateOption<bool>("--no-tweaks", "Exclude applied tweak states");
        var noDns = CreateOption<bool>("--no-dns", "Exclude DNS settings");
        var noSettings = CreateOption<bool>("--no-settings", "Exclude app settings");
        exportSubCommand.AddOption(fileOption);
        exportSubCommand.AddOption(includeTweaks);
        exportSubCommand.AddOption(includeDns);
        exportSubCommand.AddOption(includeSettings);
        exportSubCommand.AddOption(noTweaks);
        exportSubCommand.AddOption(noDns);
        exportSubCommand.AddOption(noSettings);
        exportSubCommand.SetHandler(async context =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption) ?? "config.json";
            var specifiedOptions = context.ParseResult.Inner.Tokens
                .Select(token => token.Value)
                .ToArray();
            var includeTweaksValue = context.ParseResult.GetValueForOption(includeTweaks);
            var includeDnsValue = context.ParseResult.GetValueForOption(includeDns);
            var includeSettingsValue = context.ParseResult.GetValueForOption(includeSettings);
            var noTweaksValue = context.ParseResult.GetValueForOption(noTweaks);
            var noDnsValue = context.ParseResult.GetValueForOption(noDns);
            var noSettingsValue = context.ParseResult.GetValueForOption(noSettings);
            var validationError = ValidateExportOptions(
                includeTweaksValue,
                HasExplicitOptionToken(specifiedOptions, "--include-tweaks"),
                noTweaksValue,
                includeDnsValue,
                HasExplicitOptionToken(specifiedOptions, "--include-dns"),
                noDnsValue,
                includeSettingsValue,
                HasExplicitOptionToken(specifiedOptions, "--include-settings"),
                noSettingsValue);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                Console.WriteLine(validationError);
                context.ExitCode = 1;
                return;
            }

            var options = BuildExportOptions(
                includeTweaksValue,
                noTweaksValue,
                includeDnsValue,
                noDnsValue,
                includeSettingsValue,
                noSettingsValue);

            var service = new ConfigExportService();

            Console.WriteLine($"Exporting configuration to: {file}");
            var success = await service.ExportAsync(file, options);
            Console.WriteLine(success ? "Export completed." : "Export failed.");
            context.ExitCode = success ? 0 : 2;
        });
        exportCommand.AddCommand(exportSubCommand);

        var importSubCommand = new Command("import", "Import configuration from file (default: dry-run)");
        var importFileArg = CreateArgument<string>("file", "Configuration file path");
        var applyOption = CreateOption<bool>("--apply", "Actually apply changes (default: dry-run)");
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
            Console.WriteLine($"Tweaks: {result.TweaksToApply}, DNS: {(result.DnsToSet ? "yes" : "no")}, Settings: {result.SettingsToApply}");
            Console.WriteLine($"Total changes: {result.TotalChanges}");
            context.ExitCode = result.Success ? 0 : 2;
        });
        exportCommand.AddCommand(importSubCommand);

        return exportCommand;
    }
}
