using RegProbe.Infrastructure.Elevation;

namespace RegProbe.Application.Utilities;

internal static class ElevatedHostCandidateBuilder
{
    public static IReadOnlyList<string> BuildCandidates(string baseDirectory, string exeName)
    {
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

        var solutionRoot = FindSolutionRoot(baseDirectory);
        if (!string.IsNullOrWhiteSpace(solutionRoot))
        {
            candidates.AddRange(GetDevBinCandidates(solutionRoot, baseDirectory, exeName));
        }

        return candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
                if (File.Exists(Path.Combine(current.FullName, "RegProbe.slnx"))
                    || File.Exists(Path.Combine(current.FullName, "RegProbe.sln"))
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
        var buildInfo = ElevatedHostBuildInfoExtractor.Extract(baseDirectory);

        var elevatedHostBin = Path.Combine(solutionRoot, "elevated-host", "bin");
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
}
