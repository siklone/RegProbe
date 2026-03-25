using System;
using OpenTraceProject.Core.Tasks;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedScheduledTaskResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    ScheduledTaskInfo? Info = null);
