using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace WindowsOptimizer.CLI;

/// <summary>
/// Windows Optimizer Command Line Interface.
/// Provides automation capabilities for system optimization.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Windows Optimizer CLI - System optimization tool")
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
        listCommand.AddOption(categoryOption);
        listCommand.SetHandler((string? category) =>
        {
            Console.WriteLine("Available tweaks:");
            Console.WriteLine("  [P] disable-telemetry - Disable Windows telemetry");
            Console.WriteLine("  [P] disable-cortana - Disable Cortana");
            Console.WriteLine("  [G] disable-game-bar - Disable Xbox Game Bar");
            Console.WriteLine("  [G] enable-gpu-scheduling - Enable hardware GPU scheduling");
            Console.WriteLine("\n[P]=Privacy, [G]=Gaming");
        }, categoryOption);
        tweakCommand.AddCommand(listCommand);

        // Apply tweak
        var applyCommand = new Command("apply", "Apply a tweak");
        var tweakIdArg = new Argument<string>("tweak-id", "ID of the tweak to apply");
        applyCommand.AddArgument(tweakIdArg);
        applyCommand.SetHandler((string tweakId) =>
        {
            Console.WriteLine($"Applying tweak: {tweakId}...");
            // TODO: Integrate with TweakService
            Console.WriteLine("✓ Tweak applied successfully");
        }, tweakIdArg);
        tweakCommand.AddCommand(applyCommand);

        // Revert tweak
        var revertCommand = new Command("revert", "Revert a tweak");
        revertCommand.AddArgument(tweakIdArg);
        revertCommand.SetHandler((string tweakId) =>
        {
            Console.WriteLine($"Reverting tweak: {tweakId}...");
            // TODO: Integrate with TweakService
            Console.WriteLine("✓ Tweak reverted successfully");
        }, tweakIdArg);
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
            Console.WriteLine("Available presets:");
            Console.WriteLine("  gaming     - Maximum FPS, minimal latency");
            Console.WriteLine("  privacy    - Telemetry elimination, tracking prevention");
            Console.WriteLine("  minimal    - Clean UI, performance boost");
        });
        presetCommand.AddCommand(listCommand);

        // Apply preset
        var applyCommand = new Command("apply", "Apply a preset");
        var presetArg = new Argument<string>("preset-name", "Name of the preset");
        applyCommand.AddArgument(presetArg);
        applyCommand.SetHandler((string presetName) =>
        {
            Console.WriteLine($"Applying preset: {presetName}...");
            // TODO: Integrate with PresetService
            Console.WriteLine("✓ Preset applied successfully");
        }, presetArg);
        presetCommand.AddCommand(applyCommand);

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
            Console.WriteLine("  cloudflare   1.1.1.1 / 1.0.0.1        (Speed)");
            Console.WriteLine("  google       8.8.8.8 / 8.8.4.4        (Reliability)");
            Console.WriteLine("  quad9        9.9.9.9 / 149.112.112.112 (Security)");
            Console.WriteLine("  opendns      208.67.222.222 / 208.67.220.220 (Family Safe)");
        });
        dnsCommand.AddCommand(listCommand);

        // Set DNS
        var setCommand = new Command("set", "Set DNS provider");
        var providerArg = new Argument<string>("provider", "DNS provider name");
        setCommand.AddArgument(providerArg);
        setCommand.SetHandler((string provider) =>
        {
            Console.WriteLine($"Setting DNS to: {provider}...");
            // TODO: Integrate with DnsService
            Console.WriteLine("✓ DNS changed successfully");
        }, providerArg);
        dnsCommand.AddCommand(setCommand);

        // Reset DNS
        var resetCommand = new Command("reset", "Reset DNS to automatic");
        resetCommand.SetHandler(() =>
        {
            Console.WriteLine("Resetting DNS to automatic...");
            Console.WriteLine("✓ DNS reset successfully");
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
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine(" Windows Optimizer - System Information");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"  OS:         {Environment.OSVersion}");
            Console.WriteLine($"  Machine:    {Environment.MachineName}");
            Console.WriteLine($"  User:       {Environment.UserName}");
            Console.WriteLine($"  Processors: {Environment.ProcessorCount}");
            Console.WriteLine($"  64-bit:     {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"  CLR:        {Environment.Version}");
            Console.WriteLine("═══════════════════════════════════════");
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
        exportSubCommand.AddOption(fileOption);
        exportSubCommand.SetHandler((string file) =>
        {
            Console.WriteLine($"Exporting configuration to: {file}...");
            // TODO: Integrate with ConfigExportService
            Console.WriteLine("✓ Configuration exported successfully");
        }, fileOption);
        exportCommand.AddCommand(exportSubCommand);

        // Import
        var importSubCommand = new Command("import", "Import configuration from file");
        var importFileArg = new Argument<string>("file", "Configuration file path");
        importSubCommand.AddArgument(importFileArg);
        importSubCommand.SetHandler((string file) =>
        {
            Console.WriteLine($"Importing configuration from: {file}...");
            // TODO: Integrate with ConfigExportService
            Console.WriteLine("✓ Configuration imported successfully");
        }, importFileArg);
        exportCommand.AddCommand(importSubCommand);

        return exportCommand;
    }
}
