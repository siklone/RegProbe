using System;
using WindowsOptimizer.Core.Registry;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedRegistryRequest(
    Guid RequestId,
    ElevatedRegistryOperation Operation,
    RegistryValueReference Reference,
    RegistryValueData? Value);
