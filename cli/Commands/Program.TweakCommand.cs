using System;
using System.CommandLine;
using System.Linq;
using System.Threading;
using RegProbe.Application.Services;
using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateTweakCommand()
    {
        var tweakCommand = new Command("tweak", "Manage system tweaks");

        var listCommand = new Command("list", "List all available tweaks");
        var categoryOption = new Option<string?>("--category", "Filter by category");
        var riskOption = new Option<string?>("--risk", "Filter by risk: safe, advanced, risky");
        var requiresAdminOption = new Option<bool>("--requires-admin", "Only list tweaks requiring elevation");
        var verboseOption = new Option<bool>("--verbose", "Include descriptions");
        listCommand.AddOption(categoryOption);
        listCommand.AddOption(riskOption);
        listCommand.AddOption(requiresAdminOption);
        listCommand.AddOption(verboseOption);
        listCommand.SetHandler(context =>
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
        tweakCommand.AddCommand(listCommand);

        var applyCommand = new Command("apply", "Apply a tweak (default: dry-run)");
        var tweakIdArg = new Argument<string>("tweak-id", "ID of the tweak to apply");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        var noVerifyOption = new Option<bool>("--no-verify", "Skip verify step after apply");
        var noRollbackOption = new Option<bool>("--no-rollback", "Do not rollback on failure");
        var overrideOption = new Option<bool>("--override", "Allow contributor/debug override for gated research-derived candidates");
        var overrideReasonOption = new Option<string?>("--reason", "Optional reason for a contributor/debug override");
        applyCommand.AddArgument(tweakIdArg);
        applyCommand.AddOption(applyOption);
        applyCommand.AddOption(noVerifyOption);
        applyCommand.AddOption(noRollbackOption);
        applyCommand.AddOption(overrideOption);
        applyCommand.AddOption(overrideReasonOption);
        applyCommand.SetHandler(async context =>
        {
            var tweakId = context.ParseResult.GetValueForArgument(tweakIdArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);
            var noVerify = context.ParseResult.GetValueForOption(noVerifyOption);
            var noRollback = context.ParseResult.GetValueForOption(noRollbackOption);
            var overrideRequested = context.ParseResult.GetValueForOption(overrideOption);
            var overrideReason = context.ParseResult.GetValueForOption(overrideReasonOption);

            var catalog = new TweakCatalogService();
            var promotionGateCatalog = new TweakPromotionGateCatalogService();
            var tweak = catalog.FindById(tweakId);
            if (tweak is null)
            {
                Console.WriteLine($"Tweak not found: {tweakId}");
                context.ExitCode = 1;
                return;
            }

            var applyDecision = promotionGateCatalog.EvaluateApplyRequest(tweakId, overrideRequested, overrideReason);
            if (!EnsureCanRunTweak(catalog, tweak, promotionGateCatalog, applyDecision, out var error))
            {
                Console.WriteLine(error);
                context.ExitCode = 2;
                return;
            }

            var options = new TweakExecutionOptions
            {
                DryRun = !apply,
                VerifyAfterApply = !noVerify,
                RollbackOnFailure = !noRollback
            };

            Console.WriteLine($"Tweak: {tweak.Id} - {tweak.Name}");
            Console.WriteLine($"Mode: {(options.DryRun ? "dry-run" : "apply")}, Verify: {options.VerifyAfterApply}, RollbackOnFailure: {options.RollbackOnFailure}");

            var report = await catalog.ExecuteAsync(tweak, options, null, CancellationToken.None);
            WriteReport(report);
            context.ExitCode = report.Succeeded ? 0 : 2;
        });
        tweakCommand.AddCommand(applyCommand);

        var revertCommand = new Command("revert", "Rollback a tweak (default: dry-run)");
        var revertIdArg = new Argument<string>("tweak-id", "ID of the tweak to revert");
        var revertApplyOption = new Option<bool>("--apply", "Actually rollback changes (default: dry-run)");
        var revertOverrideOption = new Option<bool>("--override", "Allow contributor/debug override for gated research-derived candidates");
        var revertOverrideReasonOption = new Option<string?>("--reason", "Optional reason for a contributor/debug override");
        revertCommand.AddArgument(revertIdArg);
        revertCommand.AddOption(revertApplyOption);
        revertCommand.AddOption(revertOverrideOption);
        revertCommand.AddOption(revertOverrideReasonOption);
        revertCommand.SetHandler(async context =>
        {
            var tweakId = context.ParseResult.GetValueForArgument(revertIdArg);
            var apply = context.ParseResult.GetValueForOption(revertApplyOption);
            var overrideRequested = context.ParseResult.GetValueForOption(revertOverrideOption);
            var overrideReason = context.ParseResult.GetValueForOption(revertOverrideReasonOption);

            var catalog = new TweakCatalogService();
            var promotionGateCatalog = new TweakPromotionGateCatalogService();
            var tweak = catalog.FindById(tweakId);
            if (tweak is null)
            {
                Console.WriteLine($"Tweak not found: {tweakId}");
                context.ExitCode = 1;
                return;
            }

            var rollbackDecision = promotionGateCatalog.EvaluateRollbackRequest(tweakId, overrideRequested, overrideReason);
            if (!EnsureCanRunTweak(catalog, tweak, promotionGateCatalog, rollbackDecision, out var error))
            {
                Console.WriteLine(error);
                context.ExitCode = 2;
                return;
            }

            foreach (var warning in rollbackDecision.Warnings)
            {
                Console.WriteLine($"Warning: {warning}");
            }

            Console.WriteLine($"Tweak: {tweak.Id} - {tweak.Name}");
            Console.WriteLine($"Mode: {(apply ? "rollback" : "dry-run")}");

            var detectStep = await catalog.ExecuteStepAsync(tweak, TweakAction.Detect, null, CancellationToken.None);
            WriteStep(detectStep);

            if (detectStep.Result.Status is TweakStatus.Failed or TweakStatus.NotApplicable)
            {
                context.ExitCode = 2;
                return;
            }

            if (!apply)
            {
                Console.WriteLine("Dry-run: rollback skipped.");
                context.ExitCode = 0;
                return;
            }

            var rollbackStep = await catalog.ExecuteStepAsync(tweak, TweakAction.Rollback, null, CancellationToken.None);
            WriteStep(rollbackStep);
            context.ExitCode = rollbackStep.Result.Status == TweakStatus.Failed ? 2 : 0;
        });
        tweakCommand.AddCommand(revertCommand);

        return tweakCommand;
    }
}
