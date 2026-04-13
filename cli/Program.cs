using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.App.Utilities;
using RegProbe.App.Services.TweakProviders;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Infrastructure.RegistryResearch;

namespace RegProbe.CLI;

/// <summary>
/// RegProbe Command Line Interface.
/// Provides automation capabilities for system optimization.
/// </summary>
class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("RegProbe CLI - System optimization tool")
        {
            Name = "winopt"
        };

        // Add subcommands
        rootCommand.AddCommand(CreateTweakCommand());
        rootCommand.AddCommand(CreatePresetCommand());
        rootCommand.AddCommand(CreateDnsCommand());
        rootCommand.AddCommand(CreateInfoCommand());
        rootCommand.AddCommand(CreateExportCommand());
        rootCommand.AddCommand(CreateResearchCommand());

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Tweak management commands.
    /// </summary>
    static Command CreateTweakCommand()
    {
        var tweakCommand = new Command("tweak", "Manage system tweaks");

        // List tweaks
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
                entries = entries.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(risk))
            {
                if (!TryParseRisk(risk, out var riskLevel))
                {
                    Console.WriteLine($"Unknown risk filter: {risk}");
                    context.ExitCode = 1;
                    return;
                }

                entries = entries.Where(e => e.Tweak.Risk == riskLevel);
            }

            if (requiresAdmin)
            {
                entries = entries.Where(e => e.Tweak.RequiresElevation);
            }

            var grouped = entries
                .OrderBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Tweak.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .GroupBy(e => e.Category, StringComparer.OrdinalIgnoreCase);

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

        // Apply tweak
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
            if (tweak == null)
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

        // Revert tweak
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
            if (tweak == null)
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

    static Command CreateResearchCommand()
    {
        var researchCommand = new Command("research", "Inspect research-derived promotion and gate state");

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
        var laneOption = new Option<string?>("--lane", "Only show blocked candidates for a specific next-missing-layer lane");
        var topOption = new Option<int?>("--top", "Limit the number of blocked candidates shown");
        var blockedJsonOption = new Option<bool>("--json", "Emit blocked candidates as JSON");
        var blockedSummaryOption = new Option<bool>("--summary", "Show blocked lane counts instead of individual candidates");
        blockedCommand.AddOption(reasonOption);
        blockedCommand.AddOption(worklistOption);
        blockedCommand.AddOption(actionableOnlyOption);
        blockedCommand.AddOption(laneOption);
        blockedCommand.AddOption(topOption);
        blockedCommand.AddOption(blockedJsonOption);
        blockedCommand.AddOption(blockedSummaryOption);
        blockedCommand.SetHandler(context =>
        {
            var reason = context.ParseResult.GetValueForOption(reasonOption);
            var emitJson = context.ParseResult.GetValueForOption(blockedJsonOption);
            var emitSummary = context.ParseResult.GetValueForOption(blockedSummaryOption);
            var useWorklist = context.ParseResult.GetValueForOption(worklistOption)
                              || context.ParseResult.GetValueForOption(actionableOnlyOption)
                              || emitSummary
                              || context.ParseResult.GetValueForOption(topOption) is > 0
                              || !string.IsNullOrWhiteSpace(context.ParseResult.GetValueForOption(laneOption));
            var catalog = new TweakPromotionGateCatalogService();
            if (useWorklist)
            {
                if (emitSummary)
                {
                    var payload = new
                    {
                        catalog.BlockedWorklist.GeneratedAt,
                        catalog.BlockedWorklist.BlockedCount,
                        LaneCounts = catalog.BlockedWorklist.LaneCounts,
                        TopActionableCandidates = catalog.BlockedWorklist.TopActionableCandidates,
                    };

                    if (emitJson)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
                    }
                    else
                    {
                        Console.WriteLine($"Blocked candidates: {catalog.BlockedWorklist.BlockedCount}");
                        foreach (var pair in catalog.BlockedWorklist.LaneCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"{pair.Key}: {pair.Value}");
                        }
                        if (catalog.BlockedWorklist.TopActionableCandidates.Count > 0)
                        {
                            Console.WriteLine("Top actionable:");
                            foreach (var candidateId in catalog.BlockedWorklist.TopActionableCandidates)
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
                    Console.WriteLine(
                        $"{entry.CandidateId} [{entry.NextMissingLayer} | {entry.Actionability} | score={entry.PriorityScore}] :: {entry.NextActionHint}");
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

        return researchCommand;
    }

    /// <summary>
    /// Preset management commands.
    /// </summary>
    static Command CreatePresetCommand()
    {
        var presetCommand = new Command("preset", "Manage optimization presets");

        // List presets
        var listCommand = new Command("list", "List available presets");
        listCommand.SetHandler(() =>
        {
            var service = new PresetService();
            var presets = service.GetAllPresets();
            Console.WriteLine("Available presets:");
            foreach (var preset in presets)
            {
                Console.WriteLine($"  {preset.Id} - {preset.Name} ({preset.TweakIds.Count} tweaks)");
                Console.WriteLine($"    {preset.Description}");
            }
        });
        presetCommand.AddCommand(listCommand);

        // Apply preset
        var applyCommand = new Command("apply", "Apply a preset (default: dry-run)");
        var presetArg = new Argument<string>("preset-name", "Name of the preset");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        applyCommand.AddArgument(presetArg);
        applyCommand.AddOption(applyOption);
        applyCommand.SetHandler(async context =>
        {
            var presetName = context.ParseResult.GetValueForArgument(presetArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);

            var service = new PresetService();
            Console.WriteLine($"Preset: {presetName}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            var progress = new Progress<int>(percent =>
            {
                if (percent % 10 == 0)
                {
                    Console.WriteLine($"Progress: {percent}%");
                }
            });

            var result = await service.ApplyPresetAsync(presetName, progress, dryRun: !apply);
            Console.WriteLine(result.Message);

            if (result.FailedTweaks.Count > 0)
            {
                Console.WriteLine("Failed tweaks:");
                foreach (var id in result.FailedTweaks)
                {
                    Console.WriteLine($"  {id}");
                }
            }

            context.ExitCode = result.Success ? 0 : 2;
        });
        presetCommand.AddCommand(applyCommand);

        // Revert preset
        var revertCommand = new Command("revert", "Rollback a preset (default: dry-run)");
        var revertArg = new Argument<string>("preset-name", "Name of the preset");
        var revertApplyOption = new Option<bool>("--apply", "Actually rollback changes (default: dry-run)");
        revertCommand.AddArgument(revertArg);
        revertCommand.AddOption(revertApplyOption);
        revertCommand.SetHandler(async context =>
        {
            var presetName = context.ParseResult.GetValueForArgument(revertArg);
            var apply = context.ParseResult.GetValueForOption(revertApplyOption);

            var service = new PresetService();
            Console.WriteLine($"Preset: {presetName}");
            Console.WriteLine($"Mode: {(apply ? "rollback" : "dry-run")}");

            var success = await service.RevertPresetAsync(presetName, dryRun: !apply);
            Console.WriteLine(success ? "Preset rollback completed." : "Preset rollback failed.");
            context.ExitCode = success ? 0 : 2;
        });
        presetCommand.AddCommand(revertCommand);

        return presetCommand;
    }

    /// <summary>
    /// DNS management commands.
    /// </summary>
    static Command CreateDnsCommand()
    {
        var dnsCommand = new Command("dns", "Manage DNS settings");

        // List providers
        var listCommand = new Command("list", "List DNS providers");
        listCommand.SetHandler(() =>
        {
            Console.WriteLine("Available DNS providers:");
            foreach (var provider in DnsService.GetProviders())
            {
                var secondary = string.IsNullOrWhiteSpace(provider.SecondaryDns) ? "" : $" / {provider.SecondaryDns}";
                Console.WriteLine($"  {provider.Name.ToLowerInvariant()}  {provider.PrimaryDns}{secondary}  ({provider.Description})");
            }
        });
        dnsCommand.AddCommand(listCommand);

        // Set DNS
        var setCommand = new Command("set", "Set DNS provider (default: dry-run)");
        var providerArg = new Argument<string>("provider", "DNS provider name");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        var flushOption = new Option<bool>("--flush", "Flush DNS cache after applying");
        setCommand.AddArgument(providerArg);
        setCommand.AddOption(applyOption);
        setCommand.AddOption(flushOption);
        setCommand.SetHandler(async context =>
        {
            var provider = context.ParseResult.GetValueForArgument(providerArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);
            var flush = context.ParseResult.GetValueForOption(flushOption);

            var service = new DnsService();
            var match = DnsService.GetProviders()
                .FirstOrDefault(p => string.Equals(p.Name, provider, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                Console.WriteLine($"Unknown DNS provider: {provider}");
                context.ExitCode = 1;
                return;
            }

            Console.WriteLine($"DNS provider: {match.Name}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            if (!apply)
            {
                Console.WriteLine("Dry-run: DNS change skipped.");
                context.ExitCode = 0;
                return;
            }

            var success = await service.SetDnsAsync(match);
            if (!success)
            {
                Console.WriteLine("Failed to update DNS settings.");
                context.ExitCode = 2;
                return;
            }

            if (flush)
            {
                await service.FlushDnsCacheAsync();
            }

            Console.WriteLine("DNS updated successfully.");
            context.ExitCode = 0;
        });
        dnsCommand.AddCommand(setCommand);

        // Reset DNS
        var resetCommand = new Command("reset", "Reset DNS to automatic (default: dry-run)");
        var resetApplyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        resetCommand.AddOption(resetApplyOption);
        resetCommand.SetHandler(async context =>
        {
            var apply = context.ParseResult.GetValueForOption(resetApplyOption);

            var service = new DnsService();
            var provider = DnsService.GetProviders()
                .First(p => string.Equals(p.Name, "Automatic", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("DNS provider: Automatic");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            if (!apply)
            {
                Console.WriteLine("Dry-run: DNS reset skipped.");
                context.ExitCode = 0;
                return;
            }

            var success = await service.SetDnsAsync(provider);
            Console.WriteLine(success ? "DNS reset successfully." : "Failed to reset DNS.");
            context.ExitCode = success ? 0 : 2;
        });
        dnsCommand.AddCommand(resetCommand);

        return dnsCommand;
    }

    /// <summary>
    /// System info command.
    /// </summary>
    static Command CreateInfoCommand()
    {
        var infoCommand = new Command("info", "Display system information");

        infoCommand.SetHandler(() =>
        {
            Console.WriteLine("=======================================");
            Console.WriteLine(" RegProbe - System Information");
            Console.WriteLine("=======================================");
            Console.WriteLine($"  OS:         {Environment.OSVersion}");
            Console.WriteLine($"  Machine:    {Environment.MachineName}");
            Console.WriteLine($"  User:       {Environment.UserName}");
            Console.WriteLine($"  Processors: {Environment.ProcessorCount}");
            Console.WriteLine($"  64-bit:     {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"  CLR:        {Environment.Version}");
            Console.WriteLine("=======================================");
        });

        return infoCommand;
    }

    /// <summary>
    /// Export/import commands.
    /// </summary>
    static Command CreateExportCommand()
    {
        var exportCommand = new Command("config", "Configuration export/import");

        // Export
        var exportSubCommand = new Command("export", "Export configuration to file");
        var fileOption = new Option<string>("--file", () => "config.json", "Output file path");
        var includeTweaks = new Option<bool>("--include-tweaks", () => true, "Include applied tweak states");
        var includeDns = new Option<bool>("--include-dns", () => true, "Include DNS settings");
        var includeSettings = new Option<bool>("--include-settings", () => true, "Include app settings");
        exportSubCommand.AddOption(fileOption);
        exportSubCommand.AddOption(includeTweaks);
        exportSubCommand.AddOption(includeDns);
        exportSubCommand.AddOption(includeSettings);
        exportSubCommand.SetHandler(async context =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption) ?? "config.json";
            var includeTweaksValue = context.ParseResult.GetValueForOption(includeTweaks);
            var includeDnsValue = context.ParseResult.GetValueForOption(includeDns);
            var includeSettingsValue = context.ParseResult.GetValueForOption(includeSettings);

            var service = new ConfigExportService();
            var options = new ExportOptions
            {
                IncludeTweakStates = includeTweaksValue,
                IncludeDnsSettings = includeDnsValue,
                IncludeAppSettings = includeSettingsValue
            };

            Console.WriteLine($"Exporting configuration to: {file}");
            var success = await service.ExportAsync(file, options);
            Console.WriteLine(success ? "Export completed." : "Export failed.");
            context.ExitCode = success ? 0 : 2;
        });
        exportCommand.AddCommand(exportSubCommand);

        // Import
        var importSubCommand = new Command("import", "Import configuration from file (default: dry-run)");
        var importFileArg = new Argument<string>("file", "Configuration file path");
        var applyOption = new Option<bool>("--apply", "Actually apply changes (default: dry-run)");
        importSubCommand.AddArgument(importFileArg);
        importSubCommand.AddOption(applyOption);
        importSubCommand.SetHandler(async context =>
        {
            var file = context.ParseResult.GetValueForArgument(importFileArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);

            var service = new ConfigExportService();
            Console.WriteLine($"Importing configuration from: {file}");
            Console.WriteLine($"Mode: {(apply ? "apply" : "dry-run")}");

            var result = await service.ImportAsync(file, dryRun: !apply);
            Console.WriteLine(result.Message);
            Console.WriteLine($"Tweaks: {result.TweaksToApply}, DNS: {(result.DnsToSet ? "yes" : "no")}, Settings: {result.SettingsToApply}");
            Console.WriteLine($"Total changes: {result.TotalChanges}");

            context.ExitCode = result.Success ? 0 : 2;
        });
        exportCommand.AddCommand(importSubCommand);

        return exportCommand;
    }

    static Command CreateResearchCommand()
    {
        var researchCommand = new Command("research", "Research automation helpers");

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

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
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

    private static object BuildJsonTweakValidationReport(string inputDirectory, JsonTweakLoader loader)
    {
        var issues = loader.ValidationIssues
            .Select(issue => new
            {
                file_path = issue.FilePath,
                code = issue.Code,
                message = issue.Message,
                entry_id = issue.EntryId
            })
            .ToArray();

        return new
        {
            generated_utc = DateTime.UtcNow.ToString("o"),
            input_directory = inputDirectory,
            loaded_definition_count = loader.Count,
            loaded_tweak_ids = loader.GetTweakIds().OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(),
            validation_issue_count = issues.Length,
            validation_issues = issues,
            status = issues.Length == 0 ? "ok" : "invalid-definitions-present"
        };
    }

    private static bool TryParseRisk(string? value, out TweakRiskLevel risk)
    {
        risk = TweakRiskLevel.Safe;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "safe" => (risk = TweakRiskLevel.Safe) == TweakRiskLevel.Safe,
            "advanced" => (risk = TweakRiskLevel.Advanced) == TweakRiskLevel.Advanced,
            "risky" => (risk = TweakRiskLevel.Risky) == TweakRiskLevel.Risky,
            _ => false
        };
    }

    private static void WriteReport(TweakExecutionReport report)
    {
        foreach (var step in report.Steps)
        {
            WriteStep(step);
        }

        Console.WriteLine(report.Succeeded ? "Result: success" : "Result: failed");
    }

    private static void WriteStep(TweakExecutionStep step)
    {
        Console.WriteLine($"  {step.Action}: {step.Result.Status} - {step.Result.Message}");
    }

    private static bool EnsureCanRunTweak(
        ITweakCatalog catalog,
        ITweak tweak,
        TweakPromotionGateCatalogService promotionGateCatalog,
        TweakMutationDecision mutationDecision,
        out string error)
    {
        error = string.Empty;
        if (!mutationDecision.Allowed)
        {
            var promotionGate = promotionGateCatalog.ResolveOrFallback(tweak.Id);
            error = $"Tweak is gated by research promotion state '{promotionGate.PromotionState}'. {promotionGate.GatingReason}";
            return false;
        }

        if (!tweak.RequiresElevation)
        {
            return true;
        }

        if (catalog.IsElevated)
        {
            return true;
        }

        if (catalog.IsElevatedHostAvailable)
        {
            return true;
        }

        error = $"Tweak requires elevation, but ElevatedHost was not found at: {catalog.ElevatedHostPath}. " +
                $"Build RegProbe.ElevatedHost or set {ElevatedHostDefaults.OverridePathEnvVar}.";
        return false;
    }

    private static string? TryFindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var depth = 0; depth < 8 && current is not null; depth++)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git"))
                || File.Exists(Path.Combine(current.FullName, "RegProbe.sln"))
                || File.Exists(Path.Combine(current.FullName, "RegProbe.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? ResolveResearchPath(string relativePath)
    {
        var repoRoot = TryFindRepoRoot();
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            var repoPath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(repoPath))
            {
                return repoPath;
            }
        }

        var docsPath = Path.Combine(AppContext.BaseDirectory, "Docs", relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(docsPath) ? docsPath : null;
    }

    private static int RunResearchPythonScript(string scriptName, IEnumerable<string> args)
    {
        var repoRoot = TryFindRepoRoot();
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            Console.WriteLine("Could not locate the repository root for research automation.");
            return 1;
        }

        var scriptPath = Path.Combine(
            repoRoot,
            "registry-research-framework",
            "scripts",
            scriptName.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(scriptPath))
        {
            Console.WriteLine($"Research script not found: {scriptPath}");
            return 1;
        }

        var launchers = new (string FileName, string[] Prefix)[]
        {
            ("python3", Array.Empty<string>()),
            ("python", Array.Empty<string>()),
            ("py", new[] { "-3" }),
        };

        foreach (var launcher in launchers)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = launcher.FileName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoRoot,
                };

                foreach (var prefix in launcher.Prefix)
                {
                    process.StartInfo.ArgumentList.Add(prefix);
                }

                process.StartInfo.ArgumentList.Add(scriptPath);
                foreach (var arg in args)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                }

                process.Start();
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    Console.WriteLine(stdout.TrimEnd());
                }
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    Console.Error.WriteLine(stderr.TrimEnd());
                }

                return process.ExitCode;
            }
            catch
            {
            }
        }

        Console.WriteLine("No supported Python launcher was available (python3, python, py -3).");
        return 1;
    }

    private static Dictionary<string, object?>? FindEvidenceAuditEntry(string candidateId)
    {
        var path = ResolveResearchPath(Path.Combine("research", "evidence-audit.json"));
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            var recordId = entry.TryGetProperty("record_id", out var recordIdProp) ? recordIdProp.GetString() : null;
            var tweakId = entry.TryGetProperty("tweak_id", out var tweakIdProp) ? tweakIdProp.GetString() : null;
            if (!string.Equals(recordId, candidateId, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(tweakId, candidateId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return JsonSerializer.Deserialize<Dictionary<string, object?>>(entry.GetRawText(), JsonOptions);
        }

        return null;
    }

    private static Dictionary<string, object?>? LoadFullEvidence(string recordId, string tweakId)
    {
        var repoRoot = TryFindRepoRoot();
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return null;
        }

        var candidateIds = new[] { recordId, tweakId }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidateId in candidateIds)
        {
            var path = Path.Combine(repoRoot, "evidence", "records", candidateId, "full-evidence.json");

            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(path), JsonOptions);
            }
        }

        return null;
    }

    private static string ResolveRegressionPackOutput(string candidateId, string? requestedOutput)
    {
        if (!string.IsNullOrWhiteSpace(requestedOutput))
        {
            return Path.GetFullPath(requestedOutput);
        }

        var repoRoot = TryFindRepoRoot();
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            return Path.Combine(repoRoot, "research", "regression-packs", $"{candidateId}.json");
        }

        return Path.GetFullPath($"{candidateId}.regression-pack.json");
    }
}
