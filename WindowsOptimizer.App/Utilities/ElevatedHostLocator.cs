using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsOptimizer.Infrastructure.Elevation;

namespace WindowsOptimizer.App.Utilities;

public static class ElevatedHostLocator
{
    public static string GetExecutablePath()
    {
        var baseDirectory = GetProcessBaseDirectory();
        var exeName = ElevatedHostDefaults.ExecutableName;

        var candidates = new List<string>();

        var overridePath = Environment.GetEnvironmentVariable(ElevatedHostDefaults.OverridePathEnvVar);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            candidates.Add(overridePath.Trim().Trim('"'));
        }

        // Preferred layout: keep the elevated host in a subfolder with its dependencies.
        candidates.Add(Path.Combine(baseDirectory, "ElevatedHost", exeName));

        // Some build flows (or older copy targets) may place the host under a "publish" folder.
        candidates.Add(Path.Combine(baseDirectory, "publish", "ElevatedHost", exeName));

        // Backward compatibility: older builds placed the executable next to the main app.
        candidates.Add(Path.Combine(baseDirectory, exeName));

        // If the app is run from a framework-only output folder, the RID output might be a child folder.
        candidates.AddRange(GetRidSiblingCandidates(baseDirectory, exeName));

        // Dev-time fallback: run directly from the ElevatedHost project's bin output if it exists.
        var solutionRoot = FindSolutionRoot(baseDirectory);
        if (!string.IsNullOrWhiteSpace(solutionRoot))
        {
            candidates.AddRange(GetDevBinCandidates(solutionRoot!, baseDirectory, exeName));
        }

        var uniqueCandidates = candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var candidate in uniqueCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        LogNotFound(baseDirectory, uniqueCandidates);
        return Path.Combine(baseDirectory, "ElevatedHost", exeName);
    }

    private static string GetProcessBaseDirectory()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                var directory = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    return directory;
                }
            }
        }
        catch
        {
        }

        return AppContext.BaseDirectory;
    }

    private static IEnumerable<string> GetRidSiblingCandidates(string baseDirectory, string exeName)
    {
        var results = new List<string>();

        try
        {
            foreach (var child in Directory.EnumerateDirectories(baseDirectory, "win-*", SearchOption.TopDirectoryOnly))
            {
                results.Add(Path.Combine(child, "ElevatedHost", exeName));
                results.Add(Path.Combine(child, "publish", "ElevatedHost", exeName));
                results.Add(Path.Combine(child, exeName));
            }
        }
        catch
        {
        }

        return results;
    }

    private static string? FindSolutionRoot(string baseDirectory)
    {
        try
        {
            var current = new DirectoryInfo(baseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "WindowsOptimizerSuite.slnx"))
                    || File.Exists(Path.Combine(current.FullName, "WindowsOptimizerSuite.sln"))
                    || Directory.Exists(Path.Combine(current.FullName, ".git")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }
        catch
        {
        }

        return null;
    }

    private static IEnumerable<string> GetDevBinCandidates(string solutionRoot, string baseDirectory, string exeName)
    {
        var results = new List<string>();
        var buildInfo = ExtractBuildInfo(baseDirectory);

        var elevatedHostBin = Path.Combine(solutionRoot, "WindowsOptimizer.ElevatedHost", "bin");
        if (!Directory.Exists(elevatedHostBin))
        {
            return results;
        }

        if (!string.IsNullOrWhiteSpace(buildInfo.Configuration)
            && !string.IsNullOrWhiteSpace(buildInfo.TargetFramework)
            && !string.IsNullOrWhiteSpace(buildInfo.RuntimeIdentifier))
        {
            results.Add(Path.Combine(
                elevatedHostBin,
                buildInfo.Configuration!,
                buildInfo.TargetFramework!,
                buildInfo.RuntimeIdentifier!,
                exeName));
        }

        // Common fallback paths.
        results.Add(Path.Combine(elevatedHostBin, "Debug", "net8.0-windows", "win-x64", exeName));
        results.Add(Path.Combine(elevatedHostBin, "Release", "net8.0-windows", "win-x64", exeName));

        // Last resort: find any matching exe under the bin folder.
        try
        {
            foreach (var match in Directory.EnumerateFiles(elevatedHostBin, exeName, SearchOption.AllDirectories)
                         .Take(5))
            {
                results.Add(match);
            }
        }
        catch
        {
        }

        return results;
    }

    private static (string? Configuration, string? TargetFramework, string? RuntimeIdentifier) ExtractBuildInfo(string baseDirectory)
    {
        try
        {
            string? configuration = null;
            string? targetFramework = null;
            string? runtimeIdentifier = null;

            var current = new DirectoryInfo(baseDirectory);
            while (current is not null)
            {
                var name = current.Name;

                if (configuration is null
                    && (string.Equals(name, "Debug", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(name, "Release", StringComparison.OrdinalIgnoreCase)))
                {
                    configuration = name;
                }
                else if (targetFramework is null
                         && name.StartsWith("net", StringComparison.OrdinalIgnoreCase)
                         && name.Contains("windows", StringComparison.OrdinalIgnoreCase))
                {
                    targetFramework = name;
                }
                else if (runtimeIdentifier is null
                         && name.StartsWith("win-", StringComparison.OrdinalIgnoreCase))
                {
                    runtimeIdentifier = name;
                }

                current = current.Parent;
            }

            return (configuration, targetFramework, runtimeIdentifier);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static void LogNotFound(string baseDirectory, IReadOnlyList<string> candidates)
    {
        try
        {
            var logPath = Path.Combine(Path.GetTempPath(), "WindowsOptimizer_Diagnostics.log");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var overridePath = Environment.GetEnvironmentVariable(ElevatedHostDefaults.OverridePathEnvVar) ?? string.Empty;

            File.AppendAllText(
                logPath,
                $"[{timestamp}] ElevatedHostLocator: Host not found.{Environment.NewLine}" +
                $"[{timestamp}] ElevatedHostLocator: BaseDirectory={baseDirectory}{Environment.NewLine}" +
                $"[{timestamp}] ElevatedHostLocator: ProcessPath={Environment.ProcessPath}{Environment.NewLine}" +
                $"[{timestamp}] ElevatedHostLocator: {ElevatedHostDefaults.OverridePathEnvVar}={overridePath}{Environment.NewLine}" +
                string.Join(Environment.NewLine, candidates.Select(path => $"[{timestamp}] ElevatedHostLocator: Candidate={path}")) +
                Environment.NewLine);
        }
        catch
        {
        }
    }
}
