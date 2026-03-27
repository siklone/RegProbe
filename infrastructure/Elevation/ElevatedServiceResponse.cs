using System;
using System.Collections.Generic;
using RegProbe.Core.Services;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedServiceResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    ServiceInfo? Info = null,
    IReadOnlyList<string>? ServiceNames = null);
