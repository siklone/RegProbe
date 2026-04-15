using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using RegProbe.Application.Services;
using RegProbe.Application.Services.TweakProviders;
using RegProbe.Application.Utilities;
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

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("RegProbe CLI - System optimization tool")
        {
            Name = "winopt"
        };

        rootCommand.AddCommand(CreateTweakCommand());
        rootCommand.AddCommand(CreatePresetCommand());
        rootCommand.AddCommand(CreateDnsCommand());
        rootCommand.AddCommand(CreateInfoCommand());
        rootCommand.AddCommand(CreateExportCommand());
        rootCommand.AddCommand(CreateResearchCommand());

        return await rootCommand.InvokeAsync(args);
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

        if (catalog.IsElevated || catalog.IsElevatedHostAvailable)
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
}
