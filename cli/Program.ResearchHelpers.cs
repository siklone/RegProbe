using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RegProbe.Application.Services.TweakProviders;

namespace RegProbe.CLI;

partial class Program
{
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
