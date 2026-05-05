using System;
using System.CommandLine;
using System.Collections.Generic;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchInspectCommand()
    {
        var command = new Command("inspect", "Inspect one tweak, record, registry value name, or registry path fragment");
        var queryArgument = CreateArgument<string>("query", "Tweak id, record id, registry value name, or registry path fragment");
        var expectedValueOption = CreateOption<string[]>("--expected-value", () => Array.Empty<string>(), "Expected value to verify against tracked targets or app writes");
        expectedValueOption.AllowMultipleArgumentsPerToken = true;
        var exactOption = CreateOption<bool>("--exact", "Require exact token matches instead of substring matches");
        var emitJsonOption = CreateOption<bool>("--json", "Emit the inspection result as JSON");
        var limitOption = CreateOption<int?>("--limit", "Maximum number of matches to emit");

        command.AddArgument(queryArgument);
        command.AddOption(expectedValueOption);
        command.AddOption(exactOption);
        command.AddOption(emitJsonOption);
        command.AddOption(limitOption);

        command.SetHandler(context =>
        {
            var query = NormalizeCliText(context.ParseResult.GetValueForArgument(queryArgument));
            var queryValidationError = ValidateRequiredCliText(query, "query");
            if (!string.IsNullOrWhiteSpace(queryValidationError))
            {
                Console.WriteLine(queryValidationError);
                context.ExitCode = 1;
                return;
            }

            var limit = context.ParseResult.GetValueForOption(limitOption);
            if (limit is <= 0)
            {
                Console.WriteLine("--limit must be greater than 0.");
                context.ExitCode = 1;
                return;
            }

            var args = new List<string> { query };
            foreach (var expectedValue in context.ParseResult.GetValueForOption(expectedValueOption) ?? Array.Empty<string>())
            {
                var normalizedExpectedValue = NormalizeOptionalCliText(expectedValue);
                if (normalizedExpectedValue is null)
                {
                    continue;
                }

                args.Add("--expected-value");
                args.Add(normalizedExpectedValue);
            }

            if (context.ParseResult.GetValueForOption(exactOption))
            {
                args.Add("--exact");
            }

            if (limit is int maxMatches)
            {
                args.Add("--limit");
                args.Add(maxMatches.ToString());
            }

            if (context.ParseResult.GetValueForOption(emitJsonOption))
            {
                args.Add("--json");
            }

            context.ExitCode = RunResearchPythonScript("check_single_tweak.py", args);
        });
        return command;
    }
}
