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
            var category = NormalizeCliText(context.ParseResult.GetValueForOption(categoryOption));
            category = string.IsNullOrWhiteSpace(category) ? null : category;
            var risk = NormalizeCliText(context.ParseResult.GetValueForOption(riskOption));
            risk = string.IsNullOrWhiteSpace(risk) ? null : risk;
            var requiresAdmin = context.ParseResult.GetValueForOption(requiresAdminOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var catalog = new TweakCatalogService();
            var entries = catalog.GetAll();
            var categoryValidationError = ValidateKnownCategory(category, entries.Select(entry => entry.Category));
            if (!string.IsNullOrWhiteSpace(categoryValidationError))
            {
                Console.WriteLine(categoryValidationError);
                context.ExitCode = 1;
                return;
            }

            var filteredEntries = entries.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                filteredEntries = filteredEntries.Where(entry => string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(risk))
            {
                if (!TryParseRisk(risk, out var riskLevel))
                {
                    Console.WriteLine($"Unknown risk filter: {risk}");
                    context.ExitCode = 1;
                    return;
                }

                filteredEntries = filteredEntries.Where(entry => entry.Tweak.Risk == riskLevel);
            }

            if (requiresAdmin)
            {
                filteredEntries = filteredEntries.Where(entry => entry.Tweak.RequiresElevation);
            }

            var grouped = filteredEntries
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
