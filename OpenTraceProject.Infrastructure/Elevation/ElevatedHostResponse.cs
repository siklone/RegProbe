using System;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedHostResponse(
    Guid RequestId,
    ElevatedHostRequestType ResponseType,
    ElevatedRegistryResponse? RegistryResponse = null,
    ElevatedServiceResponse? ServiceResponse = null,
    ElevatedScheduledTaskResponse? ScheduledTaskResponse = null,
    ElevatedFileResponse? FileResponse = null,
    ElevatedCommandResponse? CommandResponse = null,
    bool IsElevated = false);
