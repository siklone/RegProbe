using System;
using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateExportCommand()
    {
        var exportCommand = new Command("config", "Configuration export/import");

        var exportSubCommand = new Command("export", "Export configuration to file");
        var fileOption = new Option<string>("--file", () => "config.json", "Output file path");
        var includeTweaks = new Option<bool>("--include-tweaks", () => true, "Include applied tweak states");
        var includeDns = new Option<bool>("--include-dns", () => true, "Include DNS settings");
        var includeSettings = new Option<bool>("--include-settings", () => true, "Include app settings");
        exportSubCommand.AddOption(fileOption);
        exportSubCommand.AddOption(includeTweaks);
        exportSubCommand.AddOption(includeDns);
        exportSubCommand.AddOption(includeSettings);
        exportSubCommand.SetHandler(async context =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption) ?? "config.json";
            var includeTweaksValue = context.ParseResult.GetValueForOption(includeTweaks);
            var includeDnsValue = context.ParseResult.GetValueForOption(includeDns);
            var includeSettingsValue = context.ParseResult.GetValueForOption(includeSettings);

            var service = new ConfigExportService();
            var options = new ExportOptions
            {
                IncludeTweakStates = includeTweaksValue,
                IncludeDnsSettings = includeDnsValue,
                IncludeAppSettings = includeSettingsValue
            };

            Console.WriteLine($"Exporting configuration to: {file}");
            var success = await service.ExportAsync(file, options);
            Console.WriteLine(success ? "Export completed." : "Export failed.");
            context.ExitCode = success ? 0 : 2;
        });
        exportCommand.AddCommand(exportSubCommand);

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
            Console.WriteLine($"Tweaks: {result.TweaksToApply}, DNS: {(result.DnsToSet ? "yes" : "no")}, Settings: {result.SettingsToApply}");
            Console.WriteLine($"Total changes: {result.TotalChanges}");
            context.ExitCode = result.Success ? 0 : 2;
        });
        exportCommand.AddCommand(importSubCommand);

        return exportCommand;
    }
}
