using System;
using System.CommandLine;
using System.Linq;
using System.Text.Json;
using RegProbe.Application.Services;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchScoreCandidateCommand()
    {
        var command = new Command("score-candidate", "Show the score breakdown for a candidate");
        var candidateIdArgument = CreateArgument<string>("candidate-id", "Record or tweak id");
        command.AddArgument(candidateIdArgument);
        command.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(candidateIdArgument);
            var catalog = new TweakPromotionGateCatalogService();
            if (!catalog.TryResolve(candidateId, out var entry))
            {
                Console.WriteLine($"Candidate not found in promotion-gates.json: {candidateId}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                entry.CandidateId,
                entry.RecordId,
                entry.TweakId,
                entry.TweakOrigin,
                entry.PromotionState,
                entry.ScoreBreakdown,
            }, JsonOptions));
            context.ExitCode = 0;
        });
        return command;
    }

    static Command CreateResearchEvaluateGateCommand()
    {
        var command = new Command("evaluate-gate", "Show the gate evaluation for a candidate");
        var candidateIdArgument = CreateArgument<string>("candidate-id", "Record or tweak id");
        command.AddArgument(candidateIdArgument);
        command.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(candidateIdArgument);
            var catalog = new TweakPromotionGateCatalogService();
            if (!catalog.TryResolve(candidateId, out var entry))
            {
                Console.WriteLine($"Candidate not found in promotion-gates.json: {candidateId}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine(JsonSerializer.Serialize(entry, JsonOptions));
            context.ExitCode = 0;
        });
        return command;
    }

    static Command CreateResearchShowStaleCommand()
    {
        var command = new Command("show-stale", "List promoted candidates that require revalidation");
        command.SetHandler(() =>
        {
            var catalog = new TweakPromotionGateCatalogService();
            foreach (var entry in catalog.ListStalePromoted())
            {
                Console.WriteLine($"{entry.TweakId} -> {entry.GatingReason}");
            }
        });
        return command;
    }

    static Command CreateResearchShowRevalidationPendingCommand()
    {
        var command = new Command("show-revalidation-pending", "List candidates explicitly marked revalidation-pending");
        command.SetHandler(() =>
        {
            var catalog = new TweakPromotionGateCatalogService();
            foreach (var entry in catalog.ListRevalidationPending()
                         .Where(entry => string.Equals(entry.PromotionState, "revalidation-pending", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"{entry.TweakId} -> {entry.GatingReason}");
            }
        });
        return command;
    }
}
