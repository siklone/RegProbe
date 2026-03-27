using System;
using RegProbe.Core.Registry;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedRegistryRequest(
    Guid RequestId,
    ElevatedRegistryOperation Operation,
    RegistryValueReference Reference,
    RegistryValueData? Value);
