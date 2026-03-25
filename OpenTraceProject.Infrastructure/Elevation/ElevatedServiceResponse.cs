using System;
using System.Collections.Generic;
using OpenTraceProject.Core.Services;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedServiceResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    ServiceInfo? Info = null,
    IReadOnlyList<string>? ServiceNames = null);
