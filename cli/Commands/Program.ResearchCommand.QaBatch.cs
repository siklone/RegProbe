using System;
using System.Collections.Generic;
using System.CommandLine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchQaBatchCommand()
    {
        var command = new Command("qa-batch", "Plan or run a promoted desktop-app QA batch across shipped apply-allowed tweaks");
        var tweakIdOption = CreateOption<string[]>("--id", () => Array.Empty<string>(), "Explicit tweak id to include. Repeat for multiple ids.");
        tweakIdOption.AllowMultipleArgumentsPerToken = true;
        var categoryOption = CreateOption<string[]>("--category", () => Array.Empty<string>(), "Category filter to use when auto-selecting a batch.");
        categoryOption.AllowMultipleArgumentsPerToken = true;
        var limitPerCategoryOption = CreateOption<int?>("--limit-per-category", "Auto-selection cap per category.");
        var totalLimitOption = CreateOption<int?>("--total-limit", "Maximum number of auto-selected tweaks.");
        var runKvmOption = CreateOption<bool>("--run-kvm", "Run the selected batch through the KVM guest app-QA runner.");
        var waitTimeoutOption = CreateOption<int?>("--wait-timeout", "Wait timeout for the live KVM batch.");
        var emitJsonOption = CreateOption<bool>("--json", "Emit the batch plan or live result as JSON");

        command.AddOption(tweakIdOption);
        command.AddOption(categoryOption);
        command.AddOption(limitPerCategoryOption);
        command.AddOption(totalLimitOption);
        command.AddOption(runKvmOption);
        command.AddOption(waitTimeoutOption);
        command.AddOption(emitJsonOption);

        command.SetHandler(context =>
        {
            var args = new List<string>();

            foreach (var tweakId in context.ParseResult.GetValueForOption(tweakIdOption) ?? Array.Empty<string>())
            {
                var normalizedTweakId = NormalizeOptionalCliText(tweakId);
                if (normalizedTweakId is null)
                {
                    continue;
                }

                args.Add("--id");
                args.Add(normalizedTweakId);
            }

            foreach (var category in context.ParseResult.GetValueForOption(categoryOption) ?? Array.Empty<string>())
            {
                var normalizedCategory = NormalizeOptionalCliText(category);
                if (normalizedCategory is null)
                {
                    continue;
                }

                args.Add("--category");
                args.Add(normalizedCategory);
            }

            var limitPerCategory = context.ParseResult.GetValueForOption(limitPerCategoryOption);
            if (limitPerCategory is <= 0)
            {
                Console.WriteLine("--limit-per-category must be greater than 0.");
                context.ExitCode = 1;
                return;
            }

            if (limitPerCategory is int maxPerCategory)
            {
                args.Add("--limit-per-category");
                args.Add(maxPerCategory.ToString());
            }

            var totalLimit = context.ParseResult.GetValueForOption(totalLimitOption);
            if (totalLimit is <= 0)
            {
                Console.WriteLine("--total-limit must be greater than 0.");
                context.ExitCode = 1;
                return;
            }

            if (totalLimit is int maxTotal)
            {
                args.Add("--total-limit");
                args.Add(maxTotal.ToString());
            }

            var waitTimeout = context.ParseResult.GetValueForOption(waitTimeoutOption);
            if (waitTimeout is <= 0)
            {
                Console.WriteLine("--wait-timeout must be greater than 0.");
                context.ExitCode = 1;
                return;
            }

            if (waitTimeout is int timeoutSeconds)
            {
                args.Add("--wait-timeout");
                args.Add(timeoutSeconds.ToString());
            }

            if (context.ParseResult.GetValueForOption(runKvmOption))
            {
                args.Add("--run-kvm");
            }

            if (context.ParseResult.GetValueForOption(emitJsonOption))
            {
                args.Add("--json");
            }

            context.ExitCode = RunResearchPythonScript("check_promoted_tweak_app_qa_batch.py", args);
        });

        return command;
    }
}
