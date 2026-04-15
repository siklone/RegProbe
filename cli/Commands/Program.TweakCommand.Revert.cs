using System;
using System.CommandLine;
using System.Threading;
using RegProbe.Application.Services;
using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateTweakRevertCommand()
    {
        var command = new Command("revert", "Rollback a tweak (default: dry-run)");
        var tweakIdArgument = new Argument<string>("tweak-id", "ID of the tweak to revert");
        var applyOption = new Option<bool>("--apply", "Actually rollback changes (default: dry-run)");
        var overrideOption = new Option<bool>("--override", "Allow contributor/debug override for gated research-derived candidates");
        var overrideReasonOption = new Option<string?>("--reason", "Optional reason for a contributor/debug override");
        command.AddArgument(tweakIdArgument);
        command.AddOption(applyOption);
        command.AddOption(overrideOption);
        command.AddOption(overrideReasonOption);
        command.SetHandler(async context =>
        {
            var tweakId = context.ParseResult.GetValueForArgument(tweakIdArgument);
            var apply = context.ParseResult.GetValueForOption(applyOption);
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
        return command;
    }
}
