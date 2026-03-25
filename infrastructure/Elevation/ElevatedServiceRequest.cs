using System;
using OpenTraceProject.Core.Services;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedServiceRequest(
    Guid RequestId,
    ElevatedServiceOperation Operation,
    string ServiceName,
    ServiceStartMode? StartMode = null);
