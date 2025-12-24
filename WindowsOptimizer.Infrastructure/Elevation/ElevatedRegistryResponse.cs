using System;
using WindowsOptimizer.Core.Registry;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedRegistryResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    RegistryValueReadResult? ReadResult);
