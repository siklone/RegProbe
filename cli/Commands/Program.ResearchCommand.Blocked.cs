using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RegProbe.Application.Services;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchShowBlockedCommand()
    {
        var command = new Command("show-blocked", "Show blocked worklist detail for a candidate");
        var candidateIdArgument = new Argument<string>("candidate-id", "Blocked candidate id");
        var emitJsonOption = new Option<bool>("--json", "Emit blocked worklist detail as JSON");
        command.AddArgument(candidateIdArgument);
        command.AddOption(emitJsonOption);
        command.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(candidateIdArgument);
            var emitJson = context.ParseResult.GetValueForOption(emitJsonOption);
            var catalog = new TweakPromotionGateCatalogService();
            if (!catalog.TryResolveBlockedWorklist(candidateId, out var entry))
            {
                Console.WriteLine($"Candidate not found in blocked-worklist.json: {candidateId}");
                context.ExitCode = 1;
                return;
            }

            if (emitJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(entry, JsonOptions));
                context.ExitCode = 0;
                return;
            }

            Console.WriteLine(entry.CandidateId);
            Console.WriteLine($"  lane: {entry.NextMissingLayer}");
            Console.WriteLine($"  actionability: {entry.Actionability}");
            Console.WriteLine($"  priority: {entry.PriorityScore}");
            Console.WriteLine($"  blockers: {string.Join(", ", entry.PromotionBlockers)}");
            if (!string.IsNullOrWhiteSpace(entry.KeyPath))
            {
                Console.WriteLine($"  key: {entry.KeyPath}");
            }
            if (!string.IsNullOrWhiteSpace(entry.ValueName))
            {
                Console.WriteLine($"  value: {entry.ValueName}");
            }
            if (!string.IsNullOrWhiteSpace(entry.SuggestedCommand))
            {
                Console.WriteLine($"  command: {entry.SuggestedCommand}");
            }
            Console.WriteLine($"  next: {entry.NextActionHint}");
            foreach (var artifact in entry.RecentAuditArtifacts)
            {
                Console.WriteLine($"  audit: {artifact}");
            }

            context.ExitCode = 0;
        });
        return command;
    }

    static Command CreateResearchListBlockedCommand()
    {
        var command = new Command("list-blocked", "List blocked candidates");
        var reasonOption = new Option<string?>("--reason", "Only show blockers matching this reason");
        var worklistOption = new Option<bool>("--worklist", "Show the prioritized blocked worklist view");
        var actionableOnlyOption = new Option<bool>("--actionable-only", "Only show blocked candidates that are currently actionable");
        var actionabilityOption = new Option<string?>("--actionability", "Only show blocked candidates with a specific actionability (active or hold)");
        var laneOption = new Option<string?>("--lane", "Only show blocked candidates for a specific next-missing-layer lane");
        var topOption = new Option<int?>("--top", "Limit the number of blocked candidates shown");
        var emitJsonOption = new Option<bool>("--json", "Emit blocked candidates as JSON");
        var emitSummaryOption = new Option<bool>("--summary", "Show blocked lane counts instead of individual candidates");
        command.AddOption(reasonOption);
        command.AddOption(worklistOption);
        command.AddOption(actionableOnlyOption);
        command.AddOption(actionabilityOption);
        command.AddOption(laneOption);
        command.AddOption(topOption);
        command.AddOption(emitJsonOption);
        command.AddOption(emitSummaryOption);
        command.SetHandler(context =>
        {
            var reason = context.ParseResult.GetValueForOption(reasonOption);
            var emitJson = context.ParseResult.GetValueForOption(emitJsonOption);
            var emitSummary = context.ParseResult.GetValueForOption(emitSummaryOption);
            var actionability = context.ParseResult.GetValueForOption(actionabilityOption);
            if (!string.IsNullOrWhiteSpace(actionability)
                && !string.Equals(actionability, "active", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(actionability, "hold", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Unsupported actionability filter: {actionability}");
                context.ExitCode = 1;
                return;
            }

            var useWorklist = context.ParseResult.GetValueForOption(worklistOption)
                              || context.ParseResult.GetValueForOption(actionableOnlyOption)
                              || !string.IsNullOrWhiteSpace(actionability)
                              || emitSummary
                              || context.ParseResult.GetValueForOption(topOption) is > 0
                              || !string.IsNullOrWhiteSpace(context.ParseResult.GetValueForOption(laneOption));
            var catalog = new TweakPromotionGateCatalogService();

            if (useWorklist)
            {
                RenderBlockedWorklist(
                    context,
                    catalog,
                    reason,
                    context.ParseResult.GetValueForOption(laneOption),
                    actionability,
                    context.ParseResult.GetValueForOption(actionableOnlyOption),
                    context.ParseResult.GetValueForOption(topOption),
                    emitJson,
                    emitSummary);
                return;
            }

            var entries = catalog.Catalog.Entries
                .Where(entry => string.Equals(entry.PromotionState, "blocked", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(reason))
            {
                entries = entries.Where(entry => entry.PromotionBlockers.Any(blocker =>
                    blocker.Contains(reason, StringComparison.OrdinalIgnoreCase)));
            }

            var orderedEntries = entries.OrderBy(entry => entry.TweakId, StringComparer.OrdinalIgnoreCase).ToList();
            if (emitJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(orderedEntries, JsonOptions));
                context.ExitCode = 0;
                return;
            }

            foreach (var entry in orderedEntries)
            {
                Console.WriteLine($"{entry.TweakId} [{entry.TweakOrigin}] -> {entry.PromotionState} :: {entry.GatingReason}");
            }

            context.ExitCode = 0;
        });
        return command;
    }

    private static void RenderBlockedWorklist(
        InvocationContext context,
        TweakPromotionGateCatalogService catalog,
        string? reason,
        string? lane,
        string? actionability,
        bool actionableOnly,
        int? top,
        bool emitJson,
        bool emitSummary)
    {
        if (emitSummary)
        {
            var summaryEntries = catalog.ListBlockedWorklist(reason, lane, actionability, actionableOnly, top: null).ToList();
            var actionabilityCounts = summaryEntries
                .GroupBy(entry => entry.Actionability, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var laneCounts = summaryEntries
                .GroupBy(entry => entry.NextMissingLayer, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var preferredLaneOrder = catalog.BlockedWorklist.OrderedLanes
                .Where(currentLane => laneCounts.ContainsKey(currentLane));
            var fallbackLaneOrder = laneCounts.Keys
                .Except(preferredLaneOrder, StringComparer.OrdinalIgnoreCase)
                .OrderBy(currentLane => currentLane, StringComparer.OrdinalIgnoreCase);
            var orderedLanes = preferredLaneOrder.Concat(fallbackLaneOrder).ToList();
            var laneFocus = orderedLanes
                .Select(currentLane => new
                {
                    lane = currentLane,
                    entry = summaryEntries.FirstOrDefault(entry =>
                        string.Equals(entry.NextMissingLayer, currentLane, StringComparison.OrdinalIgnoreCase))
                })
                .Where(item => item.entry is not null)
                .ToDictionary(
                    item => item.lane,
                    item => new
                    {
                        item.entry!.CandidateId,
                        item.entry.SuggestedCommand,
                        item.entry.NextActionHint,
                    },
                    StringComparer.OrdinalIgnoreCase);
            var topActionableCandidates = summaryEntries
                .Where(entry => string.Equals(entry.Actionability, "active", StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.CandidateId)
                .Take(5)
                .ToList();
            var topHoldCandidates = summaryEntries
                .Where(entry => string.Equals(entry.Actionability, "hold", StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.CandidateId)
                .Take(5)
                .ToList();
            var payload = new
            {
                catalog.BlockedWorklist.GeneratedAt,
                BlockedCount = summaryEntries.Count,
                ActionabilityCounts = actionabilityCounts,
                LaneCounts = laneCounts,
                OrderedLanes = orderedLanes,
                LaneFocus = laneFocus,
                TopActionableCandidates = topActionableCandidates,
                TopHoldCandidates = topHoldCandidates,
            };

            if (emitJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
            }
            else
            {
                Console.WriteLine($"Blocked candidates: {summaryEntries.Count}");
                foreach (var pair in actionabilityCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{pair.Key}: {pair.Value}");
                }

                foreach (var currentLane in orderedLanes)
                {
                    var count = laneCounts.TryGetValue(currentLane, out var laneCount) ? laneCount : 0;
                    if (laneFocus.TryGetValue(currentLane, out var focus) && !string.IsNullOrWhiteSpace(focus.CandidateId))
                    {
                        Console.WriteLine($"{currentLane}: {count} -> {focus.CandidateId}");
                        if (!string.IsNullOrWhiteSpace(focus.SuggestedCommand))
                        {
                            Console.WriteLine($"  command: {focus.SuggestedCommand}");
                        }
                        if (!string.IsNullOrWhiteSpace(focus.NextActionHint))
                        {
                            Console.WriteLine($"  next: {focus.NextActionHint}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{currentLane}: {count}");
                    }
                }

                if (topActionableCandidates.Count > 0)
                {
                    Console.WriteLine("Top actionable:");
                    foreach (var candidateId in topActionableCandidates)
                    {
                        Console.WriteLine($"  {candidateId}");
                    }
                }

                if (topHoldCandidates.Count > 0)
                {
                    Console.WriteLine("Top holds:");
                    foreach (var candidateId in topHoldCandidates)
                    {
                        Console.WriteLine($"  {candidateId}");
                    }
                }
            }

            context.ExitCode = 0;
            return;
        }

        var entries = catalog.ListBlockedWorklist(reason, lane, actionability, actionableOnly, top).ToList();
        if (emitJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(entries, JsonOptions));
            context.ExitCode = 0;
            return;
        }

        foreach (var entry in entries)
        {
            Console.WriteLine($"{entry.CandidateId} [{entry.NextMissingLayer} | {entry.Actionability} | score={entry.PriorityScore}] :: {entry.NextActionHint}");
            if (entry.RecentAuditArtifacts.Count > 0)
            {
                Console.WriteLine($"  audit: {string.Join(", ", entry.RecentAuditArtifacts)}");
            }
        }

        context.ExitCode = 0;
    }
}
