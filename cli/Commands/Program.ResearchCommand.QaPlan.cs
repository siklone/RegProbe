using System;
using System.Collections.Generic;
using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchQaPlanCommand()
    {
        var command = new Command("qa-plan", "Generate the manual desktop-app QA plan and commands for one tweak, record, registry value name, or path query");
        var queryArgument = CreateArgument<string>("query", "Tweak id, record id, registry value name, or registry path fragment");
        var expectedValueOption = CreateOption<string[]>("--expected-value", () => Array.Empty<string>(), "Expected value to surface in the app-QA plan");
        expectedValueOption.AllowMultipleArgumentsPerToken = true;
        var exactOption = CreateOption<bool>("--exact", "Require exact token matches instead of substring matches");
        var emitJsonOption = CreateOption<bool>("--json", "Emit the app-QA plan as JSON");
        var limitOption = CreateOption<int?>("--limit", "Maximum number of QA candidates to emit");
        var appExeOption = CreateOption<string?>("--app-exe", "Windows app executable path to use in the direct-launch plan");
        var guestOutputDirOption = CreateOption<string?>("--guest-output-dir", "Windows directory where the QA JSON report should be written");
        var guestUserOption = CreateOption<string?>("--guest-user", "Guest user name to document in the plan");

        command.AddArgument(queryArgument);
        command.AddOption(expectedValueOption);
        command.AddOption(exactOption);
        command.AddOption(emitJsonOption);
        command.AddOption(limitOption);
        command.AddOption(appExeOption);
        command.AddOption(guestOutputDirOption);
        command.AddOption(guestUserOption);

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

            if (limit is int maxCandidates)
            {
                args.Add("--limit");
                args.Add(maxCandidates.ToString());
            }

            if (context.ParseResult.GetValueForOption(emitJsonOption))
            {
                args.Add("--json");
            }

            var appExe = NormalizeOptionalCliText(context.ParseResult.GetValueForOption(appExeOption));
            if (appExe is not null)
            {
                args.Add("--app-exe");
                args.Add(appExe);
            }

            var guestOutputDir = NormalizeOptionalCliText(context.ParseResult.GetValueForOption(guestOutputDirOption));
            if (guestOutputDir is not null)
            {
                args.Add("--guest-output-dir");
                args.Add(guestOutputDir);
            }

            var guestUser = NormalizeOptionalCliText(context.ParseResult.GetValueForOption(guestUserOption));
            if (guestUser is not null)
            {
                args.Add("--guest-user");
                args.Add(guestUser);
            }

            context.ExitCode = RunResearchPythonScript("check_single_tweak_app_qa.py", args);
        });

        return command;
    }
}
