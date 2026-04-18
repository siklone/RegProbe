using RegProbe.Infrastructure.Elevation;

namespace RegProbe.Application.Utilities;

internal static class ElevatedHostLocatorDiagnostics
{
    public static void LogNotFound(string baseDirectory, IReadOnlyList<string> candidates)
    {
        try
        {
            var logPath = Path.Combine(Path.GetTempPath(), "RegProbe_Diagnostics.log");
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
