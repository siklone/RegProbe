using System;
using System.IO;
using RegProbe.Infrastructure.Elevation;

namespace RegProbe.Application.Utilities;

public static class ElevatedHostLocator
{
    public static string GetExecutablePath()
    {
        var baseDirectory = GetProcessBaseDirectory();
        var exeName = ElevatedHostDefaults.ExecutableName;
        var candidates = ElevatedHostCandidateBuilder.BuildCandidates(baseDirectory, exeName);

        foreach (var candidate in candidates)
        {
            if (ElevatedHostCandidateValidator.IsCompleteHostCandidate(candidate))
            {
                return candidate;
            }
        }

        ElevatedHostLocatorDiagnostics.LogNotFound(baseDirectory, candidates);
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
}
