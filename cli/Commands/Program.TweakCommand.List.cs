using System;
using System.CommandLine;
using System.Linq;
using RegProbe.Application.Services;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateTweakListCommand()
    {
        var command = new Command("list", "List all available tweaks");
        var categoryOption = CreateOption<string?>("--category", "Filter by category");
        var riskOption = CreateOption<string?>("--risk", "Filter by risk: safe, advanced, risky");
        var requiresAdminOption = CreateOption<bool>("--requires-admin", "Only list tweaks requiring elevation");
        var verboseOption = CreateOption<bool>("--verbose", "Include descriptions");
        command.AddOption(categoryOption);
        command.AddOption(riskOption);
        command.AddOption(requiresAdminOption);
        command.AddOption(verboseOption);
        command.SetHandler(context =>
        {
            var category = context.ParseResult.GetValueForOption(categoryOption);
            var risk = context.ParseResult.GetValueForOption(riskOption);
            var requiresAdmin = context.ParseResult.GetValueForOption(requiresAdminOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var catalog = new TweakCatalogService();
            var entries = catalog.GetAll().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                entries = entries.Where(entry => string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(risk))
            {
                if (!TryParseRisk(risk, out var riskLevel))
                {
                    Console.WriteLine($"Unknown risk filter: {risk}");
                    context.ExitCode = 1;
                    return;
                }

                entries = entries.Where(entry => entry.Tweak.Risk == riskLevel);
            }

            if (requiresAdmin)
            {
                entries = entries.Where(entry => entry.Tweak.RequiresElevation);
            }

            var grouped = entries
                .OrderBy(entry => entry.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Tweak.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .GroupBy(entry => entry.Category, StringComparer.OrdinalIgnoreCase);

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
        return command;
    }
}
