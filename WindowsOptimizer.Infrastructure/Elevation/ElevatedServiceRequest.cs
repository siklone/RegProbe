using System;
using WindowsOptimizer.Infrastructure.Services;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedServiceRequest(
    Guid RequestId,
    ElevatedServiceOperation Operation,
    string ServiceName,
    ServiceStartMode? StartMode = null);
