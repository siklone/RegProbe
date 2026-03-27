using System;
using RegProbe.Core.Services;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedServiceRequest(
    Guid RequestId,
    ElevatedServiceOperation Operation,
    string ServiceName,
    ServiceStartMode? StartMode = null);
