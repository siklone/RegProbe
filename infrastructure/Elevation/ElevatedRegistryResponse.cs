using System;
using RegProbe.Core.Registry;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedRegistryResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    RegistryValueReadResult? ReadResult,
    int? HResult = null);
