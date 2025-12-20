using System;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed class ElevatedHostClientOptions
{
    public string PipeName { get; init; } = ElevatedHostDefaults.PipeName;
    public string? HostExecutablePath { get; init; }
    public int ParentProcessId { get; init; }
    public TimeSpan InitialConnectTimeout { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan StartupConnectTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
