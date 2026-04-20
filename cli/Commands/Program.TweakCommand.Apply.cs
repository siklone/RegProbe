using System;
using System.CommandLine;
using System.Threading;
using RegProbe.Application.Services;
using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateTweakApplyCommand()
    {
        var command = new Command("apply", "Apply a tweak (default: dry-run)");
        var tweakIdArgument = CreateArgument<string>("tweak-id", "ID of the tweak to apply");
        var applyOption = CreateOption<bool>("--apply", "Actually apply changes (default: dry-run)");
        var noVerifyOption = CreateOption<bool>("--no-verify", "Skip verify step after apply");
        var noRollbackOption = CreateOption<bool>("--no-rollback", "Do not rollback on failure");
        var overrideOption = CreateOption<bool>("--override", "Allow contributor/debug override for gated research-derived candidates");
        var overrideReasonOption = CreateOption<string?>("--reason", "Optional reason for a contributor/debug override");
        command.AddArgument(tweakIdArgument);
        command.AddOption(applyOption);
        command.AddOption(noVerifyOption);
        command.AddOption(noRollbackOption);
        command.AddOption(overrideOption);
        command.AddOption(overrideReasonOption);
        command.SetHandler(async context =>
        {
            var tweakId = NormalizeCliText(context.ParseResult.GetValueForArgument(tweakIdArgument));
            var apply = context.ParseResult.GetValueForOption(applyOption);
            var noVerify = context.ParseResult.GetValueForOption(noVerifyOption);
            var noRollback = context.ParseResult.GetValueForOption(noRollbackOption);
            var overrideRequested = context.ParseResult.GetValueForOption(overrideOption);
            var overrideReason = context.ParseResult.GetValueForOption(overrideReasonOption);
            var tweakIdValidationError = ValidateRequiredCliText(tweakId, "tweak-id");
            if (!string.IsNullOrWhiteSpace(tweakIdValidationError))
            {
                Console.WriteLine(tweakIdValidationError);
                context.ExitCode = 1;
                return;
            }

            var executionValidationError = ValidateApplyExecutionOptions(apply, noVerify, noRollback);
            if (!string.IsNullOrWhiteSpace(executionValidationError))
            {
                Console.WriteLine(executionValidationError);
                context.ExitCode = 1;
                return;
            }

            var overrideValidationError = ValidateOverrideOptions(overrideRequested, overrideReason);
            if (!string.IsNullOrWhiteSpace(overrideValidationError))
            {
                Console.WriteLine(overrideValidationError);
                context.ExitCode = 1;
                return;
            }

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
        return command;
    }
}
