using System;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed class ElevatedHostClientOptions
{
    public string PipeName { get; init; } = ElevatedHostDefaults.PipeName;
    public string? HostExecutablePath { get; init; }
    public int ParentProcessId { get; init; }
    public TimeSpan InitialConnectTimeout { get; init; } = TimeSpan.FromSeconds(1);
    // Allow enough time for the user to accept the UAC prompt and for the elevated host to start.
    public TimeSpan StartupConnectTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxConnectRetries { get; init; } = 3;
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(2);
}
