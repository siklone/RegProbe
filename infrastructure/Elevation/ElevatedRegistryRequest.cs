using System;
using OpenTraceProject.Core.Registry;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedRegistryRequest(
    Guid RequestId,
    ElevatedRegistryOperation Operation,
    RegistryValueReference Reference,
    RegistryValueData? Value);
