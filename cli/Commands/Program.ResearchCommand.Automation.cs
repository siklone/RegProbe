using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchValidateBatchCommand()
    {
        var command = new Command("validate-batch", "Validate invalid, undocumented, blocked, or missing-doc candidates");
        var undocumentedOption = new Option<bool>("--undocumented", "Only show undocumented candidates");
        var invalidOption = new Option<bool>("--invalid", "Only show invalid/schema-failing candidates");
        var blockedStateOption = new Option<bool>("--blocked-state", "Only show blocked candidates");
        var missingDocsOption = new Option<bool>("--missing-docs", "Only show documentation-quality failures");
        command.AddOption(undocumentedOption);
        command.AddOption(invalidOption);
        command.AddOption(blockedStateOption);
        command.AddOption(missingDocsOption);
        command.SetHandler(context =>
        {
            var args = new List<string>();
            if (context.ParseResult.GetValueForOption(undocumentedOption))
            {
                args.Add("--undocumented");
            }
            if (context.ParseResult.GetValueForOption(invalidOption))
            {
                args.Add("--invalid");
            }
            if (context.ParseResult.GetValueForOption(blockedStateOption))
            {
                args.Add("--blocked-state");
            }
            if (context.ParseResult.GetValueForOption(missingDocsOption))
            {
                args.Add("--missing-docs");
            }

            args.Add("--emit-json");
            context.ExitCode = RunResearchPythonScript("validate_research_batch.py", args);
        });
        return command;
    }

    static Command CreateResearchGenerateRegressionPackCommand()
    {
        var command = new Command("generate-regression-pack", "Generate a regression pack for one candidate or all promotable candidates");
        var candidateIdArgument = new Argument<string?>("candidate-id", () => null, "Record or tweak id");
        var allCandidatesOption = new Option<bool>("--all", "Generate regression packs for all promotable candidates");
        var statesOption = new Option<string[]>("--state", "Restrict --all to one or more promotion states")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var limitOption = new Option<int?>("--limit", "Optional max candidate count for --all");
        var outputRootOption = new Option<string?>("--output-root", "Optional output root directory");
        command.AddArgument(candidateIdArgument);
        command.AddOption(allCandidatesOption);
        command.AddOption(statesOption);
        command.AddOption(limitOption);
        command.AddOption(outputRootOption);
        command.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(candidateIdArgument);
            var allCandidates = context.ParseResult.GetValueForOption(allCandidatesOption);
            var states = context.ParseResult.GetValueForOption(statesOption) ?? Array.Empty<string>();
            var limit = context.ParseResult.GetValueForOption(limitOption);
            var outputRoot = context.ParseResult.GetValueForOption(outputRootOption);
            if (!allCandidates && string.IsNullOrWhiteSpace(candidateId))
            {
                Console.WriteLine("Provide <candidate-id> or use --all.");
                context.ExitCode = 1;
                return;
            }

            var args = new List<string>();
            if (!string.IsNullOrWhiteSpace(candidateId))
            {
                args.Add(candidateId);
            }
            if (allCandidates)
            {
                args.Add("--all");
            }
            foreach (var state in states.Where(state => !string.IsNullOrWhiteSpace(state)))
            {
                args.Add("--state");
                args.Add(state);
            }
            if (limit is int max)
            {
                args.Add("--limit");
                args.Add(max.ToString());
            }
            if (!string.IsNullOrWhiteSpace(outputRoot))
            {
                args.Add("--output-root");
                args.Add(outputRoot);
            }

            args.Add("--emit-json");
            context.ExitCode = RunResearchPythonScript("generate_regression_pack.py", args);
        });
        return command;
    }
}
