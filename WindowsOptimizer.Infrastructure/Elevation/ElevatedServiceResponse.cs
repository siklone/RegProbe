using System;
using System.Collections.Generic;
using WindowsOptimizer.Core.Services;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedServiceResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    ServiceInfo? Info = null,
    IReadOnlyList<string>? ServiceNames = null);
