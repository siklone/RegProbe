using System;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed class ElevatedHostClientOptions
{
    public string PipeName { get; init; } = ElevatedHostDefaults.PipeName;
    public string? HostExecutablePath { get; init; }
    public int ParentProcessId { get; init; }
    public TimeSpan InitialConnectTimeout { get; init; } = TimeSpan.FromSeconds(1);
    // Reduced from 30s to 5s - if elevated host can't start in 5 seconds, it's not going to start
    public TimeSpan StartupConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
