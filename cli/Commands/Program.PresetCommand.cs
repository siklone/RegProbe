using System;
using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreatePresetCommand()
    {
        var presetCommand = new Command("preset", "Manage optimization presets");

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

        var applyCommand = new Command("apply", "Apply a preset (default: dry-run)");
        var presetArg = CreateArgument<string>("preset-name", "Name of the preset");
        var applyOption = CreateOption<bool>("--apply", "Actually apply changes (default: dry-run)");
        applyCommand.AddArgument(presetArg);
        applyCommand.AddOption(applyOption);
        applyCommand.SetHandler(async context =>
        {
            var presetName = NormalizeCliText(context.ParseResult.GetValueForArgument(presetArg));
            var apply = context.ParseResult.GetValueForOption(applyOption);
            var presetValidationError = ValidateRequiredCliText(presetName, "preset-name");
            if (!string.IsNullOrWhiteSpace(presetValidationError))
            {
                Console.WriteLine(presetValidationError);
                context.ExitCode = 1;
                return;
            }

            var service = new PresetService();
            var preset = FindPresetById(service.GetAllPresets(), presetName);
            if (preset is null)
            {
                Console.WriteLine($"Preset not found: {presetName}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Preset: {preset.Id}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            var progress = new Progress<int>(percent =>
            {
                if (percent % 10 == 0)
                {
                    Console.WriteLine($"Progress: {percent}%");
                }
            });

            var result = await service.ApplyPresetAsync(preset.Id, progress, dryRun: !apply);
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

        var revertCommand = new Command("revert", "Rollback a preset (default: dry-run)");
        var revertArg = CreateArgument<string>("preset-name", "Name of the preset");
        var revertApplyOption = CreateOption<bool>("--apply", "Actually rollback changes (default: dry-run)");
        revertCommand.AddArgument(revertArg);
        revertCommand.AddOption(revertApplyOption);
        revertCommand.SetHandler(async context =>
        {
            var presetName = NormalizeCliText(context.ParseResult.GetValueForArgument(revertArg));
            var apply = context.ParseResult.GetValueForOption(revertApplyOption);
            var presetValidationError = ValidateRequiredCliText(presetName, "preset-name");
            if (!string.IsNullOrWhiteSpace(presetValidationError))
            {
                Console.WriteLine(presetValidationError);
                context.ExitCode = 1;
                return;
            }

            var service = new PresetService();
            var preset = FindPresetById(service.GetAllPresets(), presetName);
            if (preset is null)
            {
                Console.WriteLine($"Preset not found: {presetName}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"Preset: {preset.Id}");
            Console.WriteLine($"Mode: {(apply ? "rollback" : "dry-run")}");

            var success = await service.RevertPresetAsync(preset.Id, dryRun: !apply);
            Console.WriteLine(success ? "Preset rollback completed." : "Preset rollback failed.");
            context.ExitCode = success ? 0 : 2;
        });
        presetCommand.AddCommand(revertCommand);

        return presetCommand;
    }
}
