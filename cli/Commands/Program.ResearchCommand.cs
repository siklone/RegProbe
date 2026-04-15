using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using RegProbe.Application.Services;
using RegProbe.Application.Services.TweakProviders;
using RegProbe.Infrastructure.RegistryResearch;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchCommand()
    {
        var researchCommand = new Command("research", "Inspect research-derived promotion state and automation helpers");

        var scoreCommand = new Command("score-candidate", "Show the score breakdown for a candidate");
        var scoreIdArg = new Argument<string>("candidate-id", "Record or tweak id");
        scoreCommand.AddArgument(scoreIdArg);
        scoreCommand.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(scoreIdArg);
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
        researchCommand.AddCommand(scoreCommand);

        var evaluateCommand = new Command("evaluate-gate", "Show the gate evaluation for a candidate");
        var evaluateIdArg = new Argument<string>("candidate-id", "Record or tweak id");
        evaluateCommand.AddArgument(evaluateIdArg);
        evaluateCommand.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(evaluateIdArg);
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
        researchCommand.AddCommand(evaluateCommand);

        var showBlockedCommand = new Command("show-blocked", "Show blocked worklist detail for a candidate");
        var showBlockedIdArg = new Argument<string>("candidate-id", "Blocked candidate id");
        var showBlockedJsonOption = new Option<bool>("--json", "Emit blocked worklist detail as JSON");
        showBlockedCommand.AddArgument(showBlockedIdArg);
        showBlockedCommand.AddOption(showBlockedJsonOption);
        showBlockedCommand.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(showBlockedIdArg);
            var emitJson = context.ParseResult.GetValueForOption(showBlockedJsonOption);
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
        researchCommand.AddCommand(showBlockedCommand);

        var blockedCommand = new Command("list-blocked", "List blocked candidates");
        var reasonOption = new Option<string?>("--reason", "Only show blockers matching this reason");
        var worklistOption = new Option<bool>("--worklist", "Show the prioritized blocked worklist view");
        var actionableOnlyOption = new Option<bool>("--actionable-only", "Only show blocked candidates that are currently actionable");
        var actionabilityOption = new Option<string?>("--actionability", "Only show blocked candidates with a specific actionability (active or hold)");
        var laneOption = new Option<string?>("--lane", "Only show blocked candidates for a specific next-missing-layer lane");
        var topOption = new Option<int?>("--top", "Limit the number of blocked candidates shown");
        var blockedJsonOption = new Option<bool>("--json", "Emit blocked candidates as JSON");
        var blockedSummaryOption = new Option<bool>("--summary", "Show blocked lane counts instead of individual candidates");
        blockedCommand.AddOption(reasonOption);
        blockedCommand.AddOption(worklistOption);
        blockedCommand.AddOption(actionableOnlyOption);
        blockedCommand.AddOption(actionabilityOption);
        blockedCommand.AddOption(laneOption);
        blockedCommand.AddOption(topOption);
        blockedCommand.AddOption(blockedJsonOption);
        blockedCommand.AddOption(blockedSummaryOption);
        blockedCommand.SetHandler(context =>
        {
            var reason = context.ParseResult.GetValueForOption(reasonOption);
            var emitJson = context.ParseResult.GetValueForOption(blockedJsonOption);
            var emitSummary = context.ParseResult.GetValueForOption(blockedSummaryOption);
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
                if (emitSummary)
                {
                    var summaryEntries = catalog.ListBlockedWorklist(
                        reason,
                        context.ParseResult.GetValueForOption(laneOption),
                        actionability,
                        context.ParseResult.GetValueForOption(actionableOnlyOption),
                        top: null).ToList();
                    var actionabilityCounts = summaryEntries
                        .GroupBy(entry => entry.Actionability, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
                    var laneCounts = summaryEntries
                        .GroupBy(entry => entry.NextMissingLayer, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
                    var preferredLaneOrder = catalog.BlockedWorklist.OrderedLanes
                        .Where(lane => laneCounts.ContainsKey(lane));
                    var fallbackLaneOrder = laneCounts.Keys
                        .Except(preferredLaneOrder, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(lane => lane, StringComparer.OrdinalIgnoreCase);
                    var orderedLanes = preferredLaneOrder.Concat(fallbackLaneOrder).ToList();
                    var laneFocus = orderedLanes
                        .Select(lane => new
                        {
                            lane,
                            entry = summaryEntries.FirstOrDefault(entry =>
                                string.Equals(entry.NextMissingLayer, lane, StringComparison.OrdinalIgnoreCase))
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
                        foreach (var lane in orderedLanes)
                        {
                            var count = laneCounts.TryGetValue(lane, out var laneCount) ? laneCount : 0;
                            if (laneFocus.TryGetValue(lane, out var focus) && !string.IsNullOrWhiteSpace(focus.CandidateId))
                            {
                                Console.WriteLine($"{lane}: {count} -> {focus.CandidateId}");
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
                                Console.WriteLine($"{lane}: {count}");
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

                var entries = catalog.ListBlockedWorklist(
                    reason,
                    context.ParseResult.GetValueForOption(laneOption),
                    actionability,
                    context.ParseResult.GetValueForOption(actionableOnlyOption),
                    context.ParseResult.GetValueForOption(topOption)).ToList();

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
            }
            else
            {
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
            }

            context.ExitCode = 0;
        });
        researchCommand.AddCommand(blockedCommand);

        var staleCommand = new Command("show-stale", "List candidates waiting for revalidation");
        staleCommand.SetHandler(() =>
        {
            var catalog = new TweakPromotionGateCatalogService();
            foreach (var entry in catalog.ListRevalidationPending())
            {
                Console.WriteLine($"{entry.TweakId} -> {entry.GatingReason}");
            }
        });
        researchCommand.AddCommand(staleCommand);

        var revalidationCommand = new Command("show-revalidation-pending", "List candidates explicitly marked revalidation-pending");
        revalidationCommand.SetHandler(() =>
        {
            var catalog = new TweakPromotionGateCatalogService();
            foreach (var entry in catalog.ListRevalidationPending()
                         .Where(entry => string.Equals(entry.PromotionState, "revalidation-pending", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"{entry.TweakId} -> {entry.GatingReason}");
            }
        });
        researchCommand.AddCommand(revalidationCommand);

        var validateBatchCommand = new Command("validate-batch", "Validate invalid, undocumented, blocked, or missing-doc candidates");
        var undocumentedOption = new Option<bool>("--undocumented", "Only show undocumented candidates");
        var invalidOption = new Option<bool>("--invalid", "Only show invalid/schema-failing candidates");
        var blockedStateOption = new Option<bool>("--blocked-state", "Only show blocked candidates");
        var missingDocsOption = new Option<bool>("--missing-docs", "Only show documentation-quality failures");
        validateBatchCommand.AddOption(undocumentedOption);
        validateBatchCommand.AddOption(invalidOption);
        validateBatchCommand.AddOption(blockedStateOption);
        validateBatchCommand.AddOption(missingDocsOption);
        validateBatchCommand.SetHandler(context =>
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
        researchCommand.AddCommand(validateBatchCommand);

        var regressionCommand = new Command("generate-regression-pack", "Generate a regression pack for one candidate or all promotable candidates");
        var regressionIdArg = new Argument<string?>("candidate-id", () => null, "Record or tweak id");
        var regressionAllOption = new Option<bool>("--all", "Generate regression packs for all promotable candidates");
        var regressionStateOption = new Option<string[]>("--state", "Restrict --all to one or more promotion states")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var regressionLimitOption = new Option<int?>("--limit", "Optional max candidate count for --all");
        var outputRootOption = new Option<string?>("--output-root", "Optional output root directory");
        regressionCommand.AddArgument(regressionIdArg);
        regressionCommand.AddOption(regressionAllOption);
        regressionCommand.AddOption(regressionStateOption);
        regressionCommand.AddOption(regressionLimitOption);
        regressionCommand.AddOption(outputRootOption);
        regressionCommand.SetHandler(context =>
        {
            var candidateId = context.ParseResult.GetValueForArgument(regressionIdArg);
            var allCandidates = context.ParseResult.GetValueForOption(regressionAllOption);
            var states = context.ParseResult.GetValueForOption(regressionStateOption) ?? Array.Empty<string>();
            var limit = context.ParseResult.GetValueForOption(regressionLimitOption);
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
        researchCommand.AddCommand(regressionCommand);

        var normalizeCommand = new Command("normalize-registry-trace", "Normalize an ETL or Procmon CSV into a compact registry bundle.");
        var formatOption = new Option<string>("--format", "Normalization format: etl or procmon-csv") { IsRequired = true };
        var inputOption = new Option<string>("--input", "Input trace path") { IsRequired = true };
        var outputOption = new Option<string>("--output", "Output normalized bundle path") { IsRequired = true };
        var runIdOption = new Option<string>("--run-id", "Run identifier") { IsRequired = true };
        var sourceToolOption = new Option<string>("--source-tool", () => "imported", "Source tool tag");
        var capturePhaseOption = new Option<string>("--capture-phase", () => "runtime", "Capture phase tag");
        var evidenceRefsOption = new Option<string[]>("--evidence-ref", () => Array.Empty<string>(), "Evidence reference(s)");
        normalizeCommand.AddOption(formatOption);
        normalizeCommand.AddOption(inputOption);
        normalizeCommand.AddOption(outputOption);
        normalizeCommand.AddOption(runIdOption);
        normalizeCommand.AddOption(sourceToolOption);
        normalizeCommand.AddOption(capturePhaseOption);
        normalizeCommand.AddOption(evidenceRefsOption);
        normalizeCommand.SetHandler(context =>
        {
            var format = context.ParseResult.GetValueForOption(formatOption) ?? string.Empty;
            var input = context.ParseResult.GetValueForOption(inputOption) ?? string.Empty;
            var output = context.ParseResult.GetValueForOption(outputOption) ?? string.Empty;
            var runId = context.ParseResult.GetValueForOption(runIdOption) ?? string.Empty;
            var sourceTool = context.ParseResult.GetValueForOption(sourceToolOption) ?? "imported";
            var capturePhase = context.ParseResult.GetValueForOption(capturePhaseOption) ?? "runtime";
            var evidenceRefs = context.ParseResult.GetValueForOption(evidenceRefsOption) ?? Array.Empty<string>();

            try
            {
                IRegistryTraceNormalizer normalizer = format.Trim().ToLowerInvariant() switch
                {
                    "etl" => new TraceEventEtlRegistryNormalizer(),
                    "procmon-csv" => new ProcmonCsvRegistryNormalizer(),
                    _ => throw new InvalidOperationException($"Unsupported normalization format: {format}")
                };

                var bundle = normalizer.Normalize(new RegistryNormalizationRequest(
                    input,
                    runId,
                    sourceTool,
                    capturePhase,
                    evidenceRefs));

                var outputPath = Path.GetFullPath(output);
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(outputPath, JsonSerializer.Serialize(bundle, options) + Environment.NewLine);
                Console.WriteLine(outputPath);
                context.ExitCode = string.Equals(bundle.Status, "ok", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                context.ExitCode = 1;
            }
        });
        researchCommand.AddCommand(normalizeCommand);

        var validateJsonTweaksCommand = new Command("validate-json-tweaks", "Validate JSON tweak definitions and emit an invalid-definition report.");
        var inputDirectoryOption = new Option<string>("--input-dir", "Directory containing JSON tweak definitions.") { IsRequired = true };
        var reportOutputOption = new Option<string?>("--output", "Optional JSON report output path.");
        validateJsonTweaksCommand.AddOption(inputDirectoryOption);
        validateJsonTweaksCommand.AddOption(reportOutputOption);
        validateJsonTweaksCommand.SetHandler(context =>
        {
            var inputDirectory = context.ParseResult.GetValueForOption(inputDirectoryOption) ?? string.Empty;
            var reportOutput = context.ParseResult.GetValueForOption(reportOutputOption);

            try
            {
                var fullInputDirectory = Path.GetFullPath(inputDirectory);
                if (!Directory.Exists(fullInputDirectory))
                {
                    throw new DirectoryNotFoundException($"JSON tweak definition directory was not found: {fullInputDirectory}");
                }

                using var loader = new JsonTweakLoader(fullInputDirectory);
                var report = BuildJsonTweakValidationReport(fullInputDirectory, loader);
                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;

                if (!string.IsNullOrWhiteSpace(reportOutput))
                {
                    var outputPath = Path.GetFullPath(reportOutput);
                    var outputDirectory = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrWhiteSpace(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    File.WriteAllText(outputPath, json);
                    Console.WriteLine(outputPath);
                }
                else
                {
                    Console.Write(json);
                }

                context.ExitCode = loader.ValidationIssues.Count == 0 ? 0 : 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                context.ExitCode = 1;
            }
        });
        researchCommand.AddCommand(validateJsonTweaksCommand);

        return researchCommand;
    }
}
