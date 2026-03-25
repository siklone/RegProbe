using System;
using OpenTraceProject.Core.Registry;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedRegistryResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    RegistryValueReadResult? ReadResult);
