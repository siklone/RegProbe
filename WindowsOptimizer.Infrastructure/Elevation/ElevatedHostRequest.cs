using System;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedHostRequest(
    Guid RequestId,
    ElevatedHostRequestType RequestType,
    ElevatedRegistryRequest? RegistryRequest = null,
    ElevatedServiceRequest? ServiceRequest = null,
    ElevatedScheduledTaskRequest? ScheduledTaskRequest = null,
    ElevatedFileRequest? FileRequest = null,
    ElevatedCommandRequest? CommandRequest = null);
