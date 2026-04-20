using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text.Json;
using RegProbe.Application.Services;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure.Elevation;

namespace RegProbe.CLI;

/// <summary>
/// RegProbe Command Line Interface bootstrap and shared helpers.
/// </summary>
partial class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static int Main(string[] args)
    {
        var rootCommand = new RootCommand("RegProbe CLI - System optimization tool");

        rootCommand.AddCommand(CreateTweakCommand());
        rootCommand.AddCommand(CreatePresetCommand());
        rootCommand.AddCommand(CreateDnsCommand());
        rootCommand.AddCommand(CreateInfoCommand());
        rootCommand.AddCommand(CreateExportCommand());
        rootCommand.AddCommand(CreateResearchCommand());

        return rootCommand.Parse(args).Invoke();
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

        if (catalog.IsElevated || catalog.IsElevatedHostAvailable)
        {
            return true;
        }

        error = $"Tweak requires elevation, but ElevatedHost was not found at: {catalog.ElevatedHostPath}. " +
                $"Build RegProbe.ElevatedHost or set {ElevatedHostDefaults.OverridePathEnvVar}.";
        return false;
    }
}
